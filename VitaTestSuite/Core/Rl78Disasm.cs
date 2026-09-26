// ---------------------------------------------------------------------------
// RL78 disassembler.
//
// Like binutils' rl78-dis.c this does not guess: the row's SYNTAX() template
// (taken verbatim from rl78-decode.opc) decides which operands are printed, in
// which order and with which modifiers:
//
//   %0 %1     operand 0 / operand 1
//   %e        print "es:" when the instruction was ES:-prefixed
//   %!        print "!" (direct address)
//   %a        print as a branch/call target
//   %s        print SFR names (sp, psw, spl, sph, cs, es, pmc, mem)
//   %c        print the condition of the source operand (sk%c1)
//   %x        hex
//
// This is why "bt %s1, $%a0" prints the bit operand first and the branch target
// second, and why "clrb %e!0" does not print the implicit constant operand.
// ---------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text;

namespace VitaTestSuite.Core
{
    public static class Rl78Disassembler
    {
        public static string Format(MemoryHub mem, uint addr, Rl78Decoded d)
        {
            return Format((IRl78ByteSource)new HubSource(mem), addr, d);
        }

        private sealed class HubSource : IRl78ByteSource
        {
            private readonly MemoryHub m_mem;
            public HubSource(MemoryHub mem) { m_mem = mem; }
            public byte ReadByte(uint addr) { return m_mem.ReadByte(addr); }
        }

        public static string Format(IRl78ByteSource src, uint addr, Rl78Decoded d)
        {
            if (d == null) return "??";
            string syn = d.Syntax;
            if (string.IsNullOrEmpty(syn)) return "??";

            // CLR1 PSW.7 / SET1 PSW.7 are the DI / EI aliases (rl78-decoder does
            // the same through rl78->syntax).
            if ((d.Mnemonic == "clr1" || d.Mnemonic == "set1") && IsPswBit7(d.Op0))
                return d.Mnemonic == "clr1" ? "di" : "ei";

            StringBuilder sb = new StringBuilder(40);
            for (int i = 0; i < syn.Length; i++)
            {
                char c = syn[i];
                if (c == '\t') { sb.Append(' '); continue; }
                if (c != '%') { sb.Append(c); continue; }

                bool hex = false, bang = false, es = false, addrMod = false, sfr = false, cond = false;
                int j = i + 1;
                while (j < syn.Length)
                {
                    char m = syn[j];
                    if (m == 'x') hex = true;
                    else if (m == '!') bang = true;
                    else if (m == 'e') es = true;
                    else if (m == 'a') addrMod = true;
                    else if (m == 's') sfr = true;
                    else if (m == 'c') cond = true;
                    else break;
                    j++;
                }
                if (j >= syn.Length) break;
                char oc = syn[j];
                i = j;

                if (oc == '%') { sb.Append('%'); continue; }
                if (oc != '0' && oc != '1') { sb.Append(oc); continue; }

                Rl78Operand o = d.Operand(oc - '0');
                if (cond) { sb.Append(Rl78Isa.CondNames[(int)o.Condition & 7]); continue; }
                // "!%!" would print two bangs: the literal one in the template wins
                if (bang && sb.Length > 0 && sb[sb.Length - 1] == '!') bang = false;
                AppendOperand(sb, o, hex, bang, es, addrMod, sfr, d.Word);
            }
            return sb.ToString();
        }

        private static bool IsPswBit7(Rl78Operand o)
        {
            return o.Type == Rl78OpType.BitInd && o.Reg == Rl78Reg.None &&
                   o.Address == (int)Rl78Isa.SfrPsw && o.BitNumber == 7;
        }

        private static void AppendOperand(StringBuilder sb, Rl78Operand o, bool hex, bool bang,
                                          bool es, bool addrMod, bool sfr, bool word)
        {
            switch (o.Type)
            {
                case Rl78OpType.None:
                    return;

                case Rl78OpType.PreDec:
                    sb.Append("[--").Append(Rl78Isa.RegName(o.Reg)).Append(']');
                    return;
                case Rl78OpType.PostInc:
                    sb.Append('[').Append(Rl78Isa.RegName(o.Reg)).Append("++]");
                    return;

                case Rl78OpType.Imm:
                    AppendImmediate(sb, o, hex, addrMod);
                    return;

                case Rl78OpType.Reg:
                    if (o.Reg == Rl78Reg.None) return;
                    sb.Append(Rl78Isa.RegName(o.Reg));
                    return;

                case Rl78OpType.Bit:
                    if (o.Reg == Rl78Reg.None) return;
                    sb.Append(Rl78Isa.RegName(o.Reg)).Append('.').Append(Bit(o));
                    return;

                case Rl78OpType.Ind:
                case Rl78OpType.BitInd:
                    AppendAddress(sb, o, bang, es, sfr, word);
                    if (o.Type == Rl78OpType.BitInd) sb.Append('.').Append(Bit(o));
                    return;

                default:
                    return;
            }
        }

