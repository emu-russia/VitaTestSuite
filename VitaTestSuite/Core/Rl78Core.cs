// ---------------------------------------------------------------------------
// Renesas RL78 (16-bit MCU core) instruction set interpreter.
//
// On the PS Vita the RL78 core is the ErnIE system controller.
//
// MEMORY MODEL (RL78 Family User's Manual: Software, R01US0056; encodings from
// the binutils RL78 decoder, see Rl78IsaTable.g.cs):
//
//   0x00000 - 0xEFFFF  internal program memory (flash)
//   0x00000 - 0x0007F  vector table (2 bytes per vector, RESET first)
//   0x00080 - 0x000BF  CALLT table
//   0xF0000 - 0xFFFFF  data page: a16 direct, register indirect and based
//                      addressing land here (0xF0000 | a16, 0xF0000 + HL, ...)
//   0xFFF00 - 0xFFFFF  SFR window (SP 0xFFFF8, PSW 0xFFFFA, CS 0xFFFFC,
//                      ES 0xFFFFD, PMC 0xFFFFE)
//   0xFFEE0 - 0xFFEFF  register banks RB0..RB3
//
// The 0x11 prefix ("ES:") selects a bank for the next instruction only: the
// effective data address becomes (ES << 16) | a16.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;

namespace VitaTestSuite.Core
{
    public class Rl78Core : CpuCore
    {
        // ---- registers -----------------------------------------------------
        public byte A, X, B, C, D, E, H, L;
        public ushort SP, PSW;
        public byte ES, CS;

        /// <summary>ISP0/ISP1 (PSW bits 2:1) as a 2-bit value.</summary>
        public int IspLevel { get { return (PSW >> 1) & 3; } }

        /// <summary>Register bank select (RBS0 = PSW.3, RBS1 = PSW.5).</summary>
        public int Bank { get { return ((PSW >> 3) & 1) | (((PSW >> 5) & 1) << 1); } }

        /// <summary>Register bank area base: 0xFFEE0 + 8 * bank.</summary>
        public uint BankBase { get { return (uint)(0xFFEE0 + 8 * Bank); } }

        /// <summary>Word fetched from address 0 at reset.</summary>
        public uint ResetVector;

        public bool TraceInstructions;

        /// <summary>Data accesses made through the memory hub (SFR/MMIO/RAM).</summary>
        public ulong DataAccesses;

        /// <summary>Number of instructions whose encoding was not in the table.</summary>
        public ulong UnknownInstructions;

        private readonly Rl78ByteSource m_src;

        private sealed class Rl78ByteSource : IRl78ByteSource
        {
            public MemoryHub Mem;
            public byte ReadByte(uint addr) { return Mem.ReadByte(addr); }
        }

        public Rl78Core(MemoryHub mem, ILogSink log = null) : base(mem, log)
        {
            Name = "RL78";
            m_src = new Rl78ByteSource();
            m_src.Mem = mem;
            Rl78Decoder.EnsureBuilt();
            InitDispatch();
        }

        public override CpuArch Arch { get { return CpuArch.Rl78; } }

        // ---- flags ----------------------------------------------------------

        public bool FlagZ { get { return (PSW & Rl78Isa.FlagZ) != 0; } }
        public bool FlagCy { get { return (PSW & Rl78Isa.FlagCy) != 0; } }
        public bool FlagAc { get { return (PSW & Rl78Isa.FlagAc) != 0; } }
        public bool FlagIe { get { return (PSW & Rl78Isa.FlagIe) != 0; } }

        public void SetFlag(int mask, bool value)
        {
            if (value) PSW |= (ushort)mask;
            else PSW &= (ushort)~mask;
        }

        // ---- reset ----------------------------------------------------------

        public override void Reset()
        {
            InstructionCount = 0;
            CycleCount = 0;
            DataAccesses = 0;
            UnknownInstructions = 0;
            Halted = false;
            HaltReason = "";
            UndefinedInstruction = false;

            A = X = B = C = D = E = H = L = 0;
            PSW = 0x06;   // reset value: ISP0=ISP1=1, all flags clear
            ES = 0x0F;    // reset value per RL78/G13 hardware manual
            CS = 0x00;
            SP = 0;

            Mem.CurrentPC = 0;
            Mem.CurrentCore = Name;
            ResetVector = Mem.ReadHalf(0);
            Pc = ResetVector & 0xFFFFu;
        }

        public override void Reset(uint entryPoint)
        {
            Reset();
            Pc = entryPoint & 0xFFFFFu;
        }

        // ---- raw access with the control registers mirrored ------------------

        internal byte Rd(uint addr)
        {
            uint a = addr & 0xFFFFFu;
            DataAccesses++;
            if (a == Rl78Isa.SfrPsw) return (byte)(PSW & 0xFF);
            if (a == Rl78Isa.SfrEs) return ES;
            if (a == Rl78Isa.SfrCs) return CS;
            if (a == Rl78Isa.SfrSp) return (byte)(SP & 0xFF);
            return Mem.ReadByte(a);
        }

        internal void Wr(uint addr, byte v)
        {
            uint a = addr & 0xFFFFFu;
            DataAccesses++;
            if (a == Rl78Isa.SfrPsw) { PSW = (ushort)((PSW & 0xFF00) | v); return; }
            if (a == Rl78Isa.SfrEs) { ES = v; return; }
            if (a == Rl78Isa.SfrCs) { CS = v; return; }
            if (a == Rl78Isa.SfrSp) { SP = (ushort)((SP & 0xFF00) | v); return; }
            Mem.WriteByte(a, v);
        }

        internal ushort Rd16(uint addr)
        {
            uint a = addr & 0xFFFFFu;
            DataAccesses += 2;
            if (a == Rl78Isa.SfrPsw) return PSW;
            if (a == Rl78Isa.SfrSp) return SP;
            if (a == Rl78Isa.SfrEs) return (ushort)(ES | (Mem.ReadByte(a + 1) << 8));
            return Mem.ReadHalf(a);
        }

        internal void Wr16(uint addr, ushort v)
        {
            uint a = addr & 0xFFFFFu;
            DataAccesses += 2;
            if (a == Rl78Isa.SfrPsw) { PSW = v; return; }
            if (a == Rl78Isa.SfrSp) { SP = v; return; }
            if (a == Rl78Isa.SfrEs) { ES = (byte)v; Mem.WriteByte(a + 1, (byte)(v >> 8)); return; }
            Mem.WriteHalf(a, v);
        }

