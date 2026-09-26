// Toshiba MeP-c5 (Media embedded Processor) interpreter.
//
// The MeP-c5 core is used in the PS Vita as CMeP ("F00D", the crypto/security
// processor) and, together with the IVC2 coprocessor in a multi-core variant,
// as Venezia (MPE).
//
// Instruction semantics follow the CGEN architecture description used by
// binutils/GDB 2.37 (cpu/mep-core.cpu, cpu/mep-c5.cpu).  Encodings and the
// disassembly text format live in MePIsa.cs.
//
// Memory mapped I/O is NOT handled here: every access goes through MemoryHub,
// which dispatches to the device models installed by DeviceSetup.Install().

using System;
using System.Collections.Generic;
using System.Globalization;

namespace VitaTestSuite.Core
{
    public enum MePProfile
    {
        /// <summary>CMeP ("F00D"): single MeP-c5 core, no coprocessor.</summary>
        CMeP = 0,
        /// <summary>Venezia MPE: MeP-c5 + IVC2, 32/64-bit VLIW packets.</summary>
        Venezia = 1
    }

    /// <summary>Hook used for coprocessor (IVC2 / keyring / Bigmac / Bignum) accesses.</summary>
    public interface IMePCopHandler
    {
        /// <summary>A coprocessor register/memory transfer. Return the value for loads.</summary>
        uint CopAccess(MePCore core, string kind, int op, uint address, uint copReg, uint value, bool load);
    }

    public class MePCore : CpuCore
    {
        // ------------------------------------------------------------ state

        public MePProfile Profile = MePProfile.CMeP;

        /// <summary>General purpose registers $0..$15 ($13=$tp, $14=$gp, $15=$sp).</summary>
        public uint[] R = new uint[16];

        /// <summary>Program counter as visible through <c>ldc $rn,$pc</c>.</summary>
        public uint PcReg { get { return Pc; } set { Pc = value; } }

        public uint Hi, Lo, Sar, Lp, Epc, Npc, Rpb, Rpe, Rpc, Tmp;
        public uint Mb0, Me0, Mb1, Me1, Exc, Cfg, Vid, Id, Dbg, Depc, Opt, Rcfg, Ccfg;
        public uint Psw;
        public uint Cr0;   // "cp" flag register used by the bcpxx branch family

        /// <summary>Condition flags (MEP "CFR"): EGT, EZ, ... (bit i == Cond[i]).</summary>
        public bool[] Cond = new bool[8];

        /// <summary>Venezia: running 64-bit VLIW packets (PSW.OM).</summary>
        public bool VliwMode;

        /// <summary>Reset vector. On CMeP the boot ROM lives at 0x5C000 in practice;
        /// the architectural reset vector is 0x00040000.</summary>
        public uint ResetVector = 0x0;

        public bool TraceInstructions;

        public IMePCopHandler CopHandler;

        /// <summary>Number of log entries produced (for tests).</summary>
        public int LogCount;

        private string[] _logRing = new string[4096];
        private int _logHead;

        // repeat block state
        private bool _repActive;
        private bool _repPendingBack;
        private bool _repEndless;

        private readonly StepResult _res = new StepResult();

        // ------------------------------------------------------------ setup

        public MePCore(MemoryHub mem, ILogSink log = null)
            : base(mem, log)
        {
            Name = "MeP-c5";
        }

        public override CpuArch Arch { get { return CpuArch.MeP; } }

        public override void Reset()
        {
            Reset(ResetVector);
        }

        public override void Reset(uint entryPoint)
        {
            for (int i = 0; i < 16; i++) R[i] = 0;
            for (int i = 0; i < 8; i++) Cond[i] = false;
            Hi = Lo = Sar = Lp = Epc = Npc = Rpb = Rpe = Rpc = Tmp = 0;
            Mb0 = Me0 = Mb1 = Me1 = Exc = Cfg = Vid = Id = Dbg = Depc = 0;
            Opt = Rcfg = Ccfg = 0;
            Psw = 0;
            Cr0 = 0;
            VliwMode = false;
            _repActive = false;
            _repPendingBack = false;
            _repEndless = false;
            Halted = false;
            HaltReason = "";
            UndefinedInstruction = false;
            InstructionCount = 0;
            CycleCount = 0;
            Pc = entryPoint;
            Mem.CurrentPC = Pc;
            Mem.CurrentCore = Name;
            if (Mem != null) Mem.ResetDevices();
        }

        /// <summary>Snapshot of the instruction trace ring (oldest first).</summary>
        public string[] LogRing
        {
            get
            {
                int n = _logHead < _logRing.Length ? _logHead : _logRing.Length;
                string[] outArr = new string[n];
                int start = (_logHead - n + _logRing.Length * 2) % _logRing.Length;
                for (int i = 0; i < n; i++) outArr[i] = _logRing[(start + i) % _logRing.Length];
                return outArr;
            }
        }

        private void Trace(string s)
        {
            _logRing[_logHead % _logRing.Length] = s;
            _logHead++;
            LogCount++;
        }

        // ------------------------------------------------------------ memory

        private byte LoadByte(uint a) { Mem.CurrentPC = Pc; return Mem.ReadByte(a); }
        private ushort LoadHalf(uint a) { Mem.CurrentPC = Pc; return Mem.ReadHalf(a); }
        private uint LoadWord(uint a) { Mem.CurrentPC = Pc; return Mem.ReadWord(a); }
        private void StoreByte(uint a, byte v) { Mem.CurrentPC = Pc; Mem.WriteByte(a, v); }
        private void StoreHalf(uint a, ushort v) { Mem.CurrentPC = Pc; Mem.WriteHalf(a, v); }
        private void StoreWord(uint a, uint v) { Mem.CurrentPC = Pc; Mem.WriteWord(a, v); }

        // ------------------------------------------------------------ CSRs

        private uint GetCsr(int idx)
        {
            switch (idx)
            {
                case 0: return Pc;
                case 1: return Lp;
                case 2: return Sar;
                case 4: return Rpb;
                case 5: return Rpe;
                case 6: return Rpc;
                case 7: return Hi;
                case 8: return Lo;
                case 12: return Mb0;
                case 13: return Me0;
                case 14: return Mb1;
                case 15: return Me1;
                case 16: return Psw;
                case 17: return Id;
                case 18: return Tmp;
                case 19: return Epc;
                case 20: return Exc;
                case 21: return Cfg;
                case 22: return Vid;
                case 23: return Npc;
                case 24: return Dbg;
                case 25: return Depc;
                case 26: return Opt;
                case 27: return Rcfg;
                case 28: return Ccfg;
            }
            return 0;
        }

