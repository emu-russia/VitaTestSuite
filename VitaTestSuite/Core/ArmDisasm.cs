// ARMv7-A (Cortex-A9 MPCore) instruction disassembler for A32, T16 and T32.
using System;
using System.Globalization;
using System.Text;
using VitaTestSuite.Core;

namespace VitaTestSuite.Core
{
    public static class ArmDisasm
    {
        private static readonly State Shared = new State();

        public static string Disassemble(ArmCore core, uint address, bool thumb, out int length)
        {
            State s = Shared;
            s.Reset();
            s.Mem = core != null ? core.Mem : null;
            s.Thumb = thumb;
            s.Address = address;
            length = thumb ? 2 : 4;
            try
            {
                if (thumb) ThumbDecode(s, address, out length);
                else ArmDecode(s, address, out length);
            }
            catch (Exception)
            {
                length = thumb ? 2 : 4;
                s.Buf.Length = 0;
                s.S(thumb ? ".hword 0x" : ".word 0x");
                uint w = s.Mem != null ? (thumb ? FetchHalf(s, address) : FetchWord(s, address)) : 0u;
                s.Buf.Append(w.ToString(thumb ? "X4" : "X8", CultureInfo.InvariantCulture));
            }
            if (length != 2 && length != 4) length = thumb ? 2 : 4;
            return s.Buf.ToString();
        }

        public static string MnemonicOf(ArmCore core, uint address, bool thumb)
        {
            int len;
            string text = Disassemble(core, address, thumb, out len);
            int sp = text.IndexOf(' ');
            return sp < 0 ? text : text.Substring(0, sp);
        }

        public static string ConditionName(uint cond)
        {
            switch (cond)
            {
                case 0x0: return "eq";
                case 0x1: return "ne";
                case 0x2: return "cs";
                case 0x3: return "cc";
                case 0x4: return "mi";
                case 0x5: return "pl";
                case 0x6: return "vs";
                case 0x7: return "vc";
                case 0x8: return "hi";
                case 0x9: return "ls";
                case 0xA: return "ge";
                case 0xB: return "lt";
                case 0xC: return "gt";
                case 0xD: return "le";
                case 0xE: return "";
                default: return "??";
            }
        }

        internal sealed class State
        {
            public readonly StringBuilder Buf = new StringBuilder(80);
            public MemoryHub Mem;
            public bool Thumb;
            public uint Address;
            public uint Cond = 0xEu;
            public bool HasCond;
            public uint Insn;

            public void Reset() { Buf.Length = 0; Address = 0; Cond = 0xEu; HasCond = false; Insn = 0; }
            public void S(string t) { Buf.Append(t); }
            public void C(char c) { Buf.Append(c); }
            public void N(uint v) { Buf.Append(v.ToString(CultureInfo.InvariantCulture)); }
            public void Hx(uint v) { Buf.Append("0x"); Buf.Append(v.ToString("X", CultureInfo.InvariantCulture)); }
            public void Imm(uint v)
            {
                Buf.Append('#');
                if (v >= 0x80000000u) { Buf.Append("-0x"); Buf.Append(((uint)(-(int)v)).ToString("X", CultureInfo.InvariantCulture)); }
                else { Buf.Append("0x"); Buf.Append(v.ToString("X", CultureInfo.InvariantCulture)); }
            }
            public void Reg(int n) { Buf.Append('r'); Buf.Append(n.ToString(CultureInfo.InvariantCulture)); }
            public void RegList(uint mask)
            {
                Buf.Append('{');
                bool first = true;
                int i = 0;
                while (i < 16)
                {
                    if ((mask & (1u << i)) == 0) { i++; continue; }
                    int j = i;
                    while (j + 1 < 16 && (mask & (1u << (j + 1))) != 0) j++;
                    if (!first) Buf.Append(", ");
                    first = false;
                    Buf.Append('r'); N((uint)i);
                    if (j > i) { Buf.Append("-r"); N((uint)j); }
                    i = j + 1;
                }
                Buf.Append('}');
            }
            public void Shift(int type, int amount)
            {
                if (type == 0 && amount == 0) return;
                switch (type)
                {
                    case 0: S(", lsl #"); N((uint)amount); break;
                    case 1: S(", lsr #"); N((uint)(amount == 0 ? 32 : amount)); break;
                    case 2: S(", asr #"); N((uint)(amount == 0 ? 32 : amount)); break;
                    default:
                        if (amount == 0) S(", rrx");
                        else { S(", ror #"); N((uint)amount); }
                        break;
                }
            }
            public void ShiftReg(int type, int rs)
            {
                switch (type)
                {
                    case 0: S(", lsl "); Reg(rs); break;
                    case 1: S(", lsr "); Reg(rs); break;
                    case 2: S(", asr "); Reg(rs); break;
                    default: S(", ror "); Reg(rs); break;
                }
            }
            public void SFlag(bool v) { if (v) C('s'); }
            public void CondSuffix() { if (HasCond) S(ConditionName(Cond)); }
        }

        private static uint FetchWord(State s, uint addr)
        {
            if (s.Mem == null) return 0;
            s.Mem.CurrentPC = addr;
            return s.Mem.ReadWord(addr & 0xFFFFFFFCu);
        }

        private static uint FetchHalf(State s, uint addr)
        {
            if (s.Mem == null) return 0;
            s.Mem.CurrentPC = addr;
            return s.Mem.ReadHalf(addr & 0xFFFFFFFEu);
        }

        private static uint Imm12(uint instr)
        {
            uint imm = instr & 0xFFu;
            uint rot = ((instr >> 8) & 0xFu) * 2u;
            if (rot == 0) return imm;
            return (imm >> (int)rot) | (imm << (32 - (int)rot));
        }

        // ================================================================ A32

        private static void ArmDecode(State s, uint addr, out int length)
        {
            length = 4;
            uint instr = FetchWord(s, addr);
            s.Insn = instr;
            uint cond = instr >> 28;
            s.Cond = cond;
            s.HasCond = cond != 0xEu && cond != 0xFu;

            if (cond == 0xFu) { ArmUnconditional(s, addr, instr); return; }

            uint blk = (instr >> 25) & 7u;
            uint op = (instr >> 21) & 0xFu;

            if (blk == 5u) { ArmBranch(s, addr, instr); return; }

            // A conditional instruction with bits [27:24] == 1111 is SVC.  The
            // coprocessor space only occupies 110x (0xEC/0xED) and 1110 (0xEE);
            // 1111 belongs to SVC (and to the 0xF... unconditional space, which
            // is handled above).
            if ((instr & 0x0F000000u) == 0x0F000000u)
            {
                s.S("svc"); s.CondSuffix(); s.C(' ');
                s.Imm(instr & 0xFFFFFFu);
                return;
            }

            if (blk == 6u || blk == 7u) { ArmCoprocessor(s, addr, instr); return; }
            if (blk == 4u) { ArmLoadStoreMultiple(s, addr, instr); return; }

            if (blk == 2u || blk == 3u)
            {
                // 010 = load/store with immediate offset, 011 = load/store with
                // register offset.  The DSP/SIMD space that shares op1 == 011 is
                // selected by bit 4, which is a fixed 0 in the load/store form.
                if (blk == 3u && (instr & 0x10u) != 0u) { ArmMedia(s, addr, instr); return; }
                ArmLoadStore(s, addr, instr, blk == 2u);
                return;
            }

            if (blk == 1u)
            {
                if (op >= 0xAu && ((instr >> 12) & 0xFu) == 0xFu) { ArmMsrImm(s, instr); return; }
                ArmDataProcessing(s, addr, instr, true);
                return;
            }

            // 000x: H1 extra load/store (bits [7:4] in {1011, 1101, 1111}),
            // multiply/swap (bits [7:4] == 1001), the DSP media space
            // (00010xx0), the MISC space, then data processing.
            uint ga = (instr >> 20) & 0xFFu;
            uint nib = instr & 0xF0u;
            if (nib == 0xB0u || nib == 0xD0u || nib == 0xF0u) { ArmExtraLoadStore(s, addr, instr); return; }
            if (nib == 0x90u) { ArmMul(s, addr, instr); return; }
            if (ga >= 0x10u && ga <= 0x17u && (ga & 1u) == 0u &&
                (nib == 0x50u || (nib & 0x90u) == 0x80u))
            { ArmMedia(s, addr, instr); return; }
            if ((instr & 0x0FFF0FF0u) == 0x016F0F10u) { ArmClz(s, instr); return; }
            if (ga >= 0x10u && ga <= 0x17u && IsArmMiscEncoding(instr)) { ArmMisc(s, addr, instr); return; }
            ArmDataProcessing(s, addr, instr, false);
        }

        private static void ArmDataProcessing(State s, uint addr, uint instr, bool immediate)
        {
            uint op = (instr >> 21) & 0xFu;
            bool sf = (instr & (1u << 20)) != 0;
            int rn = (int)((instr >> 16) & 0xFu);
            int rd = (int)((instr >> 12) & 0xFu);

            string name;
            switch (op)
            {
                case 0x0u: name = "and"; break;
                case 0x1u: name = "eor"; break;
                case 0x2u: name = "sub"; break;
                case 0x3u: name = "rsb"; break;
                case 0x4u: name = "add"; break;
                case 0x5u: name = "adc"; break;
                case 0x6u: name = "sbc"; break;
                case 0x7u: name = "rsc"; break;
                case 0x8u: name = "tst"; break;
                case 0x9u: name = "teq"; break;
                case 0xAu: name = "cmp"; break;
                case 0xBu: name = "cmn"; break;
                case 0xCu: name = "orr"; break;
                case 0xDu: name = "mov"; break;
                case 0xEu: name = "bic"; break;
                default: name = "mvn"; break;
            }

            bool test = op >= 8u && op <= 0xBu;
            bool noRn = op == 0xDu || op == 0xFu;
            bool adr = (op == 4u || op == 2u) && !sf && rn == 15 && immediate;

            // A32 MOVW / MOVT: opcode 1000 / 1010 with S == 0.
            if (immediate && !sf && (op == 0x8u || op == 0xAu) && rd != 15)
            {
                uint imm16 = ((uint)rn << 12) | (instr & 0xFFFu);
                s.S(op == 0x8u ? "movw" : "movt");
                s.CondSuffix(); s.C(' ');
                s.Reg(rd); s.S(", ");
                if (op == 0x8u) s.Imm(imm16); else s.Hx(imm16);
                return;
            }

            s.S(name);
            if (sf && !test) s.SFlag(true);
            s.CondSuffix();
            s.C(' ');

            if (adr)
            {
                uint imm = Imm12(instr);
                s.Reg(rd); s.S(", #");
                s.Hx(op == 4u ? addr + 8u + imm : addr + 8u - imm);
                return;
            }
            if (test) { s.Reg(rn); s.S(", "); Operand2(s, instr, immediate); return; }
            s.Reg(rd);
            if (!noRn) { s.S(", "); s.Reg(rn); }
            s.S(", ");
            Operand2(s, instr, immediate);
        }

        private static void Operand2(State s, uint instr, bool immediate)
        {
            if (immediate) { s.Imm(Imm12(instr)); return; }
            s.Reg((int)(instr & 0xFu));
            if ((instr & 0x10u) == 0) s.Shift((int)((instr >> 5) & 3u), (int)((instr >> 7) & 0x1Fu));
            else s.ShiftReg((int)((instr >> 5) & 3u), (int)((instr >> 8) & 0xFu));
        }