        // ---- register file ---------------------------------------------------

        internal byte GetReg8(Rl78Reg r)
        {
            switch (r)
            {
                case Rl78Reg.X: return X;
                case Rl78Reg.A: return A;
                case Rl78Reg.C: return C;
                case Rl78Reg.B: return B;
                case Rl78Reg.E: return E;
                case Rl78Reg.D: return D;
                case Rl78Reg.L: return L;
                case Rl78Reg.H: return H;
                case Rl78Reg.ES: return ES;
                case Rl78Reg.CS: return CS;
                case Rl78Reg.PSW: return (byte)(PSW & 0xFF);
                default: return 0;
            }
        }

        internal void SetReg8(Rl78Reg r, byte v)
        {
            switch (r)
            {
                case Rl78Reg.X: X = v; break;
                case Rl78Reg.A: A = v; break;
                case Rl78Reg.C: C = v; break;
                case Rl78Reg.B: B = v; break;
                case Rl78Reg.E: E = v; break;
                case Rl78Reg.D: D = v; break;
                case Rl78Reg.L: L = v; break;
                case Rl78Reg.H: H = v; break;
                case Rl78Reg.ES: ES = v; break;
                case Rl78Reg.CS: CS = v; break;
                case Rl78Reg.PSW: PSW = (ushort)((PSW & 0xFF00) | v); break;
                default: break;
            }
        }

        internal ushort GetReg16(Rl78Reg r)
        {
            switch (r)
            {
                case Rl78Reg.AX: return (ushort)((A << 8) | X);
                case Rl78Reg.BC: return (ushort)((B << 8) | C);
                case Rl78Reg.DE: return (ushort)((D << 8) | E);
                case Rl78Reg.HL: return (ushort)((H << 8) | L);
                case Rl78Reg.SP: return SP;
                case Rl78Reg.PSW: return PSW;
                case Rl78Reg.ES: return ES;
                case Rl78Reg.CS: return CS;
                default: return GetReg8(r);
            }
        }

        internal void SetReg16(Rl78Reg r, ushort v)
        {
            switch (r)
            {
                case Rl78Reg.AX: A = (byte)(v >> 8); X = (byte)v; break;
                case Rl78Reg.BC: B = (byte)(v >> 8); C = (byte)v; break;
                case Rl78Reg.DE: D = (byte)(v >> 8); E = (byte)v; break;
                case Rl78Reg.HL: H = (byte)(v >> 8); L = (byte)v; break;
                case Rl78Reg.SP: SP = v; break;
                case Rl78Reg.PSW: PSW = v; break;
                case Rl78Reg.ES: ES = (byte)v; break;
                case Rl78Reg.CS: CS = (byte)v; break;
                default: SetReg8(r, (byte)v); break;
            }
        }

        // ---- operand addressing ---------------------------------------------

        /// <summary>
        /// Address page for data accesses: the 0xF0000 page, or the ES page while
        /// an 0x11 ES: prefix is in effect.
        /// </summary>
        private uint m_page = 0xF0000u;

        private uint AddrOf(Rl78Operand o)
        {
            switch (o.AddKind)
            {
                case Rl78IsaTable.AKCallt:
                    // the CALLT table entry address itself
                    return (uint)o.Address & 0xFFFFFu;
                case Rl78IsaTable.AKImmu2:
                case Rl78IsaTable.AKImmu3:
                    // !addr16: the page comes from ES (0xF0000 by default)
                    return (m_page | ((uint)o.Addend & 0xFFFFu)) & 0xFFFFFu;
                default:
                    break;
            }
            if (o.Reg == Rl78Reg.None) return (uint)o.Address & 0xFFFFFu;

            uint off = (uint)o.Addend;
            if (o.Reg2 != Rl78Reg.None) off += GetReg8(o.Reg2);
            switch (o.Reg)
            {
                case Rl78Reg.HL: return (m_page + (uint)(((H << 8) | L) + off)) & 0xFFFFFu;
                case Rl78Reg.DE: return (m_page + (uint)(((D << 8) | E) + off)) & 0xFFFFFu;
                case Rl78Reg.SP: return (m_page + (uint)(SP + off)) & 0xFFFFFu;
                case Rl78Reg.B: return (m_page + (uint)(B + off)) & 0xFFFFFu;
                case Rl78Reg.C: return (m_page + (uint)(C + off)) & 0xFFFFFu;
                case Rl78Reg.BC: return (m_page + (uint)(((B << 8) | C) + off)) & 0xFFFFFu;
                case Rl78Reg.A: return (m_page + (uint)(A + off)) & 0xFFFFFu;
                case Rl78Reg.X: return (m_page + (uint)(X + off)) & 0xFFFFFu;
                default: return (m_page + off) & 0xFFFFFu;
            }
        }

        // ---- generic operand read / write ---------------------------------------

        private byte ReadOp(Rl78Operand o)
        {
            switch (o.Type)
            {
                case Rl78OpType.Imm: return (byte)o.Addend;
                case Rl78OpType.Reg: return GetReg8(o.Reg);
                case Rl78OpType.Ind: return Rd(AddrOf(o));
                case Rl78OpType.Bit:
                case Rl78OpType.BitInd: return (byte)(ReadBit(o) ? 1 : 0);
                default: return 0;
            }
        }

        private void WriteOp(Rl78Operand o, byte v)
        {
            switch (o.Type)
            {
                case Rl78OpType.Reg: SetReg8(o.Reg, v); return;
                case Rl78OpType.Ind: Wr(AddrOf(o), v); return;
                case Rl78OpType.Bit:
                case Rl78OpType.BitInd: WriteBit(o, v != 0); return;
                default: return;
            }
        }

        private ushort ReadOpW(Rl78Operand o)
        {
            switch (o.Type)
            {
                case Rl78OpType.Imm: return (ushort)o.Addend;
                case Rl78OpType.Reg: return GetReg16(o.Reg);
                case Rl78OpType.Ind: return Rd16(AddrOf(o));
                default: return 0;
            }
        }

        private void WriteOpW(Rl78Operand o, ushort v)
        {
            switch (o.Type)
            {
                case Rl78OpType.Reg: SetReg16(o.Reg, v); return;
                case Rl78OpType.Ind: Wr16(AddrOf(o), v); return;
                default: return;
            }
        }