        private void SetCsr(int idx, uint v)
        {
            switch (idx)
            {
                case 0: Pc = v; break;
                case 1: Lp = v; break;
                case 2: Sar = v; break;
                case 4: Rpb = v; if (v == 0) { _repActive = false; _repEndless = false; } break;
                case 5: Rpe = v; break;
                case 6: Rpc = v; break;
                case 7: Hi = v; break;
                case 8: Lo = v; break;
                case 12: Mb0 = v; break;
                case 13: Me0 = v; break;
                case 14: Mb1 = v; break;
                case 15: Me1 = v; break;
                case 16: Psw = v; VliwMode = (v & (1u << 12)) != 0; break;
                case 17: Id = v; break;
                case 18: Tmp = v; break;
                case 19: Epc = v; break;
                case 20: Exc = v; break;
                case 21: Cfg = v; break;
                case 22: Vid = v; break;
                case 23: Npc = v; break;
                case 24: Dbg = v; break;
                case 25: Depc = v; break;
                case 26: Opt = v; break;
                case 27: Rcfg = v; break;
                case 28: Ccfg = v; break;
            }
        }

        // ------------------------------------------------------------ step

        public override StepResult Step()
        {
            StepResult res = _res;
            res.Faulted = false;
            res.Fault = "";
            res.Address = Pc;
            res.Length = 2;
            res.Text = "";

            uint pc = Pc;
            try
            {
                Mem.CurrentPC = pc;
                Mem.CurrentCore = Name;
                uint word = Mem.FetchWord(pc);

                MePInsn ins = MePIsa.Decode(word);
                if (ins == null)
                {
                    UndefinedInstruction = true;
                    res.Length = 4;
                    res.Text = "*unknown*";
                    Fault("undefined instruction 0x" + word.ToString("x8", CultureInfo.InvariantCulture) +
                          " at 0x" + pc.ToString("x", CultureInfo.InvariantCulture));
                    return res;
                }

                res.Length = ins.Len;
                if (TraceInstructions)
                {
                    string text = MePIsa.Format(ins, word, pc);
                    res.Text = text;
                    Trace(pc.ToString("x8", CultureInfo.InvariantCulture) + "  " + text);
                }
                else
                {
                    res.Text = ins.Mnem;
                }

                // The PC is advanced first; branch instructions overwrite it.
                Pc = pc + ins.Len;
                VliwMode = (Psw & (1u << 12)) != 0;

                Execute(ins, word, pc);

                CycleCount++;
                InstructionCount++;

                RepeatStepEnd(pc, ins.Len);
            }
            catch (Exception ex)
            {
                Fault("exception during step at 0x" + pc.ToString("x", CultureInfo.InvariantCulture) +
                      ": " + ex.GetType().Name + ": " + ex.Message);
                res.Faulted = true;
                res.Fault = ex.Message;
            }
            return res;
        }

        /// <summary>
        /// MeP repeat blocks: RPB..RPE are repeated RPC+1 times.  The MeP pipeline
        /// retires one further instruction after the one at RPE before branching
        /// back to RPB, so the loop body is [RPB, RPE] plus the instruction that
        /// follows RPE.
        /// </summary>
        private void RepeatStepEnd(uint pc, int len)
        {
            if (!_repActive) return;

            if (_repPendingBack)
            {
                _repPendingBack = false;
                if (_repEndless)
                {
                    Pc = Rpb;
                }
                else if (Rpc != 0)
                {
                    Rpc--;
                    Pc = Rpb;
                }
                else
                {
                    _repActive = false;
                }
                return;
            }

            if (pc == (Rpe & 0xFFFFFFFEu))
            {
                _repPendingBack = true;
            }
        }

        // ------------------------------------------------------------ execute