        private static void ArmMsrImm(State s, uint instr)
        {
            s.S("msr"); s.CondSuffix(); s.C(' ');
            Fields(s, instr); s.S(", "); s.Imm(Imm12(instr));
        }

        private static void Fields(State s, uint instr)
        {
            bool spsr = (instr & (1u << 22)) != 0;
            uint f = (instr >> 16) & 0xFu;
            s.S(spsr ? "spsr_" : "cpsr_");
            if ((f & 8u) != 0) s.C('f');
            if ((f & 4u) != 0) s.C('s');
            if ((f & 2u) != 0) s.C('x');
            if ((f & 1u) != 0) s.C('c');
        }

        // ---- multiply / swap ----------------------------------------------------

        private static void ArmMul(State s, uint addr, uint instr)
        {
            uint op = (instr >> 21) & 0xFu;
            bool sf = (instr & (1u << 20)) != 0;
            int rdLo = (int)((instr >> 12) & 0xFu);
            int rdHi = (int)((instr >> 16) & 0xFu);
            int rs = (int)((instr >> 8) & 0xFu);
            int rm = (int)(instr & 0xFu);

            // SWP / SWPB live in the same encoding space as MUL.
            if ((instr & 0x0FB00F90u) == 0x01000090u)
            {
                s.S(((instr & (1u << 22)) != 0) ? "swpb" : "swp");
                s.CondSuffix(); s.C(' ');
                s.Reg(rdLo); s.S(", "); s.Reg(rm); s.S(", ["); s.Reg(rdHi); s.C(']');
                return;
            }

            switch (op)
            {
                case 0x0u:
                    s.S("mul"); s.SFlag(sf); s.CondSuffix(); s.C(' ');
                    s.Reg(rdHi); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                    return;
                case 0x2u:
                    s.S("mla"); s.SFlag(sf); s.CondSuffix(); s.C(' ');
                    s.Reg(rdHi); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs); s.S(", "); s.Reg(rdLo);
                    return;
                case 0x4u:
                    s.S("umaal"); s.CondSuffix(); s.C(' ');
                    s.Reg(rdLo); s.S(", "); s.Reg(rdHi); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                    return;
                case 0x6u:
                    s.S("mls"); s.CondSuffix(); s.C(' ');
                    s.Reg(rdHi); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs); s.S(", "); s.Reg(rdLo);
                    return;
                case 0x8u: case 0x9u:
                    s.S("umull"); s.SFlag(sf); s.CondSuffix(); s.C(' ');
                    s.Reg(rdLo); s.S(", "); s.Reg(rdHi); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                    return;
                case 0xAu: case 0xBu:
                    s.S("umlal"); s.SFlag(sf); s.CondSuffix(); s.C(' ');
                    s.Reg(rdLo); s.S(", "); s.Reg(rdHi); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                    return;
                case 0xCu: case 0xDu:
                    s.S("smull"); s.SFlag(sf); s.CondSuffix(); s.C(' ');
                    s.Reg(rdLo); s.S(", "); s.Reg(rdHi); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                    return;
                default:
                    s.S("smlal"); s.SFlag(sf); s.CondSuffix(); s.C(' ');
                    s.Reg(rdLo); s.S(", "); s.Reg(rdHi); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                    return;
            }
        }

        // ---- media / DSP --------------------------------------------------------

        private static void ArmMedia(State s, uint addr, uint instr)
        {
            int rd = (int)((instr >> 12) & 0xFu);
            int rn = (int)((instr >> 16) & 0xFu);
            int rs = (int)((instr >> 8) & 0xFu);
            int rm = (int)(instr & 0xFu);
            uint g = (instr >> 20) & 0xFFu;
            uint o2 = (instr >> 5) & 7u;
            bool bit20 = (instr & (1u << 20)) != 0;
            bool bit22 = (instr & (1u << 22)) != 0;
            bool bit5 = (instr & 0x20u) != 0;
            bool bit6 = (instr & 0x40u) != 0;

            if (g == 0x10u && bit5 && o2 == 0u && (instr & 0x80u) != 0u && rdLoIsZero(instr))
            {
                s.S(bit22 ? "swpb" : "swp"); s.CondSuffix(); s.C(' ');
                s.Reg(rd); s.S(", "); s.Reg(rm); s.S(", ["); s.Reg(rn); s.C(']');
                return;
            }

            // ---- 00010xx0: signed multiply / saturating add-subtract families ----
            // Field map: bits [19:16] is Rd (or RdHi), bits [15:12] is Rn (or
            // RdLo), bits [11:8] is Rs, bits [3:0] is Rm, bit 5 is x and bit 6
            // is y for the halfword-select suffixes.
            uint nibA = instr & 0xF0u;
            if (g >= 0x10u && g <= 0x17u && (g & 1u) == 0u)
            {
                bool xb = (instr & 0x20u) != 0u;
                bool yb = (instr & 0x40u) != 0u;
                if (nibA == 0x50u)
                {
                    string q = g == 0x10u ? "qadd" : (g == 0x12u ? "qsub" : (g == 0x14u ? "qdadd" : "qdsub"));
                    s.S(q); s.CondSuffix(); s.C(' ');
                    s.Reg(rd); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rn);
                    return;
                }
                if ((nibA & 0x90u) == 0x80u)
                {
                    string suf = (xb ? "t" : "b") + (yb ? "t" : "b");
                    if (g == 0x10u)
                    {
                        s.S("smla" + suf); s.CondSuffix(); s.C(' ');
                        s.Reg(rn); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs); s.S(", "); s.Reg(rd);
                        return;
                    }
                    if (g == 0x12u)
                    {
                        s.S((xb ? "smulw" : "smlaw") + (yb ? "t" : "b")); s.CondSuffix(); s.C(' ');
                        s.Reg(rn); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                        if (!xb) { s.S(", "); s.Reg(rd); }
                        return;
                    }
                    if (g == 0x14u)
                    {
                        s.S("smlal" + suf); s.CondSuffix(); s.C(' ');
                        s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                        return;
                    }
                    s.S("smul" + suf); s.CondSuffix(); s.C(' ');
                    s.Reg(rn); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                    return;
                }
            }

            switch (g)
            {
                case 0x60u:
                    break;
                case 0x61u: case 0x62u: case 0x63u:
                case 0x65u: case 0x66u: case 0x67u:
                    {
                        string baseName;
                        switch (o2)
                        {
                            case 0u: baseName = "add16"; break;
                            case 1u: baseName = "asx"; break;
                            case 2u: baseName = "sax"; break;
                            case 3u: baseName = "sub16"; break;
                            case 4u: baseName = "add8"; break;
                            case 7u: baseName = "sub8"; break;
                            default: baseName = null; break;
                        }
                        if (baseName == null) break;
                        string pre;
                        if (g == 0x62u) pre = "q";
                        else if (g == 0x63u) pre = "sh";
                        else if (g == 0x65u) pre = "u";
                        else if (g == 0x66u) pre = "uq";
                        else if (g == 0x67u) pre = "uh";
                        else pre = "s";
                        s.S(pre + baseName); s.CondSuffix(); s.C(' ');
                        s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm);
                        return;
                    }
                case 0x68u:
                    if ((instr & 0xFF0u) == 0xFB0u)
                    {
                        // SEL: bits [11:4] == 1111 1011
                        s.S("sel"); s.CondSuffix(); s.C(' ');
                        s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm);
                        return;
                    }
                    if ((o2 & 3u) == 0u)
                    {
                        s.S("pkhbt"); s.CondSuffix(); s.C(' ');
                        s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm);
                        uint sh = (instr >> 7) & 0x1Fu;
                        if (sh != 0) { s.S(", lsl #"); s.N(sh); }
                        return;
                    }
                    if ((o2 & 3u) == 2u)
                    {
                        s.S("pkhtb"); s.CondSuffix(); s.C(' ');
                        s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm);
                        uint sh = (instr >> 7) & 0x1Fu;
                        if (sh == 0) sh = 32;
                        s.S(", asr #"); s.N(sh);
                        return;
                    }
                    if ((o2 & 3u) == 3u)
                    {
                        int rot = (int)((instr >> 10) & 3u) * 8;
                        bool low = rn == 15;
                        s.S(bit22 ? (low ? "uxtb16" : "uxtab16") : (low ? "sxtb16" : "sxtab16"));
                        s.CondSuffix(); s.C(' ');
                        s.Reg(rd);
                        if (!low) { s.S(", "); s.Reg(rn); }
                        s.S(", "); s.Reg(rm);
                        if (rot != 0) { s.S(", ror #"); s.N((uint)rot); }
                        return;
                    }
                    break;
                case 0x6Au: case 0x6Bu:
                case 0x6Eu: case 0x6Fu:
                    ArmSatExtend(s, instr, rd, rn, rm, g, o2);
                    return;
                case 0x69u: case 0x6Du:
                    break;
                case 0x6Cu:
                    if ((o2 & 3u) == 3u)
                    {
                        int rot = (int)((instr >> 10) & 3u) * 8;
                        bool low = rn == 15;
                        s.S(low ? "uxtb16" : "uxtab16"); s.CondSuffix(); s.C(' ');
                        s.Reg(rd);
                        if (!low) { s.S(", "); s.Reg(rn); }
                        s.S(", "); s.Reg(rm);
                        if (rot != 0) { s.S(", ror #"); s.N((uint)rot); }
                        return;
                    }
                    break;
                case 0x70u: case 0x74u:
                    {
                        bool q = (o2 & 1u) != 0;
                        string nm;
                        if (g == 0x70u) nm = q ? (bit20 ? "smlsdx" : "smusdx") : (bit20 ? "smladx" : "smuadx");
                        else nm = q ? (bit20 ? "smlsd" : "smusd") : (bit20 ? "smlad" : "smuad");
                        s.S(nm); s.CondSuffix(); s.C(' ');
                        s.Reg(rd); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                        if (bit20) { s.S(", "); s.Reg(rn); }
                        return;
                    }
                case 0x78u:
                    if (o2 == 0u)
                    {
                        s.S(bit20 ? "usada8" : "usad8"); s.CondSuffix(); s.C(' ');
                        s.Reg(rd); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                        if (bit20) { s.S(", "); s.Reg(rn); }
                        return;
                    }
                    if (o2 == 2u)
                    {
                        s.S(bit20 ? "smlsld" : "smlald"); s.CondSuffix(); s.C(' ');
                        s.Reg(rn); s.S(", "); s.Reg(rd); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                        return;
                    }
                    break;
                case 0x7Au: case 0x7Bu:
                case 0x7Cu: case 0x7Du: case 0x7Eu: case 0x7Fu:
                    ArmBitfield(s, instr, rd, rn);
                    return;
                case 0x71u: case 0x72u: case 0x73u:
                case 0x75u: case 0x76u: case 0x77u:
                    {
                        uint kind = (g >> 1) & 3u;
                        string nm = kind == 0u ? "smmul" : (kind == 2u ? "smmla" : "smmls");
                        s.S(nm);
                        if (bit5) s.C('r');
                        s.CondSuffix(); s.C(' ');
                        s.Reg(rd); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(rs);
                        if (kind != 0u) { s.S(", "); s.Reg(rn); }
                        return;
                    }
                default:
                    break;
            }

            s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        private static bool rdLoIsZero(uint instr)
        {
            return ((instr >> 8) & 0xFu) == 0u && ((instr >> 5) & 7u) == 0u && ((instr >> 12) & 0xFu) != 15u;
        }

        private static void ArmSatExtend(State s, uint instr, int rd, int rn, int rm, uint g, uint o2)
        {
            // g is 0x6A/0x6B/0x6E/0x6F: bit 2 selects the unsigned form and bit 0
            // the halfword form.  bits [6:5] == 11 selects the sign/zero extend
            // form, bit 5 set with bit 6 clear the 16-bit saturate form, and
            // bit 5 clear the plain saturate form.
            bool unsigned = (g & 4u) != 0u;
            bool half = (g & 1u) != 0u;
            uint sel = (instr >> 5) & 3u;

            if (sel == 3u)
            {
                int rot = (int)((instr >> 10) & 3u) * 8;
                string nm;
                if (rn == 15) nm = unsigned ? (half ? "uxth" : "uxtb") : (half ? "sxth" : "sxtb");
                else nm = (unsigned ? "uxta" : "sxta") + (half ? "h" : "b");
                s.S(nm); s.CondSuffix(); s.C(' ');
                s.Reg(rd); s.S(", ");
                if (rn != 15) { s.Reg(rn); s.S(", "); }
                s.Reg(rm);
                if (rot != 0) { s.S(", ror #"); s.N((uint)rot); }
                return;
            }
            if ((sel & 1u) != 0u && ((instr >> 7) & 0x1Fu) == 0x1Eu)
            {
                uint sat16 = (instr >> 16) & 0xFu;
                if (!unsigned) sat16 += 1u;
                s.S(unsigned ? "usat16" : "ssat16"); s.CondSuffix(); s.C(' ');
                s.Reg(rd); s.S(", #"); s.N(sat16); s.S(", "); s.Reg(rm);
                return;
            }
            if ((instr & 0x20u) != 0u)
            {
                s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
                return;
            }
            {
                uint satImm = (instr >> 16) & 0x1Fu;
                uint sat = unsigned ? satImm : satImm + 1u;
                uint shift = (instr >> 7) & 0x1Fu;
                bool asr = (instr & 0x40u) != 0u;
                s.S(unsigned ? "usat" : "ssat"); s.CondSuffix(); s.C(' ');
                s.Reg(rd); s.S(", #"); s.N(sat); s.S(", "); s.Reg(rm);
                if (shift != 0 || asr)
                {
                    s.S(asr ? ", asr #" : ", lsl #");
                    s.N(asr && shift == 0 ? 32u : shift);
                }
                return;
            }
        }

        private static void ArmBitfield(State s, uint instr, int rd, int rn)
        {
            int lsb = (int)((instr >> 7) & 0x1Fu);
            int field = (int)((instr >> 16) & 0x1Fu);   // msb (BFI/BFC) or width-1 (SBFX/UBFX)
            int rm = (int)(instr & 0xFu);               // source register
            uint g = (instr >> 20) & 0xFFu;
            uint sel = (instr >> 4) & 7u;

            if (sel == 1u)
            {
                s.S(rm == 15 ? "bfc" : "bfi"); s.CondSuffix(); s.C(' ');
                s.Reg(rd);
                if (rm != 15) { s.S(", "); s.Reg(rm); }
                s.S(", #"); s.N((uint)lsb); s.S(", #"); s.N((uint)(field - lsb + 1));
                return;
            }
            if (sel == 5u)
            {
                s.S((g & 4u) == 0u ? "sbfx" : "ubfx"); s.CondSuffix(); s.C(' ');
                s.Reg(rd); s.S(", "); s.Reg(rm);
                s.S(", #"); s.N((uint)lsb); s.S(", #"); s.N((uint)(field + 1));
                return;
            }
            s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        // ---- misc ---------------------------------------------------------------

        /// <summary>MRS / MSR(register) / BX / BLX / BXJ encodings in the 00010xxx space.</summary>
        private static bool IsArmMiscEncoding(uint instr)
        {
            if ((instr & 0x0FBF0FFFu) == 0x010F0000u) return true;
            if ((instr & 0x0FB0FFF0u) == 0x0120F000u) return true;
            if ((instr & 0x0FFFFFF0u) == 0x012FFF10u) return true;
            if ((instr & 0x0FFFFFF0u) == 0x012FFF20u) return true;
            if ((instr & 0x0FFFFFF0u) == 0x012FFF30u) return true;
            return false;
        }

        private static void ArmMisc(State s, uint addr, uint instr)
        {
            int rd = (int)((instr >> 12) & 0xFu);
            int rm = (int)(instr & 0xFu);
            uint op2 = (instr >> 5) & 7u;

            // BX / BLX / BXJ share bits [27:20] with MRS/MSR, so test them first.
            if ((instr & 0x0FFFFFF0u) == 0x012FFF10u) { s.S("bx"); s.CondSuffix(); s.C(' '); s.Reg(rm); return; }
            if ((instr & 0x0FFFFFF0u) == 0x012FFF20u) { s.S("bxj"); s.CondSuffix(); s.C(' '); s.Reg(rm); return; }
            if ((instr & 0x0FFFFFF0u) == 0x012FFF30u) { s.S("blx"); s.CondSuffix(); s.C(' '); s.Reg(rm); return; }

            uint o1m = (instr >> 20) & 0xFFu;
            if ((instr & 0xFFFu) == 0u && (o1m == 0x10u || o1m == 0x14u))
            {
                bool spsr = (instr & (1u << 22)) != 0;
                s.S("mrs"); s.CondSuffix(); s.C(' ');
                s.Reg(rd); s.S(", ");
                s.S(spsr ? "spsr" : "cpsr");
                return;
            }
            if ((instr & 0xFF0u) == 0u && (o1m == 0x12u || o1m == 0x16u))
            {
                s.S("msr"); s.CondSuffix(); s.C(' ');
                Fields(s, instr); s.S(", "); s.Reg(rm);
                return;
            }

            if (op2 == 1u)
            {
                s.S("clz"); s.CondSuffix(); s.C(' ');
                s.Reg(rd); s.S(", "); s.Reg(rm);
                return;
            }
            s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        private static void ArmClz(State s, uint instr)
        {
            s.S("clz"); s.CondSuffix(); s.C(' ');
            s.Reg((int)((instr >> 12) & 0xFu)); s.S(", "); s.Reg((int)(instr & 0xFu));
        }

        // ---- extra load/store ---------------------------------------------------

        private static void ArmExtraLoadStore(State s, uint addr, uint instr)
        {
            int rd = (int)((instr >> 12) & 0xFu);
            int rn = (int)((instr >> 16) & 0xFu);
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool immediate = (instr & (1u << 22)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            bool l = (instr & (1u << 20)) != 0;
            uint sel = (instr >> 5) & 3u;

            string nm;
            bool pair = false;
            // sel == (S << 1) | H: 01 = halfword, 10 = signed byte / doubleword,
            // 11 = signed halfword / doubleword store; 00 is the multiply/swap
            // nibble and never reaches this decoder.
            if (immediate)
            {
                switch (sel)
                {
                    case 1u: nm = l ? "ldrh" : "strh"; break;
                    case 2u: nm = l ? "ldrsb" : "ldrd"; pair = !l; break;
                    case 3u: nm = l ? "ldrsh" : "strd"; pair = !l; break;
                    default: nm = l ? "undef" : "undef"; break;
                }
            }
            else
            {
                switch (sel)
                {
                    case 1u: nm = l ? "ldrh" : "strh"; break;
                    case 2u: nm = l ? "ldrsb" : "ldrd"; pair = !l; break;
                    case 3u: nm = l ? "ldrsh" : "strd"; pair = !l; break;
                    default: nm = "undef"; break;
                }
            }

            if (nm == "undef") { s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture)); return; }

            s.S(nm);
            if (!p && w) s.C('t');
            s.CondSuffix(); s.C(' ');
            s.Reg(rd);
            if (pair) { s.S(", r"); s.N((uint)(rd + 1)); }
            uint imm = immediate ? ((((instr >> 8) & 0xFu) << 4) | (instr & 0xFu)) : 0u;
            ArmMemOperand(s, instr, rn, p, u, w, immediate, imm);
        }

        private static void ArmMemOperand(State s, uint instr, int rn, bool p, bool u, bool w,
                                          bool immediate, uint imm)
        {
            s.S(", [");
            s.Reg(rn);
            if (p)
            {
                if (immediate) { s.S(", "); s.Imm(u ? imm : (uint)(-(int)imm)); }
                else { s.S(", "); if (!u) s.C('-'); s.Reg((int)(instr & 0xFu)); }
                s.C(']');
                if (w) s.C('!');
            }
            else
            {
                s.C(']'); s.S(", ");
                if (immediate) s.Imm(u ? imm : (uint)(-(int)imm));
                else { if (!u) s.C('-'); s.Reg((int)(instr & 0xFu)); }
            }
        }

        // ---- load/store ---------------------------------------------------------

        private static void ArmLoadStore(State s, uint addr, uint instr, bool immediate)
        {
            int rd = (int)((instr >> 12) & 0xFu);
            int rn = (int)((instr >> 16) & 0xFu);
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool b = (instr & (1u << 22)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            bool l = (instr & (1u << 20)) != 0;

            s.S(l ? "ldr" : "str");
            if (b) s.C('b');
            if (!p && w) s.C('t');
            s.CondSuffix();
            s.C(' ');
            s.Reg(rd);

            if (rn == 15 && p && !w && immediate && (instr & 0xFFFu) != 0)
            {
                uint off = instr & 0xFFFu;
                s.S(", #");
                s.Hx(u ? addr + 8u + off : addr + 8u - off);
                return;
            }

            s.S(", [");
            s.Reg(rn);
            if (!p) { s.C(']'); s.S(", "); }
            else s.S(", ");
            if (immediate)
            {
                uint off = instr & 0xFFFu;
                s.Imm(u ? off : (uint)(-(int)off));
            }
            else
            {
                if (!u) s.C('-');
                s.Reg((int)(instr & 0xFu));
                s.Shift((int)((instr >> 5) & 3u), (int)((instr >> 7) & 0x1Fu));
            }
            if (p) { s.C(']'); if (w) s.C('!'); }
        }

        private static void ArmLoadStoreMultiple(State s, uint addr, uint instr)
        {
            int rn = (int)((instr >> 16) & 0xFu);
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool sf = (instr & (1u << 22)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            bool l = (instr & (1u << 20)) != 0;
            uint list = instr & 0xFFFFu;

            string nm = l ? "ldm" : "stm";
            bool alias = false;
            if (w && rn == 13)
            {
                if (l && u) { nm = "pop"; alias = true; }
                else if (!l && !u) { nm = "push"; alias = true; }
            }
            if (!alias)
            {
                if (u) nm += p ? "ib" : "ia";
                else nm += p ? "db" : "da";
            }

            s.S(nm); s.CondSuffix(); s.C(' ');
            if (alias) { if (!l) s.C(' '); }
            else
            {
                s.Reg(rn);
                if (w) s.C('!');
                s.S(", ");
            }
            s.RegList(list);
            if (sf) s.S("^");
        }

        private static void ArmBranch(State s, uint addr, uint instr)
        {
            bool link = (instr & (1u << 24)) != 0;
            int imm = (int)(instr & 0xFFFFFFu);
            if ((imm & 0x800000) != 0) imm |= unchecked((int)0xFF000000);
            s.S(link ? "bl" : "b");
            s.CondSuffix();
            s.S(" #");
            s.Hx(addr + 8u + (uint)(imm << 2));
        }

        // ---- coprocessor --------------------------------------------------------

        private static void ArmCoprocessor(State s, uint addr, uint instr)
        {
            uint cp = (instr >> 8) & 0xFu;
            uint blk = (instr >> 25) & 7u;

            // 0xEC/0xED: bits [24:21] == 0100 is MCRR/MRRC (bit 20 selects the
            // direction); every other combination is LDC/STC (or, for
            // coprocessors 10/11, a VFP load/store).
            if (blk == 6u)
            {
                if ((instr & 0x01E00000u) == 0x00400000u)
                {
                    bool l = (instr & (1u << 20)) != 0;
                    s.S(l ? "mrrc" : "mcrr"); s.CondSuffix(); s.C(' ');
                    s.S("p"); s.N(cp); s.S(", #"); s.N((instr >> 4) & 0xFu);
                    s.S(", "); s.Reg((int)((instr >> 12) & 0xFu));
                    s.S(", "); s.Reg((int)((instr >> 16) & 0xFu));
                    s.S(", c"); s.N(instr & 0xFu);
                    return;
                }
                if (cp == 10u || cp == 11u) { VfpLoadStore(s, addr, instr); return; }
                bool ldc = (instr & (1u << 20)) != 0;
                bool longForm = (instr & (1u << 22)) != 0;
                s.S(ldc ? "ldc" : "stc");
                if (longForm) s.C('l');
                s.CondSuffix(); s.C(' ');
                s.S("p"); s.N(cp); s.S(", c"); s.N((instr >> 12) & 0xFu);
                ArmMemOperand(s, instr, (int)((instr >> 16) & 0xFu),
                              (instr & (1u << 24)) != 0, (instr & (1u << 23)) != 0,
                              (instr & (1u << 21)) != 0, true, (instr & 0xFFu) * 4u);
                return;
            }

            // 0xEE: bit 4 == 0 is CDP, bit 4 == 1 is MCR/MRC; coprocessors 10/11
            // carry the VFP data-processing space.
            if (cp == 10u || cp == 11u) { VfpDataProcessing(s, addr, instr); return; }
            if ((instr & 0x10u) == 0u)
            {
                s.S("cdp");
                s.CondSuffix(); s.C(' ');
                s.S("p"); s.N(cp); s.S(", #"); s.N((instr >> 20) & 0xFu);
                s.S(", c"); s.N((instr >> 12) & 0xFu);
                s.S(", c"); s.N((instr >> 16) & 0xFu);
                s.S(", c"); s.N(instr & 0xFu);
                s.S(", #"); s.N((instr >> 5) & 7u);
                return;
            }

            uint op1 = (instr >> 21) & 7u;
            bool load = (instr & (1u << 20)) != 0;
            s.S(load ? "mrc" : "mcr");
            s.CondSuffix(); s.C(' ');
            s.S("p"); s.N(cp); s.S(", #"); s.N(op1);
            s.S(", "); s.Reg((int)((instr >> 12) & 0xFu));
            s.S(", c"); s.N((instr >> 16) & 0xFu);
            s.S(", c"); s.N(instr & 0xFu);
            s.S(", #"); s.N((instr >> 5) & 7u);
        }

        // ---- unconditional space ------------------------------------------------

        private static void ArmUnconditional(State s, uint addr, uint instr)
        {
            if ((instr & 0xFE000000u) == 0xFA000000u)
            {
                int imm = (int)(instr & 0xFFFFFFu);
                if ((imm & 0x800000) != 0) imm |= unchecked((int)0xFF000000);
                s.S("blx #"); s.Hx(addr + 8u + (uint)(imm << 2));
                return;
            }

            uint cp = (instr >> 8) & 0xFu;
            uint blk = (instr >> 25) & 7u;

            if (cp == 10u || cp == 11u)
            {
                if (blk == 6u) { VfpLoadStore(s, addr, instr); return; }
                VfpDataProcessing(s, addr, instr);
                return;
            }

            if ((instr & 0xFFFFFF00u) == 0xF57FF000u)
            {
                uint opc = instr & 0xFFu;
                if (opc == 0x1Fu) { s.S("clrex"); return; }
                s.S((opc & 0xF0u) == 0x40u ? "dsb" : (opc & 0xF0u) == 0x50u ? "dmb" : "isb");
                if ((opc & 0xFu) == 0xFu) s.S(" sy");
                else { s.S(" #"); s.N(opc & 0xFu); }
                return;
            }

            if ((instr & 0xFFFFFDFFu) == 0xF1010000u)
            {
                s.S("setend ");
                s.S((instr & (1u << 9)) != 0 ? "be" : "le");
                return;
            }

            if ((instr & 0xFFF10020u) == 0xF1000000u)
            {
                uint imod = (instr >> 18) & 3u;
                s.S(imod == 0u ? "cps" : (imod == 2u ? "cpsie" : "cpsid"));
                s.C(' ');
                uint aif = (instr >> 6) & 7u;
                bool any = false;
                if ((aif & 4u) != 0) { s.C('a'); any = true; }
                if ((aif & 2u) != 0) { s.C('i'); any = true; }
                if ((aif & 1u) != 0) { s.C('f'); any = true; }
                if (!any) s.S("none");
                if ((instr & (1u << 17)) != 0) { s.S(", #"); s.N(instr & 0x1Fu); }
                return;
            }

            if ((instr & 0xFFFFFFF0u) == 0xE320F000u)
            {
                switch (instr & 0xFu)
                {
                    case 0u: s.S("nop"); return;
                    case 1u: s.S("yield"); return;
                    case 2u: s.S("wfe"); return;
                    case 3u: s.S("wfi"); return;
                    case 4u: s.S("sev"); return;
                    default: s.S("hint #"); s.N(instr & 0xFu); return;
                }
            }

            if ((instr & 0x0FFFFFF0u) == 0x012FFF10u) { s.S("bx "); s.Reg((int)(instr & 0xFu)); return; }
            if ((instr & 0x0FFFFFF0u) == 0x012FFF20u) { s.S("blx "); s.Reg((int)(instr & 0xFu)); return; }
            if ((instr & 0x0FFFFFF0u) == 0x012FFF30u) { s.S("bxj "); s.Reg((int)(instr & 0xFu)); return; }

            if ((instr & 0x0FF000F0u) == 0x01600070u) { s.S("smc #"); s.N(instr & 0xFu); return; }
            if ((instr & 0x0FF000F0u) == 0x01400070u) { s.S("hvc #"); s.N(instr & 0xFu); return; }
            if ((instr & 0x0FF000F0u) == 0x01200070u)
            {
                s.S("bkpt #"); s.N((((instr >> 4) & 0xFFFu) << 4) | (instr & 0xFu));
                return;
            }

            if ((instr & 0x0FE00000u) == 0x08100000u)
            {
                int rn = (int)((instr >> 16) & 0xFu);
                bool p = (instr & (1u << 24)) != 0;
                bool u = (instr & (1u << 23)) != 0;
                bool w = (instr & (1u << 21)) != 0;
                s.S("rfe"); s.S(u ? (p ? "ib " : "ia ") : (p ? "db " : "da "));
                s.Reg(rn);
                if (w) s.C('!');
                return;
            }
            if ((instr & 0x0FE00000u) == 0x08C00000u || (instr & 0x0FE00000u) == 0x08E00000u)
            {
                bool p = (instr & (1u << 24)) != 0;
                bool u = (instr & (1u << 23)) != 0;
                bool w = (instr & (1u << 21)) != 0;
                s.S("srs"); s.S(u ? (p ? "ib " : "ia ") : (p ? "db " : "da "));
                s.C('#'); s.N(instr & 0x1Fu);
                if (w) s.C('!');
                return;
            }

            if ((instr & 0x0D700000u) == 0x05500000u) { s.S("pld"); return; }
            if ((instr & 0x0D200000u) == 0x05000000u) { s.S("pli"); return; }

            s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        // ================================================================ VFP

        private static int VdOf(uint instr)
        {
            return (int)(((instr >> 12) & 0xFu) << 1 | ((instr >> 22) & 1u));
        }

        private static void VReg(State s, bool dbl, int n)
        {
            s.C(dbl ? 'd' : 's');
            s.N((uint)n);
        }

        private static void VfpLoadStore(State s, uint addr, uint instr)
        {
            bool load = (instr & (1u << 20)) != 0;
            bool single = (instr & 0x100u) == 0u;                  // coprocessor 10 = single
            bool multiple = !((instr & (1u << 24)) != 0u && (instr & (1u << 21)) == 0u);
            bool p = (instr & (1u << 24)) != 0;
            bool u = (instr & (1u << 23)) != 0;
            bool w = (instr & (1u << 21)) != 0;
            int rn = (int)((instr >> 16) & 0xFu);
            int vd = (int)((instr >> 12) & 0xFu);
            bool dBit = (instr & (1u << 22)) != 0;
            int d = single ? ((vd << 1) | (dBit ? 1 : 0)) : (vd | (dBit ? 0x10 : 0));
            uint imm8 = instr & 0xFFu;

            if (multiple)
            {
                int count = single ? (int)imm8 : (int)(imm8 / 2u);
                s.S(load ? "vldm" : "vstm");
                s.S(u ? (p ? "ib " : "ia ") : (p ? "db " : "da "));
                s.Reg(rn);
                if (w) s.C('!');
                s.S(", {");
                for (int k = 0; k < count; k++)
                {
                    if (k != 0) s.S(", ");
                    s.C(single ? 's' : 'd');
                    s.N((uint)(d + k));
                }
                s.C('}');
                return;
            }

            s.S(load ? "vldr" : "vstr");
            s.S(single ? ".f32 " : ".f64 ");
            s.C(single ? 's' : 'd');
            s.N((uint)d);
            s.S(", [");
            s.Reg(rn);
            s.S(", ");
            if (!u) s.C('-');
            s.Imm(imm8 * 4u);
            s.C(']');
            if (p && w) s.C('!');
        }

        private static void VfpDataProcessing(State s, uint addr, uint instr)
        {
            bool sz = (instr & 0x100u) != 0;
            uint opc1 = (instr >> 20) & 0xFu;
            uint opc2 = (instr >> 6) & 0xFu;
            uint g = (instr >> 20) & 0xFFu;
            int rdCore = (int)((instr >> 12) & 0xFu);

            // Register numbering: single precision Sn = (Vn << 1) | N, double
            // precision Dn = Vn | (N << 4); the same rule uses bit 22 for the
            // destination and bit 5 for the third operand.
            uint fVd = (instr >> 12) & 0xFu, fVn = (instr >> 16) & 0xFu, fVm = instr & 0xFu;
            bool bD = (instr & (1u << 22)) != 0u, bN = (instr & 0x80u) != 0u, bM = (instr & 0x20u) != 0u;
            int vd = sz ? (int)(fVd | (bD ? 16u : 0u)) : (int)((fVd << 1) | (bD ? 1u : 0u));
            int vn = sz ? (int)(fVn | (bN ? 16u : 0u)) : (int)((fVn << 1) | (bN ? 1u : 0u));
            int vm = sz ? (int)(fVm | (bM ? 16u : 0u)) : (int)((fVm << 1) | (bM ? 1u : 0u));

            string prec = sz ? ".f64 " : ".f32 ";

            // VMOV between a core register and a single/double register or one
            // half of a double register (cond 1110 000 0 0 L idx Rt 101 sz 0001 0000).
            if ((instr & 0x0F800E1Fu) == 0x0E000A10u)
            {
                bool toCore = (instr & (1u << 20)) != 0;
                if (sz)
                {
                    int lane = (int)((instr >> 21) & 1u);
                    s.S("vmov.32 ");
                    if (toCore) { s.Reg(rdCore); s.S(", d"); s.N((uint)vn); s.C('['); s.N((uint)lane); s.C(']'); }
                    else { s.S("d"); s.N((uint)vn); s.C('['); s.N((uint)lane); s.S("], "); s.Reg(rdCore); }
                }
                else
                {
                    if (toCore) { s.S("vmov "); s.Reg(rdCore); s.S(", s"); s.N((uint)vn); }
                    else { s.S("vmov s"); s.N((uint)vn); s.S(", "); s.Reg(rdCore); }
                }
                return;
            }

            // VMRS / VMSR: cond 1110 1111 reg Rt 101 0 0001 0000.
            if ((instr & 0x0FF00F10u) == 0x0EF00A10u)
            {
                bool toCore = (instr & (1u << 20)) != 0;
                uint reg = (instr >> 16) & 0xFu;
                string sysreg;
                switch (reg)
                {
                    case 0u: sysreg = "fpsid"; break;
                    case 1u: sysreg = "fpscr"; break;
                    case 5u: sysreg = "fpsr"; break;
                    case 6u: sysreg = "mvfr0"; break;
                    case 7u: sysreg = "mvfr1"; break;
                    case 8u: sysreg = "fpexc"; break;
                    default: sysreg = "fpsr"; break;
                }
                s.S(toCore ? "vmrs " : "vmsr ");
                if (toCore && rdCore == 15) s.S("apsr_nzcv, ");
                else { s.Reg(rdCore); s.S(", "); }
                s.S(sysreg);
                return;
            }
            if ((g & 0xEFu) == 0xC4u && (instr & 0x10u) != 0u)
            {
                bool toDouble = (instr & 0x100u) != 0;
                int d2 = (int)(((instr >> 12) & 0xFu) | ((instr & (1u << 22)) != 0 ? 0x10u : 0u));
                int rt2 = (int)((instr >> 16) & 0xFu);
                if (toDouble) { s.S("vmov d"); s.N((uint)d2); s.S(", "); s.Reg(rdCore); s.S(", "); s.Reg(rt2); }
                else { s.S("vmov "); s.Reg(rdCore); s.S(", "); s.Reg(rt2); s.S(", d"); s.N((uint)d2); }
                return;
            }

            if ((instr & 0x0FB00E50u) == 0x0EB00A00u)
            {
                uint imm4H = (instr >> 16) & 0xFu;
                uint imm4L = instr & 0xFu;
                uint cmode = (instr >> 8) & 0xFu;
                if ((cmode & 0xEu) == 0xAu)
                {
                    uint imm8 = (imm4H << 4) | imm4L;
                    uint bits = ArmVfp.ExpandImmediate(imm8);
                    s.S("vmov."); s.S(sz ? "f64 " : "f32 ");
                    s.C(sz ? 'd' : 's');
                    s.N((uint)(sz ? (vd / 2) : vd));
                    s.S(", #");
                    if (sz)
                    {
                        ulong dv = ArmVfp.ExpandImmediate64(imm8);
                        double d = BitConverter.Int64BitsToDouble(unchecked((long)dv));
                        s.S(d.ToString("R", CultureInfo.InvariantCulture));
                    }
                    else s.S(ArmVfp.BitsToFloat(bits).ToString("R", CultureInfo.InvariantCulture));
                    return;
                }
            }

            if (opc1 == 0xBu && (opc2 & 5u) == 4u)
            {
                if (opc2 == 0x4u || opc2 == 0x5u)
                {
                    bool zero = (instr & 0x40u) != 0;
                    s.S("vcmp"); if ((opc2 & 1u) != 0) s.C('e');
                    s.S(prec);
                    VReg(s, sz, vd);
                    if (zero) s.S(", #0.0");
                    else { s.S(", "); VReg(s, sz, vm); }
                    return;
                }
                if (opc2 == 0x7u)
                {
                    bool toInt = (instr & 0x80u) == 0u;
                    uint ty = (instr >> 16) & 3u;
                    string t1 = (ty & 1u) != 0 ? "u" : "s";
                    string t2 = (ty & 2u) != 0 ? "32" : "16";
                    s.S("vcvt");
                    if (toInt)
                    {
                        s.S("." + t1 + t2 + (sz ? ".f64 " : ".f32 "));
                        VReg(s, false, vd); s.S(", "); VReg(s, sz, vm);
                    }
                    else
                    {
                        s.S((sz ? ".f64." : ".f32.") + t1 + t2 + " ");
                        VReg(s, sz, vd); s.S(", "); VReg(s, false, vm);
                    }
                    return;
                }
                s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
                return;
            }

            // The operation selector is bits [23:20] with bit 22 removed (bit 22
            // is the destination high bit D); bit 6 is opc3 and bit 7 is N.
            uint sel = (((instr >> 23) & 1u) << 2) | (((instr >> 21) & 1u) << 1) | ((instr >> 20) & 1u);
            bool opc3 = (instr & 0x40u) != 0;
            if (sel == 7u)
            {
                if (!opc3)
                {
                    // VMOV (immediate) was handled above; remaining encodings here
                    // are the VCVT fixed-point forms.
                    s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
                    return;
                }
                uint opc2v = (instr >> 16) & 0xFu;
                bool nBit = (instr & 0x80u) != 0;
                if (opc2v == 4u)
                {
                    s.S(nBit ? "vcmpe" : "vcmp"); s.S(prec);
                    VReg(s, sz, vd);
                    if (vm == 0) s.S(", #0.0");
                    else { s.S(", "); VReg(s, sz, vm); }
                    return;
                }
                if (opc2v == 8u || opc2v == 0xCu || opc2v == 0xDu)
                {
                    int sd = (int)((((instr >> 12) & 0xFu) << 1) | ((instr >> 22) & 1u));
                    int sm = (int)(((instr & 0xFu) << 1) | ((instr >> 5) & 1u));
                    string ty = (opc2v == 8u) ? (nBit ? "s32" : "u32") : (opc2v == 0xDu ? "s32" : "u32");
                    if (opc2v == 8u)
                    {
                        s.S("vcvt."); s.S(sz ? "f64." : "f32."); s.S(ty); s.C(' ');
                        VReg(s, sz, vd); s.S(", "); s.C('s'); s.N((uint)sm);
                    }
                    else
                    {
                        s.S(opc2v == 0xCu ? "vcvtr." : "vcvt.");
                        s.S(ty); s.S(prec);
                        s.C('s'); s.N((uint)sd); s.S(", "); VReg(s, sz, vm);
                    }
                    return;
                }
                if (opc2v == 1u)
                {
                    s.S(nBit ? "vsqrt" : "vneg"); s.S(prec);
                    VReg(s, sz, vd); s.S(", "); VReg(s, sz, vm);
                    return;
                }
                if (opc2v == 0u)
                {
                    s.S(nBit ? "vabs" : "vmov"); s.S(prec);
                    VReg(s, sz, vd); s.S(", "); VReg(s, sz, vm);
                    return;
                }
                s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
                return;
            }

            string nm;
            switch (sel)
            {
                case 0x0u: nm = opc3 ? "vmls" : "vmla"; break;
                case 0x1u: nm = opc3 ? "vnmla" : "vnmls"; break;
                case 0x2u: nm = opc3 ? "vnmul" : "vmul"; break;
                case 0x3u: nm = opc3 ? "vsub" : "vadd"; break;
                case 0x4u:
                    if (opc3) { s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture)); return; }
                    nm = "vdiv";
                    break;
                case 0x5u: nm = opc3 ? "vfnma" : "vfnms"; break;
                case 0x6u: nm = opc3 ? "vfms" : "vfma"; break;
                default:
                    s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
                    return;
            }

            s.S(nm); s.S(prec); VReg(s, sz, vd);
            s.S(", "); VReg(s, sz, vn);
            s.S(", "); VReg(s, sz, vm);
        }

        // ================================================================ Thumb

        private static void ThumbDecode(State s, uint addr, out int length)
        {
            uint hw1 = FetchHalf(s, addr);
            if ((hw1 & 0xF800u) >= 0xE800u)
            {
                uint hw2 = FetchHalf(s, addr + 2u);
                uint instr = (hw1 << 16) | hw2;
                s.Insn = instr;
                length = 4;
                Thumb32(s, addr, instr);
                return;
            }
            s.Insn = hw1;
            length = 2;
            Thumb16(s, addr, hw1);
        }

        private static void Thumb16(State s, uint addr, uint i)
        {
            uint top = (i >> 11) & 0x1Fu;
            switch (top)
            {
                case 0u: case 1u: case 2u: T16Shift(s, i); return;
                case 3u: T16AddSub(s, i); return;
                case 4u: case 5u: case 6u: case 7u: T16MovCmp(s, i); return;
                case 8u:
                    if ((i & 0x400u) != 0u) T16SpecialData(s, i);
                    else T16Alu(s, i);
                    return;
                case 9u: T16Literal(s, addr, i); return;
                case 10u: case 11u: T16LdrStrReg(s, i); return;
                case 12u: case 13u: case 14u: case 15u: T16LdrStrImm(s, i); return;
                case 16u: case 17u: T16LdrStrHalfImm(s, i); return;
                case 18u: case 19u: T16SpRel(s, i); return;
                case 20u: case 21u: T16Adr(s, addr, i); return;
                case 22u: case 23u: T16Misc(s, addr, i); return;
                case 24u: case 25u: T16LdmStm(s, i); return;
                case 26u: case 27u: T16CondBranch(s, addr, i); return;
                case 28u: T16UncondBranch(s, addr, i); return;
                default: s.S(".hword 0x"); s.S(i.ToString("X4", CultureInfo.InvariantCulture)); return;
            }
        }

        private static void T16Shift(State s, uint i)
        {
            uint op = (i >> 11) & 3u;
            uint imm5 = (i >> 6) & 0x1Fu;
            int rm = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            if (op == 3u) { s.S(".hword 0x"); s.S(i.ToString("X4", CultureInfo.InvariantCulture)); return; }
            s.S(op == 0u ? "lsls " : (op == 1u ? "lsrs " : "asrs "));
            s.Reg(rd); s.S(", "); s.Reg(rm);
            if (op == 0u) { if (imm5 != 0) { s.S(", #"); s.N(imm5); } }
            else { s.S(", #"); s.N(imm5 == 0 ? 32u : imm5); }
        }

        private static void T16AddSub(State s, uint i)
        {
            bool immediate = (i & 0x400u) != 0;
            bool sub = (i & 0x200u) != 0;
            int rn = (int)((i >> 6) & 7u);
            int rm = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            s.S(sub ? "subs " : "adds ");
            s.Reg(rd); s.S(", "); s.Reg(rm); s.S(", ");
            if (immediate) s.Imm((uint)rn);
            else s.Reg(rn);
        }

        private static void T16MovCmp(State s, uint i)
        {
            uint op = (i >> 11) & 3u;
            int rd = (int)((i >> 8) & 7u);
            uint imm8 = i & 0xFFu;
            switch (op)
            {
                case 0u: s.S("movs "); s.Reg(rd); s.S(", "); s.Imm(imm8); return;
                case 1u: s.S("cmp "); s.Reg(rd); s.S(", "); s.Imm(imm8); return;
                case 2u: s.S("adds "); s.Reg(rd); s.S(", "); s.Imm(imm8); return;
                default: s.S("subs "); s.Reg(rd); s.S(", "); s.Imm(imm8); return;
            }
        }

        private static void T16Alu(State s, uint i)
        {
            uint op = (i >> 6) & 0xFu;
            int rm = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            switch (op)
            {
                case 0x0u: s.S("ands "); break;
                case 0x1u: s.S("eors "); break;
                case 0x2u: s.S("lsls "); break;
                case 0x3u: s.S("lsrs "); break;
                case 0x4u: s.S("asrs "); break;
                case 0x5u: s.S("adcs "); break;
                case 0x6u: s.S("sbcs "); break;
                case 0x7u: s.S("rors "); break;
                case 0x8u: s.S("tst "); break;
                case 0x9u: s.S("rsbs "); break;
                case 0xAu: s.S("cmp "); break;
                case 0xBu: s.S("cmn "); break;
                case 0xCu: s.S("orrs "); break;
                case 0xDu: s.S("muls "); break;
                case 0xEu: s.S("bics "); break;
                default: s.S("mvns "); break;
            }
            s.Reg(rd); s.S(", "); s.Reg(rm);
            if (op == 0x9u) s.S(", #0x0");
        }

        private static void T16SpecialData(State s, uint i)
        {
            uint op = (i >> 8) & 3u;
            int rm = (int)((i >> 3) & 0xFu);
            int rd = (int)((i & 7u) | ((i >> 4) & 8u));
            switch (op)
            {
                case 0u: s.S("add "); s.Reg(rd); s.S(", "); s.Reg(rm); return;
                case 1u: s.S("cmp "); s.Reg(rd); s.S(", "); s.Reg(rm); return;
                case 2u: s.S("mov "); s.Reg(rd); s.S(", "); s.Reg(rm); return;
                default: s.S((i & 0x0080u) != 0 ? "blx " : "bx "); s.Reg(rm); return;
            }
        }

        private static void T16Literal(State s, uint addr, uint i)
        {
            int rd = (int)((i >> 8) & 7u);
            uint imm8 = (uint)(i & 0xFFu) * 4u;
            s.S("ldr "); s.Reg(rd); s.S(", [pc, #"); s.N(imm8); s.C(']');
            s.S("  ; "); s.Hx(((addr + 4u) & ~3u) + imm8);
        }

        private static void T16LdrStrReg(State s, uint i)
        {
            uint op = (i >> 9) & 7u;
            int rm = (int)((i >> 6) & 7u);
            int rn = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            switch (op)
            {
                case 0u: s.S("str "); break;
                case 1u: s.S("strh "); break;
                case 2u: s.S("strb "); break;
                case 3u: s.S("ldrsb "); break;
                case 4u: s.S("ldr "); break;
                case 5u: s.S("ldrh "); break;
                case 6u: s.S("ldrb "); break;
                default: s.S("ldrsh "); break;
            }
            s.Reg(rd); s.S(", ["); s.Reg(rn); s.S(", "); s.Reg(rm); s.C(']');
        }

        private static void T16LdrStrImm(State s, uint i)
        {
            bool byteOp = (i & 0x1000u) != 0;
            bool load = (i & 0x0800u) != 0;
            uint imm5 = (i >> 6) & 0x1Fu;
            int rn = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            s.S(load ? "ldr" : "str");
            if (byteOp) s.C('b');
            s.C(' ');
            s.Reg(rd); s.S(", ["); s.Reg(rn);
            if (imm5 != 0) { s.S(", #"); s.N(byteOp ? imm5 : imm5 * 4u); }
            s.C(']');
        }

        private static void T16LdrStrHalfImm(State s, uint i)
        {
            bool load = (i & 0x0800u) != 0;
            uint imm5 = (i >> 6) & 0x1Fu;
            int rn = (int)((i >> 3) & 7u);
            int rd = (int)(i & 7u);
            s.S(load ? "ldrh " : "strh ");
            s.Reg(rd); s.S(", ["); s.Reg(rn);
            if (imm5 != 0) { s.S(", #"); s.N(imm5 * 2u); }
            s.C(']');
        }

        private static void T16SpRel(State s, uint i)
        {
            bool load = (i & 0x0800u) != 0;
            int rd = (int)((i >> 8) & 7u);
            uint imm8 = (uint)(i & 0xFFu) * 4u;
            s.S(load ? "ldr " : "str ");
            s.Reg(rd); s.S(", [sp");
            if (imm8 != 0) { s.S(", #"); s.N(imm8); }
            s.C(']');
        }

        private static void T16Adr(State s, uint addr, uint i)
        {
            bool sp = (i & 0x0800u) != 0;
            int rd = (int)((i >> 8) & 7u);
            uint imm8 = (uint)(i & 0xFFu) * 4u;
            if (sp) { s.S("add "); s.Reg(rd); s.S(", sp, #"); s.N(imm8); }
            else { s.S("adr "); s.Reg(rd); s.S(", #"); s.Hx(((addr + 4u) & ~3u) + imm8); }
        }

        private static void T16LdmStm(State s, uint i)
        {
            bool load = (i & 0x0800u) != 0;
            int rn = (int)((i >> 8) & 7u);
            uint list = i & 0xFFu;
            s.S(load ? "ldm" : "stm");
            if (load) s.S("ia");
            s.C(' ');
            s.Reg(rn);
            if ((list & (1u << rn)) == 0) s.C('!');
            s.S(", ");
            s.RegList(list);
        }

        private static void T16CondBranch(State s, uint addr, uint i)
        {
            uint cond = (i >> 8) & 0xFu;
            if (cond == 0xFu) { s.S("svc #"); s.N(i & 0xFFu); return; }
            if (cond == 0xEu) { s.S("udf #"); s.N(i & 0xFFu); return; }
            int imm = (int)(i & 0xFFu);
            if ((imm & 0x80) != 0) imm |= unchecked((int)0xFFFFFF00);
            s.S("b"); s.S(ConditionName(cond));
            s.S(" #"); s.Hx(addr + 4u + (uint)(imm << 1));
        }

        private static void T16UncondBranch(State s, uint addr, uint i)
        {
            int imm = (int)(i & 0x7FFu);
            if ((imm & 0x400) != 0) imm |= unchecked((int)0xFFFFF800);
            s.S("b #"); s.Hx(addr + 4u + (uint)(imm << 1));
        }

        private static void T16Misc(State s, uint addr, uint i)
        {
            uint sub = (i >> 8) & 0xFu;

            if (sub == 0u)
            {
                bool s2 = (i & 0x80u) != 0;
                uint imm = (uint)(i & 0x7Fu) * 4u;
                s.S(s2 ? "sub sp, #" : "add sp, #"); s.N(imm);
                return;
            }
            if ((i & 0xF500u) == 0xB100u)
            {
                bool nz = (i & 0x800u) != 0;
                uint high = (i >> 9) & 1u;
                uint imm5 = (i >> 3) & 0x1Fu;
                uint offset = ((high << 5) | imm5) << 1;
                s.S(nz ? "cbnz " : "cbz ");
                s.Reg((int)(i & 7u));
                s.S(", #"); s.Hx(addr + 4u + offset);
                return;
            }
            if ((i & 0xFF00u) == 0xB200u)
            {
                uint op2 = (i >> 6) & 3u;
                int rm = (int)((i >> 3) & 7u);
                int rd = (int)(i & 7u);
                s.S(op2 == 0u ? "sxth " : (op2 == 1u ? "sxtb " : (op2 == 2u ? "uxth " : "uxtb ")));
                s.Reg(rd); s.S(", "); s.Reg(rm);
                return;
            }
            if ((i & 0xFE00u) == 0xB400u)
            {
                uint list = i & 0xFFu;
                if ((i & 0x100u) != 0) list |= 0x4000u;
                s.S("push "); s.RegList(list);
                return;
            }
            if ((i & 0xFF00u) == 0xB600u)
            {
                if ((i & 0x00F0u) == 0x0060u) { s.S("cpsie i"); return; }
                if ((i & 0x00F0u) == 0x0070u) { s.S("cpsid i"); return; }
                if ((i & 0x00FFu) == 0x0002u) { s.S("setend be"); return; }
                if ((i & 0x00FFu) == 0x0000u) { s.S("setend le"); return; }
                s.S("cps");
                return;
            }
            if ((i & 0xFF00u) == 0xBA00u)
            {
                uint a = (i >> 6) & 3u;
                int rm = (int)((i >> 3) & 7u);
                int rd = (int)(i & 7u);
                s.S(a == 0u ? "rev " : (a == 1u ? "rev16 " : "revsh "));
                s.Reg(rd); s.S(", "); s.Reg(rm);
                return;
            }
            if ((i & 0xFE00u) == 0xBC00u)
            {
                uint list = i & 0xFFu;
                if ((i & 0x100u) != 0) list |= 0x8000u;
                s.S("pop "); s.RegList(list);
                return;
            }
            if ((i & 0xFF00u) == 0xBE00u) { s.S("bkpt #"); s.N(i & 0xFFu); return; }
            if ((i & 0xFF00u) == 0xBF00u)
            {
                uint mask = i & 0xFu;
                uint firstcond = (i >> 4) & 0xFu;
                if (mask == 0u)
                {
                    switch (firstcond)
                    {
                        case 0u: s.S("nop"); return;
                        case 1u: s.S("yield"); return;
                        case 2u: s.S("wfe"); return;
                        case 3u: s.S("wfi"); return;
                        case 4u: s.S("sev"); return;
                        default: s.S("hint #"); s.N(firstcond); return;
                    }
                }
                s.S("it"); s.S(ConditionName(firstcond)); s.C(' ');
                int n = 0;
                for (int b = 3; b >= 0; b--) if ((mask & (1u << b)) != 0) n++;
                for (int b = 0; b < n; b++)
                    s.C((mask & (1u << (3 - b))) != 0 ? 't' : 'e');
                return;
            }
            s.S(".hword 0x"); s.S(i.ToString("X4", CultureInfo.InvariantCulture));
        }

        // ---- Thumb-32 -----------------------------------------------------------

        private static void Thumb32(State s, uint addr, uint instr)
        {
            uint hw1 = (instr >> 16) & 0xFFFFu;
            uint hw2 = instr & 0xFFFFu;
            uint t1 = (hw1 >> 11) & 3u;        // hw1[12:11]
            uint top = (hw1 >> 4) & 0x7Fu;     // hw1[10:4]

            if (t1 == 1u)
            {
                // 0xE800..0xEFFF
                if ((top & 0x60u) == 0x00u) { T32LoadStoreDual(s, addr, instr); return; }
                if ((top & 0x60u) == 0x20u) { T32DataProcessingShifted(s, instr); return; }
                T32Coprocessor(s, addr, instr);
                return;
            }
            if (t1 == 2u)
            {
                // 0xF000..0xF7FF.  hw1[9] == 0 -> modified immediate, else plain.
                if ((hw2 & 0x8000u) != 0u) { T32BranchMisc(s, addr, instr); return; }
                if ((hw1 & 0x200u) == 0u) { T32DataProcessingModified(s, instr); return; }
                T32DataProcessingPlain(s, instr);
                return;
            }
            // 0xF800..0xFFFF
            if ((top & 0x60u) == 0x20u) { T32Multiply(s, instr); return; }
            T32LoadStoreSingle(s, addr, instr);
        }

        private static void T32Coprocessor(State s, uint addr, uint instr)
        {
            uint cp = (instr >> 8) & 0xFu;
            if (cp == 10u || cp == 11u)
            {
                if ((instr & 0x0E000000u) == 0x0C000000u) { VfpLoadStore(s, addr, instr); return; }
                VfpDataProcessing(s, addr, instr);
                return;
            }
            if (cp == 15u)
            {
                bool load = (instr & (1u << 20)) != 0;
                uint o1 = (instr >> 21) & 7u;
                uint o2 = (instr >> 5) & 7u;
                s.S(load ? "mrc" : "mcr");
                s.S(".w p15, #"); s.N(o1);
                s.S(", r"); s.N((instr >> 12) & 0xFu);
                s.S(", c"); s.N((instr >> 16) & 0xFu);
                s.S(", c"); s.N(instr & 0xFu);
                if (o2 != 0u) { s.S(", #"); s.N(o2); }
                return;
            }
            s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        private static void T32DataProcessingShifted(State s, uint instr)
        {
            uint hw1 = (instr >> 16) & 0xFFFFu;
            uint hw2 = instr & 0xFFFFu;
            uint opc = (hw1 >> 5) & 0xFu;           // hw1[8:5]
            bool sf = (hw1 & 0x10u) != 0u;
            int rn = (int)(hw1 & 0xFu);
            int rd = (int)((hw2 >> 8) & 0xFu);
            int rm = (int)(hw2 & 0xFu);
            int type = (int)((hw2 >> 4) & 3u);
            int amount = (int)(((hw2 >> 12) & 7u) << 2 | ((hw2 >> 6) & 3u));

            string nm;
            bool test = false;
            switch (opc)
            {
                case 0x0u: nm = "and"; break;
                case 0x1u: nm = "bic"; break;
                case 0x2u: nm = rn == 15 ? "mov" : "orr"; break;
                case 0x3u: nm = rn == 15 ? "mvn" : "orn"; break;
                case 0x4u: nm = rn == 15 ? "mov" : "eor"; break;
                case 0x6u: nm = "pkh"; break;
                case 0x8u: nm = "add"; break;
                case 0xAu: nm = "adc"; break;
                case 0xBu: nm = "sbc"; break;
                case 0xDu: nm = "sub"; break;
                case 0xEu: nm = "rsb"; break;
                default: s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture)); return;
            }

            if (opc == 0x6u)
            {
                bool tb = (hw2 & 0x20u) != 0u;
                s.S(tb ? "pkhtb " : "pkhbt ");
                s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm);
                if (tb)
                {
                    int a = amount == 0 ? 32 : amount;
                    s.S(", asr #"); s.N((uint)a);
                }
                else if (amount != 0) { s.S(", lsl #"); s.N((uint)amount); }
                return;
            }

            s.S(nm);
            if (test || !sf) s.S(".w");
            s.C(' ');
            if (!(rn == 15 && (opc == 2u || opc == 3u || opc == 4u))) { s.Reg(rd); s.S(", "); }
            else { s.Reg(rd); s.S(", "); }
            if (!(rn == 15 && (opc == 2u || opc == 3u || opc == 4u))) s.Reg(rn);
            if (!(rn == 15 && (opc == 2u || opc == 3u || opc == 4u)))
            {
                s.S(", ");
            }
            s.Reg(rm);
            if (type == 0 && amount == 0) { }
            else { if (type != 0 && amount == 0) amount = 32; s.Shift(type, amount); }
        }

        private static void T32DataProcessingModified(State s, uint instr)
        {
            uint hw1 = (instr >> 16) & 0xFFFFu;
            uint hw2 = instr & 0xFFFFu;
            uint i = (hw1 >> 10) & 1u;
            uint opc = (hw1 >> 5) & 0xFu;
            bool sf = (hw1 & 0x10u) != 0u;
            int rn = (int)(hw1 & 0xFu);
            int rd = (int)((hw2 >> 8) & 0xFu);
            uint imm12v = (i << 11) | (((hw2 >> 12) & 7u) << 8) | (hw2 & 0xFFu);
            uint imm = ArmCore.ThumbExpandImm(imm12v);

            bool alias = rd == 15 && (opc == 0x0u || opc == 0x4u || opc == 0x8u || opc == 0xDu);
            string nm;
            switch (opc)
            {
                case 0x0u: nm = alias ? "tst" : "and"; break;
                case 0x1u: nm = "bic"; break;
                case 0x2u: nm = rn == 15 ? "mov" : "orr"; break;
                case 0x3u: nm = rn == 15 ? "mvn" : "orn"; break;
                case 0x4u: nm = alias ? "teq" : "eor"; break;
                case 0x8u: nm = alias ? "cmn" : "add"; break;
                case 0xAu: nm = "adc"; break;
                case 0xBu: nm = "sbc"; break;
                case 0xDu: nm = alias ? "cmp" : "sub"; break;
                case 0xEu: nm = "rsb"; break;
                default: s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture)); return;
            }
            s.S(nm);
            if (alias) { s.S(".w "); s.Reg(rn); s.S(", "); s.Imm(imm); return; }
            if (sf) s.C('s');
            s.S(".w ");
            s.Reg(rd); s.S(", ");
            if (!(rn == 15 && (opc == 2u || opc == 4u || opc == 0u))) { s.Reg(rn); s.S(", "); }
            s.Imm(imm);
        }

        private static void T32DataProcessingPlain(State s, uint instr)
        {
            uint hw1 = (instr >> 16) & 0xFFFFu;
            uint hw2 = instr & 0xFFFFu;
            uint i = (hw1 >> 10) & 1u;
            uint opc = (hw1 >> 4) & 0x1Fu;
            int rn = (int)(hw1 & 0xFu);
            int rd = (int)((hw2 >> 8) & 0xFu);
            uint imm3v = (hw2 >> 12) & 7u;
            uint imm8 = hw2 & 0xFFu;
            uint imm12v = (i << 11) | (imm3v << 8) | imm8;
            uint imm16 = (uint)(((hw1 & 0xFu) << 12) | (i << 11) | (imm3v << 8) | imm8);
            int lsb = (int)(imm3v << 2 | ((hw2 >> 6) & 3u));
            int shiftType = (int)((hw2 >> 4) & 3u);

            switch (opc)
            {
                case 0x00u: s.S("addw "); s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Imm(imm12v); return;
                case 0x04u: s.S("movw "); s.Reg(rd); s.S(", "); s.Imm(imm16); return;
                case 0x0Au: s.S("subw "); s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Imm(imm12v); return;
                case 0x0Cu: s.S("movt "); s.Reg(rd); s.S(", "); s.Hx(imm16); return;
                case 0x10u:
                    s.S("ssat "); s.Reg(rd); s.S(", #"); s.N(hw1 & 0xFu); s.S(", "); s.Reg(rn);
                    if (lsb != 0) { s.S(shiftType == 2 ? ", asr #" : ", lsl #"); s.N((uint)lsb); }
                    return;
                case 0x12u:
                    s.S("ssat16 "); s.Reg(rd); s.S(", #"); s.N(hw1 & 0xFu); s.S(", "); s.Reg(rn); return;
                case 0x14u:
                    s.S("sbfx "); s.Reg(rd); s.S(", "); s.Reg(rn);
                    s.S(", #"); s.N((uint)lsb); s.S(", #"); s.N((uint)((hw2 >> 12) + 1)); return;
                case 0x16u:
                    if (rn == 15) { s.S("bfc "); s.Reg(rd); s.S(", #"); s.N((uint)lsb); s.S(", #"); s.N((uint)((hw2 >> 12) + 1)); }
                    else
                    {
                        s.S("bfi "); s.Reg(rd); s.S(", "); s.Reg(rn);
                        s.S(", #"); s.N((uint)lsb); s.S(", #"); s.N((uint)((hw2 >> 12) + 1));
                    }
                    return;
                case 0x18u:
                    s.S("usat "); s.Reg(rd); s.S(", #"); s.N(hw1 & 0xFu); s.S(", "); s.Reg(rn);
                    if (lsb != 0) { s.S(shiftType == 2 ? ", asr #" : ", lsl #"); s.N((uint)lsb); }
                    return;
                case 0x1Au:
                    s.S("usat16 "); s.Reg(rd); s.S(", #"); s.N(hw1 & 0xFu); s.S(", "); s.Reg(rn); return;
                case 0x1Cu:
                    s.S("ubfx "); s.Reg(rd); s.S(", "); s.Reg(rn);
                    s.S(", #"); s.N((uint)lsb); s.S(", #"); s.N((uint)((hw2 >> 12) + 1)); return;
                default:
                    s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
                    return;
            }
        }

        private static void T32LoadStoreMultiple(State s, uint instr)
        {
            int rn = (int)((instr >> 16) & 0xFu);
            bool load = (instr & 0x1000u) != 0;
            bool w = (instr & 0x2000u) != 0;
            s.S(load ? "ldm" : "stm");
            if (w) s.S(".w ");
            else s.S(".w ");
            s.Reg(rn);
            if (w) s.C('!');
            s.S(", ");
            s.RegList(instr & 0xFFFFu);
        }

        private static void T32LoadStoreDual(State s, uint addr, uint instr)
        {
            uint hw1 = (instr >> 16) & 0xFFFFu;
            uint hw2 = instr & 0xFFFFu;
            int rn = (int)(hw1 & 0xFu);
            int rt = (int)((hw2 >> 12) & 0xFu);
            bool db = (hw1 & 0x100u) != 0u;      // hw1[8]
            bool u = (hw1 & 0x080u) != 0u;       // hw1[7]
            bool fixed6 = (hw1 & 0x040u) != 0u;  // hw1[6]
            bool w = (hw1 & 0x020u) != 0u;       // hw1[5]
            bool l = (hw1 & 0x010u) != 0u;       // hw1[4]

            if (!fixed6)
            {
                // LDM / STM
                uint list = hw2;
                if (l && w && rn == 13) { s.S("pop.w "); }
                else if (!l && w && rn == 13) { s.S("push.w "); }
                else
                {
                    s.S(l ? "ldm" : "stm");
                    s.S(db ? "db " : "ia ");
                    s.Reg(rn);
                    if (w) s.C('!');
                    s.S(", ");
                }
                s.RegList(list);
                return;
            }

            if (hw1 == 0xE8C0u || hw1 == 0xE8D0u)
            {
                s.S(hw1 == 0xE8C0u ? "strexb " : "ldrexb ");
                if (hw1 == 0xE8C0u)
                {
                    s.Reg((int)((hw2 >> 8) & 0xFu)); s.S(", "); s.Reg(rt); s.S(", [");
                    s.Reg(rn); s.C(']');
                }
                else { s.Reg(rt); s.S(", ["); s.Reg(rn); s.C(']'); }
                return;
            }

            if (!u && !db && !w)
            {
                uint imm = (hw2 & 0xFFu) * 4u;
                if (!l)
                {
                    s.S("strex "); s.Reg((int)((hw2 >> 8) & 0xFu)); s.S(", "); s.Reg(rt);
                    s.S(", ["); s.Reg(rn);
                    if (imm != 0) { s.S(", #"); s.N(imm); }
                    s.C(']');
                }
                else
                {
                    s.S("ldrex "); s.Reg(rt); s.S(", ["); s.Reg(rn);
                    if (imm != 0) { s.S(", #"); s.N(imm); }
                    s.C(']');
                }
                return;
            }

            // LDRD / STRD
            int rt2 = (int)((hw2 >> 8) & 0xFu);
            uint imm8 = (hw2 & 0xFFu) * 4u;
            s.S(l ? "ldrd " : "strd ");
            s.Reg(rt); s.S(", "); s.Reg(rt2); s.S(", [");
            s.Reg(rn);
            if (db)
            {
                s.S(", ");
                if (!u) s.C('-');
                s.Imm(imm8); s.C(']');
                if (w) s.C('!');
            }
            else
            {
                s.C(']'); s.S(", ");
                if (!u) s.C('-');
                s.Imm(imm8);
            }
        }

        /// <summary>T32 data processing (register): multiply and parallel add/sub (0xFA00..0xFBFF).</summary>
        private static void T32LoadStoreSingle(State s, uint addr, uint instr)
        {
            uint hw1 = (instr >> 16) & 0xFFFFu;
            uint hw2 = instr & 0xFFFFu;
            int rn = (int)(hw1 & 0xFu);
            int rt = (int)((hw2 >> 12) & 0xFu);
            bool signedSpace = (hw1 & 0x100u) != 0u;
            bool imm12Form = (hw1 & 0x80u) != 0u;
            bool isWord = (hw1 & 0x40u) != 0u;
            bool isHalf = (hw1 & 0x20u) != 0u;
            bool l = (hw1 & 0x10u) != 0u;

            if (signedSpace)
            {
                uint sizeSel = hw1 & 0x30u;
                if (sizeSel != 0x10u && sizeSel != 0x30u)
                {
                    s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
                    return;
                }
                bool half = sizeSel == 0x30u;
                s.S(half ? "ldrsh.w " : "ldrsb.w ");
                s.Reg(rt); s.S(", [");
                s.Reg(rn);
                if (imm12Form)
                {
                    uint i = (hw2 >> 11) & 1u;
                    uint imm = (i << 11) | (hw2 & 0xFFFu);
                    s.S(", #"); s.N(imm); s.C(']');
                }
                else
                {
                    bool p = (hw2 & 0x400u) != 0u;
                    bool u = (hw2 & 0x200u) != 0u;
                    bool w = (hw2 & 0x100u) != 0u;
                    uint imm8 = hw2 & 0xFFu;
                    if (p) { s.S(", "); if (!u) s.C('-'); s.Imm(imm8); s.C(']'); if (w) s.C('!'); }
                    else { s.C(']'); s.S(", "); if (!u) s.C('-'); s.Imm(imm8); }
                }
                return;
            }

            if (l && rt == 15) { s.S(signedSpace ? "pli" : "pld"); return; }

            string baseName = isWord ? "" : (isHalf ? "h" : "b");
            s.S(l ? "ldr" : "str");
            s.S(baseName);
            s.S(".w ");
            s.Reg(rt); s.S(", [");
            s.Reg(rn);
            if (imm12Form)
            {
                if ((hw2 & 0x0800u) == 0u && (hw2 & 0x0F00u) == 0x0F00u)
                {
                    s.S(", "); s.Reg((int)(hw2 & 0xFu)); s.S(", lsl #0]");
                }
                else { s.S(", #"); s.N(hw2 & 0xFFFu); s.C(']'); }
                return;
            }
            {
                bool p = (hw2 & 0x400u) != 0u;
                bool u = (hw2 & 0x200u) != 0u;
                bool w = (hw2 & 0x100u) != 0u;
                uint imm8 = hw2 & 0xFFu;
                if (p) { s.S(", "); if (!u) s.C('-'); s.Imm(imm8); s.C(']'); if (w) s.C('!'); }
                else { s.C(']'); s.S(", "); if (!u) s.C('-'); s.Imm(imm8); }
            }
        }

        private static void T32BranchMisc(State s, uint addr, uint instr)
        {
            uint hw1 = (instr >> 16) & 0xFFFFu;
            uint hw2 = instr & 0xFFFFu;
            uint sv = (hw1 >> 10) & 1u;
            uint j1 = (hw2 >> 13) & 1u;
            uint j2 = (hw2 >> 11) & 1u;
            uint imm11 = hw2 & 0x7FFu;

            if ((hw2 & 0x4000u) == 0u)
            {
                if ((hw2 & 0x1000u) != 0u)
                {
                    uint imm10 = hw1 & 0x3FFu;
                    uint i1 = (~(j1 ^ sv)) & 1u;
                    uint i2 = (~(j2 ^ sv)) & 1u;
                    int off = SignExtend((sv << 24) | (i1 << 23) | (i2 << 22) | (imm10 << 12) | (imm11 << 1), 25);
                    s.S("b.w #"); s.Hx(addr + 4u + (uint)off);
                    return;
                }
                uint cond = (hw1 >> 6) & 0xFu;
                if (cond <= 0xDu)
                {
                    uint imm6 = hw1 & 0x3Fu;
                    int off = SignExtend((sv << 20) | (j2 << 19) | (j1 << 18) | (imm6 << 12) | (imm11 << 1), 21);
                    s.S("b"); s.S(ConditionName(cond));
                    s.S(".w #"); s.Hx(addr + 4u + (uint)off);
                    return;
                }
                T32Misc(s, addr, instr);
                return;
            }

            {
                uint imm10 = hw1 & 0x3FFu;
                uint i1 = (~(j1 ^ sv)) & 1u;
                uint i2 = (~(j2 ^ sv)) & 1u;
                if ((hw2 & 0x1000u) != 0u)
                {
                    int off = SignExtend((sv << 24) | (i1 << 23) | (i2 << 22) | (imm10 << 12) | (imm11 << 1), 25);
                    s.S("bl #"); s.Hx(addr + 4u + (uint)off);
                    return;
                }
                uint imm10l = (hw2 >> 1) & 0x3FFu;
                int offx = SignExtend((sv << 24) | (i1 << 23) | (i2 << 22) | (imm10 << 12) | (imm10l << 2), 25);
                s.S("blx #"); s.Hx(((addr + 4u) & ~3u) + (uint)offx);
                return;
            }
        }

        private static int SignExtend(uint value, int bits)
        {
            uint m = 1u << (bits - 1);
            return unchecked((int)((value ^ m) - m));
        }

        private static void T32Multiply(State s, uint instr)
        {
            uint hw1 = (instr >> 16) & 0xFFFFu;
            uint hw2 = instr & 0xFFFFu;
            uint op1 = (hw1 >> 4) & 0xFu;
            int rn = (int)(hw1 & 0xFu);
            int rd = (int)((hw2 >> 8) & 0xFu);
            int ra = (int)((hw2 >> 12) & 0xFu);
            int rm = (int)(hw2 & 0xFu);
            uint op2 = (hw2 >> 4) & 0xFu;

            if (((hw1 >> 8) & 0xFu) == 0xAu)
            {
                string[] names = { "sadd16", "sasx", "ssax", "ssub16", "sadd8", "ssub8" };
                string nm = names[op2 >> 1];
                if (op1 >= 4u) nm = "u" + nm;
                else if ((op1 & 3u) == 2u) nm = "q" + nm;
                else if ((op1 & 3u) == 3u) nm = "sh" + nm;
                s.S(nm); s.C(' ');
                s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm);
                return;
            }

            if (op1 == 0u && op2 == 0u)
            {
                if (ra == 15) { s.S("mul "); s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm); }
                else { s.S("mla "); s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(ra); }
                return;
            }
            if (op1 == 1u && op2 == 0u)
            {
                s.S("mls "); s.Reg(rd); s.S(", "); s.Reg(rn); s.S(", "); s.Reg(rm); s.S(", "); s.Reg(ra);
                return;
            }
            s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
        }

        /// <summary>T32 miscellaneous control (hw1[15:11] = 11110, hw2[15:14] = 10, hw2[12] = 0, cond = 111x).</summary>
        private static void T32Misc(State s, uint addr, uint instr)
        {
            uint hw1 = (instr >> 16) & 0xFFFFu;
            uint hw2 = instr & 0xFFFFu;
            int rd = (int)((hw2 >> 8) & 0xFu);
            int rm = (int)(hw2 & 0xFu);

            if (hw1 == 0xF3AFu && (hw2 & 0x8000u) != 0u)
            {
                switch (hw2 & 0xFFu)
                {
                    case 0x00u: s.S("nop.w"); return;
                    case 0x01u: s.S("yield.w"); return;
                    case 0x02u: s.S("wfe.w"); return;
                    case 0x03u: s.S("wfi.w"); return;
                    case 0x04u: s.S("sev.w"); return;
                    default: s.S("hint.w #"); s.N(hw2 & 0xFFu); return;
                }
            }
            if (hw1 == 0xF3BFu && (hw2 & 0xFF00u) == 0x8F00u)
            {
                s.S((hw2 & 0xF0u) == 0x10u ? "clrex" : (hw2 & 0xF0u) == 0x40u ? "dsb sy"
                      : (hw2 & 0xF0u) == 0x50u ? "dmb sy" : "isb sy");
                return;
            }
            if (hw1 == 0xF3EFu && (hw2 & 0xF000u) == 0x8000u)
            {
                bool spsr = (hw2 & 0x100u) != 0u;
                s.S("mrs "); s.Reg(rd); s.S(", ");
                s.S(spsr ? "spsr" : "cpsr");
                return;
            }
            if ((hw1 & 0xFFF0u) == 0xF380u && (hw2 & 0xF000u) == 0x8000u)
            {
                uint sysm = (hw2 >> 8) & 0xFu;
                bool spsr = (hw2 & 0x100u) != 0u;
                s.S("msr ");
                s.S(spsr ? "spsr_" : "cpsr_");
                if (sysm == 0u) s.C('c');
                else if (sysm == 1u) s.C('x');
                else if (sysm == 2u) s.C('s');
                else if (sysm == 3u) s.C('f');
                else s.S("fsxc");
                s.S(", "); s.Reg(rm);
                return;
            }
            if ((hw1 & 0xFFF0u) == 0xF7F0u) { s.S("svc.w #"); s.N(hw2 & 0xFFu); return; }
            if ((hw1 & 0xFFE0u) == 0xF7E0u) { s.S("hvc.w #"); s.N(hw2 & 0xFFu); return; }
            if ((hw1 & 0xFFF0u) == 0xF7F0u) { s.S("udf.w #"); s.N(hw2 & 0xFFu); return; }
            if (hw1 == 0xF3AFu && (hw2 & 0xFFF0u) == 0x80F0u) { s.S("dbg #"); s.N(hw2 & 0xFu); return; }

            s.S(".word 0x"); s.S(instr.ToString("X8", CultureInfo.InvariantCulture));
        }

    }
}