        private bool ReadBit(Rl78Operand o)
        {
            int bit = o.BitNumber & 7;
            switch (o.Type)
            {
                case Rl78OpType.Bit:
                    if (o.Reg == Rl78Reg.PSW) return (PSW & (1 << bit)) != 0;
                    if (o.Reg == Rl78Reg.A) return ((A >> bit) & 1) != 0;
                    return false;
                case Rl78OpType.BitInd:
                    return ((Rd(AddrOf(o)) >> bit) & 1) != 0;
                default:
                    return false;
            }
        }

        private void WriteBit(Rl78Operand o, bool v)
        {
            int bit = o.BitNumber & 7;
            switch (o.Type)
            {
                case Rl78OpType.Bit:
                    if (o.Reg == Rl78Reg.PSW)
                    {
                        if (v) PSW |= (ushort)(1 << bit); else PSW &= (ushort)~(1 << bit);
                    }
                    else if (o.Reg == Rl78Reg.A)
                        A = (byte)(v ? (A | (1 << bit)) : (A & ~(1 << bit)));
                    return;
                case Rl78OpType.BitInd:
                    {
                        uint a = AddrOf(o);
                        byte b = Rd(a);
                        Wr(a, (byte)(v ? (b | (1 << bit)) : (b & ~(1 << bit))));
                        return;
                    }
                default:
                    return;
            }
        }

        // ---- step -------------------------------------------------------------

        /// <summary>Pending maskable interrupt vector, or -1.</summary>
        public int PendingVector = -1;

        public override StepResult Step()
        {
            StepResult r = new StepResult();
            r.Address = Pc;
            Mem.CurrentPC = Pc;
            Mem.CurrentCore = Name;

            try
            {
                if (PendingVector >= 0 && InterruptsEnabled) TakePending();

                Rl78Decoded d = Rl78Decoder.Decode(m_src, Pc);
                if (d == null)
                {
                    UndefinedInstruction = true;
                    UnknownInstructions++;
                    r.Length = 1;
                    r.Text = "??";
                    r.Faulted = true;
                    r.Fault = "undefined instruction at 0x" + Pc.ToString("X5", CultureInfo.InvariantCulture);
                    Fault(r.Fault);
                    InstructionCount++;
                    Pc++;
                    return r;
                }

                r.Length = d.Length;
                InstructionCount++;

                if (TraceInstructions)
                {
                    r.Text = Rl78Disassembler.Format(m_src, Pc, d);
                    Emit("cpu", string.Format(CultureInfo.InvariantCulture, "{0:X5}: {1}", Pc, r.Text));
                }

                m_page = d.HasEsPrefix ? (uint)((ES & 0x0F) << 16) : 0xF0000u;

                uint before = Pc;
                Execute(d);
                CycleCount += (ulong)Cycles(d);
                if (!Halted && Pc == before) Pc += (uint)d.Length;
                Mem.CurrentPC = Pc;
            }
            catch (Exception ex)
            {
                Fault("exception: " + ex.GetType().Name + ": " + ex.Message);
                r.Faulted = true;
                r.Fault = HaltReason;
            }

            return r;
        }

        /// <summary>Rough cycle cost: SFR/short-direct/direct accesses take longer.</summary>
        private int Cycles(Rl78Decoded d)
        {
            int c = 1;
            for (int i = 0; i < 2; i++)
            {
                Rl78Operand o = d.Operand(i);
                if (o.Type != Rl78OpType.Ind) continue;
                switch (o.AddKind)
                {
                    case Rl78IsaTable.AKSfr:
                    case Rl78IsaTable.AKSaddr:
                    case Rl78IsaTable.AKImmu2:
                    case Rl78IsaTable.AKImmu3:
                        c = 4;
                        break;
                }
            }
            return c;
        }

        // ---- dispatch ----------------------------------------------------------
        //
        // Per row: a compact execution opcode, computed once at startup from the
        // row's semantics id, its mnemonic and the shape of its operands.  The hot
        // path then never compares strings.

        private const int EO_Nop = 0, EO_Halt = 1, EO_Stop = 2, EO_Break = 3, EO_Ret = 4,
                          EO_Reti = 5, EO_Sel = 6, EO_Skip = 7, EO_Mov = 8, EO_Movs = 9,
                          EO_Push = 10, EO_Pop = 11, EO_BitMov = 12, EO_Xch = 13,
                          EO_Add = 14, EO_Addc = 15, EO_Sub = 16, EO_Subc = 17, EO_Cmp = 18,
                          EO_Logic = 19, EO_BitLogic = 20, EO_Not = 21, EO_Shift = 22,
                          EO_Branch = 23, EO_BCond = 24, EO_Btclr = 25, EO_MulDiv = 26,
                          EO_Mulu = 27, EO_Callt = 28, EO_Unknown = 29;

        private static byte[] s_eo;
        private static bool s_dispatchReady;