        private void Execute(MePInsn ins, uint word, uint pc)
        {
            switch (ins.Op)
            {
                // ------------------------------------------------- loads/stores
                case MePOp.Sb:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        StoreByte(R[rm], (byte)(R[rn] & 0xFF));
                        break;
                    }
                case MePOp.Sh:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        StoreHalf(R[rm] & 0xFFFFFFFEu, (ushort)(R[rn] & 0xFFFF));
                        break;
                    }
                case MePOp.Sw:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        StoreWord(R[rm] & 0xFFFFFFFCu, R[rn]);
                        break;
                    }
                case MePOp.Lb:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = (uint)(int)(sbyte)LoadByte(R[rm]);
                        break;
                    }
                case MePOp.Lh:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = (uint)(int)(short)LoadHalf(R[rm] & 0xFFFFFFFEu);
                        break;
                    }
                case MePOp.Lw:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = LoadWord(R[rm] & 0xFFFFFFFCu);
                        break;
                    }
                case MePOp.Lbu:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = LoadByte(R[rm]);
                        break;
                    }
                case MePOp.Lhu:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = LoadHalf(R[rm] & 0xFFFFFFFEu);
                        break;
                    }
                case MePOp.SwSp:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint d = MePIsa.FieldValue(MePField.F7u9a4, word, pc);
                        StoreWord((R[15] + d) & 0xFFFFFFFCu, R[rn]);
                        break;
                    }
                case MePOp.LwSp:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint d = MePIsa.FieldValue(MePField.F7u9a4, word, pc);
                        R[rn] = LoadWord((R[15] + d) & 0xFFFFFFFCu);
                        break;
                    }
                case MePOp.SbTp:
                    {
                        int rn = (int)MePIsa.Raw(word, 5, 3);
                        uint d = MePIsa.FieldValue(MePField.F7u9, word, pc);
                        StoreByte(R[13] + d, (byte)(R[rn] & 0xFF));
                        break;
                    }
                case MePOp.ShTp:
                    {
                        int rn = (int)MePIsa.Raw(word, 5, 3);
                        uint d = MePIsa.FieldValue(MePField.F7u9a2, word, pc);
                        StoreHalf((R[13] + d) & 0xFFFFFFFEu, (ushort)(R[rn] & 0xFFFF));
                        break;
                    }
                case MePOp.SwTp:
                    {
                        int rn = (int)MePIsa.Raw(word, 5, 3);
                        uint d = MePIsa.FieldValue(MePField.F7u9a4, word, pc);
                        StoreWord((R[13] + d) & 0xFFFFFFFCu, R[rn]);
                        break;
                    }
                case MePOp.LbTp:
                    {
                        int rn = (int)MePIsa.Raw(word, 5, 3);
                        uint d = MePIsa.FieldValue(MePField.F7u9, word, pc);
                        R[rn] = (uint)(int)(sbyte)LoadByte(R[13] + d);
                        break;
                    }
                case MePOp.LhTp:
                    {
                        int rn = (int)MePIsa.Raw(word, 5, 3);
                        uint d = MePIsa.FieldValue(MePField.F7u9a2, word, pc);
                        R[rn] = (uint)(int)(short)LoadHalf((R[13] + d) & 0xFFFFFFFEu);
                        break;
                    }
                case MePOp.LwTp:
                    {
                        int rn = (int)MePIsa.Raw(word, 5, 3);
                        uint d = MePIsa.FieldValue(MePField.F7u9a4, word, pc);
                        R[rn] = LoadWord((R[13] + d) & 0xFFFFFFFCu);
                        break;
                    }
                case MePOp.LbuTp:
                    {
                        int rn = (int)MePIsa.Raw(word, 5, 3);
                        uint d = MePIsa.FieldValue(MePField.F7u9, word, pc);
                        R[rn] = LoadByte(R[13] + d);
                        break;
                    }
                case MePOp.LhuTp:
                    {
                        int rn = (int)MePIsa.Raw(word, 5, 3);
                        uint d = MePIsa.FieldValue(MePField.F7u9a2, word, pc);
                        R[rn] = LoadHalf((R[13] + d) & 0xFFFFFFFEu);
                        break;
                    }
                case MePOp.Sb16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint d = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        StoreByte(R[rm] + d, (byte)(R[rn] & 0xFF));
                        break;
                    }
                case MePOp.Sh16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint d = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        StoreHalf((R[rm] + d) & 0xFFFFFFFEu, (ushort)(R[rn] & 0xFFFF));
                        break;
                    }
                case MePOp.Sw16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint d = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        StoreWord((R[rm] + d) & 0xFFFFFFFCu, R[rn]);
                        break;
                    }
                case MePOp.Lb16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint d = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        R[rn] = (uint)(int)(sbyte)LoadByte(R[rm] + d);
                        break;
                    }
                case MePOp.Lh16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint d = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        R[rn] = (uint)(int)(short)LoadHalf((R[rm] + d) & 0xFFFFFFFEu);
                        break;
                    }
                case MePOp.Lw16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint d = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        R[rn] = LoadWord((R[rm] + d) & 0xFFFFFFFCu);
                        break;
                    }
                case MePOp.Lbu16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint d = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        R[rn] = LoadByte(R[rm] + d);
                        break;
                    }
                case MePOp.Lhu16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint d = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        R[rn] = LoadHalf((R[rm] + d) & 0xFFFFFFFEu);
                        break;
                    }
                case MePOp.Sw24:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint a = MePIsa.FieldValue(MePField.F24u8a4n, word, pc);
                        StoreWord(a, R[rn]);
                        break;
                    }
                case MePOp.Lw24:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint a = MePIsa.FieldValue(MePField.F24u8a4n, word, pc);
                        R[rn] = LoadWord(a);
                        break;
                    }

                // ------------------------------------------------- move/extend
                case MePOp.Extb: { int rn = (int)MePIsa.Raw(word, 4, 4); R[rn] = (uint)(int)(sbyte)(byte)R[rn]; break; }
                case MePOp.Exth: { int rn = (int)MePIsa.Raw(word, 4, 4); R[rn] = (uint)(int)(short)(ushort)R[rn]; break; }
                case MePOp.Extub: { int rn = (int)MePIsa.Raw(word, 4, 4); R[rn] = R[rn] & 0xFF; break; }
                case MePOp.Extuh: { int rn = (int)MePIsa.Raw(word, 4, 4); R[rn] = R[rn] & 0xFFFF; break; }

                case MePOp.Ssarb:
                    {
                        int rm = (int)MePIsa.Raw(word, 8, 4);
                        uint d = MePIsa.FieldValue(MePField.F2u6, word, pc);
                        Sar = 32u - (((R[rm] + d) & 3u) * 8u);
                        break;
                    }

                case MePOp.Mov:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = R[rm];
                        break;
                    }
                case MePOp.Movi8:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        R[rn] = MePIsa.Sext(MePIsa.Raw(word, 8, 8), 8);
                        break;
                    }
                case MePOp.Movi16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        R[rn] = MePIsa.Sext(MePIsa.Raw(word, 16, 16), 16);
                        break;
                    }
                case MePOp.Movu24:
                    {
                        int rn = (int)MePIsa.Raw(word, 5, 3);
                        R[rn] = MePIsa.FieldValue(MePField.F24u8n, word, pc);
                        break;
                    }
                case MePOp.Movu16:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        R[rn] = MePIsa.Raw(word, 16, 16);
                        break;
                    }
                case MePOp.Movh:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        R[rn] = MePIsa.Raw(word, 16, 16) << 16;
                        break;
                    }

                // ------------------------------------------------- arithmetic
                case MePOp.Add3:
                    {
                        int rl = (int)MePIsa.Raw(word, 12, 4), rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rl] = R[rn] + R[rm];
                        break;
                    }
                case MePOp.Add:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint s = MePIsa.Sext(MePIsa.Raw(word, 8, 6), 6);
                        R[rn] = R[rn] + s;
                        break;
                    }
                case MePOp.Add3i:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint d = MePIsa.FieldValue(MePField.F7u9a4, word, pc);
                        R[rn] = R[15] + d;
                        break;
                    }
                case MePOp.Advck3:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[0] = SignedAddOverflow(R[rn], R[rm]) ? 1u : 0u;
                        break;
                    }
                case MePOp.Sub:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = R[rn] - R[rm];
                        break;
                    }
                case MePOp.Sbvck3:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[0] = SignedSubOverflow(R[rn], R[rm]) ? 1u : 0u;
                        break;
                    }
                case MePOp.Neg:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = (uint)(-(int)R[rm]);
                        break;
                    }
                case MePOp.Slt3:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[0] = ((int)R[rn] < (int)R[rm]) ? 1u : 0u;
                        break;
                    }
                case MePOp.Sltu3:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[0] = (R[rn] < R[rm]) ? 1u : 0u;
                        break;
                    }
                case MePOp.Slt3i:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint v = MePIsa.Raw(word, 8, 5);
                        R[0] = ((int)R[rn] < (int)v) ? 1u : 0u;
                        break;
                    }
                case MePOp.Sltu3i:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint v = MePIsa.Raw(word, 8, 5);
                        R[0] = (R[rn] < v) ? 1u : 0u;
                        break;
                    }
                case MePOp.Sl1ad3:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[0] = (R[rn] << 1) + R[rm];
                        break;
                    }
                case MePOp.Sl2ad3:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[0] = (R[rn] << 2) + R[rm];
                        break;
                    }
                case MePOp.Add3x:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint s = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        R[rn] = R[rm] + s;
                        break;
                    }
                case MePOp.Slt3x:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint s = MePIsa.FieldValue(MePField.F16s16, word, pc);
                        R[rn] = ((int)R[rm] < (int)s) ? 1u : 0u;
                        break;
                    }
                case MePOp.Sltu3x:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint s = MePIsa.Raw(word, 16, 16);
                        R[rn] = (R[rm] < s) ? 1u : 0u;
                        break;
                    }

                // ------------------------------------------------- logical
                case MePOp.Or: { int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4); R[rn] |= R[rm]; break; }
                case MePOp.And: { int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4); R[rn] &= R[rm]; break; }
                case MePOp.Xor: { int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4); R[rn] ^= R[rm]; break; }
                case MePOp.Nor: { int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4); R[rn] = ~(R[rn] | R[rm]); break; }
                case MePOp.Or3: { int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4); R[rn] = R[rm] | MePIsa.Raw(word, 16, 16); break; }
                case MePOp.And3: { int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4); R[rn] = R[rm] & MePIsa.Raw(word, 16, 16); break; }
                case MePOp.Xor3: { int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4); R[rn] = R[rm] ^ MePIsa.Raw(word, 16, 16); break; }

                // ------------------------------------------------- shifts
                case MePOp.Sra:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = (uint)((int)R[rn] >> (int)(R[rm] & 0x1F));
                        break;
                    }
                case MePOp.Srl:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = R[rn] >> (int)(R[rm] & 0x1F);
                        break;
                    }
                case MePOp.Sll:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = R[rn] << (int)(R[rm] & 0x1F);
                        break;
                    }
                case MePOp.Srai: { int rn = (int)MePIsa.Raw(word, 4, 4); R[rn] = (uint)((int)R[rn] >> (int)MePIsa.Raw(word, 8, 5)); break; }
                case MePOp.Srli: { int rn = (int)MePIsa.Raw(word, 4, 4); R[rn] = R[rn] >> (int)MePIsa.Raw(word, 8, 5); break; }
                case MePOp.Slli: { int rn = (int)MePIsa.Raw(word, 4, 4); R[rn] = R[rn] << (int)MePIsa.Raw(word, 8, 5); break; }
                case MePOp.Sll3: { int rn = (int)MePIsa.Raw(word, 4, 4); R[0] = R[rn] << (int)MePIsa.Raw(word, 8, 5); break; }
                case MePOp.Fsft:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        int sh = (int)(Sar & 0x3F);
                        ulong tmp = ((ulong)R[rn] << 32) | R[rm];
                        tmp = sh >= 64 ? 0 : (tmp << sh);
                        R[rn] = (uint)(tmp >> 32);
                        break;
                    }

                // ------------------------------------------------- branches
                case MePOp.Bra:
                    Pc = MePIsa.FieldValue(MePField.F12s4a2, word, pc) & 0xFFFFFFFEu;
                    break;
                case MePOp.Beqz:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        if (R[rn] == 0) Pc = MePIsa.FieldValue(MePField.F8s8a2, word, pc) & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.Bnez:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        if (R[rn] != 0) Pc = MePIsa.FieldValue(MePField.F8s8a2, word, pc) & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.Beqi:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        if (R[rn] == MePIsa.Raw(word, 8, 4)) Pc = MePIsa.FieldValue(MePField.F17s16a2, word, pc) & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.Bnei:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        if (R[rn] != MePIsa.Raw(word, 8, 4)) Pc = MePIsa.FieldValue(MePField.F17s16a2, word, pc) & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.Blti:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        if ((int)R[rn] < (int)MePIsa.Raw(word, 8, 4)) Pc = MePIsa.FieldValue(MePField.F17s16a2, word, pc) & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.Bgei:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        if ((int)R[rn] >= (int)MePIsa.Raw(word, 8, 4)) Pc = MePIsa.FieldValue(MePField.F17s16a2, word, pc) & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.Beq:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        if (R[rn] == R[rm]) Pc = MePIsa.FieldValue(MePField.F17s16a2, word, pc) & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.Bne:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        if (R[rn] != R[rm]) Pc = MePIsa.FieldValue(MePField.F17s16a2, word, pc) & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.Bsr12:
                    Lp = (pc + 2) | 1u;
                    Pc = MePIsa.FieldValue(MePField.F12s4a2, word, pc) & 0xFFFFFFFEu;
                    break;
                case MePOp.Bsr24:
                    Lp = (pc + 4) | 1u;
                    Pc = MePIsa.FieldValue(MePField.F24s5a2n, word, pc) & 0xFFFFFFFEu;
                    break;
                case MePOp.Jmp:
                    {
                        int rm = (int)MePIsa.Raw(word, 8, 4);
                        uint t = R[rm];
                        if ((t & 1u) != 0 && Profile == MePProfile.Venezia)
                        {
                            // toggle between core and VLIW operation mode
                            bool om = (Psw & (1u << 12)) != 0;
                            if (om) { Psw &= ~(1u << 12); Pc = t & 0xFFFFFFFEu; }
                            else { Psw |= (1u << 12); Pc = (t & 0xFFFFFFFCu); }
                            VliwMode = (Psw & (1u << 12)) != 0;
                        }
                        else
                        {
                            Pc = t & 0xFFFFFFFEu;
                        }
                        break;
                    }
                case MePOp.Jmp24:
                    Pc = (pc & 0xF0000000u) | (MePIsa.FieldValue(MePField.F24u5a2n, word, pc) & 0xFFFFFFFEu);
                    break;
                case MePOp.Jsr:
                    {
                        int rm = (int)MePIsa.Raw(word, 8, 4);
                        Lp = (pc + 2) | 1u;
                        Pc = R[rm] & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.Ret:
                    {
                        if ((Lp & 1u) != 0 && Profile == MePProfile.Venezia)
                        {
                            bool om = (Psw & (1u << 12)) != 0;
                            if (om) { Psw &= ~(1u << 12); Pc = Lp & 0xFFFFFFFEu; }
                            else { Psw |= (1u << 12); Pc = Lp & 0xFFFFFFFCu; }
                            VliwMode = (Psw & (1u << 12)) != 0;
                        }
                        else
                        {
                            Pc = Lp & 0xFFFFFFFEu;
                        }
                        break;
                    }
                case MePOp.Jsrv:
                    {
                        int rm = (int)MePIsa.Raw(word, 8, 4);
                        Lp = (pc + 2) | 1u;
                        Psw |= (1u << 12);
                        VliwMode = true;
                        Pc = R[rm] & 0xFFFFFFFCu;
                        break;
                    }
                case MePOp.Bsrv:
                    Lp = (pc + 4) | 1u;
                    Psw |= (1u << 12);
                    VliwMode = true;
                    Pc = MePIsa.FieldValue(MePField.F24s5a2n, word, pc) & 0xFFFFFFFCu;
                    break;

                // ------------------------------------------------- repeat
                case MePOp.Repeat:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        Rpb = pc + 4;
                        Rpe = MePIsa.FieldValue(MePField.F17s16a2, word, pc) & 0xFFFFFFFEu;
                        Rpc = R[rn];
                        _repActive = true;
                        _repPendingBack = false;
                        _repEndless = false;
                        break;
                    }
                case MePOp.Erepeat:
                    Rpb = pc + 4;
                    Rpe = (MePIsa.FieldValue(MePField.F17s16a2, word, pc) & 0xFFFFFFFEu) | 1u;
                    Rpc = 0;
                    _repActive = true;
                    _repPendingBack = false;
                    _repEndless = true;
                    break;

                // ------------------------------------------------- control regs
                case MePOp.Stc:
                case MePOp.StcLp:
                case MePOp.StcHi:
                case MePOp.StcLo:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        int csr = (int)MePIsa.FieldValue(MePField.FCsrn, word, pc);
                        SetCsr(csr, R[rn]);
                        break;
                    }
                case MePOp.Ldc:
                case MePOp.LdcLp:
                case MePOp.LdcHi:
                case MePOp.LdcLo:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        int csr = (int)MePIsa.FieldValue(MePField.FCsrn, word, pc);
                        if (csr == 0) R[rn] = pc + 4;      // reading the PC
                        else R[rn] = GetCsr(csr);
                        break;
                    }
                case MePOp.Di: Psw &= ~1u; break;
                case MePOp.Ei: Psw |= 1u; break;
                case MePOp.Reti:
                    {
                        if ((Psw & (1u << 9)) != 0)
                        {
                            Pc = Npc & 0xFFFFFFFEu;
                            Psw &= ~(1u << 9);
                        }
                        else
                        {
                            Pc = Epc & 0xFFFFFFFEu;
                        }
                        break;
                    }
                case MePOp.Halt:
                    Psw |= (1u << 11);
                    Fault("halt instruction at 0x" + pc.ToString("x", CultureInfo.InvariantCulture));
                    break;
                case MePOp.Sleep:
                    Psw |= (1u << 11);
                    Fault("sleep instruction at 0x" + pc.ToString("x", CultureInfo.InvariantCulture));
                    break;
                case MePOp.Swi:
                    {
                        uint lvl = MePIsa.Raw(word, 10, 2);
                        Exc |= (1u << (4 + (int)lvl));
                        break;
                    }
                case MePOp.Break:
                    Fault("break exception at 0x" + pc.ToString("x", CultureInfo.InvariantCulture));
                    break;
                case MePOp.Syncm: break;
                case MePOp.Synccp: break;

                case MePOp.Stcb:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint a = MePIsa.Raw(word, 16, 16);
                        CopBus("stcb", 0, a, 0, R[rn], false);
                        break;
                    }
                case MePOp.Ldcb:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint a = MePIsa.Raw(word, 16, 16);
                        R[rn] = CopBus("ldcb", 0, a, 0, 0, true);
                        break;
                    }
                case MePOp.StcbR:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        CopBus("stcb", 0, R[rm] & 0xFFFFu, 0, R[rn], false);
                        break;
                    }
                case MePOp.LdcbR:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = CopBus("ldcb", 0, R[rm] & 0xFFFFu, 0, 0, true);
                        break;
                    }

                // ------------------------------------------------- bit ops
                case MePOp.Bsetm:
                case MePOp.Bclrm:
                case MePOp.Bnotm:
                case MePOp.Btstm:
                    {
                        int rm = (int)MePIsa.Raw(word, 8, 4);
                        uint bit = MePIsa.Raw(word, 5, 3);
                        byte b = LoadByte(R[rm]);
                        if (ins.Op == MePOp.Bsetm) StoreByte(R[rm], (byte)(b | (1u << (int)bit)));
                        else if (ins.Op == MePOp.Bclrm) StoreByte(R[rm], (byte)(b & ~(1u << (int)bit)));
                        else if (ins.Op == MePOp.Bnotm) StoreByte(R[rm], (byte)(b ^ (1u << (int)bit)));
                        else R[0] = (uint)(b & (1u << (int)bit));
                        break;
                    }
                case MePOp.Tas:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        byte b = LoadByte(R[rm]);
                        StoreByte(R[rm], 1);
                        R[rn] = b;
                        break;
                    }
                case MePOp.Cache:
                case MePOp.Pref:
                case MePOp.Prefd:
                    CopBus("cache", 0, pc, 0, 0, false);
                    break;

                // ------------------------------------------------- multiply
                case MePOp.Mul:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        ulong p = (ulong)(long)(int)R[rn] * (ulong)(long)(int)R[rm];
                        Hi = (uint)(p >> 32); Lo = (uint)p;
                        break;
                    }
                case MePOp.Mulu:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        ulong p = (ulong)R[rn] * R[rm];
                        Hi = (uint)(p >> 32); Lo = (uint)p;
                        break;
                    }
                case MePOp.Mulr:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        ulong p = (ulong)(long)(int)R[rn] * (ulong)(long)(int)R[rm];
                        Hi = (uint)(p >> 32); Lo = (uint)p; R[rn] = Lo;
                        break;
                    }
                case MePOp.Mulru:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        ulong p = (ulong)R[rn] * R[rm];
                        Hi = (uint)(p >> 32); Lo = (uint)p; R[rn] = Lo;
                        break;
                    }
                case MePOp.Madd:
                case MePOp.Maddu:
                case MePOp.Maddr:
                case MePOp.Maddru:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        ulong acc = ((ulong)Hi << 32) | Lo;
                        ulong p;
                        if (ins.Op == MePOp.Madd || ins.Op == MePOp.Maddr)
                            p = (ulong)(long)(int)R[rn] * (ulong)(long)(int)R[rm];
                        else
                            p = (ulong)R[rn] * R[rm];
                        acc += p;
                        Hi = (uint)(acc >> 32); Lo = (uint)acc;
                        if (ins.Op == MePOp.Maddr || ins.Op == MePOp.Maddru) R[rn] = Lo;
                        break;
                    }
                case MePOp.Div:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        if (R[rm] == 0) { Fault("divide by zero at 0x" + pc.ToString("x", CultureInfo.InvariantCulture)); break; }
                        if (R[rn] == 0x80000000u && R[rm] == 0xFFFFFFFFu) { Lo = 0x80000000u; Hi = 0; break; }
                        Lo = (uint)((int)R[rn] / (int)R[rm]);
                        Hi = (uint)((int)R[rn] % (int)R[rm]);
                        break;
                    }
                case MePOp.Divu:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        if (R[rm] == 0) { Fault("divide by zero at 0x" + pc.ToString("x", CultureInfo.InvariantCulture)); break; }
                        Lo = R[rn] / R[rm];
                        Hi = R[rn] % R[rm];
                        break;
                    }

                // ------------------------------------------------- debug/misc
                case MePOp.Dret: Pc = Depc & 0xFFFFFFFEu; break;
                case MePOp.Dbreak: break;
                case MePOp.Ldz:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        uint v = R[rm]; int n = 32;
                        while (n > 0 && (v & 0x80000000u) == 0) { v <<= 1; n--; }
                        R[rn] = (uint)n;
                        break;
                    }
                case MePOp.Abs:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        int d = (int)R[rn] - (int)R[rm];
                        R[rn] = (uint)(d < 0 ? -d : d);
                        break;
                    }
                case MePOp.Ave:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = (uint)(((int)R[rn] + (int)R[rm] + 1) >> 1);
                        break;
                    }
                case MePOp.Min:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        if ((int)R[rn] > (int)R[rm]) R[rn] = R[rm];
                        break;
                    }
                case MePOp.Max:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        if ((int)R[rn] < (int)R[rm]) R[rn] = R[rm];
                        break;
                    }
                case MePOp.Minu:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        if (R[rn] > R[rm]) R[rn] = R[rm];
                        break;
                    }
                case MePOp.Maxu:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        if (R[rn] < R[rm]) R[rn] = R[rm];
                        break;
                    }
                case MePOp.Clip:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint n = MePIsa.Raw(word, 24, 5);
                        if (n == 0) { R[rn] = 0; break; }
                        int max = (1 << ((int)n - 1)) - 1, min = -(1 << ((int)n - 1));
                        int v = (int)R[rn];
                        if (v > max) v = max; else if (v < min) v = min;
                        R[rn] = (uint)v;
                        break;
                    }
                case MePOp.Clipu:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4);
                        uint n = MePIsa.Raw(word, 24, 5);
                        if (n == 0) { R[rn] = 0; break; }
                        uint max = (n >= 32) ? 0xFFFFFFFFu : ((1u << (int)n) - 1u);
                        if (R[rn] > max) R[rn] = max;
                        break;
                    }
                case MePOp.Sadd:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        long s = (long)(int)R[rn] + (int)R[rm];
                        if (s > int.MaxValue) R[rn] = 0x7FFFFFFFu;
                        else if (s < int.MinValue) R[rn] = 0x80000000u;
                        else R[rn] = (uint)(int)s;
                        break;
                    }
                case MePOp.Ssub:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        long s = (long)(int)R[rn] - (int)R[rm];
                        if (s > int.MaxValue) R[rn] = 0x7FFFFFFFu;
                        else if (s < int.MinValue) R[rn] = 0x80000000u;
                        else R[rn] = (uint)(int)s;
                        break;
                    }
                case MePOp.Saddu:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        ulong s = (ulong)R[rn] + R[rm];
                        R[rn] = (s > 0xFFFFFFFFu) ? 0xFFFFFFFFu : (uint)s;
                        break;
                    }
                case MePOp.Ssubu:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        R[rn] = (R[rn] < R[rm]) ? 0u : R[rn] - R[rm];
                        break;
                    }

                // ------------------------------------------------- coprocessor
                case MePOp.Bcpeq:
                case MePOp.Bcpne:
                case MePOp.Bcpat:
                case MePOp.Bcpaf:
                    {
                        uint mask = MePIsa.Raw(word, 8, 4);
                        bool take;
                        if (ins.Op == MePOp.Bcpeq) take = ((mask ^ Cr0) == 0);
                        else if (ins.Op == MePOp.Bcpne) take = ((mask ^ Cr0) != 0);
                        else if (ins.Op == MePOp.Bcpat) take = ((mask & Cr0) != 0);
                        else take = ((mask & Cr0) == 0);
                        if (take) Pc = MePIsa.FieldValue(MePField.F17s16a2, word, pc) & 0xFFFFFFFEu;
                        break;
                    }
                case MePOp.SimSyscall:
                    Emit("cpu", "[MeP-c5] simulator syscall 0x" + MePIsa.Raw(word, 5, 4).ToString("x") +
                                " at 0x" + pc.ToString("x", CultureInfo.InvariantCulture));
                    break;

                // ------------------------------------------------- cp loads/stores
                case MePOp.Swcp:
                case MePOp.Lwcp:
                case MePOp.Swcpi:
                case MePOp.Lwcpi:
                case MePOp.Swcp16:
                case MePOp.Lwcp16:
                case MePOp.Sbcpa:
                case MePOp.Lbcpa:
                case MePOp.Shcpa:
                case MePOp.Lhcpa:
                case MePOp.Swcpa:
                case MePOp.Lwcpa:
                case MePOp.Sbcpm0:
                case MePOp.Lbcpm0:
                case MePOp.Shcpm0:
                case MePOp.Lhcpm0:
                case MePOp.Swcpm0:
                case MePOp.Lwcpm0:
                case MePOp.Sbcpm1:
                case MePOp.Lbcpm1:
                case MePOp.Shcpm1:
                case MePOp.Lhcpm1:
                case MePOp.Swcpm1:
                case MePOp.Lwcpm1:
                    ExecuteCopWord(ins, word, pc);
                    break;

                case MePOp.Smcp:
                case MePOp.Lmcp:
                case MePOp.Smcpi:
                case MePOp.Lmcpi:
                case MePOp.Smcp16:
                case MePOp.Lmcp16:
                case MePOp.Smcpa:
                case MePOp.Lmcpa:
                case MePOp.Smcpm0:
                case MePOp.Lmcpm0:
                case MePOp.Smcpm1:
                case MePOp.Lmcpm1:
                    ExecuteCopWord64(ins, word, pc);
                    break;

                case MePOp.Casb3:
                case MePOp.Cash3:
                case MePOp.Casw3:
                    {
                        int rn = (int)MePIsa.Raw(word, 4, 4), rm = (int)MePIsa.Raw(word, 8, 4);
                        int rl = (int)MePIsa.FieldValue(MePField.FRl5, word, pc);
                        uint a = R[rm];
                        if (ins.Op == MePOp.Casb3)
                        {
                            byte old = LoadByte(a);
                            if (old == (byte)(R[rn] & 0xFF)) StoreByte(a, (byte)(R[rl] & 0xFF));
                            else R[rl] = old;
                        }
                        else if (ins.Op == MePOp.Cash3)
                        {
                            ushort old = LoadHalf(a & 0xFFFFFFFEu);
                            if (old == (ushort)(R[rn] & 0xFFFF)) StoreHalf(a & 0xFFFFFFFEu, (ushort)(R[rl] & 0xFFFF));
                            else R[rl] = old;
                        }
                        else
                        {
                            uint old = LoadWord(a & 0xFFFFFFFCu);
                            if (old == R[rn]) StoreWord(a & 0xFFFFFFFCu, R[rl]);
                            else R[rl] = old;
                        }
                        break;
                    }

                case MePOp.Uci:
                    Emit("cpu", "[MeP-c5] uci " + MePIsa.Format(ins, word, pc) + " (unimplemented)");
                    break;
                case MePOp.Dsp:
                case MePOp.Dsp0:
                case MePOp.Dsp1:
                    Emit("cpu", "[MeP-c5] " + MePIsa.Format(ins, word, pc) + " (unimplemented)");
                    break;

                default:
                    UndefinedInstruction = true;
                    Fault("unimplemented opcode " + ins.Op.ToString() + " (" + ins.Mnem + ") at 0x" +
                          pc.ToString("x", CultureInfo.InvariantCulture));
                    break;
            }
        }

        // ------------------------------------------------------- coprocessor

        private uint CopBus(string kind, int op, uint address, uint copReg, uint value, bool load)
        {
            if (CopHandler != null) return CopHandler.CopAccess(this, kind, op, address, copReg, value, load);
            if (load) return 0;
            return value;
        }

        private void ExecuteCopWord(MePInsn ins, uint word, uint pc)
        {
            int crn = (int)MePIsa.Raw(word, 4, 4);
            int rm = (int)MePIsa.Raw(word, 8, 4);
            uint baseAddr = R[rm];
            uint disp;
            switch (ins.Op)
            {
                case MePOp.Swcp:
                case MePOp.Lwcp:
                case MePOp.Swcpi:
                case MePOp.Lwcpi:
                    disp = 0;
                    break;
                case MePOp.Swcp16:
                case MePOp.Lwcp16:
                    disp = MePIsa.FieldValue(MePField.F16s16, word, pc);
                    break;
                case MePOp.Sbcp:
                case MePOp.Lbcp:
                case MePOp.Lbucp:
                case MePOp.Shcp:
                case MePOp.Lhcp:
                case MePOp.Lhucp:
                    disp = MePIsa.FieldValue(MePField.F12s20, word, pc);
                    break;
                default:
                    disp = MePIsa.FieldValue(MePField.FCdisp10, word, pc);
                    break;
            }

            int size = 4;
            bool load;
            switch (ins.Op)
            {
                case MePOp.Sbcp:
                case MePOp.Lbcp:
                case MePOp.Lbucp:
                case MePOp.Sbcpa:
                case MePOp.Lbcpa:
                case MePOp.Sbcpm0:
                case MePOp.Lbcpm0:
                case MePOp.Sbcpm1:
                case MePOp.Lbcpm1:
                    size = 1;
                    break;
                case MePOp.Shcp:
                case MePOp.Lhcp:
                case MePOp.Lhucp:
                case MePOp.Shcpa:
                case MePOp.Lhcpa:
                case MePOp.Shcpm0:
                case MePOp.Lhcpm0:
                case MePOp.Shcpm1:
                case MePOp.Lhcpm1:
                    size = 2;
                    break;
            }
            switch (ins.Op)
            {
                case MePOp.Lwcp:
                case MePOp.Lwcpi:
                case MePOp.Lwcp16:
                case MePOp.Lbcp:
                case MePOp.Lbucp:
                case MePOp.Lhcp:
                case MePOp.Lhucp:
                case MePOp.Lbcpa:
                case MePOp.Lhcpa:
                case MePOp.Lwcpa:
                case MePOp.Lbcpm0:
                case MePOp.Lhcpm0:
                case MePOp.Lwcpm0:
                case MePOp.Lbcpm1:
                case MePOp.Lhcpm1:
                case MePOp.Lwcpm1:
                    load = true;
                    break;
                default:
                    load = false;
                    break;
            }

            uint addr = baseAddr + disp;
            if (size == 2) addr &= 0xFFFFFFFEu;
            else if (size == 4) addr &= 0xFFFFFFFCu;

            if (load)
            {
                uint v = CopBus(size == 4 ? "lwcp" : (size == 2 ? "lhcp" : "lbcp"), size, addr, (uint)crn, 0, true);
                if (size == 1) v &= 0xFF;
                else if (size == 2) v &= 0xFFFF;
                R[crn] = v;
            }
            else
            {
                CopBus(size == 4 ? "swcp" : (size == 2 ? "shcp" : "sbcp"), size, addr, (uint)crn, R[crn], false);
            }

            // post-increment / modulo-addressing forms update $rma
            switch (ins.Op)
            {
                case MePOp.Swcpi:
                case MePOp.Lwcpi:
                    R[rm] = baseAddr + 4;
                    break;
                case MePOp.Sbcpa:
                case MePOp.Lbcpa:
                case MePOp.Sbcpm0:
                case MePOp.Lbcpm0:
                case MePOp.Sbcpm1:
                case MePOp.Lbcpm1:
                    R[rm] = baseAddr + 1;
                    break;
                case MePOp.Shcpa:
                case MePOp.Lhcpa:
                case MePOp.Shcpm0:
                case MePOp.Lhcpm0:
                case MePOp.Shcpm1:
                case MePOp.Lhcpm1:
                    R[rm] = baseAddr + 2;
                    break;
                case MePOp.Swcpa:
                case MePOp.Lwcpa:
                case MePOp.Swcpm0:
                case MePOp.Lwcpm0:
                case MePOp.Swcpm1:
                case MePOp.Lwcpm1:
                    R[rm] = baseAddr + 4;
                    break;
            }
        }

        private void ExecuteCopWord64(MePInsn ins, uint word, uint pc)
        {
            int crn = (int)MePIsa.Raw(word, 4, 4);
            int rm = (int)MePIsa.Raw(word, 8, 4);
            bool load = ins.Mnem.StartsWith("l", StringComparison.Ordinal);
            uint disp = 0;
            if (ins.Op == MePOp.Smcp16 || ins.Op == MePOp.Lmcp16)
                disp = MePIsa.FieldValue(MePField.F16s16, word, pc);
            else if (ins.Op != MePOp.Smcp && ins.Op != MePOp.Lmcp &&
                     ins.Op != MePOp.Smcpi && ins.Op != MePOp.Lmcpi)
                disp = MePIsa.FieldValue(MePField.FCdisp10, word, pc);
            uint addr = (R[rm] + disp) & 0xFFFFFFF8u;
            if (!load) CopBus("smcp", 8, addr, (uint)crn, R[crn], false);
            if (ins.Op == MePOp.Smcpa || ins.Op == MePOp.Lmcpa ||
                ins.Op == MePOp.Smcpm0 || ins.Op == MePOp.Lmcpm0 ||
                ins.Op == MePOp.Smcpm1 || ins.Op == MePOp.Lmcpm1 ||
                ins.Op == MePOp.Smcpi || ins.Op == MePOp.Lmcpi)
                R[rm] = R[rm] + 8;
        }

        // ------------------------------------------------------- helpers

        private static bool SignedAddOverflow(uint a, uint b)
        {
            int r = unchecked((int)a + (int)b);
            return (((a ^ (uint)r) & (b ^ (uint)r)) & 0x80000000u) != 0;
        }

        private static bool SignedSubOverflow(uint a, uint b)
        {
            int r = unchecked((int)a - (int)b);
            return (((a ^ (uint)r) & (~b ^ (uint)r)) & 0x80000000u) != 0;
        }

        // ------------------------------------------------------- public API

        public override string Disassemble(uint address, out int length)
        {
            Mem.CurrentPC = address;
            uint word = Mem.FetchWord(address);
            if (VliwMode && Profile == MePProfile.Venezia)
            {
                // Venezia VLIW packet: one core instruction plus coprocessor slots.
                MePInsn vi = MePIsa.Decode(word);
                uint hi = Mem.FetchWord(address + 4);
                length = 8;
                string core = vi != null ? MePIsa.Format(vi, word, address) : "*unknown*";
                return core + " ; ivc2=0x" + hi.ToString("x8", CultureInfo.InvariantCulture);
            }
            MePInsn ins = MePIsa.Decode(word);
            if (ins == null)
            {
                length = 4;
                return "*unknown*";
            }
            length = ins.Len;
            return MePIsa.Format(ins, word, address);
        }

        public override void GetRegisters(List<RegValue> regs)
        {
            for (int i = 0; i < 16; i++)
                regs.Add(new RegValue("GPR", MePIsa.RegName(i), R[i]));
            regs.Add(new RegValue("Core", "pc", Pc));
            regs.Add(new RegValue("Core", "hi", Hi));
            regs.Add(new RegValue("Core", "lo", Lo));
            regs.Add(new RegValue("Core", "sar", Sar));
            regs.Add(new RegValue("Core", "lp", Lp));
            regs.Add(new RegValue("Core", "epc", Epc));
            regs.Add(new RegValue("Core", "npc", Npc));
            regs.Add(new RegValue("Core", "rpb", Rpb));
            regs.Add(new RegValue("Core", "rpe", Rpe));
            regs.Add(new RegValue("Core", "rpc", Rpc));
            regs.Add(new RegValue("Core", "tmp", Tmp));
            regs.Add(new RegValue("Core", "psw", Psw));
            regs.Add(new RegValue("Core", "cond", CondPacked()));
            regs.Add(new RegValue("Core", "mb0", Mb0));
            regs.Add(new RegValue("Core", "me0", Me0));
            regs.Add(new RegValue("Core", "mb1", Mb1));
            regs.Add(new RegValue("Core", "me1", Me1));
            regs.Add(new RegValue("Core", "exc", Exc));
            regs.Add(new RegValue("Core", "opt", Opt));
        }

        private uint CondPacked()
        {
            uint v = 0;
            for (int i = 0; i < 8; i++) if (Cond[i]) v |= (1u << i);
            return v;
        }

        public override bool SetRegister(string name, uint value)
        {
            if (name == null) return false;
            string n = name.Trim();
            if (n.StartsWith("$", StringComparison.Ordinal)) n = n.Substring(1);
            string lower = n.ToLowerInvariant();

            int gpr = GprIndex(lower);
            if (gpr >= 0) { R[gpr] = value; return true; }

            switch (lower)
            {
                case "pc": Pc = value; return true;
                case "hi": Hi = value; return true;
                case "lo": Lo = value; return true;
                case "sar": Sar = value; return true;
                case "lp": Lp = value; return true;
                case "epc": Epc = value; return true;
                case "npc": Npc = value; return true;
                case "rpb": Rpb = value; return true;
                case "rpe": Rpe = value; return true;
                case "rpc": Rpc = value; return true;
                case "tmp": Tmp = value; return true;
                case "psw": Psw = value; VliwMode = (value & (1u << 12)) != 0; return true;
                case "cond":
                    for (int i = 0; i < 8; i++) Cond[i] = (value & (1u << i)) != 0;
                    return true;
            }
            return false;
        }

        public override bool GetRegister(string name, out uint value)
        {
            value = 0;
            if (name == null) return false;
            string n = name.Trim();
            if (n.StartsWith("$", StringComparison.Ordinal)) n = n.Substring(1);
            string lower = n.ToLowerInvariant();

            int gpr = GprIndex(lower);
            if (gpr >= 0) { value = R[gpr]; return true; }

            switch (lower)
            {
                case "pc": value = Pc; return true;
                case "hi": value = Hi; return true;
                case "lo": value = Lo; return true;
                case "sar": value = Sar; return true;
                case "lp": value = Lp; return true;
                case "epc": value = Epc; return true;
                case "npc": value = Npc; return true;
                case "rpb": value = Rpb; return true;
                case "rpe": value = Rpe; return true;
                case "rpc": value = Rpc; return true;
                case "tmp": value = Tmp; return true;
                case "psw": value = Psw; return true;
                case "cond": value = CondPacked(); return true;
            }
            return false;
        }

        private static int GprIndex(string n)
        {
            switch (n)
            {
                case "sp": return 15;
                case "tp": return 13;
                case "gp": return 14;
                case "fp": return 8;
            }
            if (n.Length >= 1 && n.Length <= 2)
            {
                int v;
                if (int.TryParse(n, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) && v >= 0 && v <= 15)
                    return v;
            }
            return -1;
        }

        public override string StatusLine
        {
            get
            {
                return Profile + (VliwMode ? " vliw" : " core") +
                       " pc=0x" + Pc.ToString("x8", CultureInfo.InvariantCulture) +
                       " psw=0x" + Psw.ToString("x8", CultureInfo.InvariantCulture) +
                       " cond=0x" + CondPacked().ToString("x2", CultureInfo.InvariantCulture) +
                       (Halted ? " HALTED(" + HaltReason + ")" : "");
            }
        }
    }
}
