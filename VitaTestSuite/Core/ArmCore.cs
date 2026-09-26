// ARMv7-A (Cortex-A9 MPCore) instruction set interpreter.
//
// Targets the PS Vita "Kermit" main CPU: ARMv7-A with TrustZone, a VFPv3-D16
// floating point unit and the ARMv7-A short-descriptor MMU, running in
// little-endian data mode with the ARM (A32) and Thumb (T16/T32) instruction
// sets.
//
// Design notes
//   * Step() is allocation free in the hot path: no LINQ, no closures and no
//     string formatting unless instruction tracing is enabled.
//   * Step() never lets an exception escape: undefined instructions halt the
//     core with UndefinedInstruction = true and are reported via ILogSink.
//   * Memory ordering and MMIO belong to MemoryHub; this core only performs the
//     virtual -> physical translation and then hands the physical address on.
//   * Coprocessors p0..p14 are routed to the pluggable CoprocessorHook delegate.
//
// References: ARM Architecture Reference Manual ARMv7-A/ARMv7-R (DDI0406C),
//   A4 (ARM instruction set encoding), A5 (16-bit Thumb), A6 (32-bit Thumb),
//   A7 (VFP / Advanced SIMD), A8 (instruction descriptions),
//   B1.8 (exceptions), B3 (VMSAv7).

using System;
using System.Collections.Generic;
using System.Globalization;

namespace VitaTestSuite.Core
{
    /// <summary>
    /// Hook for coprocessor instructions the core does not implement itself
    /// (p0..p14). Returning true means the hook handled the instruction.
    /// </summary>
    public delegate bool ArmCoprocessorHook(ArmCore core, bool twoReg, bool load, uint cpnum,
                                            uint opc1, uint crn, uint crm, uint opc2, ref uint rt, ref uint rt2);

    public class ArmCore : CpuCore
    {
        // ---------------------------------------------------------------- state

        /// <summary>R0..R15 (R15 is the PC). Banked registers live in the backing store.</summary>
        public readonly uint[] R = new uint[16];

        public uint Cpsr;

        /// <summary>Current instruction set: true = Thumb (T16/T32), false = ARM (A32).</summary>
        public bool Thumb;

        /// <summary>Reset vector used by Reset().</summary>
        public uint ResetVector;

        /// <summary>When false, the MMU is bypassed entirely (addresses are physical).</summary>
        public bool MmuEnabled;

        /// <summary>ARMv7-A short-descriptor MMU (never null).</summary>
        public ArmMmu Mmu;

        /// <summary>VFPv3-D16 unit (never null).</summary>
        public ArmVfp Vfp;

        /// <summary>Emit a "trace" line for every executed instruction.</summary>
        public bool TraceInstructions;

        /// <summary>Optional hook for the p0..p14 coprocessor space.</summary>
        public ArmCoprocessorHook CoprocessorHook;

        /// <summary>Number of exceptions taken (statistics for the GUI).</summary>
        public long ExceptionCount;

        /// <summary>Set when the last executed instruction was an undefined one.</summary>
        public uint LastUndefinedInstruction;

        // Big-endian data mode (CPSR.E / SETEND).
        private bool dataBigEndian;

        // ---- banked registers ---------------------------------------------------

        private const int ModeCount = 32;

        private readonly uint[] bankSp = new uint[ModeCount];
        private readonly uint[] bankLr = new uint[ModeCount];
        private readonly uint[] bankSpsr = new uint[ModeCount];
        private readonly uint[,] bankR8 = new uint[ModeCount, 5];

        // ---- exclusive monitor --------------------------------------------------

        private bool exclusiveValid;
        private uint exclusiveAddr;
        private ulong exclusiveValue;
        private int exclusiveSize;
        private uint exclusiveId;

        // ---- abort bookkeeping --------------------------------------------------

        private const int FaultNone = 0;
        private const int FaultPrefetch = 1;
        private const int FaultData = 2;

        private int pendingFault;
        private MmResult pendingMmFault;

        // ---- instruction decode temporaries -------------------------------------

        private uint curInstrAddr;
        private uint curInstr;
        private int curInstrLen;

        // Thumb-32 decomposed fields.
        private bool t32;
        private uint op1, op2, opc1, opc2;
        private uint opc1_16, opc1_8, opc1_x04, opc1_x02;
        private uint rd_16, rn_16, rm_16, rt_16, rd_12, rn_12, rm_12;
        private int imm3_32, imm3_16, imm3_12;
        private uint imm5_32, imm5, i8_32, i16_12, i16_8, i8_0;
        private int lsb_32, msb_32;
        private uint breg_ext_s, breg_ext_c, simm16, lsbit, reglist;
        private uint i32_32, i32_x02;

        // IT state (Thumb).
        private uint itState;
        private bool itStateValid;

        // ---------------------------------------------------------------- ctor

        public ArmCore(MemoryHub mem, ILogSink log = null)
            : base(mem, log)
        {
            Name = "ARM Cortex-A9";
            Mmu = new ArmMmu(mem);
            Vfp = new ArmVfp();
            Reset();
        }

        public override CpuArch Arch { get { return CpuArch.Arm; } }

        // ---------------------------------------------------------------- reset

        public override void Reset()
        {
            for (int i = 0; i < 16; i++) R[i] = 0;
            for (int i = 0; i < ModeCount; i++)
            {
                bankSp[i] = 0; bankLr[i] = 0; bankSpsr[i] = 0;
                for (int j = 0; j < 5; j++) bankR8[i, j] = 0;
            }
            Cpsr = 0x1D3;                 // SVC mode, ARM state, A/I/F set, flags clear
            Thumb = false;
            dataBigEndian = false;
            Halted = false;
            HaltReason = "";
            UndefinedInstruction = false;
            LastUndefinedInstruction = 0;
            InstructionCount = 0;
            CycleCount = 0;
            exclusiveValid = false;
            pendingFault = FaultNone;
            itStateValid = false;
            itState = 0;
            Mmu.Reset();
            MmuEnabled = false;
            Vfp.Reset();
            Pc = ResetVector;
            R[15] = ResetVector;
        }

        public override void Reset(uint entryPoint)
        {
            Reset();
            Pc = entryPoint;
            R[15] = entryPoint;
        }

        // ---------------------------------------------------------------- mode helpers

        public uint Mode { get { return Cpsr & 0x1Fu; } }

        public static string ModeName(uint mode)
        {
            switch (mode)
            {
                case 0x10: return "USR";
                case 0x11: return "FIQ";
                case 0x12: return "IRQ";
                case 0x13: return "SVC";
                case 0x16: return "MON";
                case 0x17: return "ABT";
                case 0x1A: return "HYP";
                case 0x1B: return "UND";
                case 0x1F: return "SYS";
                default: return "???";
            }
        }

        public bool IsPrivileged { get { return Mode != 0x10; } }

        public uint GetSpsr()
        {
            uint m = Mode;
            if (m == 0x10 || m == 0x1F) return 0;
            return bankSpsr[(int)m];
        }

        public void SetSpsr(uint v)
        {
            uint m = Mode;
            if (m == 0x10 || m == 0x1F) return;
            bankSpsr[(int)m] = v;
        }

        /// <summary>Swap the active R13/R14 (and R8..R12 for FIQ) with the given mode's bank.</summary>
        private void BankSwitch(uint oldMode, uint newMode)
        {
            if (oldMode == newMode) return;
            int o = (int)(oldMode & 0x1Fu);
            int n = (int)(newMode & 0x1Fu);

            bankSp[o] = R[13];
            bankLr[o] = R[14];
            if (o == 0x11) for (int j = 0; j < 5; j++) bankR8[o, j] = R[8 + j];

            Cpsr = (Cpsr & ~0x1Fu) | (newMode & 0x1Fu);
            R[13] = bankSp[n];
            R[14] = bankLr[n];
            if (n == 0x11) for (int j = 0; j < 5; j++) R[8 + j] = bankR8[n, j];
        }

        // ---------------------------------------------------------------- CPSR helpers

        public bool FlagN { get { return (Cpsr & 0x80000000u) != 0; } }
        public bool FlagZ { get { return (Cpsr & 0x40000000u) != 0; } }
        public bool FlagC { get { return (Cpsr & 0x20000000u) != 0; } }
        public bool FlagV { get { return (Cpsr & 0x10000000u) != 0; } }

        public void SetNZ(uint result)
        {
            Cpsr &= 0x3FFFFFFFu;
            if ((result & 0x80000000u) != 0) Cpsr |= 0x80000000u;
            if (result == 0) Cpsr |= 0x40000000u;
        }

        public void SetNZCV(uint result, bool carry, bool overflow)
        {
            Cpsr &= 0x0FFFFFFFu;
            if ((result & 0x80000000u) != 0) Cpsr |= 0x80000000u;
            if (result == 0) Cpsr |= 0x40000000u;
            if (carry) Cpsr |= 0x20000000u;
            if (overflow) Cpsr |= 0x10000000u;
        }

        /// <summary>Condition code evaluation (ARM ARM A8.3).</summary>
        public bool ConditionPassed(uint cond)
        {
            bool n = FlagN, z = FlagZ, c = FlagC, v = FlagV;
            switch (cond)
            {
                case 0x0: return z;
                case 0x1: return !z;
                case 0x2: return c;
                case 0x3: return !c;
                case 0x4: return n;
                case 0x5: return !n;
                case 0x6: return v;
                case 0x7: return !v;
                case 0x8: return c && !z;
                case 0x9: return !c || z;
                case 0xAu: return n == v;
                case 0xBu: return n != v;
                case 0xCu: return !z && (n == v);
                case 0xDu: return z || (n != v);
                default: return true;
            }
        }

        // ---------------------------------------------------------------- shifter

        private uint ShiftImm(uint value, int type, int amount, bool setCarry)
        {
            uint carry = FlagC ? 1u : 0u;
            switch (type)
            {
                case 0:
                    if (amount == 0) return value;
                    if (amount < 32) { carry = (value >> (32 - amount)) & 1u; value <<= amount; }
                    else if (amount == 32) { carry = value & 1u; value = 0; }
                    else { carry = 0; value = 0; }
                    break;
                case 1:
                    if (amount == 0) amount = 32;
                    if (amount < 32) { carry = (value >> (amount - 1)) & 1u; value >>= amount; }
                    else if (amount == 32) { carry = (value >> 31) & 1u; value = 0; }
                    else { carry = 0; value = 0; }
                    break;
                case 2:
                    if (amount == 0) amount = 32;
                    if (amount < 32) { carry = (value >> (amount - 1)) & 1u; value = (uint)((int)value >> amount); }
                    else { carry = (value >> 31) & 1u; value = (value & 0x80000000u) != 0 ? 0xFFFFFFFFu : 0u; }
                    break;
                default:
                    if (amount == 0)
                    {
                        uint nc = value & 1u;
                        value = (value >> 1) | (carry << 31);
                        carry = nc;
                    }
                    else
                    {
                        int r = amount & 31;
                        if (r == 0) carry = (value >> 31) & 1u;
                        else { value = (value >> r) | (value << (32 - r)); carry = (value >> 31) & 1u; }
                    }
                    break;
            }
            if (setCarry) { if (carry != 0) Cpsr |= 0x20000000u; else Cpsr &= ~0x20000000u; }
            return value;
        }

        private uint ShiftReg(uint value, int type, uint amount)
        {
            uint carry = FlagC ? 1u : 0u;
            if (amount == 0) return value;
            switch (type)
            {
                case 0:
                    if (amount < 32) { carry = (value >> (int)(32 - amount)) & 1u; value <<= (int)amount; }
                    else if (amount == 32) { carry = value & 1u; value = 0; }
                    else { carry = 0; value = 0; }
                    break;
                case 1:
                    if (amount < 32) { carry = (value >> (int)(amount - 1)) & 1u; value >>= (int)amount; }
                    else if (amount == 32) { carry = (value >> 31) & 1u; value = 0; }
                    else { carry = 0; value = 0; }
                    break;
                case 2:
                    if (amount < 32) { carry = (value >> (int)(amount - 1)) & 1u; value = (uint)((int)value >> (int)amount); }
                    else { carry = (value >> 31) & 1u; value = (value & 0x80000000u) != 0 ? 0xFFFFFFFFu : 0u; }
                    break;
                default:
                    {
                        int r = (int)(amount & 31);
                        if (r == 0) carry = (value >> 31) & 1u;
                        else { value = (value >> r) | (value << (32 - r)); carry = (value >> 31) & 1u; }
                    }
                    break;
            }
            if (carry != 0) Cpsr |= 0x20000000u; else Cpsr &= ~0x20000000u;
            return value;
        }

        private static uint AddWithCarry(uint a, uint b, bool carryIn, out bool carryOut, out bool overflow)
        {
            ulong sum = (ulong)a + (ulong)b + (carryIn ? 1ul : 0ul);
            uint r = (uint)sum;
            carryOut = (sum >> 32) != 0;
            overflow = ((a ^ r) & (b ^ r) & 0x80000000u) != 0;
            return r;
        }

        private static uint SubWithCarry(uint a, uint b, bool carryIn, out bool carryOut, out bool overflow)
        {
            ulong sum = (ulong)a + (ulong)(~b) + (carryIn ? 1ul : 0ul);
            uint r = (uint)sum;
            carryOut = (sum >> 32) != 0;
            overflow = ((a ^ b) & (a ^ r) & 0x80000000u) != 0;
            return r;
        }

        // ---------------------------------------------------------------- memory path

        private void SetAccessPc()
        {
            Mem.CurrentPC = curInstrAddr;
            Mem.CurrentCore = Name;
        }

        private uint MemReadWord(uint va, bool fetch)
        {
            SetAccessPc();
            if (MmuEnabled && Mmu.Enabled)
            {
                MmResult mm = Mmu.TranslateEx(va, false, fetch, Mode);
                if (!mm.Ok)
                {
                    pendingMmFault = mm;
                    pendingFault = fetch ? FaultPrefetch : FaultData;
                    if (fetch) Mmu.ReportPrefetchAbort(ref mm, va, false);
                    else Mmu.ReportDataAbort(ref mm, va, false);
                    return 0xFFFFFFFFu;
                }
                return Mem.ReadWord(mm.PhysAddr);
            }
            return Mem.ReadWord(va);
        }

        private uint MemReadHalf(uint va)
        {
            SetAccessPc();
            if (MmuEnabled && Mmu.Enabled)
            {
                MmResult mm = Mmu.TranslateEx(va, false, false, Mode);
                if (!mm.Ok)
                {
                    pendingMmFault = mm; pendingFault = FaultData;
                    Mmu.ReportDataAbort(ref mm, va, false);
                    return 0xFFFFu;
                }
                return Mem.ReadHalf(mm.PhysAddr);
            }
            return Mem.ReadHalf(va);
        }

        private uint MemReadByte(uint va)
        {
            SetAccessPc();
            if (MmuEnabled && Mmu.Enabled)
            {
                MmResult mm = Mmu.TranslateEx(va, false, false, Mode);
                if (!mm.Ok)
                {
                    pendingMmFault = mm; pendingFault = FaultData;
                    Mmu.ReportDataAbort(ref mm, va, false);
                    return 0xFFu;
                }
                return Mem.ReadByte(mm.PhysAddr);
            }
            return Mem.ReadByte(va);
        }

        private void MemWriteWord(uint va, uint value)
        {
            SetAccessPc();
            if (MmuEnabled && Mmu.Enabled)
            {
                MmResult mm = Mmu.TranslateEx(va, true, false, Mode);
                if (!mm.Ok)
                {
                    pendingMmFault = mm; pendingFault = FaultData;
                    Mmu.ReportDataAbort(ref mm, va, true);
                    return;
                }
                Mem.WriteWord(mm.PhysAddr, value);
                return;
            }
            Mem.WriteWord(va, value);
        }

        private void MemWriteHalf(uint va, uint value)
        {
            SetAccessPc();
            if (MmuEnabled && Mmu.Enabled)
            {
                MmResult mm = Mmu.TranslateEx(va, true, false, Mode);
                if (!mm.Ok)
                {
                    pendingMmFault = mm; pendingFault = FaultData;
                    Mmu.ReportDataAbort(ref mm, va, true);
                    return;
                }
                Mem.WriteHalf(mm.PhysAddr, (ushort)value);
                return;
            }
            Mem.WriteHalf(va, (ushort)value);
        }

        private void MemWriteByte(uint va, uint value)
        {
            SetAccessPc();
            if (MmuEnabled && Mmu.Enabled)
            {
                MmResult mm = Mmu.TranslateEx(va, true, false, Mode);
                if (!mm.Ok)
                {
                    pendingMmFault = mm; pendingFault = FaultData;
                    Mmu.ReportDataAbort(ref mm, va, true);
                    return;
                }
                Mem.WriteByte(mm.PhysAddr, (byte)value);
                return;
            }
            Mem.WriteByte(va, (byte)value);
        }

        private ulong MemReadDouble(uint va)
        {
            uint lo = MemReadWord(va, false);
            if (pendingFault != FaultNone) return 0;
            uint hi = MemReadWord(va + 4u, false);
            if (pendingFault != FaultNone) return 0;
            if (dataBigEndian) return ((ulong)lo << 32) | hi;
            return (ulong)lo | ((ulong)hi << 32);
        }

        private void MemWriteDouble(uint va, ulong value)
        {
            if (dataBigEndian)
            {
                MemWriteWord(va, (uint)(value >> 32));
                if (pendingFault != FaultNone) return;
                MemWriteWord(va + 4u, (uint)value);
            }
            else
            {
                MemWriteWord(va, (uint)value);
                if (pendingFault != FaultNone) return;
                MemWriteWord(va + 4u, (uint)(value >> 32));
            }
        }

        // ---------------------------------------------------------------- PC access

        private uint ReadPc()
        {
            if (Thumb) return (curInstrAddr + 4u) & 0xFFFFFFFCu;
            return curInstrAddr + 8u;
        }

        private uint ReadReg(int n)
        {
            if (n == 15) return ReadPc();
            return R[n];
        }

        private void WriteReg(int n, uint value)
        {
            if (n == 15) { Pc = value; R[15] = value; }
            else R[n] = value;
        }

        // ---------------------------------------------------------------- exceptions

        public uint VectorBase
        {
            get
            {
                uint vbar = Mmu != null ? Mmu.Vbar : 0u;
                if (vbar != 0) return vbar;
                if (Mmu != null && Mmu.HighVectors) return 0xFFFF0000u;
                return 0x00000000u;
            }
        }

        private const uint VecReset = 0x00;
        private const uint VecUndef = 0x04;
        private const uint VecSvc = 0x08;
        private const uint VecPrefetch = 0x0C;
        private const uint VecData = 0x10;
        private const uint VecHyp = 0x14;
        private const uint VecIrq = 0x18;
        private const uint VecFiq = 0x1C;

        /// <summary>Take a synchronous exception (ARM ARM B1.8.3 / B1.9).</summary>
        private void TakeException(uint vectorOffset, uint newMode, uint returnAddress)
        {
            uint oldMode = Mode;
            ExceptionCount++;

            uint savedCpsr = Cpsr;

            uint newCpsr = newMode & 0x1Fu;
            newCpsr |= 0x100u;                        // F
            if (newMode != 0x11) newCpsr |= 0x40u;    // I (FIQ does not set I)
            if (newMode != 0x1Au) newCpsr |= 0x80u;   // A (HYP does not)
            newCpsr |= (Cpsr & 0x0F000000u);          // keep NZCV
            newCpsr |= (Cpsr & 0x20000000u);          // keep E
            newCpsr &= ~0x20u;                        // ARM state on entry

            int o = (int)(oldMode & 0x1Fu);
            bankSp[o] = R[13];
            bankLr[o] = R[14];
            bankSpsr[o] = savedCpsr;
            if (o == 0x11) for (int j = 0; j < 5; j++) bankR8[o, j] = R[8 + j];

            Cpsr = newCpsr;
            Thumb = false;

            int n = (int)(newMode & 0x1Fu);
            R[13] = bankSp[n];
            R[14] = returnAddress;
            if (n == 0x11) for (int j = 0; j < 5; j++) R[8 + j] = bankR8[n, j];

            Pc = VectorBase + vectorOffset;
            R[15] = Pc;
        }

        /// <summary>Return from an exception: restore CPSR from SPSR and branch.</summary>
        private void ExceptionReturn(uint target)
        {
            uint spsr = GetSpsr();
            uint oldMode = Mode;
            uint newMode = spsr & 0x1Fu;

            int o = (int)(oldMode & 0x1Fu);
            bankSp[o] = R[13];
            bankLr[o] = R[14];
            if (o == 0x11) for (int j = 0; j < 5; j++) bankR8[o, j] = R[8 + j];

            Cpsr = spsr & ~(1u << 24);
            Thumb = (Cpsr & 0x20u) != 0;
            dataBigEndian = (Cpsr & 0x200u) != 0;

            int n = (int)(newMode & 0x1Fu);
            R[13] = bankSp[n];
            R[14] = bankLr[n];
            if (n == 0x11) for (int j = 0; j < 5; j++) R[8 + j] = bankR8[n, j];

            // The instruction set on return comes from SPSR.T, never from the
            // branch address (ARM ARM B1.8.3).
            Pc = target & ~1u;
            R[15] = Pc;
            pendingFault = FaultNone;
        }

        private void ExceptionReturnWithCpsr(uint target, uint newCpsr)
        {
            uint oldMode = Mode;
            int o = (int)(oldMode & 0x1Fu);
            bankSp[o] = R[13];
            bankLr[o] = R[14];
            if (o == 0x11) for (int j = 0; j < 5; j++) bankR8[o, j] = R[8 + j];

            Cpsr = newCpsr & ~(1u << 24);
            Thumb = (Cpsr & 0x20u) != 0;
            dataBigEndian = (Cpsr & 0x200u) != 0;
            int n = (int)(Cpsr & 0x1Fu);
            R[13] = bankSp[n];
            R[14] = bankLr[n];
            if (n == 0x11) for (int j = 0; j < 5; j++) R[8 + j] = bankR8[n, j];
            Pc = target & ~1u;
            R[15] = Pc;
            pendingFault = FaultNone;
        }

        private void RaiseDataAbort()
        {
            TakeException(VecData, 0x17, curInstrAddr + (Thumb ? 4u : 8u));
        }

        private void RaisePrefetchAbort()
        {
            TakeException(VecPrefetch, 0x17, curInstrAddr + (Thumb ? 4u : 8u));
        }

        // ---------------------------------------------------------------- Step