        private static string Bit(Rl78Operand o)
        {
            return (o.BitNumber & 7).ToString(CultureInfo.InvariantCulture);
        }

        private static void AppendImmediate(StringBuilder sb, Rl78Operand o, bool hex, bool addrMod)
        {
            switch (o.AddKind)
            {
                case Rl78IsaTable.AKField:
                case Rl78IsaTable.AKLit:
                    // shift counts ("sar a, 4") and SEL bank numbers ("rb0")
                    sb.Append(o.Addend.ToString(CultureInfo.InvariantCulture));
                    return;
                default:
                    if (addrMod || o.AddKind == Rl78IsaTable.AKImmu3)
                    {
                        // branch / call target (20-bit code space)
                        sb.Append("0x").Append(((uint)o.Addend & 0xFFFFF).ToString("X5", CultureInfo.InvariantCulture));
                        return;
                    }
                    break;
            }
            int digits = o.Size == 1 ? 2 : o.Size == 3 ? 6 : 4;
            sb.Append("0x").Append(((uint)o.Addend & 0xFFFFu).ToString("X" + digits, CultureInfo.InvariantCulture));
        }

        private static void AppendAddress(StringBuilder sb, Rl78Operand o, bool bang, bool es,
                                          bool sfr, bool word)
        {
            if (es && o.Es) sb.Append("es:");

            if (o.Reg != Rl78Reg.None)
            {
                // register indirect / based
                Rl78Reg r = o.Reg;
                if (r == Rl78Reg.B || r == Rl78Reg.C || r == Rl78Reg.BC)
                {
                    // Renesas syntax: <base>[<reg>]
                    sb.Append("0x").Append(((uint)o.Addend & 0xFFFFu).ToString("X4", CultureInfo.InvariantCulture));
                    sb.Append('[').Append(Rl78Isa.RegName(r)).Append(']');
                    return;
                }
                sb.Append('[').Append(Rl78Isa.RegName(r));
                if (o.Reg2 != Rl78Reg.None) sb.Append('+').Append(Rl78Isa.RegName(o.Reg2));
                if (o.Addend != 0)
                    sb.Append("+0x").Append(((uint)o.Addend & 0xFFu).ToString("X2", CultureInfo.InvariantCulture));
                sb.Append(']');
                return;
            }

            // direct / short-direct / SFR
            uint a = (uint)o.Address & 0xFFFFFu;
            switch (o.AddKind)
            {
                case Rl78IsaTable.AKCallt:
                    sb.Append("0x").Append(a.ToString("X4", CultureInfo.InvariantCulture));
                    return;
                case Rl78IsaTable.AKSfr:
                case Rl78IsaTable.AKSaddr:
                    break;
                default:
                    // !addr16: the data page lives in ES, so show the raw address
                    if (bang) sb.Append('!');
                    sb.Append("0x").Append((a & 0xFFFFu).ToString("X4", CultureInfo.InvariantCulture));
                    return;
            }

            if (a >= 0xFFE20u)
            {
                if (sfr && !bang)
                {
                    string name = SfrName(a, word);
                    if (name != null) { sb.Append(name); return; }
                }
                sb.Append("0x").Append(a.ToString("X5", CultureInfo.InvariantCulture));
                return;
            }
            if (bang) sb.Append('!');
            sb.Append("0x").Append((a & 0xFFFFu).ToString("X4", CultureInfo.InvariantCulture));
        }

        /// <summary>Named control registers, as in rl78-dis.c.</summary>
        private static string SfrName(uint a, bool word)
        {
            if (a == Rl78Isa.SfrPsw) return word ? null : "psw";
            if (a == Rl78Isa.SfrSp)
            {
                if (word) return "sp";
                return "spl";
            }
            if (a == Rl78Isa.SfrSp + 1) return word ? null : "sph";
            if (a == Rl78Isa.SfrCs) return word ? null : "cs";
            if (a == Rl78Isa.SfrEs) return word ? null : "es";
            if (a == Rl78Isa.SfrPmc) return word ? null : "pmc";
            if (a == Rl78Isa.SfrMem) return word ? null : "mem";
            return null;
        }
    }
}