        private static void InitDispatch()
        {
            if (s_dispatchReady) return;
            s_dispatchReady = true;
            int n = Rl78IsaTable.Count;
            byte[] eo = new byte[n];
            for (int r = 0; r < n; r++)
            {
                Rl78Id id = (Rl78Id)Rl78IsaTable.RowId[r];
                string mn = Rl78IsaTable.Mnem[Rl78IsaTable.RowS[r]];
                int k = r * 2;
                byte e = EO_Unknown;
                switch (id)
                {
                    case Rl78Id.Unknown: break;
                    case Rl78Id.Nop: e = EO_Nop; break;
                    case Rl78Id.Halt: e = EO_Halt; break;
                    case Rl78Id.Stop: e = EO_Stop; break;
                    case Rl78Id.Break: e = EO_Break; break;
                    case Rl78Id.Ret: e = EO_Ret; break;
                    case Rl78Id.Reti: e = EO_Reti; break;
                    case Rl78Id.Sel: e = EO_Sel; break;
                    case Rl78Id.Skip: e = EO_Skip; break;
                    case Rl78Id.Mulu: e = EO_Mulu; break;
                    case Rl78Id.Divhu:
                    case Rl78Id.Divwu:
                    case Rl78Id.Mulhu:
                    case Rl78Id.Mulh:
                    case Rl78Id.Mach:
                    case Rl78Id.Machu: e = EO_MulDiv; break;
                    case Rl78Id.Xch: e = EO_Xch; break;
                    case Rl78Id.Add: e = EO_Add; break;
                    case Rl78Id.Addc: e = EO_Addc; break;
                    case Rl78Id.Sub: e = EO_Sub; break;
                    case Rl78Id.Subc: e = EO_Subc; break;
                    case Rl78Id.Cmp: e = EO_Cmp; break;
                    case Rl78Id.Shl:
                    case Rl78Id.Shr:
                    case Rl78Id.Sar:
                    case Rl78Id.Rol:
                    case Rl78Id.Ror:
                    case Rl78Id.Rolc:
                    case Rl78Id.Rorc: e = EO_Shift; break;
                    case Rl78Id.Branch: e = EO_Branch; break;
                    case Rl78Id.Call: e = mn == "callt" ? (byte)EO_Callt : (byte)EO_Branch; break;
                    case Rl78Id.BranchCond: e = EO_BCond; break;
                    case Rl78Id.BranchCondClear: e = EO_Btclr; break;
                    case Rl78Id.And:
                    case Rl78Id.Or:
                        e = Rl78IsaTable.OpT[k] == Rl78IsaTable.OTBit ? (byte)EO_BitLogic : (byte)EO_Logic;
                        break;
                    case Rl78Id.Xor:
                        if (mn == "not1") e = EO_Not;
                        else e = Rl78IsaTable.OpT[k] == Rl78IsaTable.OTBit ? (byte)EO_BitLogic : (byte)EO_Logic;
                        break;
                    case Rl78Id.Mov:
                        if (mn == "push") e = EO_Push;
                        else if (mn == "pop") e = EO_Pop;
                        else if (mn == "movs") e = EO_Movs;
                        else if (mn == "set1" || mn == "clr1" || mn == "mov1") e = EO_BitMov;
                        else if (Rl78IsaTable.OpT[k] == Rl78IsaTable.OTInd &&
                                 Rl78IsaTable.OpRA[k] == Rl78IsaTable.AKSfr &&
                                 Rl78IsaTable.OpT[k + 1] == Rl78IsaTable.OTImm)
                            e = EO_MulDiv;      // "mov %s0, #%1": mul/div/mach multiplex
                        else e = EO_Mov;
                        break;
                    default: break;
                }
                eo[r] = e;
            }
            s_eo = eo;
        }

        // ---- execution ---------------------------------------------------------

        private void Execute(Rl78Decoded d)
        {
            switch (s_eo[d.Row])
            {
                case EO_Nop: break;
                case EO_Halt: Halted = true; HaltReason = "HALT"; Emit("cpu", "HALT at 0x" + Pc.ToString("X5")); break;
                case EO_Stop: Halted = true; HaltReason = "STOP"; Emit("cpu", "STOP at 0x" + Pc.ToString("X5")); break;
                case EO_Break: DoBreak(d); break;
                case EO_Ret: Pc = Pop(); break;
                case EO_Reti: DoReti(); break;
                case EO_Sel: DoSel(d); break;
                case EO_Skip: DoSkip(d); break;
                case EO_Mov: DoMov(d); break;
                case EO_Movs: DoMovs(d); break;
                case EO_BitMov: DoBitMov(d); break;
                case EO_Push: DoPush(d); break;
                case EO_Pop: DoPop(d); break;
                case EO_Xch: DoXch(d); break;
                case EO_Add: DoAdd(d, false); break;
                case EO_Addc: DoAdd(d, true); break;
                case EO_Sub: DoSub(d, false); break;
                case EO_Subc: DoSub(d, true); break;
                case EO_Cmp: DoCmp(d); break;
                case EO_Logic: DoLogic(d); break;
                case EO_BitLogic: DoBitLogic(d); break;
                case EO_Not: SetFlag(Rl78Isa.FlagCy, !FlagCy); break;
                case EO_Shift: DoShiftRot(d); break;
                case EO_Branch: DoBranch(d); break;
                case EO_BCond: DoBranchCond(d); break;
                case EO_Btclr: DoBranchCondClear(d); break;
                case EO_Mulu: DoMulu(); break;
                case EO_Callt: DoCallt(d); break;
                case EO_MulDiv: DoMulDiv(d); break;
                default:
                    Emit("cpu", "unhandled operation '" + d.Mnemonic + "' at 0x" + Pc.ToString("X5"));
                    Halted = true;
                    HaltReason = "unhandled op " + d.Mnemonic;
                    break;
            }
        }

        /// <summary>SEL RBn: op[1] is the immediate bank number.</summary>
        private void DoSel(Rl78Decoded d)
        {
            int rb = d.Op1.Addend & 3;
            PSW = (ushort)((PSW & ~(Rl78Isa.FlagRbs0 | Rl78Isa.FlagRbs1)) |
                           ((rb & 1) != 0 ? Rl78Isa.FlagRbs0 : 0) |
                           ((rb & 2) != 0 ? Rl78Isa.FlagRbs1 : 0));
        }

        // ---- MOV -----------------------------------------------------------------

        private void DoMov(Rl78Decoded d)
        {
            // CLRW/ONEW are word operations even though the .opc does not mark
            // them with W() (their only operand is always a register pair).
            bool word = d.Word || d.Mnemonic == "clrw" || d.Mnemonic == "onew";
            if (word)
                WriteOpW(d.Op0, ReadOpW(d.Op1));
            else
                WriteOp(d.Op0, ReadOp(d.Op1));
        }

        /// <summary>
        /// MOVS [HL+byte], X: store X and set the Z/CY/AC flags.
        /// </summary>
        private void DoMovs(Rl78Decoded d)
        {
            byte off = (byte)(d.Op0.Addend & 0xFF);
            Wr(AddrOf(d.Op0), X);
            bool zero = X == 0 || off == 0;
            SetFlag(Rl78Isa.FlagZ, X == 0);
            SetFlag(Rl78Isa.FlagCy, zero);
            SetFlag(Rl78Isa.FlagAc, (off & 0x0F) > (X & 0x0F));
        }