        public override StepResult Step()
        {
            StepResult res = new StepResult();
            res.Address = Pc;
            curInstrAddr = Pc;
            curInstrLen = Thumb ? 2 : 4;
            t32 = false;
            pendingFault = FaultNone;
            bool thumbAtEntry = Thumb;

            try
            {
                if (Thumb)
                {
                    R[15] = curInstrAddr + 4u;
                    uint hw1 = FetchHalf(curInstrAddr);
                    if (pendingFault == FaultNone && (hw1 & 0xF800u) >= 0xE800u)
                    {
                        uint hw2 = FetchHalf(curInstrAddr + 2u);
                        if (pendingFault == FaultNone)
                        {
                            curInstr = ((hw1 & 0xFFFFu) << 16) | (hw2 & 0xFFFFu);
                            curInstrLen = 4;
                            t32 = true;
                            DecomposeThumb32(curInstr);
                            ExecuteThumb32();
                        }
                    }
                    else if (pendingFault == FaultNone)
                    {
                        curInstr = hw1;
                        curInstrLen = 2;
                        // Thumb-16 instructions other than B<c> and CBZ/CBNZ ignore
                        // their own condition; inside an IT block the block's
                        // condition decides whether the instruction executes.
                        bool inIt = false, lastInIt = false;
                        uint tcond = ThumbCondition(ref inIt, ref lastInIt);
                        if (inIt && !ConditionPassed(tcond))
                        {
                            Pc = curInstrAddr + 2u;
                            R[15] = Pc;
                        }
                        else ExecuteThumb16();
                    }
                }
                else
                {
                    R[15] = curInstrAddr + 8u;
                    curInstr = MemReadWord(curInstrAddr, true);
                    curInstrLen = 4;
                    if (pendingFault == FaultNone) ExecuteArm();
                }
            }
            catch (Exception ex)
            {
                UndefinedInstruction = true;
                LastUndefinedInstruction = curInstr;
                Halted = true;
                HaltReason = "exception in Step() at 0x" + curInstrAddr.ToString("X8", CultureInfo.InvariantCulture) + ": " + ex.Message;
                Emit("cpu", "[" + Name + "] " + HaltReason);
                res.Faulted = true;
                res.Fault = ex.Message;
            }

            InstructionCount++;
            CycleCount++;

            if (!UndefinedInstruction && pendingFault != FaultNone)
            {
                int f = pendingFault;
                MmResult mm = pendingMmFault;
                pendingFault = FaultNone;
                if (f == FaultPrefetch) RaisePrefetchAbort(); else RaiseDataAbort();
                res.Faulted = true;
                res.Fault = Mmu.FaultText(mm.Fault, 0, false, f == FaultPrefetch);
            }
            else if (!UndefinedInstruction)
            {
                Pc = R[15];
            }

            res.Address = curInstrAddr;
            res.Length = curInstrLen;

            // Always fill the disassembly text: the GUI step listing and the trace
            // view consume StepResult directly.  The disassembler never throws.
            try
            {
                int lenTmp;
                res.Text = DisassembleAt(curInstrAddr, thumbAtEntry, out lenTmp);
            }
            catch (Exception)
            {
                res.Text = UndefinedInstruction ? ".word 0x" + curInstr.ToString("X8", CultureInfo.InvariantCulture)
                                                : "(disassembly failed)";
            }
            if (UndefinedInstruction)
            {
                res.Faulted = true;
                if (string.IsNullOrEmpty(res.Fault)) res.Fault = HaltReason;
            }
            if (TraceInstructions)
                Emit("trace", "0x" + curInstrAddr.ToString("X8", CultureInfo.InvariantCulture) + "  " + res.Text);
            return res;
        }

        private uint FetchHalf(uint addr)
        {
            SetAccessPc();
            if (MmuEnabled && Mmu.Enabled)
            {
                MmResult mm = Mmu.TranslateEx(addr, false, true, Mode);
                if (!mm.Ok)
                {
                    pendingMmFault = mm;
                    pendingFault = FaultPrefetch;
                    Mmu.ReportPrefetchAbort(ref mm, addr, false);
                    return 0xFFFFu;
                }
                return Mem.ReadHalf(mm.PhysAddr);
            }
            return Mem.ReadHalf(addr);
        }

        private string DisassembleForTrace()
        {
            int len;
            return ArmDisasm.Disassemble(this, curInstrAddr, Thumb, out len);
        }

        private void Undefined(string why)
        {
            UndefinedInstruction = true;
            LastUndefinedInstruction = curInstr;
            Halted = true;
            HaltReason = "undefined instruction 0x" + curInstr.ToString("X8", CultureInfo.InvariantCulture) +
                         " at 0x" + curInstrAddr.ToString("X8", CultureInfo.InvariantCulture) + " (" + why + ")";
            Emit("cpu", "[" + Name + "] " + HaltReason);
        }

        private void Note(string text)
        {
            Emit("cpu", "[" + Name + "@0x" + curInstrAddr.ToString("X8", CultureInfo.InvariantCulture) + "] " + text);
        }

        /// <summary>
        /// Branch to an address with BXWritePC semantics (ARM ARM A2.3.3): bit[0]
        /// of the target selects the instruction set, in *both* directions.  Used
        /// by BX, BLX (register), LDR pc, LDM/POP with PC in the list and by the
        /// Thumb-16 MOV/ADD with Rd == PC.
        /// </summary>
        private void BranchTo(uint target)
        {
            Thumb = (target & 1u) != 0u;
            Pc = target & ~1u;
            R[15] = Pc;
        }

        // ================================================================ A32

        private void ExecuteArm()
        {
            uint instr = curInstr;
            uint cond = instr >> 28;
            uint op1v = (instr >> 25) & 7u;

            if (cond == 0xFu)
            {
                ExecuteArmUnconditional(instr);
                return;
            }

            bool passed = (cond == 0xEu) || ConditionPassed(cond);
            if (!passed)
            {
                Pc = curInstrAddr + 4u;
                R[15] = Pc;
                return;
            }

            if (op1v == 0u)
            {
                // 000x: H1 extra load/store, multiply/swap, DSP media, MISC, then
                // data processing (register).  The classification is a function of
                // bits [27:20] (ga) and bits [7:4] (nib):
                //   nib in {1011,1101,1111}          : H1 extra load/store
                //   nib == 1001                      : multiply / swap
                //   ga 0x10..0x17, even ga, nib==0101 or nib in {1000,1010,1100,1110}
                //                                    : DSP media (QADD/SMLAxy/...)
                //   ga 0x10..0x17, exact masks       : MRS/MSR/BX/BLX/BXJ/CLZ
                //   otherwise                        : data processing (register)
                uint ga = (instr >> 20) & 0xFFu;
                uint nib = instr & 0xF0u;
                if (nib == 0xB0u || nib == 0xD0u || nib == 0xF0u) { DecodeArmExtraLoadStore(instr); return; }
                if (nib == 0x90u) { DecodeArmMulMedia(instr); return; }
                if (ga >= 0x10u && ga <= 0x17u && (ga & 1u) == 0u &&
                    (nib == 0x50u || (nib & 0x90u) == 0x80u))
                { DecodeArmMedia(instr); return; }
                if ((instr & 0x0FFF0FF0u) == 0x016F0F10u)
                {
                    R[(int)((instr >> 12) & 0xFu)] = Clz(R[(int)(instr & 0xFu)]);
                    Pc = curInstrAddr + 4u; R[15] = Pc; return;
                }
                if (ga >= 0x10u && ga <= 0x17u && IsArmMiscEncoding(instr)) { DecodeArmMisc(instr); return; }
                ExecuteDataProcessing(instr, (instr >> 21) & 0xFu, false);
                return;
            }

            if (op1v == 1u)
            {
                // 001x: data processing (immediate) / MSR immediate.
                uint dpOp = (instr >> 21) & 0xFu;
                if ((dpOp & 0xAu) == 0xAu && ((instr >> 12) & 0xFu) == 0xFu)
                {
                    // MSR immediate: bits [27:23] = 00110, Rn field == 1111.
                    MsrArm(instr, DecodeImm12(instr));
                    return;
                }
                ExecuteDataProcessing(instr, dpOp, true);
                return;
            }

            if (op1v == 2u || op1v == 3u)
            {
                // 010x / 011x: load/store word and unsigned byte.  Bit 4 is a
                // fixed 0 in the load/store encoding, so bit 4 set in the
                // register-offset form (011) selects the DSP/SIMD media space
                // that shares op1 == 011 (SSAT/USAT/PKH/SMUAD/parallel add-sub).
                if (op1v == 3u && (instr & 0x10u) != 0u) { DecodeArmMedia(instr); return; }
                ExecuteArmLoadStore(instr, op1v == 2u);
                return;
            }

            if (op1v == 4u) { ExecuteArmLoadStoreMultiple(instr); return; }
            if (op1v == 5u) { ExecuteArmBranch(instr); return; }

            // bits [27:24] == 1111 with cond != 1111 is SVC (the coprocessor
            // space only covers 110x (0xEC/0xED) and 1110 (0xEE)).
            if ((instr & 0x0F000000u) == 0x0F000000u)
            {
                TakeException(VecSvc, 0x13u, curInstrAddr + 4u);
                return;
            }

            ExecuteArmCoprocessor(instr);
        }
        private static uint DecodeImm12(uint instr)
        {
            uint imm = instr & 0xFFu;
            uint rot = ((instr >> 8) & 0xFu) * 2u;
            if (rot == 0) return imm;
            return (imm >> (int)rot) | (imm << (32 - (int)rot));
        }

        private uint ShifterOperand(uint instr, bool setCarry)
        {
            uint rm = instr & 0xFu;
            if ((instr & 0x10u) == 0)
            {
                int type = (int)((instr >> 5) & 3u);
                int amount = (int)((instr >> 7) & 0x1Fu);
                return ShiftImm(ReadReg((int)rm), type, amount, setCarry);
            }
            uint rs = (instr >> 8) & 0xFu;
            int t2 = (int)((instr >> 5) & 3u);
            if (rs == 15)
            {
                uint amount = (ReadPc() + 4u) & 0xFFu;
                return ShiftImm(ReadReg((int)rm), t2, (int)amount, false);
            }
            return ShiftReg(ReadReg((int)rm), t2, R[rs] & 0xFFu);
        }

