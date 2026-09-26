// AUTO-GENERATED instruction tables for the Toshiba MeP-c5 (CMeP / Venezia MPE) core.
// Derived from the CGEN architecture description used by binutils/GDB 2.37
//   cpu/mep-core.cpu  (+ cpu/mep-c5.cpu for the MeP-c5 additions).
// Do not edit by hand: the encoding tables are machine generated.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace VitaTestSuite.Core
{
    /// <summary>One entry per MeP instruction in the CGEN opcode table.</summary>
    internal enum MePOp : int
    {
        None = 0,
        Sb = 1,
        Sh = 2,
        Sw = 3,
        Lb = 4,
        Lh = 5,
        Lw = 6,
        Lbu = 7,
        Lhu = 8,
        SwSp = 9,
        LwSp = 10,
        SbTp = 11,
        ShTp = 12,
        SwTp = 13,
        LbTp = 14,
        LhTp = 15,
        LwTp = 16,
        LbuTp = 17,
        LhuTp = 18,
        Sb16 = 19,
        Sh16 = 20,
        Sw16 = 21,
        Lb16 = 22,
        Lh16 = 23,
        Lw16 = 24,
        Lbu16 = 25,
        Lhu16 = 26,
        Sw24 = 27,
        Lw24 = 28,
        Extb = 29,
        Exth = 30,
        Extub = 31,
        Extuh = 32,
        Ssarb = 33,
        Mov = 34,
        Movi8 = 35,
        Movi16 = 36,
        Movu24 = 37,
        Movu16 = 38,
        Movh = 39,
        Add3 = 40,
        Add = 41,
        Add3i = 42,
        Advck3 = 43,
        Sub = 44,
        Sbvck3 = 45,
        Neg = 46,
        Slt3 = 47,
        Sltu3 = 48,
        Slt3i = 49,
        Sltu3i = 50,
        Sl1ad3 = 51,
        Sl2ad3 = 52,
        Add3x = 53,
        Slt3x = 54,
        Sltu3x = 55,
        Or = 56,
        And = 57,
        Xor = 58,
        Nor = 59,
        Or3 = 60,
        And3 = 61,
        Xor3 = 62,
        Sra = 63,
        Srl = 64,
        Sll = 65,
        Srai = 66,
        Srli = 67,
        Slli = 68,
        Sll3 = 69,
        Fsft = 70,
        Bra = 71,
        Beqz = 72,
        Bnez = 73,
        Beqi = 74,
        Bnei = 75,
        Blti = 76,
        Bgei = 77,
        Beq = 78,
        Bne = 79,
        Bsr12 = 80,
        Bsr24 = 81,
        Jmp = 82,
        Jmp24 = 83,
        Jsr = 84,
        Ret = 85,
        Repeat = 86,
        Erepeat = 87,
        StcLp = 88,
        StcHi = 89,
        StcLo = 90,
        Stc = 91,
        LdcLp = 92,
        LdcHi = 93,
        LdcLo = 94,
        Ldc = 95,
        Di = 96,
        Ei = 97,
        Reti = 98,
        Halt = 99,
        Sleep = 100,
        Swi = 101,
        Break = 102,
        Syncm = 103,
        Stcb = 104,
        Ldcb = 105,
        Bsetm = 106,
        Bclrm = 107,
        Bnotm = 108,
        Btstm = 109,
        Tas = 110,
        Cache = 111,
        Mul = 112,
        Mulu = 113,
        Mulr = 114,
        Mulru = 115,
        Madd = 116,
        Maddu = 117,
        Maddr = 118,
        Maddru = 119,
        Div = 120,
        Divu = 121,
        Dret = 122,
        Dbreak = 123,
        Ldz = 124,
        Abs = 125,
        Ave = 126,
        Min = 127,
        Max = 128,
        Minu = 129,
        Maxu = 130,
        Clip = 131,
        Clipu = 132,
        Sadd = 133,
        Ssub = 134,
        Saddu = 135,
        Ssubu = 136,
        Swcp = 137,
        Lwcp = 138,
        Smcp = 139,
        Lmcp = 140,
        Swcpi = 141,
        Lwcpi = 142,
        Smcpi = 143,
        Lmcpi = 144,
        Swcp16 = 145,
        Lwcp16 = 146,
        Smcp16 = 147,
        Lmcp16 = 148,
        Sbcpa = 149,
        Lbcpa = 150,
        Shcpa = 151,
        Lhcpa = 152,
        Swcpa = 153,
        Lwcpa = 154,
        Smcpa = 155,
        Lmcpa = 156,
        Sbcpm0 = 157,
        Lbcpm0 = 158,
        Shcpm0 = 159,
        Lhcpm0 = 160,
        Swcpm0 = 161,
        Lwcpm0 = 162,
        Smcpm0 = 163,
        Lmcpm0 = 164,
        Sbcpm1 = 165,
        Lbcpm1 = 166,
        Shcpm1 = 167,
        Lhcpm1 = 168,
        Swcpm1 = 169,
        Lwcpm1 = 170,
        Smcpm1 = 171,
        Lmcpm1 = 172,
        Bcpeq = 173,
        Bcpne = 174,
        Bcpat = 175,
        Bcpaf = 176,
        Synccp = 177,
        Jsrv = 178,
        Bsrv = 179,
        SimSyscall = 180,
        Ri0 = 181,
        Ri1 = 182,
        Ri2 = 183,
        Ri3 = 184,
        Ri4 = 185,
        Ri5 = 186,
        Ri6 = 187,
        Ri7 = 188,
        Ri8 = 189,
        Ri9 = 190,
        Ri10 = 191,
        Ri11 = 192,
        Ri12 = 193,
        Ri13 = 194,
        Ri14 = 195,
        Ri15 = 196,
        Ri17 = 197,
        Ri20 = 198,
        Ri21 = 199,
        Ri22 = 200,
        Ri23 = 201,
        Ri26 = 202,
        StcbR = 203,
        LdcbR = 204,
        Pref = 205,
        Prefd = 206,
        Casb3 = 207,
        Cash3 = 208,
        Casw3 = 209,
        Sbcp = 210,
        Lbcp = 211,
        Lbucp = 212,
        Shcp = 213,
        Lhcp = 214,
        Lhucp = 215,
        Lbucpa = 216,
        Lhucpa = 217,
        Lbucpm0 = 218,
        Lhucpm0 = 219,
        Lbucpm1 = 220,
        Lhucpm1 = 221,
        Uci = 222,
        Dsp = 223,
        Dsp0 = 224,
        Dsp1 = 225,
    }

    internal enum MePField : byte
    {
        Const = 0,
        F12s20,
        F12s4a2,
        F16s16,
        F16u16,
        F17s16a2,
        F24s5a2n,
        F24u4n,
        F24u5a2n,
        F24u8a4n,
        F24u8n,
        F2u10,
        F2u6,
        F3u5,
        F4u8,
        F5u24,
        F5u8,
        F6s8,
        F7u9,
        F7u9a2,
        F7u9a4,
        F8s8,
        F8s8a2,
        FC5Rm,
        FC5Rnm,
        FCallnum,
        FCcrn,
        FCdisp10,
        FCrn,
        FCrnx,
        FCsrn,
        FRl5,
        Rl,
        Rm,
        Rn,
        Rn3,
    }

    internal enum MePPrint : byte
    {
        Reg,
        CpReg,
        Csrn,
        UHex,
        SDec,
        Label,
        Cdisp10,
    }

    /// <summary>Description of one operand of one instruction.</summary>
    internal struct MePOpnd
    {
        public MePField Field;   // Const => use Const
        public uint Const;
        public MePPrint Print;
        public MePOpnd(MePField f, uint c, MePPrint p) { Field = f; Const = c; Print = p; }
    }

    internal sealed class MePInsn
    {
        public readonly string Mnem;
        public readonly string Fmt;
        public readonly uint Mask;
        public readonly uint Value;
        public readonly byte Len;    // 2 or 4 bytes
        public readonly MePOp Op;
        public readonly MePOpnd[] Ops;

        public MePInsn(string mnem, string fmt, uint mask, uint value, byte len, MePOp op, MePOpnd[] ops)
        { Mnem = mnem; Fmt = fmt; Mask = mask; Value = value; Len = len; Op = op; Ops = ops; }
    }

    internal static partial class MePIsa
    {
        public static readonly MePInsn[] Table = new MePInsn[]
        {
            new MePInsn("sb", "sb {0},({1})", 0x0000F00Fu, 0x00000008u, 2, MePOp.Sb, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sh", "sh {0},({1})", 0x0000F00Fu, 0x00000009u, 2, MePOp.Sh, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sw", "sw {0},({1})", 0x0000F00Fu, 0x0000000Au, 2, MePOp.Sw, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lb", "lb {0},({1})", 0x0000F00Fu, 0x0000000Cu, 2, MePOp.Lb, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lh", "lh {0},({1})", 0x0000F00Fu, 0x0000000Du, 2, MePOp.Lh, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lw", "lw {0},({1})", 0x0000F00Fu, 0x0000000Eu, 2, MePOp.Lw, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lbu", "lbu {0},({1})", 0x0000F00Fu, 0x0000000Bu, 2, MePOp.Lbu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lhu", "lhu {0},({1})", 0x0000F00Fu, 0x0000000Fu, 2, MePOp.Lhu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sw", "sw {0},{1}({2})", 0x0000F083u, 0x00004002u, 2, MePOp.SwSp, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9a4, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 15u, MePPrint.Reg) }),
            new MePInsn("lw", "lw {0},{1}({2})", 0x0000F083u, 0x00004003u, 2, MePOp.LwSp, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9a4, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 15u, MePPrint.Reg) }),
            new MePInsn("sb", "sb {0},{1}({2})", 0x0000F880u, 0x00008000u, 2, MePOp.SbTp, new MePOpnd[] { new MePOpnd(MePField.Rn3, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 13u, MePPrint.Reg) }),
            new MePInsn("sh", "sh {0},{1}({2})", 0x0000F881u, 0x00008080u, 2, MePOp.ShTp, new MePOpnd[] { new MePOpnd(MePField.Rn3, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9a2, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 13u, MePPrint.Reg) }),
            new MePInsn("sw", "sw {0},{1}({2})", 0x0000F883u, 0x00004082u, 2, MePOp.SwTp, new MePOpnd[] { new MePOpnd(MePField.Rn3, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9a4, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 13u, MePPrint.Reg) }),
            new MePInsn("lb", "lb {0},{1}({2})", 0x0000F880u, 0x00008800u, 2, MePOp.LbTp, new MePOpnd[] { new MePOpnd(MePField.Rn3, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 13u, MePPrint.Reg) }),
            new MePInsn("lh", "lh {0},{1}({2})", 0x0000F881u, 0x00008880u, 2, MePOp.LhTp, new MePOpnd[] { new MePOpnd(MePField.Rn3, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9a2, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 13u, MePPrint.Reg) }),
            new MePInsn("lw", "lw {0},{1}({2})", 0x0000F883u, 0x00004083u, 2, MePOp.LwTp, new MePOpnd[] { new MePOpnd(MePField.Rn3, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9a4, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 13u, MePPrint.Reg) }),
            new MePInsn("lbu", "lbu {0},{1}({2})", 0x0000F880u, 0x00004880u, 2, MePOp.LbuTp, new MePOpnd[] { new MePOpnd(MePField.Rn3, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 13u, MePPrint.Reg) }),
            new MePInsn("lhu", "lhu {0},{1}({2})", 0x0000F881u, 0x00008881u, 2, MePOp.LhuTp, new MePOpnd[] { new MePOpnd(MePField.Rn3, 0u, MePPrint.Reg), new MePOpnd(MePField.F7u9a2, 0u, MePPrint.UHex), new MePOpnd(MePField.Const, 13u, MePPrint.Reg) }),
            new MePInsn("sb", "sb {0},{1}({2})", 0x0000F00Fu, 0x0000C008u, 4, MePOp.Sb16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sh", "sh {0},{1}({2})", 0x0000F00Fu, 0x0000C009u, 4, MePOp.Sh16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sw", "sw {0},{1}({2})", 0x0000F00Fu, 0x0000C00Au, 4, MePOp.Sw16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lb", "lb {0},{1}({2})", 0x0000F00Fu, 0x0000C00Cu, 4, MePOp.Lb16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lh", "lh {0},{1}({2})", 0x0000F00Fu, 0x0000C00Du, 4, MePOp.Lh16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lw", "lw {0},{1}({2})", 0x0000F00Fu, 0x0000C00Eu, 4, MePOp.Lw16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lbu", "lbu {0},{1}({2})", 0x0000F00Fu, 0x0000C00Bu, 4, MePOp.Lbu16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lhu", "lhu {0},{1}({2})", 0x0000F00Fu, 0x0000C00Fu, 4, MePOp.Lhu16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sw", "sw {0},({1})", 0x0000F003u, 0x0000E002u, 4, MePOp.Sw24, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F24u8a4n, 0u, MePPrint.Label) }),
            new MePInsn("lw", "lw {0},({1})", 0x0000F003u, 0x0000E003u, 4, MePOp.Lw24, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F24u8a4n, 0u, MePPrint.Label) }),
            new MePInsn("extb", "extb {0}", 0x0000F0FFu, 0x0000100Du, 2, MePOp.Extb, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("exth", "exth {0}", 0x0000F0FFu, 0x0000102Du, 2, MePOp.Exth, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("extub", "extub {0}", 0x0000F0FFu, 0x0000108Du, 2, MePOp.Extub, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("extuh", "extuh {0}", 0x0000F0FFu, 0x000010ADu, 2, MePOp.Extuh, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("ssarb", "ssarb {0}({1})", 0x0000FC0Fu, 0x0000100Cu, 2, MePOp.Ssarb, new MePOpnd[] { new MePOpnd(MePField.F2u6, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("mov", "mov {0},{1}", 0x0000F00Fu, 0x00000000u, 2, MePOp.Mov, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("mov", "mov {0},{1}", 0x0000F000u, 0x00005000u, 2, MePOp.Movi8, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F8s8, 0u, MePPrint.SDec) }),
            new MePInsn("mov", "mov {0},{1}", 0x0000F0FFu, 0x0000C001u, 4, MePOp.Movi16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec) }),
            new MePInsn("movu", "movu {0},{1}", 0x0000F800u, 0x0000D000u, 4, MePOp.Movu24, new MePOpnd[] { new MePOpnd(MePField.Rn3, 0u, MePPrint.Reg), new MePOpnd(MePField.F24u8n, 0u, MePPrint.UHex) }),
            new MePInsn("movu", "movu {0},{1}", 0x0000F0FFu, 0x0000C011u, 4, MePOp.Movu16, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("movh", "movh {0},{1}", 0x0000F0FFu, 0x0000C021u, 4, MePOp.Movh, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("add3", "add3 {0},{1},{2}", 0x0000F000u, 0x00009000u, 2, MePOp.Add3, new MePOpnd[] { new MePOpnd(MePField.Rl, 0u, MePPrint.Reg), new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("add", "add {0},{1}", 0x0000F003u, 0x00006000u, 2, MePOp.Add, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F6s8, 0u, MePPrint.SDec) }),
            new MePInsn("add3", "add3 {0},{1},{2}", 0x0000F083u, 0x00004000u, 2, MePOp.Add3i, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Const, 15u, MePPrint.Reg), new MePOpnd(MePField.F7u9a4, 0u, MePPrint.UHex) }),
            new MePInsn("advck3", "advck3 $0,{0},{1}", 0x0000F00Fu, 0x00000007u, 2, MePOp.Advck3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sub", "sub {0},{1}", 0x0000F00Fu, 0x00000004u, 2, MePOp.Sub, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sbvck3", "sbvck3 $0,{0},{1}", 0x0000F00Fu, 0x00000005u, 2, MePOp.Sbvck3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("neg", "neg {0},{1}", 0x0000F00Fu, 0x00000001u, 2, MePOp.Neg, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("slt3", "slt3 $0,{0},{1}", 0x0000F00Fu, 0x00000002u, 2, MePOp.Slt3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sltu3", "sltu3 $0,{0},{1}", 0x0000F00Fu, 0x00000003u, 2, MePOp.Sltu3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("slt3", "slt3 $0,{0},{1}", 0x0000F007u, 0x00006001u, 2, MePOp.Slt3i, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F5u8, 0u, MePPrint.UHex) }),
            new MePInsn("sltu3", "sltu3 $0,{0},{1}", 0x0000F007u, 0x00006005u, 2, MePOp.Sltu3i, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F5u8, 0u, MePPrint.UHex) }),
            new MePInsn("sl1ad3", "sl1ad3 $0,{0},{1}", 0x0000F00Fu, 0x00002006u, 2, MePOp.Sl1ad3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sl2ad3", "sl2ad3 $0,{0},{1}", 0x0000F00Fu, 0x00002007u, 2, MePOp.Sl2ad3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("add3", "add3 {0},{1},{2}", 0x0000F00Fu, 0x0000C000u, 4, MePOp.Add3x, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec) }),
            new MePInsn("slt3", "slt3 {0},{1},{2}", 0x0000F00Fu, 0x0000C002u, 4, MePOp.Slt3x, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec) }),
            new MePInsn("sltu3", "sltu3 {0},{1},{2}", 0x0000F00Fu, 0x0000C003u, 4, MePOp.Sltu3x, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("or", "or {0},{1}", 0x0000F00Fu, 0x00001000u, 2, MePOp.Or, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("and", "and {0},{1}", 0x0000F00Fu, 0x00001001u, 2, MePOp.And, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("xor", "xor {0},{1}", 0x0000F00Fu, 0x00001002u, 2, MePOp.Xor, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("nor", "nor {0},{1}", 0x0000F00Fu, 0x00001003u, 2, MePOp.Nor, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("or3", "or3 {0},{1},{2}", 0x0000F00Fu, 0x0000C004u, 4, MePOp.Or3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("and3", "and3 {0},{1},{2}", 0x0000F00Fu, 0x0000C005u, 4, MePOp.And3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("xor3", "xor3 {0},{1},{2}", 0x0000F00Fu, 0x0000C006u, 4, MePOp.Xor3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("sra", "sra {0},{1}", 0x0000F00Fu, 0x0000200Du, 2, MePOp.Sra, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("srl", "srl {0},{1}", 0x0000F00Fu, 0x0000200Cu, 2, MePOp.Srl, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sll", "sll {0},{1}", 0x0000F00Fu, 0x0000200Eu, 2, MePOp.Sll, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sra", "sra {0},{1}", 0x0000F007u, 0x00006003u, 2, MePOp.Srai, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F5u8, 0u, MePPrint.UHex) }),
            new MePInsn("srl", "srl {0},{1}", 0x0000F007u, 0x00006002u, 2, MePOp.Srli, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F5u8, 0u, MePPrint.UHex) }),
            new MePInsn("sll", "sll {0},{1}", 0x0000F007u, 0x00006006u, 2, MePOp.Slli, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F5u8, 0u, MePPrint.UHex) }),
            new MePInsn("sll3", "sll3 $0,{0},{1}", 0x0000F007u, 0x00006007u, 2, MePOp.Sll3, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F5u8, 0u, MePPrint.UHex) }),
            new MePInsn("fsft", "fsft {0},{1}", 0x0000F00Fu, 0x0000200Fu, 2, MePOp.Fsft, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("bra", "bra {0}", 0x0000F001u, 0x0000B000u, 2, MePOp.Bra, new MePOpnd[] { new MePOpnd(MePField.F12s4a2, 0u, MePPrint.Label) }),
            new MePInsn("beqz", "beqz {0},{1}", 0x0000F001u, 0x0000A000u, 2, MePOp.Beqz, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F8s8a2, 0u, MePPrint.Label) }),
            new MePInsn("bnez", "bnez {0},{1}", 0x0000F001u, 0x0000A001u, 2, MePOp.Bnez, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F8s8a2, 0u, MePPrint.Label) }),
            new MePInsn("beqi", "beqi {0},{1},{2}", 0x0000F00Fu, 0x0000E000u, 4, MePOp.Beqi, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F4u8, 0u, MePPrint.UHex), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("bnei", "bnei {0},{1},{2}", 0x0000F00Fu, 0x0000E004u, 4, MePOp.Bnei, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F4u8, 0u, MePPrint.UHex), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("blti", "blti {0},{1},{2}", 0x0000F00Fu, 0x0000E00Cu, 4, MePOp.Blti, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F4u8, 0u, MePPrint.UHex), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("bgei", "bgei {0},{1},{2}", 0x0000F00Fu, 0x0000E008u, 4, MePOp.Bgei, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F4u8, 0u, MePPrint.UHex), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("beq", "beq {0},{1},{2}", 0x0000F00Fu, 0x0000E001u, 4, MePOp.Beq, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("bne", "bne {0},{1},{2}", 0x0000F00Fu, 0x0000E005u, 4, MePOp.Bne, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("bsr", "bsr {0}", 0x0000F001u, 0x0000B001u, 2, MePOp.Bsr12, new MePOpnd[] { new MePOpnd(MePField.F12s4a2, 0u, MePPrint.Label) }),
            new MePInsn("bsr", "bsr {0}", 0x0000F80Fu, 0x0000D809u, 4, MePOp.Bsr24, new MePOpnd[] { new MePOpnd(MePField.F24s5a2n, 0u, MePPrint.Label) }),
            new MePInsn("jmp", "jmp {0}", 0x0000FF0Fu, 0x0000100Eu, 2, MePOp.Jmp, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("jmp", "jmp {0}", 0x0000F80Fu, 0x0000D808u, 4, MePOp.Jmp24, new MePOpnd[] { new MePOpnd(MePField.F24u5a2n, 0u, MePPrint.Label) }),
            new MePInsn("jsr", "jsr {0}", 0x0000FF0Fu, 0x0000100Fu, 2, MePOp.Jsr, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("ret", "ret", 0x0000FFFFu, 0x00007002u, 2, MePOp.Ret, new MePOpnd[] {  }),
            new MePInsn("repeat", "repeat {0},{1}", 0x0000F0FFu, 0x0000E009u, 4, MePOp.Repeat, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("erepeat", "erepeat {0}", 0x0000FFFFu, 0x0000E019u, 4, MePOp.Erepeat, new MePOpnd[] { new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("stc", "stc {0},$lp", 0x0000F0FFu, 0x00007018u, 2, MePOp.StcLp, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("stc", "stc {0},$hi", 0x0000F0FFu, 0x00007078u, 2, MePOp.StcHi, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("stc", "stc {0},$lo", 0x0000F0FFu, 0x00007088u, 2, MePOp.StcLo, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("stc", "stc {0},{1}", 0x0000F00Eu, 0x00007008u, 2, MePOp.Stc, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.FCsrn, 0u, MePPrint.Csrn) }),
            new MePInsn("ldc", "ldc {0},$lp", 0x0000F0FFu, 0x0000701Au, 2, MePOp.LdcLp, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("ldc", "ldc {0},$hi", 0x0000F0FFu, 0x0000707Au, 2, MePOp.LdcHi, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("ldc", "ldc {0},$lo", 0x0000F0FFu, 0x0000708Au, 2, MePOp.LdcLo, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg) }),
            new MePInsn("ldc", "ldc {0},{1}", 0x0000F00Eu, 0x0000700Au, 2, MePOp.Ldc, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.FCsrn, 0u, MePPrint.Csrn) }),
            new MePInsn("di", "di", 0x0000FFFFu, 0x00007000u, 2, MePOp.Di, new MePOpnd[] {  }),
            new MePInsn("ei", "ei", 0x0000FFFFu, 0x00007010u, 2, MePOp.Ei, new MePOpnd[] {  }),
            new MePInsn("reti", "reti", 0x0000FFFFu, 0x00007012u, 2, MePOp.Reti, new MePOpnd[] {  }),
            new MePInsn("halt", "halt", 0x0000FFFFu, 0x00007022u, 2, MePOp.Halt, new MePOpnd[] {  }),
            new MePInsn("sleep", "sleep", 0x0000FFFFu, 0x00007062u, 2, MePOp.Sleep, new MePOpnd[] {  }),
            new MePInsn("swi", "swi {0}", 0x0000FFCFu, 0x00007006u, 2, MePOp.Swi, new MePOpnd[] { new MePOpnd(MePField.F2u10, 0u, MePPrint.UHex) }),
            new MePInsn("break", "break", 0x0000FFFFu, 0x00007032u, 2, MePOp.Break, new MePOpnd[] {  }),
            new MePInsn("syncm", "syncm", 0x0000FFFFu, 0x00007011u, 2, MePOp.Syncm, new MePOpnd[] {  }),
            new MePInsn("stcb", "stcb {0},{1}", 0x0000F0FFu, 0x0000F004u, 4, MePOp.Stcb, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("ldcb", "ldcb {0},{1}", 0x0000F0FFu, 0x0000F014u, 4, MePOp.Ldcb, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("bsetm", "bsetm ({0}),{1}", 0x0000F80Fu, 0x00002000u, 2, MePOp.Bsetm, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F3u5, 0u, MePPrint.UHex) }),
            new MePInsn("bclrm", "bclrm ({0}),{1}", 0x0000F80Fu, 0x00002001u, 2, MePOp.Bclrm, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F3u5, 0u, MePPrint.UHex) }),
            new MePInsn("bnotm", "bnotm ({0}),{1}", 0x0000F80Fu, 0x00002002u, 2, MePOp.Bnotm, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F3u5, 0u, MePPrint.UHex) }),
            new MePInsn("btstm", "btstm $0,({0}),{1}", 0x0000F80Fu, 0x00002003u, 2, MePOp.Btstm, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F3u5, 0u, MePPrint.UHex) }),
            new MePInsn("tas", "tas {0},({1})", 0x0000F00Fu, 0x00002004u, 2, MePOp.Tas, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("cache", "cache {0},({1})", 0x0000F00Fu, 0x00007004u, 2, MePOp.Cache, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.UHex), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("mul", "mul {0},{1}", 0x0000F00Fu, 0x00001004u, 2, MePOp.Mul, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("mulu", "mulu {0},{1}", 0x0000F00Fu, 0x00001005u, 2, MePOp.Mulu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("mulr", "mulr {0},{1}", 0x0000F00Fu, 0x00001006u, 2, MePOp.Mulr, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("mulru", "mulru {0},{1}", 0x0000F00Fu, 0x00001007u, 2, MePOp.Mulru, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("madd", "madd {0},{1}", 0xFFFFF00Fu, 0x3004F001u, 4, MePOp.Madd, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("maddu", "maddu {0},{1}", 0xFFFFF00Fu, 0x3005F001u, 4, MePOp.Maddu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("maddr", "maddr {0},{1}", 0xFFFFF00Fu, 0x3006F001u, 4, MePOp.Maddr, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("maddru", "maddru {0},{1}", 0xFFFFF00Fu, 0x3007F001u, 4, MePOp.Maddru, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("div", "div {0},{1}", 0x0000F00Fu, 0x00001008u, 2, MePOp.Div, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("divu", "divu {0},{1}", 0x0000F00Fu, 0x00001009u, 2, MePOp.Divu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("dret", "dret", 0x0000FFFFu, 0x00007013u, 2, MePOp.Dret, new MePOpnd[] {  }),
            new MePInsn("dbreak", "dbreak", 0x0000FFFFu, 0x00007033u, 2, MePOp.Dbreak, new MePOpnd[] {  }),
            new MePInsn("ldz", "ldz {0},{1}", 0xFFFFF00Fu, 0x0000F001u, 4, MePOp.Ldz, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("abs", "abs {0},{1}", 0xFFFFF00Fu, 0x0003F001u, 4, MePOp.Abs, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("ave", "ave {0},{1}", 0xFFFFF00Fu, 0x0002F001u, 4, MePOp.Ave, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("min", "min {0},{1}", 0xFFFFF00Fu, 0x0004F001u, 4, MePOp.Min, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("max", "max {0},{1}", 0xFFFFF00Fu, 0x0005F001u, 4, MePOp.Max, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("minu", "minu {0},{1}", 0xFFFFF00Fu, 0x0006F001u, 4, MePOp.Minu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("maxu", "maxu {0},{1}", 0xFFFFF00Fu, 0x0007F001u, 4, MePOp.Maxu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("clip", "clip {0},{1}", 0xFF07F0FFu, 0x1000F001u, 4, MePOp.Clip, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F5u24, 0u, MePPrint.UHex) }),
            new MePInsn("clipu", "clipu {0},{1}", 0xFF07F0FFu, 0x1001F001u, 4, MePOp.Clipu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.F5u24, 0u, MePPrint.UHex) }),
            new MePInsn("sadd", "sadd {0},{1}", 0xFFFFF00Fu, 0x0008F001u, 4, MePOp.Sadd, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("ssub", "ssub {0},{1}", 0xFFFFF00Fu, 0x000AF001u, 4, MePOp.Ssub, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("saddu", "saddu {0},{1}", 0xFFFFF00Fu, 0x0009F001u, 4, MePOp.Saddu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("ssubu", "ssubu {0},{1}", 0xFFFFF00Fu, 0x000BF001u, 4, MePOp.Ssubu, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("swcp", "swcp {0},({1})", 0x0000F00Fu, 0x00003008u, 2, MePOp.Swcp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lwcp", "lwcp {0},({1})", 0x0000F00Fu, 0x00003009u, 2, MePOp.Lwcp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("smcp", "smcp {0},({1})", 0x0000F00Fu, 0x0000300Au, 2, MePOp.Smcp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lmcp", "lmcp {0},({1})", 0x0000F00Fu, 0x0000300Bu, 2, MePOp.Lmcp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("swcpi", "swcpi {0},({1}+)", 0x0000F00Fu, 0x00003000u, 2, MePOp.Swcpi, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lwcpi", "lwcpi {0},({1}+)", 0x0000F00Fu, 0x00003001u, 2, MePOp.Lwcpi, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("smcpi", "smcpi {0},({1}+)", 0x0000F00Fu, 0x00003002u, 2, MePOp.Smcpi, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lmcpi", "lmcpi {0},({1}+)", 0x0000F00Fu, 0x00003003u, 2, MePOp.Lmcpi, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("swcp", "swcp {0},{1}({2})", 0x0000F00Fu, 0x0000F00Cu, 4, MePOp.Swcp16, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lwcp", "lwcp {0},{1}({2})", 0x0000F00Fu, 0x0000F00Du, 4, MePOp.Lwcp16, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("smcp", "smcp {0},{1}({2})", 0x0000F00Fu, 0x0000F00Eu, 4, MePOp.Smcp16, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lmcp", "lmcp {0},{1}({2})", 0x0000F00Fu, 0x0000F00Fu, 4, MePOp.Lmcp16, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sbcpa", "sbcpa {0},({1}+),{2}", 0xFC00F00Fu, 0x0000F005u, 4, MePOp.Sbcpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lbcpa", "lbcpa {0},({1}+),{2}", 0xFC00F00Fu, 0x4000F005u, 4, MePOp.Lbcpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("shcpa", "shcpa {0},({1}+),{2}", 0xFC00F00Fu, 0x1000F005u, 4, MePOp.Shcpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lhcpa", "lhcpa {0},({1}+),{2}", 0xFC00F00Fu, 0x5000F005u, 4, MePOp.Lhcpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("swcpa", "swcpa {0},({1}+),{2}", 0xFC00F00Fu, 0x2000F005u, 4, MePOp.Swcpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lwcpa", "lwcpa {0},({1}+),{2}", 0xFC00F00Fu, 0x6000F005u, 4, MePOp.Lwcpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("smcpa", "smcpa {0},({1}+),{2}", 0xFC00F00Fu, 0x3000F005u, 4, MePOp.Smcpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lmcpa", "lmcpa {0},({1}+),{2}", 0xFC00F00Fu, 0x7000F005u, 4, MePOp.Lmcpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("sbcpm0", "sbcpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0x0800F005u, 4, MePOp.Sbcpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lbcpm0", "lbcpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0x4800F005u, 4, MePOp.Lbcpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("shcpm0", "shcpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0x1800F005u, 4, MePOp.Shcpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lhcpm0", "lhcpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0x5800F005u, 4, MePOp.Lhcpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("swcpm0", "swcpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0x2800F005u, 4, MePOp.Swcpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lwcpm0", "lwcpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0x6800F005u, 4, MePOp.Lwcpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("smcpm0", "smcpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0x3800F005u, 4, MePOp.Smcpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lmcpm0", "lmcpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0x7800F005u, 4, MePOp.Lmcpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("sbcpm1", "sbcpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0x0C00F005u, 4, MePOp.Sbcpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lbcpm1", "lbcpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0x4C00F005u, 4, MePOp.Lbcpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("shcpm1", "shcpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0x1C00F005u, 4, MePOp.Shcpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lhcpm1", "lhcpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0x5C00F005u, 4, MePOp.Lhcpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("swcpm1", "swcpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0x2C00F005u, 4, MePOp.Swcpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lwcpm1", "lwcpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0x6C00F005u, 4, MePOp.Lwcpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("smcpm1", "smcpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0x3C00F005u, 4, MePOp.Smcpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lmcpm1", "lmcpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0x7C00F005u, 4, MePOp.Lmcpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("bcpeq", "bcpeq {0},{1}", 0x0000FF0Fu, 0x0000D804u, 4, MePOp.Bcpeq, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.UHex), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("bcpne", "bcpne {0},{1}", 0x0000FF0Fu, 0x0000D805u, 4, MePOp.Bcpne, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.UHex), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("bcpat", "bcpat {0},{1}", 0x0000FF0Fu, 0x0000D806u, 4, MePOp.Bcpat, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.UHex), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("bcpaf", "bcpaf {0},{1}", 0x0000FF0Fu, 0x0000D807u, 4, MePOp.Bcpaf, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.UHex), new MePOpnd(MePField.F17s16a2, 0u, MePPrint.Label) }),
            new MePInsn("synccp", "synccp", 0x0000FFFFu, 0x00007021u, 2, MePOp.Synccp, new MePOpnd[] {  }),
            new MePInsn("jsrv", "jsrv {0}", 0x0000FF0Fu, 0x0000180Fu, 2, MePOp.Jsrv, new MePOpnd[] { new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("bsrv", "bsrv {0}", 0x0000F80Fu, 0x0000D80Bu, 4, MePOp.Bsrv, new MePOpnd[] { new MePOpnd(MePField.F24s5a2n, 0u, MePPrint.Label) }),
            new MePInsn("--syscall--", "--syscall--", 0x0000F8EFu, 0x00007800u, 4, MePOp.SimSyscall, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x00000006u, 2, MePOp.Ri0, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000100Au, 2, MePOp.Ri1, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000100Bu, 2, MePOp.Ri2, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x00002005u, 2, MePOp.Ri3, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x00002008u, 2, MePOp.Ri4, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x00002009u, 2, MePOp.Ri5, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000200Au, 2, MePOp.Ri6, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000200Bu, 2, MePOp.Ri7, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x00003004u, 2, MePOp.Ri8, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x00003005u, 2, MePOp.Ri9, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x00003006u, 2, MePOp.Ri10, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x00003007u, 2, MePOp.Ri11, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000300Cu, 2, MePOp.Ri12, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000300Du, 2, MePOp.Ri13, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000300Eu, 2, MePOp.Ri14, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000300Fu, 2, MePOp.Ri15, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x00007007u, 2, MePOp.Ri17, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000700Eu, 2, MePOp.Ri20, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000700Fu, 2, MePOp.Ri21, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000C007u, 2, MePOp.Ri22, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000E00Du, 2, MePOp.Ri23, new MePOpnd[] {  }),
            new MePInsn("--reserved--", "--reserved--", 0x0000F00Fu, 0x0000F008u, 2, MePOp.Ri26, new MePOpnd[] {  }),
            new MePInsn("stcb", "stcb {0},({1})", 0x0000F00Fu, 0x0000700Cu, 2, MePOp.StcbR, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("ldcb", "ldcb {0},({1})", 0x0000F00Fu, 0x0000700Du, 2, MePOp.LdcbR, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("pref", "pref {0},({1})", 0x0000F00Fu, 0x00007005u, 2, MePOp.Pref, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.UHex), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("pref", "pref {0},{1}({2})", 0x0000F00Fu, 0x0000F003u, 4, MePOp.Prefd, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.UHex), new MePOpnd(MePField.F16s16, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("casb3", "casb3 {0},{1},({2})", 0xF0FFF00Fu, 0x2000F001u, 4, MePOp.Casb3, new MePOpnd[] { new MePOpnd(MePField.FRl5, 0u, MePPrint.Reg), new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("cash3", "cash3 {0},{1},({2})", 0xF0FFF00Fu, 0x2001F001u, 4, MePOp.Cash3, new MePOpnd[] { new MePOpnd(MePField.FRl5, 0u, MePPrint.Reg), new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("casw3", "casw3 {0},{1},({2})", 0xF0FFF00Fu, 0x2002F001u, 4, MePOp.Casw3, new MePOpnd[] { new MePOpnd(MePField.FRl5, 0u, MePPrint.Reg), new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("sbcp", "sbcp {0},{1}({2})", 0xF000F00Fu, 0x0000F006u, 4, MePOp.Sbcp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F12s20, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lbcp", "lbcp {0},{1}({2})", 0xF000F00Fu, 0x4000F006u, 4, MePOp.Lbcp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F12s20, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lbucp", "lbucp {0},{1}({2})", 0xF000F00Fu, 0xC000F006u, 4, MePOp.Lbucp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F12s20, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("shcp", "shcp {0},{1}({2})", 0xF000F00Fu, 0x1000F006u, 4, MePOp.Shcp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F12s20, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lhcp", "lhcp {0},{1}({2})", 0xF000F00Fu, 0x5000F006u, 4, MePOp.Lhcp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F12s20, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lhucp", "lhucp {0},{1}({2})", 0xF000F00Fu, 0xD000F006u, 4, MePOp.Lhucp, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.F12s20, 0u, MePPrint.SDec), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg) }),
            new MePInsn("lbucpa", "lbucpa {0},({1}+),{2}", 0xFC00F00Fu, 0xC000F005u, 4, MePOp.Lbucpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lhucpa", "lhucpa {0},({1}+),{2}", 0xFC00F00Fu, 0xD000F005u, 4, MePOp.Lhucpa, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lbucpm0", "lbucpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0xC800F005u, 4, MePOp.Lbucpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lhucpm0", "lhucpm0 {0},({1}+),{2}", 0xFC00F00Fu, 0xD800F005u, 4, MePOp.Lhucpm0, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lbucpm1", "lbucpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0xCC00F005u, 4, MePOp.Lbucpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("lhucpm1", "lhucpm1 {0},({1}+),{2}", 0xFC00F00Fu, 0xDC00F005u, 4, MePOp.Lhucpm1, new MePOpnd[] { new MePOpnd(MePField.FCrn, 0u, MePPrint.CpReg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.FCdisp10, 0u, MePPrint.Cdisp10) }),
            new MePInsn("uci", "uci {0},{1},{2}", 0x0000F00Fu, 0x0000F002u, 4, MePOp.Uci, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("dsp", "dsp {0},{1},{2}", 0x0000F00Fu, 0x0000F000u, 4, MePOp.Dsp, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.Rm, 0u, MePPrint.Reg), new MePOpnd(MePField.F16u16, 0u, MePPrint.UHex) }),
            new MePInsn("dsp0", "dsp0 {0}", 0x0000F00Fu, 0x0000F000u, 4, MePOp.Dsp0, new MePOpnd[] { new MePOpnd(MePField.FC5Rnm, 0u, MePPrint.UHex) }),
            new MePInsn("dsp1", "dsp1 {0},{1}", 0x0000F00Fu, 0x0000F000u, 4, MePOp.Dsp1, new MePOpnd[] { new MePOpnd(MePField.Rn, 0u, MePPrint.Reg), new MePOpnd(MePField.FC5Rm, 0u, MePPrint.UHex) }),
        };
    }

    // ------------------------------------------------------------------
    // Hand written part of the decoder: CGEN bit-field extraction, opcode
    // lookup and the objdump compatible operand formatter.
    // ------------------------------------------------------------------
    internal static partial class MePIsa
    {
        internal static uint Raw(uint word, int start, int length)
        {
            int baseOff = start & ~15;
            int local = start - baseOff;
            uint chunk = (word >> baseOff) & 0xFFFFu;
            return (chunk >> (16 - local - length)) & ((1u << length) - 1u);
        }

        internal static uint Sext(uint v, int bits)
        {
            int sh = 32 - bits;
            return (uint)((int)(v << sh) >> sh);
        }

        private static uint Rel(uint word, uint pc, int start, int length, int shift)
        {
            return (uint)((int)pc + (int)(Sext(Raw(word, start, length), length) << shift));
        }

        internal static uint FieldValue(MePField f, uint word, uint pc)
        {
            switch (f)
            {
                case MePField.Const: return 0;
                case MePField.Rn: return Raw(word, 4, 4);
                case MePField.Rm: return Raw(word, 8, 4);
                case MePField.Rl: return Raw(word, 12, 4);
                case MePField.Rn3: return Raw(word, 5, 3);
                case MePField.F8s8a2: return Rel(word, pc, 8, 7, 1);
                case MePField.F12s4a2: return Rel(word, pc, 4, 11, 1);
                case MePField.F17s16a2: return Rel(word, pc, 16, 16, 1);
                case MePField.F24s5a2n:
                    {
                        uint hi = Raw(word, 16, 16);
                        uint lo = Raw(word, 5, 7);
                        uint disp = (hi << 8) | (lo << 1);
                        if ((disp & 0x800000u) != 0) disp |= 0xFF000000u;
                        return (uint)((int)pc + (int)disp);
                    }
                case MePField.F24u5a2n:
                    {
                        uint hi = Raw(word, 16, 16);
                        uint lo = Raw(word, 5, 7);
                        return (hi << 8) | (lo << 1);
                    }
                case MePField.F16s16: return Sext(Raw(word, 16, 16), 16);
                case MePField.F16u16: return Raw(word, 16, 16);
                case MePField.F2u6: return Sext(Raw(word, 6, 2), 2);
                case MePField.F2u10: return Raw(word, 10, 2);
                case MePField.F6s8: return Sext(Raw(word, 8, 6), 6);
                case MePField.F8s8: return Sext(Raw(word, 8, 8), 8);
                case MePField.F24u8a4n:
                    {
                        uint hi = Raw(word, 16, 16);
                        uint lo = Raw(word, 8, 6);
                        return (hi << 8) | (lo << 2);
                    }
                case MePField.F24u4n:
                    {
                        uint hi = Raw(word, 4, 8);
                        uint lo = Raw(word, 16, 16);
                        return (hi << 16) | lo;
                    }
                case MePField.FCallnum:
                    return (Raw(word, 5, 1) << 3) | (Raw(word, 6, 1) << 2) |
                           (Raw(word, 7, 1) << 1) | Raw(word, 11, 1);
                case MePField.F3u5: return Raw(word, 5, 3);
                case MePField.F4u8: return Raw(word, 8, 4);
                case MePField.F5u8: return Raw(word, 8, 5);
                case MePField.F7u9: return Raw(word, 9, 7);
                case MePField.F7u9a2: return Raw(word, 9, 6) << 1;
                case MePField.F7u9a4: return Raw(word, 9, 5) << 2;
                case MePField.F24u8n:
                    {
                        uint hi = Raw(word, 16, 16);
                        uint lo = Raw(word, 8, 8);
                        return (hi << 8) | lo;
                    }
                case MePField.F5u24: return Raw(word, 24, 5);
                case MePField.FCdisp10:
                    {
                        uint val = Raw(word, 22, 10);
                        uint t = ((val & 0x80u) != 0) ? (val ^ 0x300u) : val;
                        int c = ((t & 0x200u) != 0) ? (int)t - 0x400 : (int)t;
                        if ((c & 0x200) != 0) return (uint)((c & 0x3FF) - 0x400);
                        return (uint)(c & 0x3FF);
                    }
                case MePField.FCsrn: return (Raw(word, 15, 1) << 4) | Raw(word, 8, 4);
                case MePField.FCrn: return Raw(word, 4, 4);
                case MePField.FCrnx: return (Raw(word, 28, 1) << 4) | Raw(word, 4, 4);
                case MePField.FCcrn: return (Raw(word, 28, 2) << 4) | Raw(word, 4, 4);
                case MePField.FRl5: return Raw(word, 20, 4);
                case MePField.F12s20: return Sext(Raw(word, 20, 12), 12);
                case MePField.FC5Rm: return (Raw(word, 8, 4) << 16) | Raw(word, 16, 16);
                case MePField.FC5Rnm: return (Raw(word, 4, 8) << 16) | Raw(word, 16, 16);
            }
            return 0;
        }

        private static MePInsn[][] _buckets;

        static MePIsa()
        {
            List<MePInsn>[] tmp = new List<MePInsn>[16];
            for (int i = 0; i < 16; i++) tmp[i] = new List<MePInsn>();
            for (int i = 0; i < Table.Length; i++)
            {
                int major = (int)((Table[i].Value >> 12) & 0xF);
                tmp[major].Add(Table[i]);
            }
            _buckets = new MePInsn[16][];
            for (int i = 0; i < 16; i++)
            {
                MePInsn[] arr = tmp[i].ToArray();
                int[] idx = new int[arr.Length];
                for (int k = 0; k < arr.Length; k++) idx[k] = k;
                Array.Sort(idx, delegate (int x, int y)
                {
                    int px = PopCount(arr[x].Mask), py = PopCount(arr[y].Mask);
                    if (px != py) return py - px;
                    return x - y;
                });
                MePInsn[] sorted = new MePInsn[arr.Length];
                for (int k = 0; k < arr.Length; k++) sorted[k] = arr[idx[k]];
                _buckets[i] = sorted;
            }
        }

        private static int PopCount(uint v)
        {
            int n = 0;
            while (v != 0) { n += (int)(v & 1u); v >>= 1; }
            return n;
        }

        /// <summary>Look up the instruction matching a 32-bit little-endian word.</summary>
        internal static MePInsn Decode(uint word)
        {
            MePInsn[] b = _buckets[(word >> 12) & 0xF];
            for (int i = 0; i < b.Length; i++)
            {
                if ((word & b[i].Mask) == b[i].Value) return b[i];
            }
            return null;
        }

        /// <summary>Instruction length in bytes: the major opcode decides whether the
        /// instruction is a 16-bit or a 32-bit form.</summary>
        internal static int LengthOf(uint word)
        {
            return (((word >> 12) & 0xF) >= 12) ? 4 : 2;
        }

        private static readonly string[] RegNames = new string[]
        {
            "0","1","2","3","4","5","6","7","8","9","10","11","12","tp","gp","sp"
        };

        private static readonly string[] CsrNames = new string[]
        {
            "pc","lp","sar","???","rpb","rpe","rpc","hi","lo","???","???","???",
            "mb0","me0","mb1","me1","psw","id","tmp","epc","exc","cfg","vid","npc",
            "dbg","depc","opt","rcfg","ccfg","???","???","???"
        };

        internal static string RegName(int n)
        {
            if (n < 0 || n > 15) return "?";
            return "$" + RegNames[n];
        }

        internal static string CsrName(int n)
        {
            if (n < 0 || n >= CsrNames.Length) return "???";
            string s = CsrNames[n];
            if (s == "???") return s;
            return "$" + s;
        }

        internal static string Format(MePInsn ins, uint word, uint pc)
        {
            // "mov $0,$0" is the architectural nop and objdump prints it as such.
            if (ins.Op == MePOp.Mov && Raw(word, 4, 4) == 0 && Raw(word, 8, 4) == 0) return "nop";
            if (ins.Ops.Length == 0) return ins.Fmt;
            object[] args = new object[ins.Ops.Length];
            for (int i = 0; i < ins.Ops.Length; i++)
            {
                MePOpnd o = ins.Ops[i];
                uint v = (o.Field == MePField.Const) ? o.Const : FieldValue(o.Field, word, pc);
                switch (o.Print)
                {
                    case MePPrint.Reg:
                        args[i] = RegName((int)(v & 0xF));
                        break;
                    case MePPrint.CpReg:
                        args[i] = "$c" + v.ToString(CultureInfo.InvariantCulture);
                        break;
                    case MePPrint.Csrn:
                        args[i] = CsrName((int)v);
                        break;
                    case MePPrint.SDec:
                    case MePPrint.Cdisp10:
                        args[i] = ((int)v).ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        args[i] = "0x" + v.ToString("x", CultureInfo.InvariantCulture);
                        break;
                }
            }
            return string.Format(CultureInfo.InvariantCulture, ins.Fmt, args);
        }
    }
}