        /// <summary>
        /// SET1 / CLR1 / MOV1.  One of the operands may be the CY bit, which the
        /// .opc writes as PSW.0 (DCY/SCY).
        /// </summary>
        private void DoBitMov(Rl78Decoded d)
        {
            bool dstCy = d.Op0.Type == Rl78OpType.Bit && d.Op0.Reg == Rl78Reg.PSW;
            bool srcCy = d.Op1.Type == Rl78OpType.Bit && d.Op1.Reg == Rl78Reg.PSW;
            if (dstCy)
            {
                SetFlag(Rl78Isa.FlagCy, d.Op1.Type == Rl78OpType.Imm ? d.Op1.Addend != 0 : ReadBit(d.Op1));
                return;
            }
            if (srcCy) { WriteBit(d.Op0, FlagCy); return; }
            if (d.Op1.Type == Rl78OpType.Imm) WriteBit(d.Op0, d.Op1.Addend != 0);
            else WriteBit(d.Op0, ReadBit(d.Op1));
        }

        private void DoXch(Rl78Decoded d)
        {
            if (d.Word)
            {
                ushort x0 = ReadOpW(d.Op0);
                ushort x1 = ReadOpW(d.Op1);
                WriteOpW(d.Op0, x1);
                WriteOpW(d.Op1, x0);
                return;
            }
            byte b0 = ReadOp(d.Op0);
            byte b1 = ReadOp(d.Op1);
            WriteOp(d.Op0, b1);
            WriteOp(d.Op1, b0);
        }

        // ---- bit logic ----------------------------------------------------------

        /// <summary>AND1 / OR1 / XOR1 CY, bit.</summary>
        private void DoBitLogic(Rl78Decoded d)
        {
            bool v = ReadBit(d.Op1);
            bool cy = FlagCy;
            bool res = d.Id == Rl78Id.And ? (cy && v) : d.Id == Rl78Id.Or ? (cy || v) : (cy ^ v);
            SetFlag(Rl78Isa.FlagCy, res);
        }

        // ---- arithmetic --------------------------------------------------------------

        private void DoAdd(Rl78Decoded d, bool withCarry)
        {
            int fl = d.Flags;
            if (d.Word)
            {
                ushort wdst = ReadOpW(d.Op0);
                ushort wsrc = ReadOpW(d.Op1);
                int wcy = withCarry && FlagCy ? 1 : 0;
                int wsum = wdst + wsrc + wcy;
                ushort wres = (ushort)wsum;
                WriteOpW(d.Op0, wres);
                if ((fl & Rl78Isa.FlagZ) != 0) SetFlag(Rl78Isa.FlagZ, wres == 0);
                if ((fl & Rl78Isa.FlagCy) != 0) SetFlag(Rl78Isa.FlagCy, wsum > 0xFFFF);
                if ((fl & Rl78Isa.FlagAc) != 0)
                    SetFlag(Rl78Isa.FlagAc, (((wdst & 0xFFF) + (wsrc & 0xFFF) + wcy) & 0x1000) != 0);
                return;
            }
            byte ad = ReadOp(d.Op0);
            byte bd = ReadOp(d.Op1);
            int cy = withCarry && FlagCy ? 1 : 0;
            int sum = ad + bd + cy;
            byte res = (byte)sum;
            WriteOp(d.Op0, res);
            if ((fl & Rl78Isa.FlagZ) != 0) SetFlag(Rl78Isa.FlagZ, res == 0);
            if ((fl & Rl78Isa.FlagCy) != 0) SetFlag(Rl78Isa.FlagCy, sum > 0xFF);
            if ((fl & Rl78Isa.FlagAc) != 0)
                SetFlag(Rl78Isa.FlagAc, (((ad & 0xF) + (bd & 0xF) + cy) & 0x10) != 0);
        }

        private void DoSub(Rl78Decoded d, bool withCarry)
        {
            int fl = d.Flags;
            if (d.Word)
            {
                ushort wdst = ReadOpW(d.Op0);
                ushort wsrc = ReadOpW(d.Op1);
                int wbin = withCarry && FlagCy ? 1 : 0;
                int wdiff = wdst - wsrc - wbin;
                ushort wres = (ushort)wdiff;
                WriteOpW(d.Op0, wres);
                if ((fl & Rl78Isa.FlagZ) != 0) SetFlag(Rl78Isa.FlagZ, wres == 0);
                if ((fl & Rl78Isa.FlagCy) != 0) SetFlag(Rl78Isa.FlagCy, wdiff < 0);
                if ((fl & Rl78Isa.FlagAc) != 0)
                    SetFlag(Rl78Isa.FlagAc, (wdst & 0xF) < ((wsrc & 0xF) + wbin));
                return;
            }
            byte ad = ReadOp(d.Op0);
            byte bd = ReadOp(d.Op1);
            int bin = withCarry && FlagCy ? 1 : 0;
            int diff = ad - bd - bin;
            byte res = (byte)diff;
            WriteOp(d.Op0, res);
            if ((fl & Rl78Isa.FlagZ) != 0) SetFlag(Rl78Isa.FlagZ, res == 0);
            if ((fl & Rl78Isa.FlagCy) != 0) SetFlag(Rl78Isa.FlagCy, diff < 0);
            if ((fl & Rl78Isa.FlagAc) != 0)
                SetFlag(Rl78Isa.FlagAc, (ad & 0xF) < ((bd & 0xF) + bin));
        }

        private void DoCmp(Rl78Decoded d)
        {
            int fl = d.Flags;
            if (d.Mnemonic == "cmps")
            {
                // CMPS X, [HL+byte]
                byte src = ReadOp(d.Op1);
                int diff = X - src;
                if ((fl & Rl78Isa.FlagZ) != 0) SetFlag(Rl78Isa.FlagZ, (byte)diff == 0);
                if ((fl & Rl78Isa.FlagCy) != 0) SetFlag(Rl78Isa.FlagCy, X == 0 || src == 0);
                if ((fl & Rl78Isa.FlagAc) != 0) SetFlag(Rl78Isa.FlagAc, (X & 0xF) < (src & 0xF));
                return;
            }
            if (d.Word)
            {
                ushort wdst = ReadOpW(d.Op0);
                ushort wsrc = ReadOpW(d.Op1);
                int wdiff = wdst - wsrc;
                if ((fl & Rl78Isa.FlagZ) != 0) SetFlag(Rl78Isa.FlagZ, (ushort)wdiff == 0);
                if ((fl & Rl78Isa.FlagCy) != 0) SetFlag(Rl78Isa.FlagCy, wdiff < 0);
                if ((fl & Rl78Isa.FlagAc) != 0) SetFlag(Rl78Isa.FlagAc, (wdst & 0xF) < (wsrc & 0xF));
                return;
            }
            byte ad = ReadOp(d.Op0);
            byte bd = ReadOp(d.Op1);
            int diff2 = ad - bd;
            if ((fl & Rl78Isa.FlagZ) != 0) SetFlag(Rl78Isa.FlagZ, (byte)diff2 == 0);
            if ((fl & Rl78Isa.FlagCy) != 0) SetFlag(Rl78Isa.FlagCy, diff2 < 0);
            if ((fl & Rl78Isa.FlagAc) != 0) SetFlag(Rl78Isa.FlagAc, (ad & 0xF) < (bd & 0xF));
        }