        private void ExecuteDataProcessing(uint instr, uint op, bool immediate)
        {
            bool s = (instr & (1u << 20)) != 0;
            int rn = (int)((instr >> 16) & 0xFu);
            int rd = (int)((instr >> 12) & 0xFu);
            bool logical = op <= 1u || op >= 8u;

            // A32 MOVW / MOVT: bits [27:25] = 001, opcode 1000 (MOVW) or 1010
            // (MOVT) with S == 0; otherwise those opcodes are TST / CMP.
            if (immediate && !s && (op == 0x8u || op == 0xAu) && rd != 15)
            {
                uint imm16 = ((uint)rn << 12) | (instr & 0xFFFu);
                if (op == 0x8u) R[rd] = imm16;                       // MOVW
                else R[rd] = (R[rd] & 0x0000FFFFu) | (imm16 << 16);   // MOVT
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            uint op2;
            bool shifterCarry = FlagC;
            if (immediate) op2 = DecodeImm12(instr);
            else
            {
                op2 = ShifterOperand(instr, s && logical);
                shifterCarry = FlagC;
            }

            uint a = (op == 0xFu || op == 0xDu) ? 0u : ReadReg(rn);
            uint result;
            bool carry = FlagC, overflow = FlagV;
            bool writeResult = true;

            switch (op)
            {
                case 0x0u: result = a & op2; break;
                case 0x1u: result = a ^ op2; break;
                case 0x2u: result = SubWithCarry(a, op2, true, out carry, out overflow); break;
                case 0x3u: result = SubWithCarry(op2, a, true, out carry, out overflow); break;
                case 0x4u: result = AddWithCarry(a, op2, false, out carry, out overflow); break;
                case 0x5u: result = AddWithCarry(a, op2, FlagC, out carry, out overflow); break;
                case 0x6u: result = SubWithCarry(a, op2, FlagC, out carry, out overflow); break;
                case 0x7u: result = SubWithCarry(op2, a, FlagC, out carry, out overflow); break;
                case 0x8u: result = a & op2; writeResult = false; break;
                case 0x9u: result = a ^ op2; writeResult = false; break;
                case 0xAu: result = SubWithCarry(a, op2, true, out carry, out overflow); writeResult = false; break;
                case 0xBu: result = AddWithCarry(a, op2, false, out carry, out overflow); writeResult = false; break;
                case 0xCu: result = a | op2; break;
                case 0xDu: result = op2; break;
                case 0xEu: result = a & ~op2; break;
                default: result = ~op2; break;
            }

            if (writeResult)
            {
                if (rd == 15)
                {
                    if (s)
                    {
                        if ((Cpsr & 0x1Fu) == 0x10)
                            Emit("cpu", "[" + Name + "] S bit with Rd=15 in User mode is unpredictable");
                        ExceptionReturn(result);
                        return;
                    }
                    // ARM ALUWritePC: the instruction set does not change.
                    Pc = result & ~3u;
                    R[15] = Pc;
                    return;
                }
                R[rd] = result;
            }

            if (s)
            {
                if (logical)
                {
                    SetNZ(result);
                    // ShifterOperand already updated C when a shift was applied;
                    // for register-specified shifts the carry is also updated.
                    if (immediate || !(shifterCarry || true)) { }
                }
                else SetNZCV(result, carry, overflow);
            }

            Pc = curInstrAddr + 4u;
            R[15] = Pc;
        }

        // ---- multiply / media (bits [27:23] = 00001) ---------------------------

        private void DecodeArmMulMedia(uint instr)
        {
            uint op = (instr >> 21) & 0xFu;
            bool s = (instr & (1u << 20)) != 0;
            int rd = (int)((instr >> 16) & 0xFu);
            int rn = (int)((instr >> 12) & 0xFu);
            int rs = (int)((instr >> 8) & 0xFu);
            int rm = (int)(instr & 0xFu);
            int rnField = (int)((instr >> 16) & 0xFu);
            int rdField = (int)((instr >> 12) & 0xFu);

            // SDIV/UDIV: 0111 0001 / 0111 0011 with Rd = bits [19:16] and
            // Rn = bits [15:12]; the 4-bit opcode field is x001 and the low nibble
            // of Rm is zero, which is what separates them from MULS/MLAS.
            if (op == 0x1u && (instr & 0xFu) == 0u)
            {
                uint dividend = R[rnField];
                uint divisor = R[(int)rs];
                uint quotient;
                if (divisor == 0) quotient = 0u;   // SDIV/UDIV by zero yields 0
                else if ((instr & (1u << 22)) != 0u)
                {
                    // UDIV
                    quotient = dividend / divisor;
                }
                else
                {
                    int a = (int)dividend, b = (int)divisor;
                    quotient = b == 0 ? 0u
                        : (a == int.MinValue && b == -1) ? unchecked((uint)int.MinValue)
                        : (uint)(a / b);
                }
                R[rnField] = quotient;
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            if (op == 0x1u)
            {
                // SMLABB/SMLABT/SMLATB/SMLATT (x=bit5, y=bit6) and SMLAWB/SMLAWT.
                uint r = DspSmla(instr, rdField, rnField, rs, rm);
                R[rdField] = r;
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }
            if (op == 0x3u)
            {
                // SMULBB/SMULBT/SMULTB/SMULTT, SMULWB/SMULWT
                R[rdField] = DspSmul(instr, rs, rm);
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }
            if (op == 0x5u)
            {
                // SMLALBB/SMLALBT/SMLALTB/SMLALTT
                DecodeSmlalxy(instr, (uint)rdField, (uint)rnField, rm, rs);
                return;
            }

            if (op == 0x2u || op == 0x6u)
            {
                // SMLABB.. with A bit (bit 22) set: SMLAW/SMULW
                if (op == 0x2u) { R[rdField] = DspSmulw(instr, rs, rm); }
                else { R[rdField] = DspSmlaw(instr, rdField, rnField, rs, rm); }
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            // op == 0, 4, 8..15: the classic multiply group.
            switch (op)
            {
                case 0u:
                    {
                        uint r = R[rm] * R[rs];
                        R[rnField] = r;
                        if (s) SetNZ(r);
                        break;
                    }
                case 4u:
                    {
                        ulong r = (ulong)R[rm] * R[rs] + R[rdField] + R[rnField];
                        R[rdField] = (uint)r;
                        R[rnField] = (uint)(r >> 32);
                        break;
                    }
                case 6u:
                    {
                        uint r = R[rnField] - R[rm] * R[rs];
                        R[rdField] = r;
                        break;
                    }
                case 8u:
                case 9u:
                case 0xAu:
                case 0xBu:
                case 0xCu:
                case 0xDu:
                case 0xEu:
                case 0xFu:
                    {
                        bool unsigned = (op & 4u) == 0u;
                        bool accumulate = (op & 2u) != 0u;
                        ulong prod = unsigned
                            ? (ulong)R[rm] * R[rs]
                            : (ulong)((long)(int)R[rm] * (int)R[rs]);
                        if (accumulate) prod += (ulong)R[rnField] | ((ulong)R[rdField] << 32);
                        R[rnField] = (uint)prod;
                        R[rdField] = (uint)(prod >> 32);
                        if (s)
                        {
                            Cpsr &= 0x0FFFFFFFu;
                            if ((R[rdField] & 0x80000000u) != 0) Cpsr |= 0x80000000u;
                            if (R[rdField] == 0 && R[rnField] == 0) Cpsr |= 0x40000000u;
                        }
                        break;
                    }
                default:
                    // MLAs for op==1 were handled above; op==3/5/7 as well.
                    if (op == 1u || op == 3u) { }
                    Undefined("multiply op=" + op);
                    return;
            }
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        private uint DspSmla(uint instr, int rd, int rn, int rs, int rm)
        {
            bool bit5 = (instr & 0x20u) != 0;
            bool bit6 = (instr & 0x40u) != 0;
            if (!bit5)
            {
                // SMLAWB / SMLAWT: word x halfword, result = (prod + acc) >> 16
                long prod = (long)(int)R[rm] * (long)(bit6 ? (short)(R[rs] >> 16) : (short)(R[rs] & 0xFFFFu));
                long sum = prod + (int)R[rn];
                bool ov = sum != (long)(int)(sum >> 16) && false;
                bool ovf = (sum > 2147483647L) || (sum < -2147483648L);
                if (ovf) Cpsr |= 0x10000000u; else Cpsr &= ~0x10000000u;
                return (uint)(int)(sum >> 16);
            }
            long p = (long)(bit5 ? (short)(R[rm] >> 16) : (short)(R[rm] & 0xFFFFu)) *
                     (long)(bit6 ? (short)(R[rs] >> 16) : (short)(R[rs] & 0xFFFFu));
            long s2 = p + (int)R[rn];
            bool ov2 = (s2 > 2147483647L) || (s2 < -2147483648L);
            if (ov2) Cpsr |= 0x10000000u; else Cpsr &= ~0x10000000u;
            return (uint)(int)s2;
        }

        private uint DspSmlaw(uint instr, int rd, int rn, int rs, int rm)
        {
            return DspSmla(instr, rd, rn, rs, rm);
        }

        private uint DspSmulw(uint instr, int rs, int rm)
        {
            bool bit6 = (instr & 0x40u) != 0;
            long p = (long)(int)R[rm] * (long)(bit6 ? (short)(R[rs] >> 16) : (short)(R[rs] & 0xFFFFu)) >> 16;
            return (uint)(int)p;
        }

        private uint DspSmul(uint instr, int rs, int rm)
        {
            bool bit5 = (instr & 0x20u) != 0;
            bool bit6 = (instr & 0x40u) != 0;
            if (!bit5)
            {
                // SMULWB / SMULWT
                long p = (long)(int)R[rm] * (long)(bit6 ? (short)(R[rs] >> 16) : (short)(R[rs] & 0xFFFFu));
                return (uint)(int)(p >> 16);
            }
            long v = (long)((short)(R[rm] & 0xFFFFu));
            if (bit5) v = (short)(R[rm] >> 16);
            long w = bit6 ? (short)(R[rs] >> 16) : (short)(R[rs] & 0xFFFFu);
            return (uint)(int)(v * w);
        }

        private void DecodeSmlalxy(uint instr, uint rdLo, uint rdHi, int rm, int rs)
        {
            bool x = (instr & 0x20u) != 0;
            bool y = (instr & 0x40u) != 0;
            long prod = (long)(x ? (short)(R[rm] >> 16) : (short)(R[rm] & 0xFFFFu)) *
                        (long)(y ? (short)(R[rs] >> 16) : (short)(R[rs] & 0xFFFFu));
            long acc = (long)((ulong)R[(int)rdLo] | ((ulong)R[(int)rdHi] << 32));
            long sum = acc + prod;
            R[(int)rdLo] = (uint)sum;
            R[(int)rdHi] = (uint)((ulong)sum >> 32);
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        // ---- media space (bits [27:25] = 011, bit 4 = 1) ------------------------

        private void DecodeArmMedia(uint instr)
        {
            uint o1 = (instr >> 20) & 0xFFu;
            uint o2 = (instr >> 5) & 7u;
            int rd = (int)((instr >> 12) & 0xFu);
            int rn = (int)((instr >> 16) & 0xFu);
            int rs = (int)((instr >> 8) & 0xFu);
            int rm = (int)(instr & 0xFu);

            switch (o1)
            {
                case 0x10u: case 0x12u: case 0x14u: case 0x16u:
                    DecodeMediaA(instr, rd, rn, rs, rm, o1);
                    break;
                case 0x60u:
                    Undefined("media 0110 0000");
                    return;
                case 0x61u: case 0x62u: case 0x63u:
                case 0x65u: case 0x66u: case 0x67u:
                    ParallelAddSub(instr, rd, rn, rm, o1, o2);
                    break;
                case 0x68u:
                    if ((instr & 0xFF0u) == 0xFB0u)
                    {
                        // SEL: each byte of Rd comes from Rn or Rm according to
                        // the corresponding GE flag.
                        R[rd] = SelectBytes(R[rn], R[rm], Cpsr);
                        break;
                    }
                    if ((o2 & 3u) == 0u)
                    {
                        // PKHBT: Rd = (Rn << shift)[31:16] : Rm[15:0]
                        uint shift = (instr >> 7) & 0x1Fu;
                        R[rd] = (ShiftImm(R[rn], 0, (int)shift, false) & 0xFFFF0000u) | (R[rm] & 0xFFFFu);
                    }
                    else if ((o2 & 3u) == 2u)
                    {
                        // PKHTB: Rd = Rn[31:16] : (Rm ASR shift)[15:0]
                        uint shift = (instr >> 7) & 0x1Fu;
                        if (shift == 0) shift = 32;
                        R[rd] = (R[rn] & 0xFFFF0000u) | (ShiftImm(R[rm], 2, (int)shift, false) & 0xFFFFu);
                    }
                    else if ((o2 & 3u) == 3u)
                    {
                        // SXTB16 / SXTAB16
                        uint src = RotateRight(R[rm], (int)((instr >> 10) & 3u) * 8);
                        uint ext = (uint)(int)(short)(src & 0xFFFFu);
                        ext |= (uint)(int)(short)(src >> 16) << 16;
                        R[rd] = rn == 15 ? ext : ext + R[rn];
                    }
                    else { Undefined("media 0110 1000 op2=" + o2); return; }
                    break;
                case 0x6Au:
                case 0x6Bu:
                    DecodeSatExtend(instr, rd, rn, rm, o1, o2);
                    break;
                case 0x6Cu:
                    if ((o2 & 3u) == 3u)
                    {
                        // UXTB16 / UXTAB16
                        uint srcU = RotateRight(R[rm], (int)((instr >> 10) & 3u) * 8);
                        uint extU = (srcU & 0xFFFFu) | ((srcU >> 16) << 16);
                        R[rd] = rn == 15 ? extU : extU + R[rn];
                    }
                    else { Undefined("media 0110 1100 op2=" + o2); return; }
                    break;
                case 0x6Eu:
                case 0x6Fu:
                    DecodeSatExtend(instr, rd, rn, rm, o1, o2);
                    break;
                case 0x70u:
                case 0x74u:
                    DecodeSmuad(instr, rd, rn, rs, rm, o1, o2);
                    break;
                case 0x78u:
                    if (o2 == 0u)
                    {
                        bool a = (instr & (1u << 20)) != 0;
                        int diff = 0;
                        for (int k = 0; k < 4; k++)
                        {
                            int b1 = (int)((R[rm] >> (k * 8)) & 0xFFu);
                            int b2 = (int)((R[rs] >> (k * 8)) & 0xFFu);
                            diff += b1 > b2 ? b1 - b2 : b2 - b1;
                        }
                        uint res = (uint)diff;
                        if (a) res += R[rn];
                        R[rd] = res;
                    }
                    else if (o2 == 2u)
                    {
                        // SMLALD (A=0) / SMLSLD (A=1)
                        bool sub = (instr & (1u << 20)) != 0;
                        long p1 = (long)(short)(R[rm] & 0xFFFFu) * (long)(short)(R[rs] & 0xFFFFu);
                        long p2 = (long)(short)(R[rm] >> 16) * (long)(short)(R[rs] >> 16);
                        long acc = (long)((ulong)R[rn] | ((ulong)R[rd] << 32));
                        long res2 = sub ? acc - (p1 + p2) : acc + (p1 + p2);
                        R[rn] = (uint)res2;
                        R[rd] = (uint)((ulong)res2 >> 32);
                    }
                    else { Undefined("media 0111 1000 op2=" + o2); return; }
                    break;
                case 0x7Au: case 0x7Bu:
                case 0x7Cu: case 0x7Du: case 0x7Eu: case 0x7Fu:
                    DecodeBitfield(instr, rd, rn, rs);
                    break;
                case 0x75u: case 0x76u: case 0x77u:
                case 0x71u: case 0x72u: case 0x73u:
                    DecodeSmmul(instr, rd, rn, rs, rm, o1);
                    break;
                default:
                    Undefined("media op1=0x" + o1.ToString("X2", CultureInfo.InvariantCulture));
                    return;
            }
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        /// <summary>
        /// 00010xx0 media space: QADD/QSUB/QDADD/QDSUB and the SMLAxy/SMLAWy/
        /// SMULxy/SMULWy/SMLALxy signed multiply families.  Field map: bits
        /// [19:16] is Rd / RdHi, bits [15:12] is Rn / RdLo, bits [11:8] is Rs,
        /// bits [3:0] is Rm, bit 5 is x and bit 6 is y (halfword selectors).
        /// </summary>
        private void DecodeMediaA(uint instr, int rd, int rn, int rs, int rm, uint ga)
        {
            uint nib = instr & 0xF0u;
            bool x = (instr & 0x20u) != 0u;
            bool y = (instr & 0x40u) != 0u;
            int mh = x ? (int)(short)(R[rm] >> 16) : (int)(short)(R[rm] & 0xFFFFu);
            int sh = y ? (int)(short)(R[rs] >> 16) : (int)(short)(R[rs] & 0xFFFFu);

            if (nib == 0x50u)
            {
                long rmV = (long)(int)R[rm];
                long rnV = (long)(int)R[rn];
                long v;
                if (ga == 0x10u) v = rmV + rnV;
                else if (ga == 0x12u) v = rmV - rnV;
                else
                {
                    long dbl = 2L * rnV;
                    if (dbl > int.MaxValue) { dbl = int.MaxValue; Cpsr |= 0x08000000u; }
                    else if (dbl < int.MinValue) { dbl = int.MinValue; Cpsr |= 0x08000000u; }
                    v = (ga == 0x14u) ? rmV + dbl : rmV - dbl;
                }
                int res;
                if (v > int.MaxValue) { res = int.MaxValue; Cpsr |= 0x08000000u; }
                else if (v < int.MinValue) { res = int.MinValue; Cpsr |= 0x08000000u; }
                else res = (int)v;
                R[rd] = (uint)res;
                return;
            }

            if ((nib & 0x90u) != 0x80u)
            {
                Undefined("media 0001 0xx0 0x" + instr.ToString("X8", CultureInfo.InvariantCulture));
                return;
            }

            switch (ga)
            {
                case 0x10u:
                    R[rn] = (uint)((int)R[rd] + mh * sh);
                    return;
                case 0x12u:
                    {
                        long p = (long)mh * (long)(int)R[rs];
                        int hi = (int)(p >> 16);
                        R[rn] = x ? (uint)hi : (uint)((int)R[rd] + hi);
                        return;
                    }
                case 0x14u:
                    {
                        long acc = (long)((ulong)R[rd] | ((ulong)R[rn] << 32));
                        long res2 = acc + (long)mh * (long)sh;
                        R[rd] = (uint)res2;
                        R[rn] = (uint)((ulong)res2 >> 32);
                        return;
                    }
                default:
                    R[rn] = (uint)(mh * sh);
                    return;
            }
        }

        private static uint SelectBytes(uint a, uint b, uint cpsr)
        {
            uint r = 0;
            for (int k = 0; k < 4; k++)
            {
                bool ge = (cpsr & (0x10000000u >> k)) != 0;
                uint mask = 0xFFu << (k * 8);
                r |= ge ? (b & mask) : (a & mask);
            }
            return r;
        }

        private void ParallelAddSub(uint instr, int rd, int rn, int rm, uint op1, uint op2)
        {
            bool unsigned = op1 >= 5u;
            bool halving = (op1 == 3u || op1 == 7u);
            bool saturating = (op1 == 2u || op1 == 6u);
            uint a = R[rn], b = R[rm];
            uint result = 0;
            uint ge = 0;

            // bits [7:5] select the operation: 0 = add16, 1 = asx, 2 = sax,
            // 3 = sub16, 4 = add8, 7 = sub8 (5 and 6 are reserved).
            bool is8 = (op2 == 4u || op2 == 7u);
            bool isAsx = (op2 == 1u);
            bool isSax = (op2 == 2u);
            bool subtract = (op2 == 3u || op2 == 7u);

            if (is8)
            {
                for (int k = 0; k < 4; k++)
                {
                    int x = (int)((a >> (k * 8)) & 0xFFu);
                    int y = (int)((b >> (k * 8)) & 0xFFu);
                    if (!unsigned) { x = (sbyte)x; y = (sbyte)y; }
                    int r = subtract ? x - y : x + y;
                    if (saturating) r = SaturateSigned(r, 8);
                    else if (halving) r >>= 1;
                    result |= (uint)(r & 0xFF) << (k * 8);
                    if (r >= 0) ge |= 0x10000000u >> k;
                }
            }
            else if (isAsx || isSax)
            {
                int a0 = (int)(a & 0xFFFFu), a1 = (int)(a >> 16);
                int b0 = (int)(b & 0xFFFFu), b1 = (int)(b >> 16);
                if (!unsigned) { a0 = (short)a0; a1 = (short)a1; b0 = (short)b0; b1 = (short)b1; }
                int r0, r1;
                if (isAsx)
                {
                    // ASX: (Rn[31:16] op Rm[15:0]) : (Rn[15:0] op Rm[31:16])
                    if (subtract) { r0 = a0 - b1; r1 = a1 - b0; }
                    else { r0 = a0 + b1; r1 = a1 + b0; }
                }
                else
                {
                    // SAX: (Rn[31:16] op Rm[31:16]) : (Rn[15:0] op Rm[15:0])
                    if (subtract) { r0 = a0 - b0; r1 = a1 - b1; }
                    else { r0 = a0 + b0; r1 = a1 + b1; }
                }
                if (saturating) { r0 = SaturateSigned(r0, 16); r1 = SaturateSigned(r1, 16); }
                else if (halving) { r0 >>= 1; r1 >>= 1; }
                result = (uint)(((r1 & 0xFFFF) << 16) | (r0 & 0xFFFF));
            }
            else
            {
                int a0 = (int)(a & 0xFFFFu), a1 = (int)(a >> 16);
                int b0 = (int)(b & 0xFFFFu), b1 = (int)(b >> 16);
                if (!unsigned) { a0 = (short)a0; a1 = (short)a1; b0 = (short)b0; b1 = (short)b1; }
                int r0, r1;
                if (subtract) { r0 = a0 - b0; r1 = a1 - b1; }
                else { r0 = a0 + b0; r1 = a1 + b1; }
                if (saturating) { r0 = SaturateSigned(r0, 16); r1 = SaturateSigned(r1, 16); }
                else if (halving) { r0 >>= 1; r1 >>= 1; }
                result = (uint)(((r1 & 0xFFFF) << 16) | (r0 & 0xFFFF));
            }

            R[rd] = result;
            if (is8) Cpsr = (Cpsr & 0x0FFFFFFFu) | ge;
        }

        private static int SaturateSigned(int v, int bits)
        {
            int max = (1 << (bits - 1)) - 1;
            int min = -(1 << (bits - 1));
            if (v > max) return max;
            if (v < min) return min;
            return v;
        }

        /// <summary>SSAT/USAT/SXTAB/SXTAH/UXTAB/UXTAH and the Rn=1111 extract forms.</summary>
        private void DecodeSatExtend(uint instr, int rd, int rn, int rm, uint op1, uint op2)
        {
            // op1 (bits [27:20]) is 0x6A/0x6B/0x6E/0x6F: bit 2 selects the
            // unsigned form and bit 0 the halfword form.  bits [6:5] == 11 is
            // the sign/zero extend form, bit 5 set with bit 6 clear the 16-bit
            // saturate form and bit 5 clear the plain saturate form.
            bool unsigned = (op1 & 4u) != 0u;
            bool half = (op1 & 1u) != 0u;
            uint sel = (instr >> 5) & 3u;

            if (sel == 3u)
            {
                // SXTB/SXTH/UXTB/UXTH (Rn == 1111) and SXTAB/SXTAH/UXTAB/UXTAH.
                int rot = (int)((instr >> 10) & 3u) * 8;
                uint src = RotateRight(R[rm], rot);
                uint ext;
                if (half) ext = unsigned ? (src & 0xFFFFu) : (uint)(int)(short)(src & 0xFFFFu);
                else ext = unsigned ? (src & 0xFFu) : (uint)(int)(sbyte)(src & 0xFFu);
                R[rd] = rn == 15 ? ext : ext + R[rn];
                return;
            }
            if ((sel & 1u) != 0u && ((instr >> 7) & 0x1Fu) == 0x1Eu)
            {
                // SSAT16 / USAT16: saturate each halfword independently.
                int sat16 = (int)((instr >> 16) & 0xFu);
                uint v = R[rm] & 0xFFFFu;
                if (unsigned)
                {
                    uint max = sat16 >= 16 ? 0xFFFFu : ((1u << sat16) - 1u);
                    if (v > max) { v = max; Cpsr |= 0x08000000u; }
                }
                else
                {
                    int bits16 = sat16 + 1;
                    int sv = (int)(short)v;
                    int max = (1 << (bits16 - 1)) - 1;
                    int min = -(1 << (bits16 - 1));
                    if (sv > max) { sv = max; Cpsr |= 0x08000000u; }
                    else if (sv < min) { sv = min; Cpsr |= 0x08000000u; }
                    v = (uint)(sv & 0xFFFF);
                }
                R[rd] = v;
                return;
            }
            {
                // SSAT / USAT.
                uint satImm = (instr >> 16) & 0x1Fu;
                bool asr = (instr & 0x40u) != 0;
                int shift = (int)((instr >> 7) & 0x1Fu);
                uint value = ShiftImm(R[rm], asr ? 2 : 0, shift, false);
                int sat;
                if (unsigned)
                {
                    int max = (int)satImm >= 32 ? 0x7FFFFFFF : (1 << (int)satImm) - 1;
                    long lv = (long)(int)value;
                    if (lv > max) { sat = max; Cpsr |= 0x08000000u; }
                    else if (lv < 0) { sat = 0; Cpsr |= 0x08000000u; }
                    else sat = (int)lv;
                }
                else sat = SaturateSigned((int)value, (int)satImm + 1);
                R[rd] = (uint)sat;
                return;
            }
        }

        /// <summary>SMUAD/SMUSD (A=0) and SMLAD/SMLSD (A=1).</summary>
        private void DecodeSmuad(uint instr, int rd, int rn, int rs, int rm, uint op1, uint op2)
        {
            bool a = (instr & (1u << 20)) != 0;
            bool x = (instr & 0x20u) != 0;
            bool y = (instr & 0x40u) != 0;
            bool alternating = op1 == 0x74u;  // bits [23:20] = 0100/0101: X/Y swap
            int m0 = x ? (short)(R[rm] >> 16) : (short)(R[rm] & 0xFFFFu);
            int m1 = x ? (short)(R[rm] & 0xFFFFu) : (short)(R[rm] >> 16);
            int s0 = y ? (short)(R[rs] >> 16) : (short)(R[rs] & 0xFFFFu);
            int s1 = y ? (short)(R[rs] & 0xFFFFu) : (short)(R[rs] >> 16);
            if (alternating) { int t = m0; m0 = m1; m1 = t; }

            long p1 = (long)m0 * s0;
            long p2 = (long)m1 * s1;
            long res = (op2 == 1u || op2 == 3u || op2 == 5u || op2 == 7u) ? (p1 - p2) : (p1 + p2);
            // SMUSD/SMLSD use op2 == 1 (i.e. bit 5 clear? no): the Q bit is op2[0].
            bool subtract = false;
            // SMLSD/SMUSD are selected by the "sub" form; the decoder already
            // distinguishes via op2 patterns - see the caller.
            res = subtract ? (p1 - p2) : res;
            if (a) res += (long)(int)R[rn];
            bool ov = res > 2147483647L || res < -2147483648L;
            if (ov) Cpsr |= 0x10000000u; else Cpsr &= ~0x10000000u;
            R[rd] = (uint)(int)res;
        }

        private void DecodeSmmul(uint instr, int rd, int rn, int rs, int rm, uint op1)
        {
            bool round = (instr & 0x20u) != 0;
            long prod = (long)(int)R[rm] * (long)(int)R[rs];
            long res = prod;
            uint kind = (op1 >> 1) & 3u;   // 0 = SMMUL, 2 = SMMLA, 3 = SMMLS
            if (kind == 2u) res = prod + (long)(int)R[rn];
            else if (kind == 3u) res = (long)(int)R[rn] - prod;
            if (round) res += 0x80000000L;
            R[rd] = (uint)(int)(res >> 32);
        }

        private void DecodeBitfield(uint instr, int rd, int rn, int rs)
        {
            int lsb = (int)((instr >> 7) & 0x1Fu);
            int field = (int)((instr >> 16) & 0x1Fu);   // msb (BFI/BFC) or width-1 (SBFX/UBFX)
            int rm = (int)(instr & 0xFu);               // source register
            uint o1 = (instr >> 20) & 0xFFu;
            // bits [6:4] select the instruction: 001 = BFI/BFC (Rm == 1111),
            // 101 = SBFX (o1 0x7A/0x7B) or UBFX (o1 0x7E/0x7F).
            uint sel = (instr >> 4) & 7u;      // bits [6:4]

            if (sel == 1u)
            {
                int width = field - lsb + 1;
                if (width < 1 || lsb + width > 32) { Undefined("BFI/BFC width"); return; }
                uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
                uint src = rm == 15 ? 0u : R[rm];
                R[rd] = (R[rd] & ~(mask << lsb)) | ((src & mask) << lsb);
                return;
            }

            if (sel == 5u)
            {
                bool unsigned = (o1 & 4u) != 0u;
                int w = field + 1;
                if (w <= 0 || w > 32 || lsb + w > 32) { Undefined("bitfield width"); return; }
                uint shifted = R[rm] >> lsb;
                uint value = w >= 32 ? shifted : (shifted & ((1u << w) - 1u));
                if (!unsigned && w < 32 && (value & (1u << (w - 1))) != 0) value |= ~((1u << w) - 1u);
                R[rd] = value;
                return;
            }

            Undefined("bitfield encoding 0x" + instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        private static uint RotateRight(uint v, int r)
        {
            if (r == 0) return v;
            return (v >> r) | (v << (32 - r));
        }

        public static uint Clz(uint v)
        {
            if (v == 0) return 32;
            uint n = 0;
            if ((v & 0xFFFF0000u) == 0) { n += 16; v <<= 16; }
            if ((v & 0xFF000000u) == 0) { n += 8; v <<= 8; }
            if ((v & 0xF0000000u) == 0) { n += 4; v <<= 4; }
            if ((v & 0xC0000000u) == 0) { n += 2; v <<= 2; }
            if ((v & 0x80000000u) == 0) { n += 1; }
            return n;
        }

        // ---- misc (bits [27:23] = 00010/00011/00110/00111) ----------------------

        /// <summary>
        /// True when the instruction is one of the MISC encodings that share the
        /// 00010xxx opcode space with the DSP multiplies: MRS, MSR (register),
        /// BX, BLX, BXJ and CLZ.
        /// </summary>
        private static bool IsArmMiscEncoding(uint instr)
        {
            if ((instr & 0x0FBF0FFFu) == 0x010F0000u) return true;   // MRS
            if ((instr & 0x0FB0FFF0u) == 0x0120F000u) return true;   // MSR (register)
            if ((instr & 0x0FFFFFF0u) == 0x012FFF10u) return true;   // BX
            if ((instr & 0x0FFFFFF0u) == 0x012FFF20u) return true;   // BXJ
            if ((instr & 0x0FFFFFF0u) == 0x012FFF30u) return true;   // BLX
            return false;
        }

        private void DecodeArmMisc(uint instr)
        {
            uint o1 = (instr >> 20) & 0xFFu;
            uint op2v = (instr >> 5) & 7u;
            int rd = (int)((instr >> 12) & 0xFu);
            int rn = (int)((instr >> 16) & 0xFu);
            int rs = (int)((instr >> 8) & 0xFu);
            int rm = (int)(instr & 0xFu);
            int rmField = rm;

            // BX / BLX / BXJ are 0001 0010 1111 1111 1111 00L0 Rm and must be
            // tested before MRS/MSR (they share bits [27:20]).
            if ((instr & 0x0FFFFFF0u) == 0x012FFF10u ||
                (instr & 0x0FFFFFF0u) == 0x012FFF20u ||
                (instr & 0x0FFFFFF0u) == 0x012FFF30u)
            {
                uint target = ReadReg(rmField);
                if ((instr & 0x30u) == 0x30u) R[14] = curInstrAddr + 4u;   // BLX
                BranchTo(target);
                return;
            }
            if (o1 == 0x10u || o1 == 0x14u)
            {
                // MRS: bit 22 selects SPSR
                bool spsr = (instr & (1u << 22)) != 0;
                R[rd] = spsr ? GetSpsr() : Cpsr;
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }
            if (o1 == 0x12u || o1 == 0x16u)
            {
                // MSR (register)
                MsrArm(instr, ReadReg(rmField));
                return;
            }
            if (o1 == 0x16u) { Undefined("misc 00010 110"); return; }

            // bit4 == 1 group (00010 0xx1 ... ) - BX/BLX/BXJ/CLZ
            if ((instr & 0x10u) != 0)
            {
                if ((instr & 0x0FFFFFF0u) == 0x012FFF10u ||
                    (instr & 0x0FFFFFF0u) == 0x012FFF20u)
                {
                    uint target = ReadReg(rmField);
                    if ((instr & 0x20u) != 0) R[14] = curInstrAddr + 4u;   // BLX
                    BranchTo(target);
                    return;
                }
                if ((instr & 0x0FFF0FF0u) == 0x016F0F10u)
                {
                    R[rd] = Clz(R[rmField]);
                    if (rd == 15) { BranchTo(R[15]); return; }
                    Pc = curInstrAddr + 4u; R[15] = Pc; return;
                }
                // BXJ
                if ((instr & 0x0FFFFFF0u) == 0x012FFF30u)
                {
                    BranchTo(ReadReg(rmField));
                    return;
                }
                // The H1 group (extra load/store) has (instr & 0xF10) == 0x100.
                if ((instr & 0xF10u) == 0x100u) { DecodeArmExtraLoadStore(instr); return; }
                Undefined("misc bit4 group 0x" + instr.ToString("X8", CultureInfo.InvariantCulture));
                return;
            }

        }

        private void MsrArm(uint instr, uint value)
        {
            bool spsr = (instr & (1u << 22)) != 0;
            uint fields = (instr >> 16) & 0xFu;
            uint mask = 0;
            if ((fields & 8u) != 0) mask |= 0xFF000000u;
            if ((fields & 4u) != 0) mask |= 0x00FF0000u;
            if ((fields & 2u) != 0) mask |= 0x0000FF00u;
            if ((fields & 1u) != 0) mask |= 0x000000FFu;
            if (mask == 0u) { Undefined("MSR with an empty field mask"); return; }

            if (spsr)
            {
                if (!IsPrivileged) { Undefined("MSR SPSR from User mode"); return; }
                uint old = GetSpsr();
                SetSpsr((old & ~mask) | (value & mask));
            }
            else WriteCpsrMasked(value, mask);
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        /// <summary>Write CPSR honouring the field mask and the current privilege level.</summary>
        public void WriteCpsrMasked(uint value, uint mask)
        {
            uint oldMode = Mode;
            uint userMask = IsPrivileged ? mask : (mask & 0xF0000000u);
            uint newCpsr = (Cpsr & ~userMask) | (value & userMask);
            newCpsr &= ~(1u << 24);

            uint newMode = newCpsr & 0x1Fu;
            if (!IsPrivileged) newMode = oldMode;

            if (newMode != oldMode)
            {
                BankSwitch(oldMode, newMode);
                Cpsr = (Cpsr & ~userMask) | (value & userMask);
                Cpsr = (Cpsr & ~0x1Fu) | newMode;
                Cpsr &= ~(1u << 24);
            }
            else Cpsr = newCpsr;

            Thumb = (Cpsr & 0x20u) != 0;
            dataBigEndian = (Cpsr & 0x200u) != 0;
        }

        // ---- extra load/store (halfword, signed, doubleword) ---------------------

        private void DecodeArmExtraLoadStore(uint instr)
        {
            int rd = (int)((instr >> 12) & 0xFu);
            int rn = (int)((instr >> 16) & 0xFu);
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool immediate = (instr & (1u << 22)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            bool l = (instr & (1u << 20)) != 0;
            uint op2v = (instr >> 5) & 3u;

            uint offset;
            if (immediate)
            {
                offset = (((instr >> 8) & 0xFu) << 4) | (instr & 0xFu);
            }
            else offset = R[(int)(instr & 0xFu)];
            if (!u) offset = (uint)(-(int)offset);

            uint baseAddr = ReadReg(rn);
            uint addr = p ? (baseAddr + offset) : baseAddr;
            uint writeback = baseAddr + offset;

            // bits [6:5] are (S, H): 01 = halfword, 10 = signed byte (L == 1) or
            // LDRD (L == 0), 11 = signed halfword (L == 1) or STRD (L == 0).
            if (!l && (op2v == 2u || op2v == 3u))
            {
                bool pairLoad = (op2v == 2u);
                if (pairLoad)
                {
                    ulong v = MemReadDouble(addr);
                    if (pendingFault != FaultNone) return;
                    if (rd == 15) { Undefined("LDRD with Rd == PC"); return; }
                    if (rd == 14) { R[14] = (uint)v; R[0] = (uint)(v >> 32); }
                    else { R[rd] = (uint)v; R[rd + 1] = (uint)(v >> 32); }
                }
                else
                {
                    if (rd == 15 || rd == 14) { Undefined("STRD with an invalid register pair"); return; }
                    ulong v = (ulong)R[rd] | ((ulong)R[rd + 1] << 32);
                    MemWriteDouble(addr, v);
                    if (pendingFault != FaultNone) return;
                }
                if (w && rn != 15) R[rn] = writeback;
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            bool signedLoad = (op2v == 2u) || (op2v == 3u);
            bool half = (op2v == 1u) || (op2v == 3u);

            if (l)
            {
                uint raw = half ? MemReadHalf(addr) : MemReadByte(addr);
                if (pendingFault != FaultNone) return;
                uint result;
                if (signedLoad)
                    result = half ? (uint)(int)(short)(raw & 0xFFFFu) : (uint)(int)(sbyte)(raw & 0xFFu);
                else result = raw & 0xFFFFu;
                WriteReg(rd, result);
                if (rd == 15) { BranchTo(R[15]); return; }
            }
            else
            {
                if (half) MemWriteHalf(addr, R[rd] & 0xFFFFu);
                else MemWriteByte(addr, R[rd] & 0xFFu);
                if (pendingFault != FaultNone) return;
            }

            if (w) R[rn] = writeback;
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        // ---- load/store ---------------------------------------------------------

        private void ExecuteArmLoadStore(uint instr, bool immediate)
        {
            int rd = (int)((instr >> 12) & 0xFu);
            int rn = (int)((instr >> 16) & 0xFu);
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool b = (instr & (1u << 22)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            bool l = (instr & (1u << 20)) != 0;

            uint offset;
            if (immediate) offset = instr & 0xFFFu;
            else
            {
                uint rmVal = R[(int)(instr & 0xFu)];
                int type = (int)((instr >> 5) & 3u);
                int amount = (int)((instr >> 7) & 0x1Fu);
                offset = ShiftImm(rmVal, type, amount, false);
            }
            if (!u) offset = (uint)(-(int)offset);

            uint baseAddr = ReadReg(rn);
            uint addr = p ? (baseAddr + offset) : baseAddr;
            uint writeback = baseAddr + offset;

            if (l)
            {
                if (b)
                {
                    uint v = MemReadByte(addr);
                    if (pendingFault != FaultNone) return;
                    WriteReg(rd, v & 0xFFu);
                }
                else
                {
                    bool unaligned = (addr & 3u) != 0;
                    if (unaligned && MmuEnabled && Mmu.StrictAlignment)
                    {
                        MmResult mm = new MmResult();
                        mm.Fault = MmFaultKind.Alignment;
                        mm.FsrFull = 1u;
                        mm.FsrStatus = 1u;
                        Mmu.ReportDataAbort(ref mm, addr, false);
                        pendingMmFault = mm;
                        pendingFault = FaultData;
                        return;
                    }
                    uint v = MemReadWord(addr & ~3u, false);
                    if (pendingFault != FaultNone) return;
                    if (unaligned)
                    {
                        int rot = (int)(addr & 3u) * 8;
                        v = (v >> rot) | (v << (32 - rot));
                    }
                    WriteReg(rd, v);
                }
                if (rd == 15) { BranchTo(R[15]); return; }
            }
            else
            {
                if (b) MemWriteByte(addr, R[rd] & 0xFFu);
                else MemWriteWord(addr & ~3u, R[rd]);
                if (pendingFault != FaultNone) return;
            }

            if (w) R[rn] = writeback;
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        private void ExecuteArmLoadStoreMultiple(uint instr)
        {
            int rn = (int)((instr >> 16) & 0xFu);
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool s = (instr & (1u << 22)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            bool l = (instr & (1u << 20)) != 0;
            uint list = instr & 0xFFFFu;

            if (list == 0) { Pc = curInstrAddr + 4u; R[15] = Pc; return; }

            uint baseAddr = ReadReg(rn);
            int count = PopCount(list);
            uint start = u ? baseAddr : baseAddr - (uint)(count * 4);
            if (p == l) start += 4u;
            uint newBase = u ? baseAddr + (uint)(count * 4) : baseAddr - (uint)(count * 4);

            bool userBank = s && !(l && (list & 0x8000u) != 0);
            uint savedSp = 0, savedLr = 0;
            if (userBank)
            {
                savedSp = R[13]; savedLr = R[14];
                R[13] = bankSp[0x10];
                R[14] = bankLr[0x10];
            }

            uint addr = start;
            if (l)
            {
                for (int k = 0; k < 16; k++)
                {
                    if ((list & (1u << k)) == 0) continue;
                    uint v = MemReadWord(addr, false);
                    if (pendingFault != FaultNone) break;
                    if (k == 15) R[15] = v; else R[k] = v;
                    addr += 4u;
                }
            }
            else
            {
                uint pcValue = ReadPc();
                for (int k = 0; k < 16; k++)
                {
                    if ((list & (1u << k)) == 0) continue;
                    MemWriteWord(addr, k == 15 ? pcValue : R[k]);
                    if (pendingFault != FaultNone) break;
                    addr += 4u;
                }
            }

            if (userBank)
            {
                if (l)
                {
                    bankSp[0x10] = R[13];
                    bankLr[0x10] = R[14];
                }
                R[13] = savedSp;
                R[14] = savedLr;
            }

            if (pendingFault != FaultNone) return;

            if (l && (list & 0x8000u) != 0)
            {
                uint target = R[15];
                if (s) { ExceptionReturn(target); return; }
                BranchTo(target);
                return;
            }

            if (w && rn != 15) R[rn] = newBase;
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        // ---- branches -----------------------------------------------------------

        private void ExecuteArmBranch(uint instr)
        {
            bool link = (instr & (1u << 24)) != 0;
            int imm = (int)(instr & 0xFFFFFFu);
            if ((imm & 0x800000) != 0) imm |= unchecked((int)0xFF000000);
            uint target = curInstrAddr + 8u + (uint)(imm << 2);
            if (link) R[14] = curInstrAddr + 4u;
            Pc = target;
            R[15] = Pc;
        }

        // ---- unconditional space ------------------------------------------------

        private void ExecuteArmUnconditional(uint instr)
        {
            uint blk = (instr >> 25) & 7u;

            // 1111 101x: BLX immediate (and the undefined H == 10 form)
            if (blk == 5u)
            {
                if ((instr & 0x10u) == 0u)
                {
                    int imm = (int)(instr & 0xFFFFFFu);
                    if ((imm & 0x800000) != 0) imm |= unchecked((int)0xFF000000);
                    uint target = curInstrAddr + 8u + (uint)(imm << 2);
                    R[14] = curInstrAddr + 4u;
                    Thumb = true;
                    Pc = target & ~3u;
                    R[15] = Pc;
                    return;
                }
                Undefined("BLX immediate with H == 10");
                return;
            }

            // 1111 110x / 1111 111x: coprocessor and VFP
            if (blk == 6u || blk == 7u)
            {
                if ((instr & 0x10u) == 0u)
                {
                    if (blk == 6u) { ExecuteArmCoprocessor(instr); return; }
                    DecodeArmMiscUncond(instr);
                    return;
                }
                if (blk == 6u)
                {
                    // MCRR/MRRC and LDC/STC space.
                    ExecuteArmCoprocessor(instr);
                    return;
                }
                // 1111 1111 xxxx .... : VFP/NEON/coprocessor
                ExecuteArmVfp(instr);
                return;
            }

            // 1111 010x / 1111 011x: memory hints, CPS, SETEND, RFE/SRS
            if (blk == 2u || blk == 3u)
            {
                if ((instr & 0x0FE00000u) == 0x0F800000u && (instr & 0x10u) == 0u)
                {
                    DecodeArmCpsSetend(instr);
                    return;
                }
                if ((instr & 0x0F000000u) == 0x09000000u)
                {
                    DecodeArmRfeSrs(instr);
                    return;
                }
                // PLD/PLDW/PLI preload hints
                if ((instr & 0x0D700000u) == 0x05500000u || (instr & 0x0D200000u) == 0x05000000u)
                {
                    Pc = curInstrAddr + 4u; R[15] = Pc; return;
                }
                Undefined("unconditional 0x" + instr.ToString("X8", CultureInfo.InvariantCulture));
                return;
            }

            // 1111 000x / 1111 001x: memory hints (PLD/PLI), CPS, SETEND, SRS/RFE
            if (blk == 0u || blk == 1u)
            {
                if ((instr & 0x0D700000u) == 0x05500000u || (instr & 0x0D200000u) == 0x05000000u)
                {
                    Pc = curInstrAddr + 4u; R[15] = Pc; return;
                }
                if ((instr & 0x0F000000u) == 0x09000000u)
                {
                    DecodeArmRfeSrs(instr);
                    return;
                }
                if ((instr & 0x0FE00000u) == 0x00000000u && (instr & 0x10u) == 0u)
                {
                    DecodeArmCpsSetend(instr);
                    return;
                }
                Undefined("unconditional 0x" + instr.ToString("X8", CultureInfo.InvariantCulture));
                return;
            }

            // 1111 100x: RFE/SRS
            if (blk == 4u)
            {
                DecodeArmRfeSrs(instr);
                return;
            }

            Undefined("unconditional 0x" + instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        private void DecodeArmMiscUncond(uint instr)
        {
            // CLREX (1111 0101 0111 1111 1111 0000 0001 1111)
            if ((instr & 0xFFFFFFF0u) == 0xF57FF010u)
            {
                exclusiveValid = false;
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }
            // DSB/DMB/ISB (1111 0101 0111 1111 1111 0000 0xxx xxxx)
            if ((instr & 0xFFFFFF00u) == 0xF57FF000u)
            {
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            // BX / BLX / BXJ
            if ((instr & 0x0FFFFFF0u) == 0x012FFF10u)
            {
                BranchTo(ReadReg((int)(instr & 0xFu)));
                return;
            }
            if ((instr & 0x0FFFFFF0u) == 0x012FFF20u)
            {
                R[14] = curInstrAddr + 4u;
                BranchTo(ReadReg((int)(instr & 0xFu)));
                return;
            }
            if ((instr & 0x0FFFFFF0u) == 0x012FFF30u)
            {
                BranchTo(ReadReg((int)(instr & 0xFu)));
                return;
            }

            // HVC / SMC
            if ((instr & 0x0FF000F0u) == 0x01400070u)
            {
                TakeException(VecHyp, 0x1A, curInstrAddr + 4u);
                return;
            }
            if ((instr & 0x0FF000F0u) == 0x01600070u)
            {
                Note("SMC (secure monitor call) ignored");
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            // MSR immediate with cond == 1111 (1111 0011 0x0x ...) is not valid;
            // 1111 0xx1 xxxx xxxx xxxx xxxx xxxx xxxx is the "misc" space.
            Undefined("unconditional misc 0x" + instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        private void DecodeArmRfeSrs(uint instr)
        {
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            bool rfe = (instr & (1u << 20)) != 0;
            int rn = (int)((instr >> 16) & 0xFu);

            if (rfe)
            {
                // RFE: load PC and CPSR from the exception return state.
                uint baseAddr = ReadReg(rn);
                uint addr;
                if (u) addr = p ? baseAddr + 4u : baseAddr;
                else addr = p ? baseAddr - 4u : baseAddr - 8u;
                uint newCpsr = MemReadWord(addr, false);
                if (pendingFault != FaultNone) return;
                uint newPc = MemReadWord(addr + 4u, false);
                if (pendingFault != FaultNone) return;
                if (w && rn != 15) R[rn] = u ? baseAddr + 8u : baseAddr - 8u;
                ExceptionReturnWithCpsr(newPc & ~3u, newCpsr);
                return;
            }

            // SRS: store the return state using the mode in bits [4:0].
            if (!IsPrivileged) { Undefined("SRS from User mode"); return; }
            uint mode = instr & 0x1Fu;
            uint savedCpsr, savedLr;
            if (mode == 0x10 || mode == 0x1F)
            {
                savedCpsr = Cpsr;
                savedLr = R[14];
            }
            else
            {
                savedCpsr = bankSpsr[(int)mode];
                savedLr = bankLr[(int)mode];
            }
            uint baseV = ReadReg(rn);
            uint a2;
            if (u) a2 = p ? baseV + 4u : baseV;
            else a2 = p ? baseV - 4u : baseV - 8u;
            MemWriteWord(a2, savedCpsr);
            if (pendingFault != FaultNone) return;
            MemWriteWord(a2 + 4u, savedLr);
            if (pendingFault != FaultNone) return;
            if (w && rn != 15) R[rn] = u ? baseV + 8u : baseV - 8u;
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        private void DecodeArmCpsSetend(uint instr)
        {
            // SETEND: 1111 0001 0000 0001 0000 0000 0 E 0 0
            if ((instr & 0xFFFFFDFFu) == 0xF1010000u)
            {
                dataBigEndian = (instr & (1u << 9)) != 0;
                if (dataBigEndian) Cpsr |= 0x200u; else Cpsr &= ~0x200u;
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            // CPS/CPSIE/CPSID: 1111 0001 0000 imod(2) M(1) 0 AIF(3) mode(5)
            if ((instr & 0xFFF10020u) == 0xF1000000u || (instr & 0xFFF10020u) == 0xF1000020u)
            {
                uint imod = (instr >> 18) & 3u;
                uint m = (instr >> 17) & 1u;
                uint aif = (instr >> 6) & 7u;
                uint mode = instr & 0x1Fu;
                if (!IsPrivileged) { Undefined("CPS from User mode"); return; }

                if (m != 0 && mode != 0)
                {
                    BankSwitch(Mode, mode);
                    Cpsr = (Cpsr & ~0x1Fu) | (mode & 0x1Fu);
                    Thumb = (Cpsr & 0x20u) != 0;
                }
                if (imod == 2u)
                {
                    if ((aif & 4u) != 0) Cpsr &= ~0x100u;
                    if ((aif & 2u) != 0) Cpsr &= ~0x80u;
                    if ((aif & 1u) != 0) Cpsr &= ~0x40u;
                }
                else if (imod == 3u)
                {
                    if ((aif & 4u) != 0) Cpsr |= 0x100u;
                    if ((aif & 2u) != 0) Cpsr |= 0x80u;
                    if ((aif & 1u) != 0) Cpsr |= 0x40u;
                }
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            // Hint instructions: 1111 0011 0010 0000 1111 0000 0000 xxxx
            if ((instr & 0xFFFFFFF0u) == 0xE320F000u)
            {
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            Undefined("CPS/SETEND/hint 0x" + instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        // ---- coprocessor (A32) --------------------------------------------------

        private void ExecuteArmCoprocessor(uint instr)
        {
            uint cpnum = (instr >> 8) & 0xFu;
            uint opc1v = (instr >> 21) & 7u;
            uint crn = (instr >> 16) & 0xFu;
            uint crm = instr & 0xFu;
            uint opc2v = (instr >> 5) & 7u;
            int rd = (int)((instr >> 12) & 0xFu);
            uint blk = (instr >> 25) & 7u;

            if (blk == 6u)
            {
                // bits [24:21] == 0100 selects MCRR/MRRC; every other
                // combination is LDC/STC (or a VFP load/store for coprocessors
                // 10/11).
                if ((instr & 0x01E00000u) == 0x00400000u)
                {
                    uint rt2 = (instr >> 16) & 0xFu;
                    bool load = (instr & (1u << 20)) != 0;
                    uint rt = (uint)rd;
                    uint mcrrOpc1 = (instr >> 4) & 0xFu;
                    if (cpnum == 15) { Undefined("MCRR/MRRC p15"); return; }
                    if (CoprocessorHook != null && CoprocessorHook(this, true, load, cpnum, mcrrOpc1, crn, crm, opc2v, ref rt, ref rt2))
                    {
                        if (load) { R[rd] = rt; R[(int)rt2] = rt2; }
                        Pc = curInstrAddr + 4u; R[15] = Pc; return;
                    }
                    Note("MCRR/MRRC p" + Num(cpnum) + " ignored");
                    Pc = curInstrAddr + 4u; R[15] = Pc; return;
                }
                if (cpnum == 10u || cpnum == 11u)
                {
                    bool single = cpnum == 10u;
                    // VLDR/VSTR are P == 1, W == 0; anything else in this space
                    // is VLDM/VSTM (addressing mode from P/U/W).
                    if ((instr & (1u << 24)) != 0u && (instr & (1u << 21)) == 0u)
                        ExecuteVfpLoadStore(instr, cpnum, single);
                    else
                        ExecuteVfpLoadStoreMultiple(instr, single);
                    return;
                }
                Note("LDC/STC p" + Num(cpnum) + " ignored");
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            // 0xEE: bit 4 == 1 is MCR/MRC (CP15 included), bit 4 == 0 is CDP;
            // coprocessors 10/11 carry the VFP data-processing space in both.
            if ((instr & 0x10u) != 0u)
            {
                if (cpnum == 15u)
                {
                    bool load = (instr & (1u << 20)) != 0;
                    if (load)
                    {
                        uint v = Cp15Read(opc1v, crn, crm, opc2v, rd);
                        if (rd != 15) R[rd] = v;
                    }
                    else Cp15Write(opc1v, crn, crm, opc2v, rd == 15 ? 0u : R[rd]);
                    Pc = curInstrAddr + 4u; R[15] = Pc; return;
                }
                if (cpnum == 10u || cpnum == 11u)
                {
                    ExecuteArmVfp(instr);
                    return;
                }
                bool l = (instr & (1u << 20)) != 0;
                if (CoprocessorHook != null)
                {
                    uint rt = rd == 15 ? 0u : R[rd];
                    uint rt2 = 0;
                    if (CoprocessorHook(this, false, l, cpnum, opc1v, crn, crm, opc2v, ref rt, ref rt2))
                    {
                        if (l && rd != 15) R[rd] = rt;
                        Pc = curInstrAddr + 4u; R[15] = Pc; return;
                    }
                }
                Note("MCR/MRC p" + Num(cpnum) + " ignored");
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            if (cpnum == 10u || cpnum == 11u) { ExecuteArmVfp(instr); return; }
            Note("CDP p" + Num(cpnum) + " ignored");
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        private static string Num(uint v)
        {
            return v.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>CP15 MRC: read a system control register.</summary>
        public uint Cp15Read(uint opc1v, uint crn, uint crm, uint opc2v, int rt)
        {
            if (opc1v == 4u)
            {
                // Cache maintenance / configuration reads.
                if (crn == 0u) return 0u;
                return 0u;
            }
            switch (crn)
            {
                case 0u:
                    // The identification registers are selected by CRm and Op2
                    // (MIDR is c0,c0,0; ID_PFR0 is c0,c1,0; ID_ISAR0 is c0,c2,0).
                    switch (crm)
                    {
                        case 0u:
                            switch (opc2v)
                            {
                                case 0u: return 0x410FC090u;   // MIDR: Cortex-A9 r0p0
                                case 1u: return 0x0C000000u;   // CTR
                                case 2u: return 0x80000000u;   // TCMTR
                                case 3u: return 0x00000000u;   // TLBTR
                                default: return 0u;
                            }
                        case 1u:
                            switch (opc2v)
                            {
                                case 0u: return 0x000F0000u;   // ID_PFR0
                                case 1u: return 0x00000001u;   // ID_PFR1 (TrustZone)
                                case 2u: return 0x00000000u;   // ID_DFR0
                                case 3u: return 0x00000000u;   // ID_AFR0
                                case 4u: return 0x00100010u;   // ID_MMFR0
                                case 5u: return 0x00000000u;   // ID_MMFR1
                                case 6u: return 0x00000000u;   // ID_MMFR2
                                case 7u: return 0x00000000u;   // ID_MMFR3
                                default: return 0u;
                            }
                        case 2u:
                            switch (opc2v)
                            {
                                case 0u: return 0x01101110u;   // ID_ISAR0
                                case 1u: return 0x02111000u;   // ID_ISAR1
                                case 2u: return 0x32112110u;   // ID_ISAR2
                                case 3u: return 0x01101011u;   // ID_ISAR3
                                case 4u: return 0x21111010u;   // ID_ISAR4
                                case 5u: return 0x00000000u;   // ID_ISAR5
                                default: return 0u;
                            }
                        default: return 0u;
                    }
                case 1u:
                    // Control registers: c1,c0,0 = SCTLR, c1,c0,2 = CPACR.
                    if (crm == 0u && opc2v == 0u) return Mmu.Sctlr;
                    return 0u;
                case 5u:
                    if (crm == 0u && opc2v == 0u) return Mmu.Dfsr;
                    if (crm == 0u && opc2v == 1u) return Mmu.Dfar;
                    if (crm == 1u && opc2v == 0u) return Mmu.Ifsr;
                    if (crm == 1u && opc2v == 1u) return Mmu.Ifar;
                    if (crm == 1u && opc2v == 2u) return Mmu.Adfsr;
                    if (crm == 1u && opc2v == 3u) return Mmu.Aifsr;
                    return 0u;
                case 6u:
                    if (crm == 0u && opc2v == 0u) return Mmu.Dfar;
                    if (crm == 0u && opc2v == 2u) return Mmu.Ifar;
                    return 0u;
                case 13u:
                    if (crm == 0u && opc2v == 1u) return Mmu.ContextIdr;
                    return 0u;
                default:
                    return 0u;
            }
        }

        /// <summary>CP15 MCR: write a system control register.</summary>
        public void Cp15Write(uint opc1v, uint crn, uint crm, uint opc2v, uint value)
        {
            switch (crn)
            {
                case 1u:
                    if (crm == 0u && opc2v == 0u)
                    {
                        Mmu.Sctlr = value;
                        MmuEnabled = (value & 1u) != 0;
                        Note("SCTLR = 0x" + value.ToString("X8", CultureInfo.InvariantCulture) + " (MMU " + (MmuEnabled ? "on" : "off") + ")");
                    }
                    break;
                case 2u:
                    if (crm == 0u && opc2v == 0u) Mmu.Ttbr0 = value & 0xFFFFC000u;
                    else if (crm == 0u && opc2v == 1u) Mmu.Ttbr1 = value;
                    else if (crm == 0u && opc2v == 2u) Mmu.Ttbcr = value & 7u;
                    break;
                case 3u:
                    Mmu.Dacr = value;
                    break;
                case 5u:
                    if (crm == 0u && opc2v == 0u) Mmu.Dfsr = value & 0xFFFu;
                    else if (crm == 0u && opc2v == 1u) Mmu.Dfar = value;
                    else if (crm == 1u && opc2v == 0u) Mmu.Ifsr = value & 0xFFFu;
                    else if (crm == 1u && opc2v == 1u) Mmu.Ifar = value;
                    break;
                case 6u:
                    if (crm == 0u && opc2v == 0u) Mmu.Dfar = value;
                    else if (crm == 0u && opc2v == 2u) Mmu.Ifar = value;
                    break;
                case 7u:
                    Cp15CacheOp(opc1v, crm, opc2v, value);
                    break;
                case 10u:
                    if (crm == 2u && opc2v == 0u) Mmu.Prrr = value;
                    else if (crm == 2u && opc2v == 1u) Mmu.Nmrr = value;
                    break;
                case 12u:
                    Mmu.Vbar = value;
                    break;
                case 13u:
                    if (crm == 0u && opc2v == 1u) Mmu.ContextIdr = value;
                    break;
                default:
                    break;
            }
        }

        private void Cp15CacheOp(uint opc1v, uint crm, uint opc2v, uint value)
        {
            string name = null;
            if (opc1v == 0u && crm == 5u && opc2v == 0u) name = "ICIALLU";
            else if (opc1v == 0u && crm == 5u && opc2v == 1u) name = "ICIMVAU";
            else if (opc1v == 0u && crm == 5u && opc2v == 4u) name = "BPIALL";
            else if (opc1v == 0u && crm == 6u && opc2v == 1u) name = "BPIALLIS";
            else if (opc1v == 0u && crm == 6u && opc2v == 2u) name = "BPMVA";
            else if (opc1v == 0u && crm == 7u && opc2v == 4u) name = "BPIMVA";
            else if (opc1v == 0u && crm == 8u && opc2v == 0u) name = "TLBIALL";
            else if (opc1v == 0u && crm == 8u && opc2v == 1u) name = "TLBIMVA";
            else if (opc1v == 0u && crm == 8u && opc2v == 2u) name = "TLBIASID";
            else if (opc1v == 0u && crm == 8u && opc2v == 3u) name = "TLBIMVAA";
            else if (opc1v == 0u && crm == 8u && opc2v == 7u) name = "TLBIALLIS";
            else if (opc1v == 0u && crm == 10u && opc2v == 1u) name = "DCCMVAC";
            else if (opc1v == 0u && crm == 10u && opc2v == 4u) name = "DCCMVAU";
            else if (opc1v == 0u && crm == 10u && opc2v == 5u) name = "DCCSW";
            else if (opc1v == 0u && crm == 10u && opc2v == 7u) name = "DCCIMVAC";
            else if (opc1v == 0u && crm == 11u && opc2v == 0u) name = "DCCMVAU";
            else if (opc1v == 0u && crm == 14u && opc2v == 1u) name = "DCCIMVAC";
            else if (opc1v == 0u && crm == 14u && opc2v == 2u) name = "DCCISW";
            else if (opc1v == 0u && crm == 15u && opc2v == 1u) name = "DCCSW";
            else if (opc1v == 0u && crm == 15u && opc2v == 2u) name = "DCCISW";
            else if (opc1v == 4u && crm == 8u && opc2v == 0u) name = "ICIMVAU";
            else if (opc1v == 4u && crm == 10u && opc2v == 4u) name = "DCCMVAU";
            else if (opc1v == 6u && crm == 1u) name = "DCIMVAC";
            else if (opc1v == 6u && crm == 2u) name = "DCISW";

            if (name != null)
                Note("TLB/cache maintenance " + name + "(0x" + value.ToString("X8", CultureInfo.InvariantCulture) + ")");
            else
                Note("CP15 c7 op1=" + Num(opc1v) + " crm=" + Num(crm) + " opc2=" + Num(opc2v) + " ignored");
        }

        // ---------------------------------------------------------------- Thumb-16
        //
        // ARM ARM A5.2.  top = hw[15:11].
        //   0..2   shift by immediate (LSL / LSR / ASR)
        //   3      add/subtract (register or 3-bit immediate)
        //   4..7   move/compare/add/subtract immediate
        //   8      ALU register (bit 10 == 0) or special data / BX / BLX (bit 10 == 1)
        //   9      LDR (literal)
        //   10,11  load/store (register offset)
        //   12..15 load/store (immediate)
        //   16,17  load/store halfword (immediate)
        //   18,19  SP-relative load/store
        //   20,21  ADR / ADD SP
        //   22,23  miscellaneous (ADD/SUB SP, CBZ, extend, CPS, REV, PUSH/POP,
        //          BKPT, IT/hints)
        //   24,25  STM / LDM
        //   26,27  conditional branch / SVC / UDF
        //   28     unconditional branch
        //   29..31 32-bit instruction prefixes (handled by Step())

        private void ExecuteThumb16()
        {
            uint i = curInstr;
            uint top = (i >> 11) & 0x1Fu;
            switch (top)
            {
                case 0u: case 1u: case 2u: ThumbShiftImm(i); return;
                case 3u: ThumbAddSub(i); return;
                case 4u: case 5u: case 6u: case 7u: ThumbMovCmpImm(i); return;
                case 8u:
                    if ((i & 0x400u) != 0u) ThumbSpecialData(i);
                    else ThumbAluOps(i);
                    return;
                case 9u: ThumbLiteralLoad(i); return;
                case 10u: case 11u: ThumbLoadStoreReg(i); return;
                case 12u: case 13u: case 14u: case 15u: ThumbLoadStoreImm(i); return;
                case 16u: case 17u: ThumbLoadStoreHalfImm(i); return;
                case 18u: case 19u: ThumbSpRelative(i); return;
                case 20u: case 21u: ThumbAdr(i); return;
                case 22u: case 23u: ThumbMisc16(i); return;
                case 24u: case 25u: ThumbLdmStm(i); return;
                case 26u: case 27u: ThumbCondBranch(i); return;
                case 28u: ThumbUncondBranch(i); return;
                default: Undefined("Thumb-16 0x" + i.ToString("X4", CultureInfo.InvariantCulture)); return;
            }
        }

        private void ThumbShiftImm(uint i)
        {
            uint op = (i >> 11) & 3u;
            uint imm5 = (i >> 6) & 0x1Fu;
            int rm = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            if (op == 3u) { Undefined("Thumb shift op 11"); return; }

            uint src = R[rm];
            uint v;
            bool carry;
            if (op == 0u && imm5 == 0u) { v = src; carry = FlagC; }
            else
            {
                int amount = (op == 0u) ? (int)imm5 : (imm5 == 0u ? 32 : (int)imm5);
                carry = ((src >> (amount - 1)) & 1u) != 0u;
                v = ShiftImm(src, (int)op, amount, false);
            }
            R[rd] = v;
            SetNZ(v);
            if (carry) Cpsr |= 0x20000000u; else Cpsr &= ~0x20000000u;
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbAddSub(uint i)
        {
            bool immediate = (i & 0x400u) != 0u;
            bool sub = (i & 0x200u) != 0u;
            int rn = (int)((i >> 6) & 7u);       // Rn or imm3
            int rm = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            uint op2v = immediate ? (uint)rn : R[rm];
            bool carry, overflow;
            uint result = sub ? SubWithCarry(R[rm], op2v, true, out carry, out overflow)
                              : AddWithCarry(R[rm], op2v, false, out carry, out overflow);
            R[rd] = result;
            SetNZCV(result, carry, overflow);
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbMovCmpImm(uint i)
        {
            uint op = (i >> 11) & 3u;
            int rd = (int)((i >> 8) & 7u);
            uint imm8 = i & 0xFFu;
            bool carry, overflow;
            uint r;
            switch (op)
            {
                case 0u: R[rd] = imm8; SetNZ(imm8); break;
                case 1u: r = SubWithCarry(imm8, R[rd], true, out carry, out overflow); SetNZCV(r, carry, overflow); break;
                case 2u: r = AddWithCarry(R[rd], imm8, false, out carry, out overflow); R[rd] = r; SetNZCV(r, carry, overflow); break;
                default: r = SubWithCarry(R[rd], imm8, true, out carry, out overflow); R[rd] = r; SetNZCV(r, carry, overflow); break;
            }
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbAluOps(uint i)
        {
            uint op = (i >> 6) & 0xFu;
            int rm = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            uint a = R[rd], b = R[rm];
            bool carry = FlagC, overflow = FlagV;
            uint result;
            switch (op)
            {
                case 0x0u: result = a & b; R[rd] = result; SetNZ(result); break;
                case 0x1u: result = a ^ b; R[rd] = result; SetNZ(result); break;
                case 0x2u: result = ShiftReg(a, 0, b & 0xFFu); R[rd] = result; SetNZ(result); break;
                case 0x3u: result = ShiftReg(a, 1, b & 0xFFu); R[rd] = result; SetNZ(result); break;
                case 0x4u: result = ShiftReg(a, 2, b & 0xFFu); R[rd] = result; SetNZ(result); break;
                case 0x5u: result = AddWithCarry(a, b, carry, out carry, out overflow); R[rd] = result; SetNZCV(result, carry, overflow); break;
                case 0x6u: result = SubWithCarry(a, b, carry, out carry, out overflow); R[rd] = result; SetNZCV(result, carry, overflow); break;
                case 0x7u: result = ShiftReg(a, 3, b & 0xFFu); R[rd] = result; SetNZ(result); break;
                case 0x8u: result = a & b; SetNZ(result); break;
                case 0x9u: result = SubWithCarry(0u, b, true, out carry, out overflow); R[rd] = result; SetNZCV(result, carry, overflow); break;
                case 0xAu: result = SubWithCarry(a, b, true, out carry, out overflow); SetNZCV(result, carry, overflow); break;
                case 0xBu: result = AddWithCarry(a, b, false, out carry, out overflow); SetNZCV(result, carry, overflow); break;
                case 0xCu: result = a | b; R[rd] = result; SetNZ(result); break;
                case 0xDu: result = a * b; R[rd] = result; SetNZ(result); break;
                case 0xEu: result = a & ~b; R[rd] = result; SetNZ(result); break;
                default: result = ~b; R[rd] = result; SetNZ(result); break;
            }
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbSpecialData(uint i)
        {
            uint op = (i >> 8) & 3u;
            int rm = (int)((i >> 3) & 0xFu);
            int rd = (int)((i & 7u) | ((i >> 4) & 8u));
            uint a = ReadReg(rd);
            uint b = ReadReg(rm);
            bool carry, overflow;

            switch (op)
            {
                case 0u:
                    {
                        uint r = AddWithCarry(a, b, false, out carry, out overflow);
                        if (rd == 15) { Pc = r & ~1u; R[15] = Pc; break; }
                        R[rd] = r;
                        break;
                    }
                case 1u:
                    {
                        uint r = SubWithCarry(a, b, true, out carry, out overflow);
                        SetNZCV(r, carry, overflow);
                        break;
                    }
                case 2u:
                    if (rd == 15) BranchTo(b);
                    else R[rd] = b;
                    break;
                default:
                    if ((i & 0x0080u) != 0u) R[14] = (curInstrAddr + 2u) | 1u;
                    BranchTo(ReadReg(rm));
                    return;
            }
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbLiteralLoad(uint i)
        {
            int rd = (int)((i >> 8) & 7u);
            uint imm8 = (uint)(i & 0xFFu) * 4u;
            uint addr = ((curInstrAddr + 4u) & ~3u) + imm8;
            uint v = MemReadWord(addr, false);
            if (pendingFault != FaultNone) return;
            R[rd] = v;
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbLoadStoreReg(uint i)
        {
            uint op = (i >> 9) & 7u;
            int rm = (int)((i >> 6) & 7u);
            int rn = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            uint addr = R[rn] + R[rm];
            switch (op)
            {
                case 0u: MemWriteWord(addr & ~3u, R[rd]); break;
                case 1u: MemWriteHalf(addr & ~1u, R[rd] & 0xFFFFu); break;
                case 2u: MemWriteByte(addr, R[rd] & 0xFFu); break;
                case 3u: { uint v = MemReadByte(addr); if (pendingFault == FaultNone) R[rd] = (uint)(int)(sbyte)(v & 0xFFu); break; }
                case 4u: { uint v = MemReadWord(addr & ~3u, false); if (pendingFault == FaultNone) R[rd] = v; break; }
                case 5u: { uint v = MemReadHalf(addr & ~1u); if (pendingFault == FaultNone) R[rd] = v & 0xFFFFu; break; }
                case 6u: { uint v = MemReadByte(addr); if (pendingFault == FaultNone) R[rd] = v & 0xFFu; break; }
                default: { uint v = MemReadHalf(addr & ~1u); if (pendingFault == FaultNone) R[rd] = (uint)(int)(short)(v & 0xFFFFu); break; }
            }
            if (pendingFault != FaultNone) return;
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbLoadStoreImm(uint i)
        {
            bool byteOp = (i & 0x1000u) != 0u;
            bool load = (i & 0x0800u) != 0u;
            uint imm5 = (i >> 6) & 0x1Fu;
            int rn = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            uint offset = byteOp ? imm5 : imm5 * 4u;
            uint addr = R[rn] + offset;
            if (load)
            {
                uint v = byteOp ? MemReadByte(addr) : MemReadWord(addr & ~3u, false);
                if (pendingFault != FaultNone) return;
                R[rd] = byteOp ? (v & 0xFFu) : v;
            }
            else
            {
                if (byteOp) MemWriteByte(addr, R[rd] & 0xFFu);
                else MemWriteWord(addr & ~3u, R[rd]);
                if (pendingFault != FaultNone) return;
            }
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbLoadStoreHalfImm(uint i)
        {
            bool load = (i & 0x0800u) != 0u;
            uint imm5 = (i >> 6) & 0x1Fu;
            int rn = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            uint addr = R[rn] + imm5 * 2u;
            if (load) { uint v = MemReadHalf(addr & ~1u); if (pendingFault == FaultNone) R[rd] = v & 0xFFFFu; }
            else MemWriteHalf(addr & ~1u, R[rd] & 0xFFFFu);
            if (pendingFault != FaultNone) return;
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbSpRelative(uint i)
        {
            bool load = (i & 0x0800u) != 0u;
            int rd = (int)((i >> 8) & 7u);
            uint imm8 = (uint)(i & 0xFFu) * 4u;
            uint addr = R[13] + imm8;
            if (load) { uint v = MemReadWord(addr & ~3u, false); if (pendingFault == FaultNone) R[rd] = v; }
            else MemWriteWord(addr & ~3u, R[rd]);
            if (pendingFault != FaultNone) return;
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbAdr(uint i)
        {
            bool sp = (i & 0x0800u) != 0u;
            int rd = (int)((i >> 8) & 7u);
            uint imm8 = (uint)(i & 0xFFu) * 4u;
            R[rd] = (sp ? R[13] : ((curInstrAddr + 4u) & ~3u)) + imm8;
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbLdmStm(uint i)
        {
            bool load = (i & 0x0800u) != 0u;
            int rn = (int)((i >> 8) & 7u);
            uint list = i & 0xFFu;
            if (list == 0u) { Pc = curInstrAddr + 2u; R[15] = Pc; return; }
            int count = PopCount(list);
            uint baseAddr = R[rn];
            uint addr = baseAddr;
            bool wb = true;
            if (load)
            {
                for (int r = 0; r < 8; r++)
                {
                    if ((list & (1u << r)) == 0) continue;
                    uint v = MemReadWord(addr, false);
                    if (pendingFault != FaultNone) return;
                    R[r] = v;
                    addr += 4u;
                }
                if ((list & (1u << rn)) != 0) wb = false;
            }
            else
            {
                for (int r = 0; r < 8; r++)
                {
                    if ((list & (1u << r)) == 0) continue;
                    MemWriteWord(addr, R[r]);
                    if (pendingFault != FaultNone) return;
                    addr += 4u;
                }
                if ((list & (1u << rn)) != 0) wb = false;
            }
            if (wb) R[rn] = baseAddr + (uint)(count * 4);
            Pc = curInstrAddr + 2u; R[15] = Pc;
        }

        private void ThumbCondBranch(uint i)
        {
            uint cond = (i >> 8) & 0xFu;
            if (cond == 0xFu) { TakeException(VecSvc, 0x13, curInstrAddr + 2u); return; }
            if (cond == 0xEu) { Undefined("Thumb UDF"); return; }
            int imm = (int)(i & 0xFFu);
            if ((imm & 0x80) != 0) imm |= unchecked((int)0xFFFFFF00);
            if (ConditionPassed(cond)) { Pc = curInstrAddr + 4u + (uint)(imm << 1); R[15] = Pc; }
            else { Pc = curInstrAddr + 2u; R[15] = Pc; }
        }

        private void ThumbUncondBranch(uint i)
        {
            int imm = (int)(i & 0x7FFu);
            if ((imm & 0x400) != 0) imm |= unchecked((int)0xFFFFF800);
            Pc = curInstrAddr + 4u + (uint)(imm << 1);
            R[15] = Pc;
        }

        private void ThumbMisc16(uint i)
        {
            uint sub = (i >> 8) & 0xFu;

            if (sub == 0u)
            {
                bool s2 = (i & 0x80u) != 0u;
                uint imm = (uint)(i & 0x7Fu) * 4u;
                R[13] = s2 ? R[13] - imm : R[13] + imm;
                Pc = curInstrAddr + 2u; R[15] = Pc; return;
            }
            if ((i & 0xF500u) == 0xB100u)
            {
                // CBZ / CBNZ
                bool nz = (i & 0x800u) != 0u;
                uint high = (i >> 9) & 1u;
                uint imm5 = (i >> 3) & 0x1Fu;
                uint offset = ((high << 5) | imm5) << 1;
                bool zero = R[(int)(i & 7u)] == 0u;
                if (nz ? !zero : zero) { Pc = curInstrAddr + 4u + offset; R[15] = Pc; }
                else { Pc = curInstrAddr + 2u; R[15] = Pc; }
                return;
            }
            if ((i & 0xFF00u) == 0xB200u)
            {
                uint op2v = (i >> 6) & 3u;
                int rm = (int)((i >> 3) & 7u);
                int rd = (int)(i & 7u);
                switch (op2v)
                {
                    case 0u: R[rd] = (uint)(int)(short)(R[rm] & 0xFFFFu); break;
                    case 1u: R[rd] = (uint)(int)(sbyte)(R[rm] & 0xFFu); break;
                    case 2u: R[rd] = R[rm] & 0xFFFFu; break;
                    default: R[rd] = R[rm] & 0xFFu; break;
                }
                Pc = curInstrAddr + 2u; R[15] = Pc; return;
            }
            if ((i & 0xFE00u) == 0xB400u)
            {
                uint list = i & 0xFFu;
                if ((i & 0x100u) != 0u) list |= 0x4000u;
                uint sp = R[13] - (uint)(PopCount(list) * 4);
                uint addr = sp;
                for (int r = 0; r < 16; r++)
                {
                    if ((list & (1u << r)) == 0) continue;
                    MemWriteWord(addr, R[r]);
                    if (pendingFault != FaultNone) return;
                    addr += 4u;
                }
                R[13] = sp;
                Pc = curInstrAddr + 2u; R[15] = Pc; return;
            }
            if ((i & 0xFF00u) == 0xB600u)
            {
                if ((i & 0x00F0u) == 0x0060u) { if (IsPrivileged) Cpsr |= 0x40u; }
                else if ((i & 0x00F0u) == 0x0070u) { if (IsPrivileged) Cpsr &= ~0x40u; }
                else if ((i & 0x00FFu) == 0x0002u) { dataBigEndian = true; Cpsr |= 0x200u; }
                else if ((i & 0x00FFu) == 0x0000u) { dataBigEndian = false; Cpsr &= ~0x200u; }
                Pc = curInstrAddr + 2u; R[15] = Pc; return;
            }
            if ((i & 0xFF00u) == 0xBA00u)
            {
                uint a = (i >> 6) & 3u;
                int rm = (int)((i >> 3) & 7u);
                int rd = (int)(i & 7u);
                uint v = R[rm];
                switch (a)
                {
                    case 0u: R[rd] = ReverseBytes(v); break;
                    case 1u: R[rd] = (ReverseBytes(v) >> 16) | (ReverseBytes(v) << 16); break;
                    default: R[rd] = (uint)(int)(short)(((v & 0xFFu) << 8) | ((v >> 8) & 0xFFu)); break;
                }
                Pc = curInstrAddr + 2u; R[15] = Pc; return;
            }
            if ((i & 0xFE00u) == 0xBC00u)
            {
                uint list = i & 0xFFu;
                if ((i & 0x100u) != 0u) list |= 0x8000u;
                uint sp = R[13];
                uint addr = sp;
                bool pcLoaded = false;
                for (int r = 0; r < 16; r++)
                {
                    if ((list & (1u << r)) == 0) continue;
                    uint v = MemReadWord(addr, false);
                    if (pendingFault != FaultNone) return;
                    if (r == 15) { R[15] = v; pcLoaded = true; }
                    else R[r] = v;
                    addr += 4u;
                }
                R[13] = sp + (uint)(PopCount(list) * 4);
                if (pcLoaded) { BranchTo(R[15]); return; }
                Pc = curInstrAddr + 2u; R[15] = Pc; return;
            }
            if ((i & 0xFF00u) == 0xBE00u)
            {
                Note("BKPT #" + Num(i & 0xFFu));
                TakeException(VecPrefetch, 0x17, curInstrAddr + 2u);
                return;
            }
            if ((i & 0xFF00u) == 0xBF00u)
            {
                uint mask = i & 0xFu;
                uint firstcond = (i >> 4) & 0xFu;
                if (mask == 0u)
                {
                    // NOP / YIELD / WFE / WFI / SEV / DBG
                    Pc = curInstrAddr + 2u; R[15] = Pc; return;
                }
                if (firstcond == 0xFu || (firstcond == 0xEu && (mask & 0x7u) == 0u))
                {
                    Undefined("IT with an invalid condition");
                    return;
                }
                itState = ((firstcond << 4) | mask) & 0xFFu;
                itStateValid = true;
                Pc = curInstrAddr + 2u; R[15] = Pc; return;
            }
            Undefined("Thumb-16 misc 0x" + i.ToString("X4", CultureInfo.InvariantCulture));
        }

        // ---------------------------------------------------------------- IT state

        /// <summary>Effective condition for a Thumb instruction, honouring IT blocks.</summary>
        private uint ThumbCondition(ref bool inItBlock, ref bool lastInIt)
        {
            if (!itStateValid)
            {
                inItBlock = false;
                lastInIt = false;
                return 0xEu;
            }
            uint itstate = itState & 0xFFu;
            uint cond = itstate >> 4;
            uint mask = itstate & 0xFu;
            inItBlock = true;

            if ((mask & 7u) == 0u)
            {
                lastInIt = true;
                itStateValid = false;
                itState = 0;
            }
            else
            {
                lastInIt = false;
                uint newMask = (mask << 1) & 0xFu;
                if ((mask & 0x10u) != 0) newMask |= 1u;
                itState = (cond << 4) | (newMask & 0xFu);
            }
            return cond;
        }

        // ---------------------------------------------------------------- Thumb-32
        //
        // ARM ARM A5.3 / A6.3.  hw1 is the first halfword, hw2 the second.
        //
        //  hw1[15:11] == 11101 (0xE800..0xEFFF): op1 = hw1[12:11] == 01
        //      op = hw1[10:4]
        //        op[6:5] == 00 -> load/store multiple, dual, exclusive, table branch
        //        op[6:5] == 01 -> data processing (shifted register)  0xEA00..0xEBFF
        //        op[6]   == 1  -> coprocessor / VFP / NEON           0xEC00..0xEFFF
        //  hw1[15:11] == 11110 (0xF000..0xF7FF): op1 = 10
        //      hw2[15] == 1 -> branch and miscellaneous control
        //      hw2[15] == 0 -> op = hw1[10:4]
        //        op[6:5] == 00 -> data processing (modified immediate) 0xF000..0xF1FF
        //        op[6:5] == 01 -> data processing (plain binary immediate) 0xF200..0xF3FF
        //  hw1[15:11] == 11111 (0xF800..0xFFFF): op1 = 11
        //      op = hw1[10:4]
        //        op[6:5] == 01 -> data processing (register) / multiply 0xFA00..0xFBFF
        //        otherwise     -> load/store single

        /// <summary>Sign-extend a value held in the low <paramref name="bits"/> bits.</summary>
        private static int SignExtend(uint value, int bits)
        {
            uint m = 1u << (bits - 1);
            return unchecked((int)((value ^ m) - m));
        }

        private static int PopCount(uint v)
        {
            int n = 0;
            while (v != 0) { n += (int)(v & 1u); v >>= 1; }
            return n;
        }

        private static uint ReverseBytes(uint v)
        {
            return ((v & 0x000000FFu) << 24) | ((v & 0x0000FF00u) << 8) |
                   ((v & 0x00FF0000u) >> 8) | ((v & 0xFF000000u) >> 24);
        }

        /// <summary>ThumbExpandImm - expand a T32 12-bit modified immediate (ARM ARM A7.4.3).</summary>
        public static uint ThumbExpandImm(uint imm12)
        {
            uint top8 = (imm12 >> 8) & 0xFu;
            uint low8 = imm12 & 0xFFu;
            if ((imm12 & 0xC00u) == 0u)
            {
                switch (top8)
                {
                    case 0u: return low8;
                    case 1u: return (low8 << 16) | low8;
                    case 2u: return (low8 << 24) | (low8 << 8);
                    case 3u: return (low8 << 24) | (low8 << 16) | (low8 << 8) | low8;
                    default:
                        {
                            uint v = low8 | 0x80u;
                            int rot = (int)(top8 - 8);
                            return (v >> rot) | (v << (32 - rot));
                        }
                }
            }
            uint unrotated = 0x80u | low8;
            int r = (int)((imm12 >> 7) & 0x1Fu);
            if (r == 0) return unrotated;
            return (unrotated >> r) | (unrotated << (32 - r));
        }

        private void DecomposeThumb32(uint instr)
        {
            // Kept for the handlers that were written against named fields.
            op1 = (instr >> 27) & 3u;
            op2 = (instr >> 20) & 0x3Fu;
            opc1 = (instr >> 20) & 0xFu;
            opc2 = (instr >> 4) & 0xFu;
            opc1_16 = (instr >> 16) & 0xFu;
            opc1_8 = (instr >> 8) & 0xFu;
            opc1_x04 = (instr >> 4) & 0xFu;
            opc1_x02 = (instr >> 2) & 0x3FFFu;

            rd_16 = (instr >> 8) & 0xFu;
            rn_16 = (instr >> 16) & 0xFu;
            rm_16 = instr & 0xFu;
            rt_16 = (instr >> 12) & 0xFu;
            rd_12 = (instr >> 8) & 0xFu;
            rn_12 = (instr >> 16) & 0xFu;
            rm_12 = instr & 0xFu;

            imm3_32 = (int)((instr >> 12) & 7u);
            imm3_16 = (int)((instr >> 16) & 7u);
            imm3_12 = (int)((instr >> 12) & 7u);
            imm5_32 = (instr >> 12) & 0x1Fu;
            imm5 = (instr >> 7) & 0x1Fu;
            i8_32 = (instr >> 16) & 0xFFu;
            i16_12 = (uint)(((instr >> 26) & 1u) << 11 | ((instr >> 12) & 7u) << 8 | (instr & 0xFFu));
            i16_8 = instr & 0xFFu;
            i8_0 = instr & 0xFFu;
            breg_ext_s = (instr >> 26) & 1u;
            breg_ext_c = (instr >> 13) & 1u;
            simm16 = instr & 0xFFFFu;
            lsbit = (instr >> 5) & 3u;
            reglist = instr & 0xFFFFu;
            lsb_32 = (int)(((instr >> 12) & 7u) << 2 | ((instr >> 6) & 3u));
            msb_32 = (int)((instr >> 16) & 0x1Fu);
            i32_32 = ((uint)(((instr >> 26) & 1u) << 11 | ((instr >> 12) & 7u) << 8 | (instr & 0xFFu)));
            i32_x02 = (instr >> 2) & 0x3FFFu;
        }

        private void ExecuteThumb32()
        {
            bool inIt = false, lastInIt = false;
            uint cond = ThumbCondition(ref inIt, ref lastInIt);

            if (inIt)
            {
                if (cond == 0xFu) { Undefined("T32 instruction with cond AL inside an IT block"); return; }
                if (!ConditionPassed(cond)) { Pc = curInstrAddr + 4u; R[15] = Pc; return; }
            }

            uint hw1 = curInstr >> 16;
            uint hw2 = curInstr & 0xFFFFu;
            uint t1 = (hw1 >> 11) & 3u;        // hw1[12:11]
            uint top = (hw1 >> 4) & 0x7Fu;     // hw1[10:4]

            if (t1 == 1u)
            {
                // 0xE800..0xEFFF
                if ((top & 0x60u) == 0x00u) { Thumb32LoadStoreDualExclTable(); return; }
                if ((top & 0x60u) == 0x20u) { Thumb32DataProcessingShifted(); return; }
                Thumb32Coprocessor();
                return;
            }

            if (t1 == 2u)
            {
                // 0xF000..0xF7FF.  hw1[9] == 0 selects the modified-immediate
                // form, hw1[9] == 1 the plain binary immediate form.
                if ((hw2 & 0x8000u) != 0u) { Thumb32BranchMisc(); return; }
                if ((hw1 & 0x200u) == 0u) { Thumb32DataProcessingModified(); return; }
                Thumb32DataProcessingPlain();
                return;
            }

            // 0xF800..0xFFFF
            if ((top & 0x60u) == 0x20u) { Thumb32DataProcessingRegister(); return; }
            Thumb32LoadStoreSingle();
        }

        /// <summary>T32 A6.3.1 - data processing (shifted register), 0xEA00..0xEBFF.</summary>
        private void Thumb32DataProcessingShifted()
        {
            uint hw1 = curInstr >> 16;
            uint hw2 = curInstr & 0xFFFFu;
            uint opc = (hw1 >> 5) & 0xFu;         // hw1[8:5]
            bool sf = (hw1 & 0x10u) != 0u;        // hw1[4]
            int rn = (int)(hw1 & 0xFu);
            int rd = (int)((hw2 >> 8) & 0xFu);
            int rm = (int)(hw2 & 0xFu);
            int type = (int)((hw2 >> 4) & 3u);    // hw2[5:4]
            int amount = (int)(((hw2 >> 12) & 7u) << 2 | ((hw2 >> 6) & 3u));

            // Register-controlled shift: opc == 0000 and type == 00 and imm3:imm2 == 0.
            if (opc == 0u && type == 0 && amount == 0 && (hw2 & 0x00F0u) == 0u)
            {
                // Shift (register): Rn is the destination, hw2[11:8] the amount reg.
                int rd2 = (int)((hw2 >> 8) & 0xFu);
                ShiftReg(R[rm], 0, R[rd2] & 0xFFu);
                uint r0 = ShiftReg(R[rm], 0, R[rd2] & 0xFFu);
                R[rn] = r0;
                if (sf) SetNZ(r0);
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            uint shifted;
            switch (opc)
            {
                case 0xAu: shifted = ShiftImm(R[rm], 0, amount, false); break;   // no such opc
                default: shifted = ShiftImm(R[rm], type, amount, false); break;
            }

            uint result;
            bool carry = FlagC, overflow = FlagV;
            bool write = true;
            string dbg = null;
            switch (opc)
            {
                case 0x0u: result = R[rn] & shifted; break;
                case 0x1u: result = R[rn] & ~shifted; break;
                case 0x2u:
                    if (rn == 15) { result = shifted; dbg = null; }
                    else result = R[rn] | shifted;
                    break;
                case 0x3u: result = rn == 15 ? ~shifted : (R[rn] | ~shifted); break;
                case 0x4u: result = rn == 15 ? shifted : (R[rn] ^ shifted); break;
                case 0x6u:
                    // PKH (T32): PKHBT / PKHTB
                    {
                        uint amount2 = (uint)(((hw2 >> 12) & 7u) << 2 | ((hw2 >> 6) & 3u));
                        bool tb = (hw2 & 0x20u) != 0u;
                        if (!tb)
                        {
                            uint sh = ShiftImm(R[rn], 0, (int)amount2, false);
                            result = (sh & 0xFFFF0000u) | (R[rm] & 0xFFFFu);
                        }
                        else
                        {
                            int amt = (int)(amount2 == 0 ? 32u : amount2);
                            uint sh = ShiftImm(R[rm], 2, amt, false);
                            result = (R[rn] & 0xFFFF0000u) | (sh & 0xFFFFu);
                        }
                        break;
                    }
                case 0x8u: result = AddWithCarry(R[rn], shifted, false, out carry, out overflow); break;
                case 0xAu: result = AddWithCarry(R[rn], shifted, FlagC, out carry, out overflow); break;
                case 0xBu: result = SubWithCarry(R[rn], shifted, FlagC, out carry, out overflow); break;
                case 0xDu: result = SubWithCarry(R[rn], shifted, true, out carry, out overflow); break;
                case 0xEu: result = SubWithCarry(shifted, R[rn], true, out carry, out overflow); break;
                default:
                    Undefined("T32 shifted-register opc=0x" + opc.ToString("X", CultureInfo.InvariantCulture));
                    return;
            }
            if (dbg != null) { }

            R[rd] = result;
            if (sf)
            {
                if (opc <= 7u) SetNZ(result);
                else SetNZCV(result, carry, overflow);
            }
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        /// <summary>T32 A6.3.3 - data processing (modified immediate), 0xF000..0xF1FF.</summary>
        private void Thumb32DataProcessingModified()
        {
            uint hw1 = curInstr >> 16;
            uint hw2 = curInstr & 0xFFFFu;
            uint i = (hw1 >> 10) & 1u;
            uint opc = (hw1 >> 5) & 0xFu;         // hw1[8:5]
            bool sf = (hw1 & 0x10u) != 0u;        // hw1[4]
            int rn = (int)(hw1 & 0xFu);
            int rd = (int)((hw2 >> 8) & 0xFu);
            uint imm12v = (i << 11) | (((hw2 >> 12) & 7u) << 8) | (hw2 & 0xFFu);
            uint imm = ThumbExpandImm(imm12v);

            // The "Rd == 1111" forms of AND/EOR/ADD/SUB are the TST/TEQ/CMN/CMP
            // aliases; the tested value is in the Rn field.
            if (rd == 15 && (opc == 0x0u || opc == 0x4u || opc == 0x8u || opc == 0xDu))
            {
                uint a2 = R[rn];
                uint r2;
                bool c2 = FlagC, v2 = FlagV;
                if (opc == 0x0u) r2 = a2 & imm;
                else if (opc == 0x4u) r2 = a2 ^ imm;
                else if (opc == 0x8u) r2 = AddWithCarry(a2, imm, false, out c2, out v2);
                else r2 = SubWithCarry(a2, imm, true, out c2, out v2);
                if (sf) { if (opc == 0x0u || opc == 0x4u) SetNZ(r2); else SetNZCV(r2, c2, v2); }
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            uint result;
            bool carry = FlagC, overflow = FlagV;
            bool logical = false;
            switch (opc)
            {
                case 0x0u: result = R[rn] & imm; logical = true; break;
                case 0x1u: result = R[rn] & ~imm; logical = true; break;
                case 0x2u: result = rn == 15 ? imm : (R[rn] | imm); logical = true; break;
                case 0x3u: result = rn == 15 ? ~imm : (R[rn] | ~imm); logical = true; break;
                case 0x4u: result = rn == 15 ? imm : (R[rn] ^ imm); logical = true; break;
                case 0x8u: result = AddWithCarry(R[rn], imm, false, out carry, out overflow); break;
                case 0xAu: result = AddWithCarry(R[rn], imm, FlagC, out carry, out overflow); break;
                case 0xBu: result = SubWithCarry(R[rn], imm, FlagC, out carry, out overflow); break;
                case 0xDu: result = SubWithCarry(R[rn], imm, true, out carry, out overflow); break;
                case 0xEu: result = SubWithCarry(imm, R[rn], true, out carry, out overflow); break;
                default:
                    Undefined("T32 modified-immediate opc=0x" + opc.ToString("X", CultureInfo.InvariantCulture));
                    return;
            }
            R[rd] = result;
            if (sf)
            {
                if (logical) SetNZ(result);
                else SetNZCV(result, carry, overflow);
            }
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        /// <summary>T32 A6.3.4 - data processing (plain binary immediate), 0xF200..0xF3FF.</summary>
        private void Thumb32DataProcessingPlain()
        {
            uint hw1 = curInstr >> 16;
            uint hw2 = curInstr & 0xFFFFu;
            uint i = (hw1 >> 10) & 1u;
            uint opc = (hw1 >> 4) & 0x1Fu;        // hw1[8:4]
            int rn = (int)(hw1 & 0xFu);
            int rd = (int)((hw2 >> 8) & 0xFu);
            uint imm3v = (hw2 >> 12) & 7u;
            uint imm8 = hw2 & 0xFFu;
            uint imm12v = (i << 11) | (imm3v << 8) | imm8;
            int lsb = (int)(imm3v << 2 | ((hw2 >> 6) & 3u));
            int shiftType = (int)((hw2 >> 4) & 3u);

            switch (opc)
            {
                case 0x00u: // ADDW
                    R[rd] = R[rn] + imm12v;
                    break;
                case 0x04u: // MOVW
                    R[rd] = (uint)(((hw1 & 0xFu) << 12) | (i << 11) | (imm3v << 8) | imm8);
                    break;
                case 0x0Au: // SUBW
                    R[rd] = R[rn] - imm12v;
                    break;
                case 0x0Cu: // MOVT - writes the top half only
                    R[rd] = (R[rd] & 0x0000FFFFu) |
                            (uint)((((hw1 & 0xFu) << 12) | (i << 11) | (imm3v << 8) | imm8) << 16);
                    break;
                case 0x10u: // SSAT
                    {
                        uint sat = hw1 & 0xFu;
                        uint v = ShiftImm(R[rn], shiftType, lsb, false);
                        R[rd] = (uint)SaturateSigned((int)v, (int)sat + 1);
                        break;
                    }
                case 0x12u: // SSAT16
                    {
                        int sat = (int)(hw1 & 0xFu);
                        int v0 = (short)(R[rn] & 0xFFFFu);
                        int v1 = (short)(R[rn] >> 16);
                        R[rd] = (uint)(((SaturateSigned(v1, sat + 1) & 0xFFFF) << 16) | (SaturateSigned(v0, sat + 1) & 0xFFFF));
                        break;
                    }
                case 0x14u: // SBFX
                case 0x1Cu: // UBFX
                    {
                        int width = (int)(hw2 >> 12);
                        int l2 = (int)(((hw2 >> 6) & 3u) | ((hw2 >> 10) & 0x1Cu));
                        int w = width + 1;
                        if (w <= 0 || w > 32 || l2 + w > 32) { Undefined("T32 bitfield width"); return; }
                        uint shifted = R[rn] >> l2;
                        uint value = w >= 32 ? shifted : (shifted & ((1u << w) - 1u));
                        if (opc == 0x14u && w < 32 && (value & (1u << (w - 1))) != 0) value |= ~((1u << w) - 1u);
                        R[rd] = value;
                        break;
                    }
                case 0x16u: // BFI / BFC
                    {
                        int width = (int)(hw2 >> 12);
                        int l2 = (int)(((hw2 >> 6) & 3u) | ((hw2 >> 10) & 0x1Cu));
                        int w = width + 1;
                        if (w <= 0 || w > 32 || l2 + w > 32) { Undefined("T32 BFI width"); return; }
                        uint mask = w >= 32 ? 0xFFFFFFFFu : ((1u << w) - 1u);
                        uint sr = rn == 15 ? 0u : R[rn];
                        R[rd] = (R[rd] & ~(mask << l2)) | ((sr & mask) << l2);
                        break;
                    }
                case 0x18u: // USAT
                    {
                        uint sat = hw1 & 0xFu;
                        uint v = ShiftImm(R[rn], shiftType, lsb, false);
                        long lv = (long)(int)v;
                        int max = sat >= 32u ? 0x7FFFFFFF : (1 << (int)sat) - 1;
                        uint res;
                        if (lv > max) res = (uint)max;
                        else if (lv < 0) res = 0u;
                        else res = (uint)lv;
                        R[rd] = res;
                        break;
                    }
                case 0x1Au: // USAT16
                    {
                        int sat = (int)(hw1 & 0xFu);
                        int max = sat >= 16 ? 0xFFFF : (1 << sat) - 1;
                        int v0 = (int)(R[rn] & 0xFFFFu);
                        int v1 = (int)(R[rn] >> 16);
                        R[rd] = (uint)((((v1 > max ? max : v1) & 0xFFFF) << 16) | ((v0 > max ? max : v0) & 0xFFFF));
                        break;
                    }
                default:
                    Undefined("T32 plain-immediate opc=0x" + opc.ToString("X", CultureInfo.InvariantCulture));
                    return;
            }
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        /// <summary>T32 A6.3.10 - data processing (register): multiply and parallel add/sub, 0xFA00..0xFBFF.</summary>
        private void Thumb32DataProcessingRegister()
        {
            uint hw1 = curInstr >> 16;
            uint hw2 = curInstr & 0xFFFFu;
            uint op1 = (hw1 >> 4) & 0xFu;          // hw1[7:4]
            int rn = (int)(hw1 & 0xFu);
            int rd = (int)((hw2 >> 8) & 0xFu);
            int ra = (int)((hw2 >> 12) & 0xFu);
            int rm = (int)(hw2 & 0xFu);
            uint op2 = (hw2 >> 4) & 0xFu;          // hw2[7:4]

            if (((hw1 >> 8) & 0xFu) == 0xAu)
            {
                // Parallel addition and subtraction
                ParallelAddSub32(op1, op2, rd, rn, rm);
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            switch (op1)
            {
                case 0x0u:
                    if (op2 == 0u)
                    {
                        if (ra == 15) R[rd] = R[rn] * R[rm];                 // MUL
                        else R[rd] = R[rn] * R[rm] + R[ra];                  // MLA
                        Pc = curInstrAddr + 4u; R[15] = Pc;
                        return;
                    }
                    break;
                case 0x1u:
                    if (op2 == 0u)
                    {
                        R[rd] = R[ra] - R[rn] * R[rm];                       // MLS
                        Pc = curInstrAddr + 4u; R[15] = Pc;
                        return;
                    }
                    break;
                case 0x8u:
                    if (op2 == 0u) { R[rd] = (uint)((long)(int)R[rn] * (long)(int)R[rm] >> 32); Pc = curInstrAddr + 4u; R[15] = Pc; return; }  // SMULL
                    if (op2 == 1u) { R[rd] = (uint)((ulong)R[rn] * R[rm] >> 32); Pc = curInstrAddr + 4u; R[15] = Pc; return; }                 // UMULL
                    break;
                default:
                    break;
            }
            Undefined("T32 data-processing register op1=0x" + op1.ToString("X", CultureInfo.InvariantCulture) +
                      " op2=0x" + op2.ToString("X", CultureInfo.InvariantCulture));
        }

        /// <summary>T32 parallel add/subtract (SADD16/ASX/.../UHSUB8).</summary>
        private void ParallelAddSub32(uint op1, uint op2, int rd, int rn, int rm)
        {
            bool unsigned = op1 >= 4u;
            bool halving = (op1 & 3u) == 3u;
            bool saturating = (op1 & 3u) == 2u;
            // op1: 0 = add/sub, 1 = q, 2 = sh, 3 = add/sub (u variants by bit 2)
            unsigned = (op1 & 4u) != 0u;
            saturating = (op1 & 3u) == 2u;
            halving = (op1 & 3u) == 3u;

            uint a = R[rn], b = R[rm];
            uint result;
            bool is8 = (op2 & 1u) != 0u;
            bool isAsx = op2 == 2u || op2 == 6u;
            bool isSax = op2 == 3u || op2 == 7u;
            bool subtract = op2 >= 4u;

            if (is8)
            {
                result = 0;
                for (int k = 0; k < 4; k++)
                {
                    int x = (int)((a >> (k * 8)) & 0xFFu);
                    int y = (int)((b >> (k * 8)) & 0xFFu);
                    if (!unsigned) { x = (sbyte)x; y = (sbyte)y; }
                    int r = subtract ? x - y : x + y;
                    if (saturating) r = SaturateSigned(r, 8);
                    else if (halving) r >>= 1;
                    result |= (uint)(r & 0xFF) << (k * 8);
                }
            }
            else if (isAsx || isSax)
            {
                int a0 = (int)(a & 0xFFFFu), a1 = (int)(a >> 16);
                int b0 = (int)(b & 0xFFFFu), b1 = (int)(b >> 16);
                if (!unsigned) { a0 = (short)a0; a1 = (short)a1; b0 = (short)b0; b1 = (short)b1; }
                int r0, r1;
                if (isAsx) { r0 = a0 + (subtract ? -b1 : b1); r1 = a1 + (subtract ? -b0 : b0); }
                else { r0 = a0 + (subtract ? -b0 : b0); r1 = a1 + (subtract ? -b1 : b1); }
                if (saturating) { r0 = SaturateSigned(r0, 16); r1 = SaturateSigned(r1, 16); }
                else if (halving) { r0 >>= 1; r1 >>= 1; }
                result = (uint)(((r1 & 0xFFFF) << 16) | (r0 & 0xFFFF));
            }
            else
            {
                int a0 = (int)(a & 0xFFFFu), a1 = (int)(a >> 16);
                int b0 = (int)(b & 0xFFFFu), b1 = (int)(b >> 16);
                if (!unsigned) { a0 = (short)a0; a1 = (short)a1; b0 = (short)b0; b1 = (short)b1; }
                int r0, r1;
                if (subtract) { r0 = a0 - b0; r1 = a1 - b1; }
                else { r0 = a0 + b0; r1 = a1 + b1; }
                if (saturating) { r0 = SaturateSigned(r0, 16); r1 = SaturateSigned(r1, 16); }
                else if (halving) { r0 >>= 1; r1 >>= 1; }
                result = (uint)(((r1 & 0xFFFF) << 16) | (r0 & 0xFFFF));
            }
            R[rd] = result;
        }

        /// <summary>T32 A6.3.5 - load/store multiple, dual, exclusive and table branch (0xE800..0xE9FF).</summary>
        private void Thumb32LoadStoreMultiple()
        {
            Thumb32LoadStoreDualExclTable();
        }

        private void Thumb32LoadStoreDualExclTable()
        {
            uint hw1 = curInstr >> 16;
            uint hw2 = curInstr & 0xFFFFu;
            int rn = (int)(hw1 & 0xFu);
            int rt = (int)((hw2 >> 12) & 0xFu);
            bool db = (hw1 & 0x100u) != 0u;      // hw1[8] : P for LDRD/STRD, DB for LDM/STM
            bool u = (hw1 & 0x080u) != 0u;       // hw1[7]
            bool fixed6 = (hw1 & 0x040u) != 0u;  // hw1[6]
            bool w = (hw1 & 0x020u) != 0u;       // hw1[5]
            bool l = (hw1 & 0x010u) != 0u;       // hw1[4]

            if (!fixed6)
            {
                // LDM / STM (IA when hw1[8] == 0, DB when hw1[8] == 1)
                uint list = hw2;
                if (list == 0u) { Pc = curInstrAddr + 4u; R[15] = Pc; return; }
                int count = PopCount(list);
                bool pcInList = (list & 0x8000u) != 0u;
                if (!l && pcInList) { Undefined("T32 STM with PC in the list"); return; }

                if (l)
                {
                    uint addr = db ? ReadReg(rn) - (uint)(count * 4) : ReadReg(rn);
                    if (db && !w) { /* LDMDB without writeback still uses the descending base */ }
                    for (int r = 0; r < 16; r++)
                    {
                        if ((list & (1u << r)) == 0) continue;
                        uint v = MemReadWord(addr, false);
                        if (pendingFault != FaultNone) return;
                        if (r == 15) R[15] = v; else R[r] = v;
                        addr += 4u;
                    }
                    if (w && rn != 15) R[rn] = db ? ReadReg(rn) - (uint)(count * 4) : ReadReg(rn) + (uint)(count * 4);
                    if (pcInList && pendingFault == FaultNone) { BranchTo(R[15]); return; }
                }
                else
                {
                    uint baseAddr = ReadReg(rn);
                    uint addr = db ? baseAddr - (uint)(count * 4) : baseAddr;
                    uint pcv = ReadPc();
                    for (int r = 0; r < 16; r++)
                    {
                        if ((list & (1u << r)) == 0) continue;
                        MemWriteWord(addr, r == 15 ? pcv : R[r]);
                        if (pendingFault != FaultNone) return;
                        addr += 4u;
                    }
                    if (w && rn != 15) R[rn] = db ? baseAddr - (uint)(count * 4) : baseAddr + (uint)(count * 4);
                }
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            // Exclusive access: STREX/LDREX/STREXB/LDREXB/STREXH/LDREXH
            if (!u && !db && !w)
            {
                uint imm = (hw2 & 0xFFu) * 4u;
                if (!l)
                {
                    int rd = (int)((hw2 >> 8) & 0xFu);
                    uint status = 1u;
                    if (exclusiveValid && exclusiveAddr == ReadReg(rn) && exclusiveId == Mmu.ContextIdr)
                    {
                        MemWriteWord(ReadReg(rn), R[rt]);
                        if (pendingFault != FaultNone) return;
                        status = 0u;
                    }
                    exclusiveValid = false;
                    R[rd] = status;
                    Pc = curInstrAddr + 4u; R[15] = Pc;
                    return;
                }
                uint v = MemReadWord(ReadReg(rn) + imm, false);
                if (pendingFault != FaultNone) return;
                exclusiveValid = true;
                exclusiveAddr = ReadReg(rn) + imm;
                exclusiveId = Mmu.ContextIdr;
                R[rt] = v;
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            if (w && !l && !u && db)
            {
                // STREXH/LDREXH share the 0xE840/0xE850 group with hw2[7:0] == 0xF4
                Undefined("T32 exclusive halfword");
                return;
            }
            if (db && !u && !(hw1 == 0xE8C0u || hw1 == 0xE8D0u))
            {
                Undefined("T32 exclusive byte/halfword");
                return;
            }
            if (hw1 == 0xE8C0u || hw1 == 0xE8D0u)
            {
                // STREXB / LDREXB
                bool load = hw1 == 0xE8D0u;
                if (!load)
                {
                    int rd = (int)((hw2 >> 8) & 0xFu);
                    uint status = 1u;
                    if (exclusiveValid && exclusiveAddr == ReadReg(rn) && exclusiveId == Mmu.ContextIdr)
                    {
                        MemWriteByte(ReadReg(rn), R[rt] & 0xFFu);
                        if (pendingFault != FaultNone) return;
                        status = 0u;
                    }
                    exclusiveValid = false;
                    R[rd] = status;
                }
                else
                {
                    uint v = MemReadByte(ReadReg(rn));
                    if (pendingFault != FaultNone) return;
                    exclusiveValid = true;
                    exclusiveAddr = ReadReg(rn);
                    exclusiveId = Mmu.ContextIdr;
                    R[rt] = v & 0xFFu;
                }
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            // Table branch: TBB/TBH use hw2[7:4] == 0 and hw2[3:0] == 0
            if (u && !db && w && hw2 == 0x0000u)
            {
                bool h = (hw1 & 0x10u) != 0u;
                Undefined("T32 table branch");
                return;
            }

            // LDRD / STRD
            int rt2 = (int)((hw2 >> 8) & 0xFu);
            uint imm8 = (hw2 & 0xFFu) * 4u;
            uint baseAddr2 = ReadReg(rn);
            uint address = baseAddr2;
            if (db) address = u ? baseAddr2 + imm8 : baseAddr2 - imm8;
            if (l)
            {
                ulong v = MemReadDouble(address);
                if (pendingFault != FaultNone) return;
                R[rt] = (uint)v;
                R[rt2] = (uint)(v >> 32);
            }
            else
            {
                ulong v = (ulong)R[rt] | ((ulong)R[rt2] << 32);
                MemWriteDouble(address, v);
                if (pendingFault != FaultNone) return;
            }
            if (w && rn != 15) R[rn] = u ? baseAddr2 + imm8 : baseAddr2 - imm8;
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        /// <summary>T32 A6.3.8/9 - load/store single (0xF800..0xF9FF, 0xFC00..0xFFFF).</summary>
        private void Thumb32LoadStoreSingle()
        {
            uint hw1 = curInstr >> 16;
            uint hw2 = curInstr & 0xFFFFu;
            int rn = (int)(hw1 & 0xFu);
            int rt = (int)((hw2 >> 12) & 0xFu);
            bool signedSpace = (hw1 & 0x100u) != 0u;   // hw1[8]
            bool imm12Form = (hw1 & 0x80u) != 0u;      // hw1[7]
            bool isWord = (hw1 & 0x40u) != 0u;         // hw1[6]
            bool isHalf = (hw1 & 0x20u) != 0u;         // hw1[5]
            bool l = (hw1 & 0x10u) != 0u;              // hw1[4]

            if (signedSpace)
            {
                uint sizeSel = hw1 & 0x30u;
                if (sizeSel != 0x10u && sizeSel != 0x30u) { Undefined("T32 NEON load/store"); return; }
                bool half = sizeSel == 0x30u;
                bool imm12b = (hw1 & 0x80u) != 0u;
                uint addr;
                if (imm12b)
                {
                    uint i = (hw2 >> 11) & 1u;
                    uint imm = (i << 11) | (hw2 & 0xFFFu);
                    if ((hw2 & 0x0800u) == 0u) { Undefined("T32 NEON load/store"); return; }
                    addr = ReadReg(rn) + ((i << 11) | (hw2 & 0xFFFu));
                    uint v = half ? MemReadHalf(addr) : MemReadByte(addr);
                    if (pendingFault != FaultNone) return;
                    R[rt] = half ? (uint)(int)(short)(v & 0xFFFFu) : (uint)(int)(sbyte)(v & 0xFFu);
                }
                else
                {
                    bool p = (hw2 & 0x400u) != 0u;
                    bool u = (hw2 & 0x200u) != 0u;
                    bool w = (hw2 & 0x100u) != 0u;
                    uint imm8 = hw2 & 0xFFu;
                    uint baseAddr = ReadReg(rn);
                    addr = p ? (u ? baseAddr + imm8 : baseAddr - imm8) : baseAddr;
                    uint v = half ? MemReadHalf(addr) : MemReadByte(addr);
                    if (pendingFault != FaultNone) return;
                    R[rt] = half ? (uint)(int)(short)(v & 0xFFFFu) : (uint)(int)(sbyte)(v & 0xFFu);
                    if (w && p && rn != 15) R[rn] = u ? baseAddr + imm8 : baseAddr - imm8;
                }
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            // A load with Rt == 1111 is the PLD/PLI preload hint (no effect).
            if (l && rt == 15)
            {
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            int size = isWord ? 2 : (isHalf ? 1 : 0);   // 0 byte, 1 halfword, 2 word

            if (imm12Form)
            {
                if ((hw2 & 0x0800u) == 0u)
                {
                    // Register offset form
                    int rm = (int)(hw2 & 0xFu);
                    uint addr2 = ReadReg(rn) + R[rm];
                    if (l)
                    {
                        uint v;
                        if (size == 0) v = MemReadByte(addr2) & 0xFFu;
                        else if (size == 1) v = MemReadHalf(addr2 & ~1u) & 0xFFFFu;
                        else v = MemReadWord(addr2 & ~3u, false);
                        if (pendingFault != FaultNone) return;
                        R[rt] = v;
                    }
                    else
                    {
                        if (size == 0) MemWriteByte(addr2, R[rt] & 0xFFu);
                        else if (size == 1) MemWriteHalf(addr2 & ~1u, R[rt] & 0xFFFFu);
                        else MemWriteWord(addr2 & ~3u, R[rt]);
                        if (pendingFault != FaultNone) return;
                    }
                    Pc = curInstrAddr + 4u; R[15] = Pc;
                    return;
                }
                uint imm12 = hw2 & 0xFFFu;
                uint address = ReadReg(rn) + imm12;
                if (l)
                {
                    uint v;
                    if (size == 0) v = MemReadByte(address) & 0xFFu;
                    else if (size == 1) v = MemReadHalf(address & ~1u) & 0xFFFFu;
                    else v = MemReadWord(address & ~3u, false);
                    if (pendingFault != FaultNone) return;
                    R[rt] = v;
                }
                else
                {
                    if (size == 0) MemWriteByte(address, R[rt] & 0xFFu);
                    else if (size == 1) MemWriteHalf(address & ~1u, R[rt] & 0xFFFFu);
                    else MemWriteWord(address & ~3u, R[rt]);
                    if (pendingFault != FaultNone) return;
                }
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            // 8-bit immediate with P/U/W
            {
                bool p = (hw2 & 0x400u) != 0u;
                bool u = (hw2 & 0x200u) != 0u;
                bool w = (hw2 & 0x100u) != 0u;
                uint imm8 = hw2 & 0xFFu;
                uint baseAddr = ReadReg(rn);
                uint address = p ? (u ? baseAddr + imm8 : baseAddr - imm8) : baseAddr;
                if (l)
                {
                    uint v;
                    if (size == 0) v = MemReadByte(address) & 0xFFu;
                    else if (size == 1) v = MemReadHalf(address & ~1u) & 0xFFFFu;
                    else v = MemReadWord(address & ~3u, false);
                    if (pendingFault != FaultNone) return;
                    R[rt] = v;
                }
                else
                {
                    if (size == 0) MemWriteByte(address, R[rt] & 0xFFu);
                    else if (size == 1) MemWriteHalf(address & ~1u, R[rt] & 0xFFFFu);
                    else MemWriteWord(address & ~3u, R[rt]);
                    if (pendingFault != FaultNone) return;
                }
                if (w && p && rn != 15) R[rn] = u ? baseAddr + imm8 : baseAddr - imm8;
                Pc = curInstrAddr + 4u; R[15] = Pc;
            }
        }

        /// <summary>T32 coprocessor / VFP / NEON space, 0xEC00..0xEFFF.</summary>
        private void Thumb32Coprocessor()
        {
            // The coprocessor number lives in bits [11:8] of the 32-bit instruction.
            uint cp = (curInstr >> 8) & 0xFu;
            bool load = hw2Bit(20);

            if (cp == 10u || cp == 11u)
            {
                if ((curInstr & 0x0E000000u) == 0x0C000000u)
                {
                    if ((curInstr & 0x02000000u) != 0u) ExecuteVfpLoadStoreMultiple(curInstr, (curInstr & 0x100u) != 0);
                    else ExecuteVfpLoadStore(curInstr, cp, (curInstr & 0x100u) != 0);
                    return;
                }
                ExecuteArmVfp(curInstr);
                return;
            }
            if (cp == 15u)
            {
                int rd = (int)(((curInstr >> 12) & 0xFu));
                uint o1 = (curInstr >> 21) & 7u;
                uint crn2 = (curInstr >> 16) & 0xFu;
                uint crm2 = curInstr & 0xFu;
                uint o2 = (curInstr >> 5) & 7u;
                if (load) { uint v = Cp15Read(o1, crn2, crm2, o2, rd); if (rd != 15) R[rd] = v; }
                else Cp15Write(o1, crn2, crm2, o2, rd == 15 ? 0u : R[rd]);
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }
            Note("T32 coprocessor p" + Num(cp) + " ignored");
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        private bool hw2Bit(int bit)
        {
            return ((curInstr >> bit) & 1u) != 0u;
        }

        /// <summary>T32 A6.3.10/11 - branches and miscellaneous control (hw2[15] == 1).</summary>
        private void Thumb32BranchMisc()
        {
            uint hw1 = curInstr >> 16;
            uint hw2 = curInstr & 0xFFFFu;
            uint sv = (hw1 >> 10) & 1u;
            uint j1 = (hw2 >> 13) & 1u;
            uint j2 = (hw2 >> 11) & 1u;
            uint imm11 = hw2 & 0x7FFu;

            // The IT state was already advanced by ExecuteThumb32().  A branch as the
            // last instruction of an IT block must not carry its own condition.
            bool inIt = false, lastInIt = false;
            uint itCond = ThumbConditionPeek(ref inIt, ref lastInIt);
            if (inIt && lastInIt && (hw2 & 0x4000u) == 0u && (hw2 & 0x1000u) == 0u &&
                ((hw1 >> 6) & 0xFu) <= 0xDu)
            {
                Undefined("T32 conditional branch as the last instruction of an IT block");
                return;
            }

            if ((hw2 & 0x4000u) == 0u)
            {
                // hw2[15:14] == 10
                if ((hw2 & 0x1000u) != 0u)
                {
                    // T4: B.W (unconditional)
                    uint imm10 = hw1 & 0x3FFu;
                    uint i1 = (~(j1 ^ sv)) & 1u;
                    uint i2 = (~(j2 ^ sv)) & 1u;
                    int off = SignExtend((sv << 24) | (i1 << 23) | (i2 << 22) | (imm10 << 12) | (imm11 << 1), 25);
                    Pc = curInstrAddr + 4u + (uint)off;
                    R[15] = Pc;
                    return;
                }
                uint cond = (hw1 >> 6) & 0xFu;
                if (cond <= 0xDu)
                {
                    // T3: B<c>.W
                    uint imm6 = hw1 & 0x3Fu;
                    int off = SignExtend((sv << 20) | (j2 << 19) | (j1 << 18) | (imm6 << 12) | (imm11 << 1), 21);
                    if (ConditionPassed(cond)) { Pc = curInstrAddr + 4u + (uint)off; R[15] = Pc; }
                    else { Pc = curInstrAddr + 4u; R[15] = Pc; }
                    return;
                }
                // cond == 111x : miscellaneous control
                Thumb32Misc();
                return;
            }

            // hw2[15:14] == 11 : BLX (hw2[12] == 0) or BL (hw2[12] == 1)
            {
                uint imm10 = hw1 & 0x3FFu;
                uint i1 = (~(j1 ^ sv)) & 1u;
                uint i2 = (~(j2 ^ sv)) & 1u;
                if ((hw2 & 0x1000u) != 0u)
                {
                    int off = SignExtend((sv << 24) | (i1 << 23) | (i2 << 22) | (imm10 << 12) | (imm11 << 1), 25);
                    R[14] = (curInstrAddr + 4u) | 1u;
                    Pc = curInstrAddr + 4u + (uint)off;
                    R[15] = Pc;
                    return;
                }
                if ((hw2 & 1u) != 0u) { Undefined("T32 BLX with hw2[0] == 1"); return; }
                uint imm10l = (hw2 >> 1) & 0x3FFu;
                int offx = SignExtend((sv << 24) | (i1 << 23) | (i2 << 22) | (imm10 << 12) | (imm10l << 2), 25);
                uint target = (curInstrAddr + 4u) & ~3u;
                R[14] = (curInstrAddr + 4u) | 1u;
                Thumb = false;
                Pc = target + (uint)offx;
                R[15] = Pc;
                return;
            }
        }

        /// <summary>Read the pending IT condition without advancing the IT state.</summary>
        private uint ThumbConditionPeek(ref bool inItBlock, ref bool lastInIt)
        {
            if (!itStateValid) { inItBlock = false; lastInIt = false; return 0xEu; }
            uint itstate = itState & 0xFFu;
            uint cond = itstate >> 4;
            uint mask = itstate & 0xFu;
            inItBlock = true;
            lastInIt = (mask & 7u) == 0u;
            return cond;
        }

        private void Thumb32Misc()
        {
            uint hw1 = curInstr >> 16;
            uint hw2 = curInstr & 0xFFFFu;
            int rd = (int)((hw2 >> 8) & 0xFu);
            int rm = (int)(hw2 & 0xFu);

            // Hints: 1111 0011 1010 ....
            if (hw1 == 0xF3AFu && (hw2 & 0x8000u) != 0u)
            {
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }
            if (hw1 == 0xF3BFu && (hw2 & 0xFF00u) == 0x8F00u)
            {
                // CLREX / DSB / DMB / ISB
                if ((hw2 & 0xF0u) == 0x10u) exclusiveValid = false;
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }
            // MRS: 1111 0011 1110 1111 1000 Rd 0000 0000
            if (hw1 == 0xF3EFu && (hw2 & 0xF000u) == 0x8000u)
            {
                bool spsr = (hw2 & 0x100u) != 0u;
                R[rd] = spsr ? GetSpsr() : Cpsr;
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }
            // MSR (register): 1111 0011 1000 1110 1000 Rd 0000 Rm
            if ((hw1 & 0xFFF0u) == 0xF380u && (hw2 & 0xF000u) == 0x8000u)
            {
                uint sysm = (hw2 >> 8) & 0xFu;
                bool spsr = (hw2 & 0x100u) != 0u;
                uint val = R[rm];
                uint mask = 0xFFFFFFFFu;
                if (sysm == 0u) mask = 0x000000FFu;
                else if (sysm == 1u) mask = 0x0000FF00u;
                else if (sysm == 2u) mask = 0x00FF0000u;
                else if (sysm == 3u) mask = 0xFF000000u;
                if (spsr) SetSpsr((GetSpsr() & ~mask) | (val & mask));
                else WriteCpsrMasked(val, mask);
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }
            // SVC.W / UDF.W / HVC
            if ((hw1 & 0xFFF0u) == 0xF7F0u) { TakeException(VecSvc, 0x13, curInstrAddr + 4u); return; }
            if ((hw1 & 0xFFE0u) == 0xF7E0u) { TakeException(VecHyp, 0x1A, curInstrAddr + 4u); return; }
            if ((hw1 & 0xFFF0u) == 0xF7F0u) { Undefined("T32 UDF.W"); return; }
            if (hw1 == 0xF3AFu && (hw2 & 0xFFF0u) == 0x80F0u)
            {
                Pc = curInstrAddr + 4u; R[15] = Pc; return;   // DBG
            }

            Undefined("T32 misc control 0x" + curInstr.ToString("X8", CultureInfo.InvariantCulture));
        }

        // ---------------------------------------------------------------- VFP

        /// <summary>VFP/NEON entry point for the A32 encoding (cond == 1111, blk == 111).</summary>
        private void ExecuteArmVfp(uint instr)
        {
            uint cpnum = (instr >> 8) & 0xFu;

            if (cpnum == 10u || cpnum == 11u)
            {
                bool l = (instr & (1u << 20)) != 0;
                if ((instr & 0x0E000000u) == 0x0C000000u)
                {
                    // VLDR/VSTR use P == 1, W == 0; otherwise VLDM/VSTM with the
                    // addressing mode encoded in P/U/W.  Coprocessor 10 addresses
                    // the single-precision file, 11 the double-precision file.
                    bool singleLs = cpnum == 10u;
                    if ((instr & (1u << 24)) != 0u && (instr & (1u << 21)) == 0u)
                        ExecuteVfpLoadStore(instr, cpnum, singleLs);
                    else
                        ExecuteVfpLoadStoreMultiple(instr, singleLs);
                    return;
                }
                if ((instr & 0x10u) == 0u)
                {
                    ExecuteVfpDataProcessing(instr, cpnum == 10u);
                    return;
                }
                // 2-register transfers (VMOV two singles <-> double, core transfers).
                ExecuteVfpTwoRegister(instr, cpnum == 10u);
                return;
            }

            // Advanced SIMD (cpnum 10 with the NEON bit set) and other coprocessors.
            if (cpnum == 10u || cpnum == 11u)
            {
                Undefined("Advanced SIMD not implemented");
                return;
            }
            Note("VFP/coprocessor p" + Num(cpnum) + " ignored");
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        private void ExecuteVfpDataProcessing(uint instr, bool cp10)
        {
            if (!Vfp.Enabled) { Undefined("VFP disabled (FPEXC.EN == 0)"); return; }

            bool sz = (instr & 0x100u) != 0;          // bit 8: 0 = single, 1 = double
            uint opcv1 = (instr >> 20) & 0xFu;
            uint opcv2 = (instr >> 6) & 0xFu;

            // Register numbering: single precision Sn = (Vn << 1) | N and double
            // precision Dn = Vn | (N << 4), using bit 22 for the destination and
            // bit 5 for the third operand.
            uint fVd = (instr >> 12) & 0xFu, fVn = (instr >> 16) & 0xFu, fVm = instr & 0xFu;
            bool bD = (instr & (1u << 22)) != 0u, bN = (instr & 0x80u) != 0u, bM = (instr & 0x20u) != 0u;
            int vd = sz ? (int)(fVd | (bD ? 16u : 0u)) : (int)((fVd << 1) | (bD ? 1u : 0u));
            int vn = sz ? (int)(fVn | (bN ? 16u : 0u)) : (int)((fVn << 1) | (bN ? 1u : 0u));
            int vm = sz ? (int)(fVm | (bM ? 16u : 0u)) : (int)((fVm << 1) | (bM ? 1u : 0u));

            ExecuteVfpOp(sz, instr, opcv1, opcv2, vd, vn, vm);
        }

        private void ExecuteVfpOp(bool dbl, uint instr, uint opcv1, uint opcv2, int vd, int vn, int vm)
        {
            // The operation selector is bits [23:20] with bit 22 removed: bit 22
            // is the destination high bit D (already folded into vd).  Bit 6 is
            // opc3 and bit 7 is the N bit (quiet compare / E variant).
            uint sel = (uint)((((instr >> 23) & 1u) << 2) | (((instr >> 21) & 1u) << 1) | ((instr >> 20) & 1u));
            bool opc3 = (instr & 0x40u) != 0;
            bool nBit = (instr & 0x80u) != 0;
            uint opc2v = (instr >> 16) & 0xFu;

            if (sel == 7u)
            {
                if (!opc3)
                {
                    // VMOV (immediate): imm8 = imm4H(bits [19:16]) : imm4L(bits [3:0]).
                    uint imm4H = (instr >> 16) & 0xFu;
                    uint imm8 = (imm4H << 4) | (instr & 0xFu);
                    if (dbl) Vfp.WriteD(vd, ArmVfp.ExpandImmediate64(imm8));
                    else Vfp.WriteS(vd, ArmVfp.ExpandImmediate(imm8));
                    Pc = curInstrAddr + 4u; R[15] = Pc;
                    return;
                }
                if (opc2v == 4u)
                {
                    // VCMP (N == 0) / VCMPE (N == 1); Vm == 0 compares with 0.0.
                    if (dbl) Vfp.Compare(Vfp.ReadF64(vd), vm == 0 ? 0.0 : Vfp.ReadF64(vm), nBit);
                    else Vfp.Compare((double)Vfp.ReadF32(vd), vm == 0 ? 0.0 : (double)Vfp.ReadF32(vm), nBit);
                    Pc = curInstrAddr + 4u; R[15] = Pc;
                    return;
                }
                if (opc2v == 8u || opc2v == 0xCu || opc2v == 0xDu)
                {
                    // VCVT / VCVTR between floating point and 32-bit integers.
                    // The integer operand is always addressed as an S register.
                    int sd = (int)((((instr >> 12) & 0xFu) << 1) | ((instr >> 22) & 1u));
                    int sm = (int)(((instr & 0xFu) << 1) | ((instr >> 5) & 1u));
                    if (opc2v == 8u)
                    {
                        uint raw = Vfp.ReadS(sm);
                        if (dbl) Vfp.WriteF64(vd, nBit ? (double)(int)raw : (double)raw);
                        else Vfp.WriteF32(vd, nBit ? (float)(int)raw : (float)raw);
                    }
                    else
                    {
                        bool signedInt = (opc2v == 0xDu) || (opc2v == 0xCu ? false : nBit);
                        uint v = dbl ? Vfp.FloatToInt(Vfp.ReadF64(vm), !signedInt, false)
                                     : Vfp.FloatToInt((double)Vfp.ReadF32(vm), !signedInt, false);
                        Vfp.WriteS(sd, v);
                    }
                    Pc = curInstrAddr + 4u; R[15] = Pc;
                    return;
                }
                if (opc2v == 1u)
                {
                    if (nBit)
                    {
                        if (dbl) Vfp.WriteF64(vd, Vfp.SqrtDouble(Vfp.ReadF64(vm)));
                        else Vfp.WriteF32(vd, Vfp.SqrtSingle(Vfp.ReadF32(vm)));
                    }
                    else
                    {
                        if (dbl) Vfp.WriteF64(vd, -Vfp.ReadF64(vm));
                        else Vfp.WriteF32(vd, -Vfp.ReadF32(vm));
                    }
                    Pc = curInstrAddr + 4u; R[15] = Pc;
                    return;
                }
                if (opc2v == 0u)
                {
                    if (dbl) Vfp.WriteF64(vd, nBit ? Math.Abs(Vfp.ReadF64(vm)) : Vfp.ReadF64(vm));
                    else Vfp.WriteF32(vd, nBit ? Math.Abs(Vfp.ReadF32(vm)) : Vfp.ReadF32(vm));
                    Pc = curInstrAddr + 4u; R[15] = Pc;
                    return;
                }
                Undefined("VFP 2-register opc2=0x" + opc2v.ToString("X", CultureInfo.InvariantCulture));
                return;
            }

            if (sel == 4u && opc3) { Undefined("VFP VDIV with opc3 == 1"); return; }

            if (dbl)
            {
                double a = Vfp.ReadF64(vn), b = Vfp.ReadF64(vm), d = Vfp.ReadF64(vd);
                switch (sel)
                {
                    case 0u: // VMLA / VMLS
                        Vfp.WriteF64(vd, opc3 ? Vfp.SubDouble(d, Vfp.MulDouble(a, b))
                                              : Vfp.AddDouble(d, Vfp.MulDouble(a, b)));
                        break;
                    case 1u: // VNMLS (opc3 == 0) / VNMLA (opc3 == 1)
                        Vfp.WriteF64(vd, opc3 ? Vfp.AddDouble(d, Vfp.MulDouble(a, b))
                                              : Vfp.SubDouble(d, Vfp.MulDouble(a, b)));
                        break;
                    case 2u: // VMUL / VNMUL
                        {
                            double p = Vfp.MulDouble(a, b);
                            Vfp.WriteF64(vd, opc3 ? -p : p);
                            break;
                        }
                    case 3u: // VADD / VSUB
                        Vfp.WriteF64(vd, opc3 ? Vfp.SubDouble(a, b) : Vfp.AddDouble(a, b));
                        break;
                    case 4u: // VDIV
                        Vfp.WriteF64(vd, Vfp.DivDouble(a, b));
                        break;
                    case 5u: // VFNMS (opc3 == 0) / VFNMA (opc3 == 1)
                        Vfp.WriteF64(vd, opc3 ? Vfp.SubDouble(Vfp.MulDouble(a, b), d)
                                              : -Vfp.AddDouble(d, Vfp.MulDouble(a, b)));
                        break;
                    default: // VFMA / VFMS
                        Vfp.WriteF64(vd, opc3 ? Vfp.SubDouble(d, Vfp.MulDouble(a, b))
                                              : Vfp.AddDouble(d, Vfp.MulDouble(a, b)));
                        break;
                }
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            float af = Vfp.ReadF32(vn), bf = Vfp.ReadF32(vm), df = Vfp.ReadF32(vd);
            switch (sel)
            {
                case 0u:
                    Vfp.WriteF32(vd, opc3 ? Vfp.SubSingle(df, Vfp.MulSingle(af, bf))
                                          : Vfp.AddSingle(df, Vfp.MulSingle(af, bf)));
                    break;
                case 1u:
                    Vfp.WriteF32(vd, opc3 ? Vfp.AddSingle(df, Vfp.MulSingle(af, bf))
                                          : Vfp.SubSingle(df, Vfp.MulSingle(af, bf)));
                    break;
                case 2u:
                    {
                        float p = Vfp.MulSingle(af, bf);
                        Vfp.WriteF32(vd, opc3 ? -p : p);
                        break;
                    }
                case 3u:
                    Vfp.WriteF32(vd, opc3 ? Vfp.SubSingle(af, bf) : Vfp.AddSingle(af, bf));
                    break;
                case 4u:
                    Vfp.WriteF32(vd, Vfp.DivSingle(af, bf));
                    break;
                case 5u:
                    Vfp.WriteF32(vd, opc3 ? Vfp.SubSingle(Vfp.MulSingle(af, bf), df)
                                          : -Vfp.AddSingle(df, Vfp.MulSingle(af, bf)));
                    break;
                default:
                    Vfp.WriteF32(vd, opc3 ? Vfp.SubSingle(df, Vfp.MulSingle(af, bf))
                                          : Vfp.AddSingle(df, Vfp.MulSingle(af, bf)));
                    break;
            }
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        private void ExecuteVfpCompareConvert(bool dbl, uint instr, uint opcv2, int vd, int vn, int vm)
        {
            bool unsigned = (instr & 0x80u) != 0;
            bool zero = (instr & 0x40u) != 0;

            if (dbl)
            {
                switch (opcv2 & 0xFu)
                {
                    case 0x4u:
                    case 0x5u:
                        {
                            bool quiet = (opcv2 & 1u) != 0;
                            double rhs = zero ? 0.0 : Vfp.ReadF64(vm);
                            Vfp.Compare(Vfp.ReadF64(vd), rhs, quiet);
                            break;
                        }
                    case 0x7u:
                        {
                            int ty = (int)((instr >> 16) & 3u);
                            if ((instr & 0x80u) == 0u)
                            {
                                // VCVT.<ty>.F64 - float to integer
                                uint v = Vfp.FloatToInt(Vfp.ReadF64(vm), (ty & 1u) != 0, (ty & 2u) != 0);
                                Vfp.WriteS(vd, v);
                            }
                            else
                            {
                                // VCVT.F64.<ty> - integer to float
                                uint raw = Vfp.ReadS(vm);
                                double d = (ty & 1u) != 0 ? (double)raw : (double)(int)raw;
                                Vfp.WriteF64(vd, d);
                            }
                            break;
                        }
                    default:
                        Undefined("VFP double convert opc2=0x" + opcv2.ToString("X", CultureInfo.InvariantCulture));
                        return;
                }
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            switch (opcv2 & 0xFu)
            {
                case 0x4u:
                case 0x5u:
                    {
                        bool quiet = (opcv2 & 1u) != 0;
                        double rhs = zero ? 0.0 : (double)Vfp.ReadF32(vm);
                        Vfp.Compare((double)Vfp.ReadF32(vd), rhs, quiet);
                        break;
                    }
                case 0x7u:
                    {
                        int ty = (int)((instr >> 16) & 3u);
                        if ((instr & 0x80u) == 0u)
                        {
                            uint v = Vfp.FloatToInt((double)Vfp.ReadF32(vm), unsigned, (ty & 2u) != 0);
                            Vfp.WriteS(vd, v);
                        }
                        else
                        {
                            uint raw = Vfp.ReadS(vm);
                            float f = unsigned ? (float)raw : (float)(int)raw;
                            Vfp.WriteF32(vd, f);
                        }
                        break;
                    }
                default:
                    Undefined("VFP single convert opc2=0x" + opcv2.ToString("X", CultureInfo.InvariantCulture));
                    return;
            }
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        private void ExecuteVfpTwoRegister(uint instr, bool cp10)
        {
            if (!Vfp.Enabled) { Undefined("VFP disabled (FPEXC.EN == 0)"); return; }
            uint opcv1 = (instr >> 20) & 0xFu;
            uint opcv2 = (instr >> 6) & 0xFu;
            int rd = (int)((instr >> 12) & 0xFu);
            int sn = (int)((instr & 0xFu) << 1 | ((instr >> 7) & 1u));
            int dm = (int)((instr & 0xFu) << 1 | ((instr >> 5) & 1u));
            int sm = (int)((instr & 0xFu) << 1 | ((instr >> 5) & 1u));
            int dd = (int)(((instr >> 12) & 0xFu) << 1 | ((instr >> 22) & 1u));
            if (dm > 31) dm -= 32;
            if (dd > 31) dd -= 32;

            // VMOV two singles <-> one double: 1110 1110 1011 0 D 11 Vd 101 000 M 0
            if ((instr & 0x0FE00F10u) == 0x0C400A10u || (instr & 0x0FE00F10u) == 0x0C400B10u)
            {
                bool toDouble = (instr & 0x100u) != 0;
                if (toDouble)
                {
                    // VMOV Dd, Rt, Rt2
                    ulong v = (ulong)R[rd] | ((ulong)R[(int)((instr >> 16) & 0xFu)] << 32);
                    Vfp.WriteD(dd >> 1, v);
                }
                else
                {
                    ulong v = Vfp.ReadD(dd >> 1);
                    R[rd] = (uint)v;
                    R[(int)((instr >> 16) & 0xFu)] = (uint)(v >> 32);
                }
                Pc = curInstrAddr + 4u; R[15] = Pc; return;
            }

            uint idx = (instr >> 16) & 0xFu;
            bool high = (instr & 0x80u) != 0u;
            int snReg = (int)((idx << 1) | (high ? 1u : 0u));
            int dnReg = (int)(idx | (high ? 16u : 0u));

            // VMOV between a core register and a single/double register or one
            // half of a double register.
            if ((instr & 0x0F800E1Fu) == 0x0E000A10u)
            {
                bool toCore = (instr & (1u << 20)) != 0;
                if (cp10)
                {
                    if (toCore) R[rd] = Vfp.ReadS(snReg);
                    else Vfp.WriteS(snReg, R[rd]);
                }
                else
                {
                    int lane = (int)((instr >> 21) & 1u);
                    ulong v = Vfp.ReadD(dnReg);
                    if (toCore) R[rd] = (uint)((v >> (lane * 32)) & 0xFFFFFFFFul);
                    else
                    {
                        ulong mask = 0xFFFFFFFFul << (lane * 32);
                        Vfp.WriteD(dnReg, (v & ~mask) | (((ulong)R[rd] << (lane * 32)) & mask));
                    }
                }
                Pc = curInstrAddr + 4u; R[15] = Pc;
                return;
            }

            if (cp10)
            {
                // VMRS / VMSR
                if (opcv1 == 0x7u)
                {
                    uint reg = (instr >> 16) & 0xFu;
                    if ((instr & (1u << 20)) != 0)
                    {
                        uint v;
                        switch (reg)
                        {
                            case 0u: v = Vfp.Fpsid; break;
                            case 1u: v = Vfp.Fpscr; break;
                            case 6u: v = Vfp.Mvfr0; break;
                            case 7u: v = Vfp.Mvfr1; break;
                            default: v = 0u; break;
                        }
                        if (rd == 15) Cpsr = (Cpsr & 0x0FFFFFFFu) | (v & 0xF0000000u);
                        else R[rd] = v;
                    }
                    else
                    {
                        if (reg == 1u) Vfp.Fpscr = (Vfp.Fpscr & 0x0FFFFFFFu) | (R[rd] & 0xF0000000u);
                        else if (reg == 8u) Vfp.Fpexc = R[rd];
                    }
                    Pc = curInstrAddr + 4u; R[15] = Pc; return;
                }
            }

            if ((instr & 0x0FB00F50u) == 0x0EB00A40u)
            {
                // VMOV immediate
                uint imm4 = (instr >> 16) & 0xFu;
                uint imm3 = (instr >> 12) & 7u;
                uint cmode = (instr >> 8) & 0xFu;
                if ((cmode & 9u) == 8u && (instr & 0x100u) != 0)
                {
                    uint imm8 = (imm4 << 4) | (imm3 << 1) | ((instr >> 6) & 1u);
                    uint bits = ArmVfp.ExpandImmediate(imm8);
                    Vfp.WriteS(dd, bits);
                    Pc = curInstrAddr + 4u; R[15] = Pc; return;
                }
            }

            Undefined("VFP register transfer 0x" + instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        private void ExecuteVfpLoadStore(uint instr, uint cpnum, bool single)
        {
            if (!Vfp.Enabled) { Undefined("VFP disabled (FPEXC.EN == 0)"); return; }
            bool load = (instr & (1u << 20)) != 0;
            int rn = (int)((instr >> 16) & 0xFu);
            bool dBit = (instr & (1u << 22)) != 0;
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            uint imm8 = (instr & 0xFFu) * 4u;
            uint baseAddr = ReadReg(rn);
            uint addr = p ? (u ? baseAddr + imm8 : baseAddr - imm8) : baseAddr;

            if (single)
            {
                int s = (int)((((instr >> 12) & 0xFu) << 1) | (dBit ? 1u : 0u));
                if (load) { uint v = MemReadWord(addr & ~3u, false); if (pendingFault == FaultNone) Vfp.WriteS(s & 31, v); }
                else MemWriteWord(addr & ~3u, Vfp.ReadS(s & 31));
            }
            else
            {
                int d = (int)(((instr >> 12) & 0xFu) | (dBit ? 0x10u : 0u));
                if (load)
                {
                    ulong v = MemReadDouble(addr & ~7u);
                    if (pendingFault == FaultNone) Vfp.WriteD(d, v);
                }
                else MemWriteDouble(addr & ~7u, Vfp.ReadD(d));
            }
            if (pendingFault != FaultNone) return;
            if (w && rn != 15) R[rn] = u ? baseAddr + imm8 : baseAddr - imm8;
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        private void ExecuteVfpLoadStoreMultiple(uint instr, bool single)
        {
            if (!Vfp.Enabled) { Undefined("VFP disabled (FPEXC.EN == 0)"); return; }
            bool load = (instr & (1u << 20)) != 0;
            int rn = (int)((instr >> 16) & 0xFu);
            bool dBit = (instr & (1u << 22)) != 0;
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            uint imm8 = instr & 0xFFu;

            int count = single ? (int)imm8 : (int)(imm8 / 2u);
            int first = single
                ? (int)(((instr >> 12) & 0xFu) << 1 | (dBit ? 1u : 0u))
                : (int)(((instr >> 12) & 0xFu) | (dBit ? 0x10u : 0u));

            uint baseAddr = ReadReg(rn);
            uint delta = (uint)(count * (single ? 4 : 8));
            uint addr;
            if (u) addr = p ? baseAddr + (single ? 4u : 8u) : baseAddr;
            else addr = p ? baseAddr - delta : baseAddr - delta + (single ? 4u : 8u);

            for (int n = 0; n < count; n++)
            {
                if (single)
                {
                    int s = (first + n) & 31;
                    if (load) { uint v = MemReadWord(addr, false); if (pendingFault == FaultNone) Vfp.WriteS(s, v); }
                    else MemWriteWord(addr, Vfp.ReadS(s));
                    addr += 4u;
                }
                else
                {
                    int d = (first + n) & 15;
                    if (load)
                    {
                        ulong v = MemReadDouble(addr);
                        if (pendingFault == FaultNone) Vfp.WriteD(d, v);
                    }
                    else MemWriteDouble(addr, Vfp.ReadD(d));
                    addr += 8u;
                }
                if (pendingFault != FaultNone) return;
            }

            if (w && rn != 15) R[rn] = u ? baseAddr + delta : baseAddr - delta;
            Pc = curInstrAddr + 4u; R[15] = Pc;
        }

        // ================================================================ public API

        public override string Disassemble(uint address, out int length)
        {
            return ArmDisasm.Disassemble(this, address, Thumb, out length);
        }

        /// <summary>
        /// Disassemble one instruction at <paramref name="address"/> using an
        /// explicit instruction-set selection, independently of the core's current
        /// state.  Used by the "dis &lt;addr&gt; &lt;count&gt; [arm|thumb]" command.
        /// Never throws.
        /// </summary>
        public string DisassembleAt(uint address, bool thumb, out int length)
        {
            return ArmDisasm.Disassemble(this, address, thumb, out length);
        }

        /// <summary>Static form of <see cref="DisassembleAt(uint, bool, out int)"/>.</summary>
        public static string DisassembleAt(ArmCore core, uint address, bool thumb, out int length)
        {
            return ArmDisasm.Disassemble(core, address, thumb, out length);
        }

        public override void GetRegisters(List<RegValue> regs)
        {
            for (int i = 0; i < 16; i++)
            {
                string note = null;
                if (i == 13) note = "SP";
                else if (i == 14) note = "LR";
                else if (i == 15) note = "PC";
                regs.Add(new RegValue("ARM", "R" + i, i == 15 ? Pc : R[i], note));
            }
            string flags = (FlagN ? "N" : "n") + (FlagZ ? "Z" : "z") + (FlagC ? "C" : "c") + (FlagV ? "V" : "v");
            regs.Add(new RegValue("ARM", "CPSR", Cpsr, ModeName(Mode) + (Thumb ? " Thumb" : " ARM") + " " + flags));
            if (Mode != 0x10 && Mode != 0x1F)
                regs.Add(new RegValue("ARM", "SPSR", GetSpsr(), ModeName(GetSpsr() & 0x1Fu)));
            regs.Add(new RegValue("CP15", "MIDR", 0x410FC090u, "Cortex-A9 r0p0"));
            regs.Add(new RegValue("CP15", "SCTLR", Mmu.Sctlr, MmuEnabled ? "MMU on" : "MMU off"));
            regs.Add(new RegValue("CP15", "TTBR0", Mmu.Ttbr0));
            regs.Add(new RegValue("CP15", "TTBR1", Mmu.Ttbr1));
            regs.Add(new RegValue("CP15", "TTBCR", Mmu.Ttbcr));
            regs.Add(new RegValue("CP15", "DACR", Mmu.Dacr));
            regs.Add(new RegValue("CP15", "DFSR", Mmu.Dfsr));
            regs.Add(new RegValue("CP15", "DFAR", Mmu.Dfar));
            regs.Add(new RegValue("CP15", "IFSR", Mmu.Ifsr));
            regs.Add(new RegValue("CP15", "IFAR", Mmu.Ifar));
            regs.Add(new RegValue("CP15", "VBAR", Mmu.Vbar));
            regs.Add(new RegValue("VFP", "FPSCR", Vfp.Fpscr));
            regs.Add(new RegValue("VFP", "FPEXC", Vfp.Fpexc, Vfp.Enabled ? "enabled" : "disabled"));
            for (int i = 0; i < 16; i++)
                regs.Add(new RegValue("VFP", "D" + i, (uint)Vfp.ReadD(i), "0x" + Vfp.ReadD(i).ToString("X16", CultureInfo.InvariantCulture)));
        }

        public override bool SetRegister(string name, uint value)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToUpperInvariant();
            if (n == "PC") { Pc = value; R[15] = value; return true; }
            if (n == "SP") { R[13] = value; return true; }
            if (n == "LR") { R[14] = value; return true; }
            if (n == "CPSR") { WriteCpsrMasked(value, 0xFFFFFFFFu); return true; }
            if (n == "SPSR") { SetSpsr(value); return true; }
            if (n == "FPSCR") { Vfp.Fpscr = value; return true; }
            if (n == "FPEXC") { Vfp.Fpexc = value; return true; }
            if (n == "THUMB") { Thumb = value != 0; if (Thumb) Cpsr |= 0x20u; else Cpsr &= ~0x20u; return true; }
            if (n == "MMU") { MmuEnabled = value != 0; if (value != 0) Mmu.Sctlr |= 1u; else Mmu.Sctlr &= ~1u; return true; }
            if (n == "TTBR0") { Mmu.Ttbr0 = value; return true; }
            if (n == "TTBR1") { Mmu.Ttbr1 = value; return true; }
            if (n == "TTBCR") { Mmu.Ttbcr = value; return true; }
            if (n == "DACR") { Mmu.Dacr = value; return true; }
            if (n == "VBAR") { Mmu.Vbar = value; return true; }
            if (n == "SCTLR") { Mmu.Sctlr = value; MmuEnabled = (value & 1u) != 0; return true; }
            if (n.Length >= 2 && (n[0] == 'R' || n[0] == 'S' || n[0] == 'D'))
            {
                int idx;
                if (int.TryParse(n.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out idx))
                {
                    if (n[0] == 'R' && idx >= 0 && idx < 16) { R[idx] = value; if (idx == 15) Pc = value; return true; }
                    if (n[0] == 'S' && idx >= 0 && idx < 32) { Vfp.WriteS(idx, value); return true; }
                    if (n[0] == 'D' && idx >= 0 && idx < 16) { Vfp.WriteD(idx, value); return true; }
                }
            }
            return false;
        }

        public override bool GetRegister(string name, out uint value)
        {
            value = 0;
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToUpperInvariant();
            if (n == "PC") { value = Pc; return true; }
            if (n == "SP") { value = R[13]; return true; }
            if (n == "LR") { value = R[14]; return true; }
            if (n == "CPSR") { value = Cpsr; return true; }
            if (n == "SPSR") { value = GetSpsr(); return true; }
            if (n == "FPSCR") { value = Vfp.Fpscr; return true; }
            if (n == "FPEXC") { value = Vfp.Fpexc; return true; }
            if (n == "MIDR") { value = 0x410FC090u; return true; }
            if (n == "SCTLR") { value = Mmu.Sctlr; return true; }
            if (n == "DFSR") { value = Mmu.Dfsr; return true; }
            if (n == "DFAR") { value = Mmu.Dfar; return true; }
            if (n == "IFSR") { value = Mmu.Ifsr; return true; }
            if (n == "IFAR") { value = Mmu.Ifar; return true; }
            if (n == "TTBR0") { value = Mmu.Ttbr0; return true; }
            if (n == "TTBR1") { value = Mmu.Ttbr1; return true; }
            if (n == "DACR") { value = Mmu.Dacr; return true; }
            if (n == "VBAR") { value = Mmu.Vbar; return true; }
            if (n.Length >= 2 && (n[0] == 'R' || n[0] == 'S' || n[0] == 'D'))
            {
                int idx;
                if (int.TryParse(n.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out idx))
                {
                    if (n[0] == 'R' && idx >= 0 && idx < 16) { value = idx == 15 ? Pc : R[idx]; return true; }
                    if (n[0] == 'S' && idx >= 0 && idx < 32) { value = Vfp.ReadS(idx); return true; }
                    if (n[0] == 'D' && idx >= 0 && idx < 16) { value = (uint)Vfp.ReadD(idx); return true; }
                }
            }
            return false;
        }

        public override string StatusLine
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} {1}  {2}{3}{4}{5}  mmu={6}",
                    Thumb ? "Thumb" : "ARM", ModeName(Mode),
                    FlagN ? "N" : "n", FlagZ ? "Z" : "z", FlagC ? "C" : "c", FlagV ? "V" : "v",
                    MmuEnabled ? "on" : "off");
            }
        }
    }
}
