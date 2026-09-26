// ---------------------------------------------------------------------------
// RL78 instruction decoder.
//
// Decodes one instruction into a table row (Rl78IsaTable.g.cs) plus the
// operand slots op[0] (destination) and op[1] (source), exactly as binutils'
// rl78-decode.c does.  Every row stores the fixed bits of its opcode, so the
// lookup is "read the switch byte, then test each candidate row with
// (byte & mask) == value"; for a two-byte opcode the whole 16-bit word
// (prefix byte + switch byte) is matched.
//
// Operand data is read at the offset the generator recorded (OpOff), which is
// the order in which the .opc macros consume bytes - *not* the slot order:
// for "bt %s1, $%a0" the short-direct byte comes before the displacement even
// though the destination slot is the branch target.
//
// The lookup is exact and allocation free: candidate rows are bucketed by the
// opcode byte at run time, and rows are inserted so that the most specific
// encoding is tested first.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace VitaTestSuite.Core
{
    public static class Rl78Decoder
    {
        public const int MaxLen = 7;

        private static readonly int[] s_prefixRows = new int[256];   // opcode bytes that are prefixes
        private static bool s_ready;

        public static int RowCount { get { return Rl78IsaTable.Count; } }

        public static void EnsureBuilt()
        {
            if (s_ready) return;
            s_ready = true;
            int n = Rl78IsaTable.Count;
            for (int r = 0; r < n; r++)
            {
                if (Rl78IsaTable.RowQ[r] == 2)
                {
                    // Two-byte opcode: selected by the prefix byte, matched on the
                    // whole 16-bit word.
                    byte p = Rl78IsaTable.RowP[r];
                    s_prefixRows[p] = 1;
                    if (s_byByte[p] == null) s_byByte[p] = new List<int>(8);
                    s_byByte[p].Add(r);
                }
                else
                {
                    // One-byte opcode: the fixed value may still leave variable field
                    // bits, so register the row under every byte value it matches.
                    int val = Rl78IsaTable.RowW[r];
                    int mask = Rl78IsaTable.RowM[r] & 0xFF;
                    for (int b = 0; b < 256; b++)
                    {
                        if ((b & mask) != (val & mask)) continue;
                        if (s_byByte[b] == null) s_byByte[b] = new List<int>(8);
                        s_byByte[b].Add(r);
                    }
                }
            }
        }

        private static readonly List<int>[] s_byByte = new List<int>[256];

        /// <summary>
        /// Select the most specific row whose fixed bits match the opcode.  For a
        /// two-byte opcode the whole 16-bit word is matched; for a one-byte opcode
        /// the single byte is.  The mask size is the tie-break, mirroring
        /// binutils' "spell out more bits first" rule.
        /// </summary>
        private static int Find(byte op0, byte op1, bool twoByte)
        {
            int best = -1;
            int bestMask = -1;
            int word = twoByte ? ((op0 << 8) | op1) : op0;
            List<int> l = s_byByte[op0];
            if (l == null) return -1;
            for (int i = 0; i < l.Count; i++)
            {
                int r = l[i];
                if (twoByte ? Rl78IsaTable.RowQ[r] != 2 : Rl78IsaTable.RowQ[r] != 1) continue;
                int m = Rl78IsaTable.RowM[r];
                int v = twoByte ? ((Rl78IsaTable.RowP[r] << 8) | Rl78IsaTable.RowW[r])
                                : Rl78IsaTable.RowW[r];
                if ((word & m) != (v & m)) continue;
                int bits = PopCount(m);
                if (bits > bestMask) { bestMask = bits; best = r; }
            }
            return best;
        }

        private static int PopCount(int v)
        {
            int c = 0;
            while (v != 0) { c += v & 1; v >>= 1; }
            return c;
        }

        /// <summary>True when the byte introduces a two-byte opcode.</summary>
        public static bool IsPrefix(byte b)
        {
            EnsureBuilt();
            return s_prefixRows[b] != 0;
        }

        public static Rl78Decoded Decode(IRl78ByteSource src, uint addr)
        {
            EnsureBuilt();
            bool es = false;
            int hdr = 0;
            byte b0 = src.ReadByte(addr);
            if (b0 == 0x11)
            {
                es = true;
                hdr = 1;
                b0 = src.ReadByte(addr + 1);
            }

            int row;
            if (s_prefixRows[b0] != 0)
            {
                byte b1 = src.ReadByte(addr + (uint)(hdr + 1));
                row = Find(b0, b1, true);
            }
            else
            {
                row = Find(b0, 0, false);
            }
            if (row < 0) return null;

            Rl78Decoded d = new Rl78Decoded();
            d.Row = row;
            d.HasEsPrefix = es;
            d.Length = Rl78IsaTable.RowN[row] + hdr;
            d.Id = (Rl78Id)Rl78IsaTable.RowId[row];
            d.Flags = Rl78IsaTable.RowFl[row];
            d.Word = Rl78IsaTable.RowSz[row] != 0;
            d.Syntax = Rl78IsaTable.Syn[Rl78IsaTable.RowSyn[row]];
            d.Mnemonic = Rl78IsaTable.Mnem[Rl78IsaTable.RowS[row]];

            // switch byte: for op[0] rows it is the first byte after an ES prefix,
            // for op[1] rows the second opcode byte
            int sw = Rl78IsaTable.RowQ[row] == 2 ? src.ReadByte(addr + (uint)(hdr + 1))
                                                 : b0;

            d.Op0 = DecodeOperand(src, addr, row, 0, sw, hdr, es, d.Length);
            d.Op1 = DecodeOperand(src, addr, row, 1, sw, hdr, es, d.Length);
            return d;
        }

        /// <summary>Diagnostic probe: which row index matches the given opcode bytes.</summary>
        public static int FindProbe(byte op0, byte op1)
        {
            EnsureBuilt();
            return Find(op0, op1, s_prefixRows[op0] != 0);
        }

        /// <summary>Diagnostic probe: the candidate row list for an opcode byte.</summary>
        public static string SlotProbe(int group, int key)
        {
            EnsureBuilt();
            List<int> l = s_byByte[key];
            if (l == null) return "(null)";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < l.Count; i++)
                sb.AppendFormat("[r{0} q{1} m{2:X4} v{3:X2} {4}] ", l[i], Rl78IsaTable.RowQ[l[i]],
                                Rl78IsaTable.RowM[l[i]], Rl78IsaTable.RowW[l[i]],
                                Rl78IsaTable.Mnem[Rl78IsaTable.RowS[l[i]]]);
            return sb.ToString();
        }

        public static bool IsPrefixProbe(byte b) { EnsureBuilt(); return s_prefixRows[b] != 0; }

        private static Rl78Operand DecodeOperand(IRl78ByteSource src, uint addr, int row,
                                                 int slot, int sw, int hdr, bool es, int len)
        {
            int k = row * 2 + slot;
            Rl78Operand op = new Rl78Operand();
            op.Type = (Rl78OpType)Rl78IsaTable.OpT[k];
            op.Condition = (Rl78Cond)Rl78IsaTable.OpCN[k];
            if (op.Type == Rl78OpType.None) return op;

            // ---- register / base register ----
            switch (Rl78IsaTable.OpRM[k])
            {
                case Rl78IsaTable.RMFixed:
                    op.Reg = (Rl78Reg)Rl78IsaTable.OpRF[k];
                    break;
                case Rl78IsaTable.RMField8:
                    // the 3-bit field selects X..H, i.e. RL78_Reg_X + field
                    op.Reg = (Rl78Reg)((int)Rl78Reg.X +
                                       ((sw >> Rl78IsaTable.OpRS[k]) & Rl78IsaTable.OpRX[k]));
                    break;
                case Rl78IsaTable.RMField16:
                    // the 2-bit field selects AX..HL, i.e. RL78_Reg_AX + field
                    op.Reg = (Rl78Reg)((int)Rl78Reg.AX +
                                       ((sw >> Rl78IsaTable.OpRS[k]) & Rl78IsaTable.OpRX[k]));
                    break;
                default:
                    op.Reg = Rl78Reg.None;
                    break;
            }
            op.Reg2 = (Rl78Reg)Rl78IsaTable.OpR2[k];

            // ---- bit number ----
            int bs = Rl78IsaTable.OpBS[k];
            if (bs == Rl78IsaTable.BSFixed) op.BitNumber = Rl78IsaTable.OpBF[k];
            else if (bs != Rl78IsaTable.BSNone)
                op.BitNumber = (byte)((sw >> bs) & Rl78IsaTable.OpBX[k]);

            op.Es = es && (op.Type == Rl78OpType.Ind || op.Type == Rl78OpType.BitInd);
            op.AddKind = (byte)Rl78IsaTable.OpRA[k];
            op.Offset = hdr + Rl78IsaTable.RowQ[row] + Rl78IsaTable.OpOff[k];

            // ---- addend ----
            switch (Rl78IsaTable.OpRA[k])
            {
                case Rl78IsaTable.AKNone:
                    break;

                case Rl78IsaTable.AKLit:
                    op.Addend = Rl78IsaTable.OpRP[k];
                    break;

                case Rl78IsaTable.AKField:
                    op.Addend = (sw >> Rl78IsaTable.OpRS[k]) & Rl78IsaTable.OpRX[k];
                    break;

                case Rl78IsaTable.AKImmu1:
                    op.Addend = (int)Read(src, addr, op.Offset, 1);
                    op.Size = 1;
                    break;
                case Rl78IsaTable.AKImmu2:
                    op.Addend = (int)Read(src, addr, op.Offset, 2);
                    op.Size = 2;
                    break;
                case Rl78IsaTable.AKImmu3:
                    op.Addend = (int)Read(src, addr, op.Offset, 3);
                    op.Size = 3;
                    break;
                case Rl78IsaTable.AKImms1:
                    op.Addend = (sbyte)src.ReadByte(addr + (uint)op.Offset);
                    op.Size = 1;
                    break;
                case Rl78IsaTable.AKImms2:
                    op.Addend = (short)Read(src, addr, op.Offset, 2);
                    op.Size = 2;
                    break;

                case Rl78IsaTable.AKSfr:
                    {
                        int n = src.ReadByte(addr + (uint)op.Offset);
                        op.Addend = (int)Rl78Isa.Sfr(n);
                        op.Address = op.Addend;
                        op.Size = 1;
                        break;
                    }
                case Rl78IsaTable.AKSaddr:
                    {
                        int n = src.ReadByte(addr + (uint)op.Offset);
                        op.Addend = (int)Rl78Isa.ShortDirect(n);
                        op.Address = op.Addend;
                        op.Size = 1;
                        break;
                    }

                case Rl78IsaTable.AKRel8:
                    {
                        int disp = (sbyte)src.ReadByte(addr + (uint)op.Offset);
                        op.Addend = (int)addr + len + disp;
                        op.Address = op.Addend;
                        op.Size = 1;
                        break;
                    }
                case Rl78IsaTable.AKRel16:
                    {
                        int disp = (short)Read(src, addr, op.Offset, 2);
                        op.Addend = (int)addr + len + disp;
                        op.Address = op.Addend;
                        op.Size = 2;
                        break;
                    }

                case Rl78IsaTable.AKCallt:
                    {
                        // 0x80 + mm*16 + nnn*2 : the CALLT table entry address
                        int nnn = (sw >> Rl78IsaTable.OpRS[k]) & Rl78IsaTable.OpRX[k];
                        int mm = (sw >> Rl78IsaTable.OpBS[k]) & Rl78IsaTable.OpBX[k];
                        op.Addend = 0x80 + mm * 16 + nnn * 2;
                        op.Address = op.Addend;
                        break;
                    }

                default:
                    break;
            }

            // For an address operand the decoder can resolve alone (SFR, saddr,
            // !addr16, CALLT) precompute the linear address; register based forms
            // are resolved by the core against the current register file.
            if ((op.Type == Rl78OpType.Ind || op.Type == Rl78OpType.BitInd) &&
                op.Reg == Rl78Reg.None &&
                (Rl78IsaTable.OpRA[k] == Rl78IsaTable.AKImmu2 ||
                 Rl78IsaTable.OpRA[k] == Rl78IsaTable.AKImmu3 ||
                 Rl78IsaTable.OpRA[k] == Rl78IsaTable.AKRel8 ||
                 Rl78IsaTable.OpRA[k] == Rl78IsaTable.AKRel16))
            {
                op.Address = op.Addend;
            }
            return op;
        }

        private static uint Read(IRl78ByteSource src, uint addr, int pos, int n)
        {
            uint v = 0;
            for (int i = 0; i < n; i++)
                v |= (uint)src.ReadByte(addr + (uint)(pos + i)) << (8 * i);
            return v;
        }

        /// <summary>Instruction length in bytes; 1 when the encoding is unknown.</summary>
        public static int Length(IRl78ByteSource src, uint addr)
        {
            Rl78Decoded d = Decode(src, addr);
            return d == null ? 1 : d.Length;
        }
    }
}