        private void DoLogic(Rl78Decoded d)
        {
            byte a = ReadOp(d.Op0);
            byte b = ReadOp(d.Op1);
            byte r = d.Id == Rl78Id.And ? (byte)(a & b) : d.Id == Rl78Id.Or ? (byte)(a | b) : (byte)(a ^ b);
            WriteOp(d.Op0, r);
            if ((d.Flags & Rl78Isa.FlagZ) != 0) SetFlag(Rl78Isa.FlagZ, r == 0);
        }

        // ---- multiply / divide ----------------------------------------------------------

        private void DoMulu()
        {
            ushort r = (ushort)(A * X);
            A = (byte)(r >> 8);
            X = (byte)r;
        }

        /// <summary>
        /// The 0x61/0xCE page multiplexes mul/div/mach on the immediate byte:
        /// "mov 0xFFEFB, #n" is MULHU/MULH/DIVHU/DIVWU/MACHU/MACH (RL78/G14).
        /// </summary>
        private void DoMulDiv(Rl78Decoded d)
        {
            if (d.Id == Rl78Id.Mulu) { DoMulu(); return; }
            if (d.Id != Rl78Id.Mov) { DoIdMulDiv(d.Id); return; }
            if (d.Op0.Addend != (int)Rl78Isa.Sfr(0xFB)) { DoMov(d); return; }
            switch (d.Op1.Addend & 0xFF)
            {
                case 0x01: DoMulhu(false); return;
                case 0x02: DoMulhu(true); return;
                case 0x03: DoDivhu(); return;
                case 0x0B: DoDivwu(); return;
                case 0x05: DoMach(false); return;
                case 0x06: DoMach(true); return;
                default: DoMov(d); return;
            }
        }

        private void DoIdMulDiv(Rl78Id id)
        {
            switch (id)
            {
                case Rl78Id.Mulhu: DoMulhu(false); break;
                case Rl78Id.Mulh: DoMulhu(true); break;
                case Rl78Id.Divhu: DoDivhu(); break;
                case Rl78Id.Divwu: DoDivwu(); break;
                case Rl78Id.Machu: DoMach(false); break;
                case Rl78Id.Mach: DoMach(true); break;
            }
        }

        private void DoMulhu(bool signed)
        {
            uint r;
            if (signed) r = (uint)((int)(short)((A << 8) | X) * (int)(short)((B << 8) | C));
            else r = (uint)(((A << 8) | X) * ((B << 8) | C));
            A = (byte)(r >> 8);
            X = (byte)r;
            B = (byte)(r >> 24);
            C = (byte)(r >> 16);
        }

        private void DoDivhu()
        {
            ushort dividend = (ushort)((A << 8) | X);
            ushort divisor = (ushort)((D << 8) | E);
            if (divisor == 0) { A = 0xFF; X = 0xFF; D = 0xFF; E = 0xFF; return; }
            ushort q = (ushort)(dividend / divisor);
            ushort rem = (ushort)(dividend % divisor);
            A = (byte)(q >> 8); X = (byte)q;
            D = (byte)(rem >> 8); E = (byte)rem;
        }

        private void DoDivwu()
        {
            uint dividend = ((uint)((B << 8) | C) << 16) | (uint)((A << 8) | X);
            uint divisor = ((uint)((D << 8) | E) << 16) | (uint)((H << 8) | L);
            if (divisor == 0)
            {
                A = X = B = C = 0xFF;
                D = E = H = L = 0xFF;
                return;
            }
            uint q = dividend / divisor;
            uint rem = dividend % divisor;
            A = (byte)(q >> 24); X = (byte)(q >> 16);
            B = (byte)(q >> 8); C = (byte)q;
            D = (byte)(rem >> 24); E = (byte)(rem >> 16);
            H = (byte)(rem >> 8); L = (byte)rem;
        }

        private void DoMach(bool signed)
        {
            uint macr = ((uint)((D << 8) | E) << 16) | (uint)((H << 8) | L);
            uint prod;
            if (signed) prod = (uint)((int)(short)((A << 8) | X) * (int)(short)((B << 8) | C));
            else prod = (uint)(((A << 8) | X) * ((B << 8) | C));
            ulong sum = (ulong)macr + prod;
            SetFlag(Rl78Isa.FlagCy, sum > 0xFFFFFFFFu);
            SetFlag(Rl78Isa.FlagAc, signed && ((prod >> 31) != 0));
            uint mr = (uint)sum;
            Wr(0xFFFF0, (byte)mr);
            Wr(0xFFFF1, (byte)(mr >> 8));
            Wr(0xFFFF2, (byte)(mr >> 16));
            Wr(0xFFFF3, (byte)(mr >> 24));
        }

        // ---- shifts and rotates -----------------------------------------------------------

        private void DoShiftRot(Rl78Decoded d)
        {
            int cnt = 1;
            if (d.Op1.Type == Rl78OpType.Imm) cnt = d.Op1.Addend & 0xFF;
            if (cnt == 0) return;

            if (d.Word)
            {
                ushort v = ReadOpW(d.Op0);
                int cy = FlagCy ? 1 : 0;
                for (int i = 0; i < cnt; i++)
                {
                    bool outBit;
                    switch (d.Id)
                    {
                        case Rl78Id.Shl: outBit = (v & 0x8000) != 0; v = (ushort)(v << 1); break;
                        case Rl78Id.Shr: outBit = (v & 1) != 0; v = (ushort)(v >> 1); break;
                        case Rl78Id.Sar: outBit = (v & 1) != 0; v = (ushort)((short)v >> 1); break;
                        case Rl78Id.Rol: outBit = (v & 0x8000) != 0; v = (ushort)((v << 1) | (outBit ? 1 : 0)); break;
                        case Rl78Id.Ror: outBit = (v & 1) != 0; v = (ushort)((v >> 1) | (outBit ? 0x8000 : 0)); break;
                        case Rl78Id.Rolc: outBit = (v & 0x8000) != 0; v = (ushort)((v << 1) | cy); break;
                        default: outBit = (v & 1) != 0; v = (ushort)((v >> 1) | (cy << 15)); break;
                    }
                    cy = outBit ? 1 : 0;
                }
                SetFlag(Rl78Isa.FlagCy, cy != 0);
                WriteOpW(d.Op0, v);
                return;
            }

            byte b = ReadOp(d.Op0);
            int c = FlagCy ? 1 : 0;
            for (int i = 0; i < cnt; i++)
            {
                bool outBit;
                switch (d.Id)
                {
                    case Rl78Id.Shl: outBit = (b & 0x80) != 0; b = (byte)(b << 1); break;
                    case Rl78Id.Shr: outBit = (b & 1) != 0; b = (byte)(b >> 1); break;
                    case Rl78Id.Sar: outBit = (b & 1) != 0; b = (byte)((sbyte)b >> 1); break;
                    case Rl78Id.Rol: outBit = (b & 0x80) != 0; b = (byte)((b << 1) | (outBit ? 1 : 0)); break;
                    case Rl78Id.Ror: outBit = (b & 1) != 0; b = (byte)((b >> 1) | (outBit ? 0x80 : 0)); break;
                    case Rl78Id.Rolc: outBit = (b & 0x80) != 0; b = (byte)((b << 1) | c); break;
                    default: outBit = (b & 1) != 0; b = (byte)((b >> 1) | (c << 7)); break;
                }
                c = outBit ? 1 : 0;
            }
            SetFlag(Rl78Isa.FlagCy, c != 0);
            WriteOp(d.Op0, b);
        }

        // ---- branches ----------------------------------------------------------------------

        private void DoBranch(Rl78Decoded d)
        {
            uint target;
            if (d.Op0.Type == Rl78OpType.Reg && d.Op0.Reg != Rl78Reg.None)
            {
                // BR AX / CALL AX: the page comes from CS
                target = (uint)(((CS & 0x0F) << 16) | GetReg16(d.Op0.Reg));
            }
            else
            {
                target = (uint)d.Op0.Addend & 0xFFFFFu;
            }
            if (d.Id == Rl78Id.Call) Push((ushort)(Pc + (uint)d.Length));
            Pc = target;
        }

        /// <summary>CALLT [addr5]: the target is the word in the CALLT table.</summary>
        private void DoCallt(Rl78Decoded d)
        {
            int entry = d.Op0.Address & 0xFFFF;
            uint target = Mem.ReadHalf((uint)entry) & 0xFFFFu;
            Push((ushort)(Pc + (uint)d.Length));
            Pc = target;
        }

        private void DoBranchCond(Rl78Decoded d)
        {
            int target = d.Op0.Type == Rl78OpType.Imm ? d.Op0.Addend : (int)Pc;
            switch (d.Op1.Type)
            {
                case Rl78OpType.Bit:
                case Rl78OpType.BitInd:
                    {
                        bool v = ReadBit(d.Op1);
                        bool take = d.Op1.Condition == Rl78Cond.T ? v : !v;
                        if (take) Pc = (uint)target;
                        return;
                    }
                default:
                    {
                        bool take;
                        switch (d.Op1.Condition)
                        {
                            case Rl78Cond.C: take = FlagCy; break;
                            case Rl78Cond.NC: take = !FlagCy; break;
                            case Rl78Cond.H: take = !FlagCy && !FlagZ; break;
                            case Rl78Cond.NH: take = FlagCy || FlagZ; break;
                            case Rl78Cond.Z: take = FlagZ; break;
                            case Rl78Cond.NZ: take = !FlagZ; break;
                            case Rl78Cond.T: take = true; break;
                            default: take = false; break;
                        }
                        if (take) Pc = (uint)target;
                        return;
                    }
            }
        }

        /// <summary>BTCLR: branch when the bit is set, then clear it.</summary>
        private void DoBranchCondClear(Rl78Decoded d)
        {
            if (ReadBit(d.Op1))
            {
                WriteBit(d.Op1, false);
                Pc = (uint)(d.Op0.Addend & 0xFFFFF);
            }
        }

        private void DoSkip(Rl78Decoded d)
        {
            bool take;
            switch (d.Op1.Condition)
            {
                case Rl78Cond.C: take = FlagCy; break;
                case Rl78Cond.NC: take = !FlagCy; break;
                case Rl78Cond.Z: take = FlagZ; break;
                case Rl78Cond.NZ: take = !FlagZ; break;
                case Rl78Cond.H: take = !FlagCy && !FlagZ; break;
                case Rl78Cond.NH: take = FlagCy || FlagZ; break;
                case Rl78Cond.T: take = true; break;
                default: take = false; break;
            }
            if (take)
            {
                uint next = Pc + (uint)d.Length;
                int len = Rl78Decoder.Length(m_src, next);
                Pc = next + (uint)len;
            }
        }

        private void DoPush(Rl78Decoded d)
        {
            Rl78Operand src = d.Op0.Type == Rl78OpType.PreDec ? d.Op1 : d.Op0;
            Push(ReadOpW(src));
        }

        private void DoPop(Rl78Decoded d)
        {
            Rl78Operand dst = d.Op0.Type == Rl78OpType.PostInc ? d.Op1 : d.Op0;
            WriteOpW(dst, Pop());
        }

        private void DoBreak(Rl78Decoded d)
        {
            PushFrame((ushort)(Pc + (uint)d.Length), PSW);
            PSW = (ushort)(PSW & ~Rl78Isa.FlagIe);
            Pc = (uint)(Mem.ReadHalf(0x7E) & 0xFFFF);
            Emit("cpu", "BRK -> vector 0x007E, PC=0x" + Pc.ToString("X5"));
        }

        private void DoReti()
        {
            ushort psw;
            uint pc = PopFrame(out psw);
            Pc = pc;
            PSW = psw;
            Emit("cpu", "RETI -> PC=0x" + Pc.ToString("X5"));
        }

        // ---- stack --------------------------------------------------------------------------

        private void Push(ushort v)
        {
            SP -= 2;
            Wr16(0xF0000u | SP, v);
        }

        private ushort Pop()
        {
            ushort v = Rd16(0xF0000u | SP);
            SP += 2;
            return v;
        }

        private void PushFrame(ushort pc, ushort psw)
        {
            SP -= 1;
            Wr(0xF0000u | SP, (byte)(psw & 0xFF));
            SP -= 3;
            Wr(0xF0000u | SP, (byte)(pc & 0xFF));
            Wr(0xF0000u | (uint)(SP + 1), (byte)(pc >> 8));
            Wr(0xF0000u | (uint)(SP + 2), 0);
        }

        private uint PopFrame(out ushort psw)
        {
            uint pc = (uint)(Rd(0xF0000u | SP) | (Rd(0xF0000u | (uint)(SP + 1)) << 8));
            SP += 3;
            psw = Rd(0xF0000u | (uint)SP);
            SP += 1;
            return pc & 0xFFFF;
        }

        // ---- interrupts ---------------------------------------------------------------------

        public bool InterruptsEnabled { get { return (PSW & Rl78Isa.FlagIe) != 0; } }

        /// <summary>Request a maskable interrupt; taken before the next instruction when IE=1.</summary>
        public void Interrupt(int vector)
        {
            if (InterruptsEnabled) PendingVector = vector & 0x3F;
            else Emit("cpu", "interrupt vector " + vector + " masked (IE=0)");
        }

        /// <summary>RL78 has no non-maskable interrupt: non-maskable events are reset sources.</summary>
        public void Nmi()
        {
            Emit("cpu", "Nmi(): RL78 has no NMI; restarting at the reset vector");
            Pc = ResetVector;
        }

        private void TakePending()
        {
            int v = PendingVector;
            PendingVector = -1;
            PushFrame((ushort)Pc, PSW);
            PSW = (ushort)(PSW & ~Rl78Isa.FlagIe);
            Pc = (uint)(Mem.ReadHalf((uint)(v * 2)) & 0xFFFF);
            Emit("cpu", "interrupt vector " + v + " -> PC=0x" + Pc.ToString("X5"));
        }

        // ---- disassembly ---------------------------------------------------------------------

        public override string Disassemble(uint address, out int length)
        {
            Mem.CurrentPC = address;
            Rl78Decoded d = Rl78Decoder.Decode(m_src, address);
            if (d == null)
            {
                length = 1;
                return "??";
            }
            length = d.Length;
            return Rl78Disassembler.Format(m_src, address, d);
        }

        // ---- register interface -----------------------------------------------------------------

        public override void GetRegisters(List<RegValue> regs)
        {
            regs.Add(new RegValue("general", "PC", Pc));
            regs.Add(new RegValue("general", "AX", (uint)((A << 8) | X)));
            regs.Add(new RegValue("general", "BC", (uint)((B << 8) | C)));
            regs.Add(new RegValue("general", "DE", (uint)((D << 8) | E)));
            regs.Add(new RegValue("general", "HL", (uint)((H << 8) | L)));
            regs.Add(new RegValue("general", "SP", SP));
            regs.Add(new RegValue("general", "PSW", PSW,
                (FlagZ ? "Z " : "- ") + (FlagAc ? "AC " : "- ") + (FlagCy ? "CY " : "- ") +
                (FlagIe ? "IE " : "- ") + "bank" + Bank));
            regs.Add(new RegValue("general", "ES", ES));
            regs.Add(new RegValue("general", "CS", CS));
            regs.Add(new RegValue("general", "A", A));
            regs.Add(new RegValue("general", "X", X));
            regs.Add(new RegValue("general", "B", B));
            regs.Add(new RegValue("general", "C", C));
            regs.Add(new RegValue("general", "D", D));
            regs.Add(new RegValue("general", "E", E));
            regs.Add(new RegValue("general", "H", H));
            regs.Add(new RegValue("general", "L", L));
        }

        public override bool SetRegister(string name, uint value)
        {
            if (name == null) return false;
            switch (name.ToLowerInvariant())
            {
                case "a": A = (byte)value; return true;
                case "x": X = (byte)value; return true;
                case "b": B = (byte)value; return true;
                case "c": C = (byte)value; return true;
                case "d": D = (byte)value; return true;
                case "e": E = (byte)value; return true;
                case "h": H = (byte)value; return true;
                case "l": L = (byte)value; return true;
                case "ax": A = (byte)(value >> 8); X = (byte)value; return true;
                case "bc": B = (byte)(value >> 8); C = (byte)value; return true;
                case "de": D = (byte)(value >> 8); E = (byte)value; return true;
                case "hl": H = (byte)(value >> 8); L = (byte)value; return true;
                case "sp": SP = (ushort)value; return true;
                case "psw": PSW = (ushort)value; return true;
                case "es": ES = (byte)value; return true;
                case "cs": CS = (byte)value; return true;
                case "pc": Pc = value & 0xFFFFF; Mem.CurrentPC = Pc; return true;
                default: return false;
            }
        }

        public override bool GetRegister(string name, out uint value)
        {
            value = 0;
            if (name == null) return false;
            switch (name.ToLowerInvariant())
            {
                case "a": value = A; return true;
                case "x": value = X; return true;
                case "b": value = B; return true;
                case "c": value = C; return true;
                case "d": value = D; return true;
                case "e": value = E; return true;
                case "h": value = H; return true;
                case "l": value = L; return true;
                case "ax": value = (uint)((A << 8) | X); return true;
                case "bc": value = (uint)((B << 8) | C); return true;
                case "de": value = (uint)((D << 8) | E); return true;
                case "hl": value = (uint)((H << 8) | L); return true;
                case "sp": value = SP; return true;
                case "psw": value = PSW; return true;
                case "es": value = ES; return true;
                case "cs": value = CS; return true;
                case "pc": value = Pc; return true;
                default: return false;
            }
        }

        public override string StatusLine
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "RL78 PC={0:X5} AX={1:X4} BC={2:X4} DE={3:X4} HL={4:X4} SP={5:X4} PSW={6:X2} [{7}{8}{9}{10}] ES={11:X2} CS={12:X2} bank={13}",
                    Pc, (A << 8) | X, (B << 8) | C, (D << 8) | E, (H << 8) | L, SP, PSW,
                    FlagZ ? "Z" : "-", FlagAc ? "A" : "-", FlagCy ? "C" : "-", FlagIe ? "I" : "-",
                    ES, CS, Bank);
            }
        }
    }
}
