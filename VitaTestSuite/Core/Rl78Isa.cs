// ---------------------------------------------------------------------------
// RL78 instruction set architecture definitions.
//
// ENCODING SOURCE OF TRUTH
// ------------------------
// The instruction table in Rl78IsaTable.g.cs is machine generated from GNU
// binutils' RL78 decoder (opcodes/rl78-decode.opc, via the C it generates,
// rl78-decode.c) and its reference printer (opcodes/rl78-dis.c).
//
// OPERAND MODEL
// -------------
// Like binutils, a decoded instruction carries exactly two operand slots,
// op[0] = destination and op[1] = source, as built by the .opc macros
// (DR/DRB/DRW, SR/SRB/SRW, DM/SM/DM2/SM2, DC/SC, DB/SB, COND, DPUSH/SPOP).
// An operand is one of:
//
//   Imm      an immediate value (immediate, shift count, bank number, target)
//   Reg      a register (X A C B E D L H AX BC DE HL SP PSW CS ES)
//   Ind      [reg (+reg2) (+addend)] or a direct/short-direct/SFR address
//   Bit      reg.bit
//   BitInd   [reg (+addend)].bit / addr.bit
//   PreDec   [--SP]   (PUSH)
//   PostInc  [SP++]   (POP)
//
// Register numbers match binutils' RL78_Register enumeration, which is also
// the only place where the raw opcode register fields differ from the ISA
// register order: the 3-bit field value n selects register n + RL78_Reg_X.
//
// Register model
// --------------
// On real silicon the control registers are memory mapped (SP 0xFFFF8,
// PSW 0xFFFFA, CS 0xFFFFC, ES 0xFFFFD, PMC 0xFFFFE).  This core keeps them as
// fields for speed and mirrors accesses so that a direct access to the SFR
// window stays coherent.
// ---------------------------------------------------------------------------

using System;

namespace VitaTestSuite.Core
{
    /// <summary>RL78 registers, numbered exactly like binutils' RL78_Register.</summary>
    public enum Rl78Reg
    {
        None = 0,
        // 8-bit registers: the opcode field value is (reg - X)
        X = 1,
        A = 2,
        C = 3,
        B = 4,
        E = 5,
        D = 6,
        L = 7,
        H = 8,
        // 16-bit register pairs: the opcode field value is (reg - AX)
        AX = 9,
        BC = 10,
        DE = 11,
        HL = 12,
        // control registers (unordered in binutils)
        SP = 13,
        PSW = 14,
        CS = 15,
        ES = 16,
        PMC = 17,
        MEM = 18
    }

    /// <summary>RL78 condition codes, encoded in the low 3 bits of the branch field.</summary>
    public enum Rl78Cond
    {
        T = 0,
        F = 1,
        C = 2,
        NC = 3,
        H = 4,
        NH = 5,
        Z = 6,
        NZ = 7
    }

    /// <summary>Operand kind, mirroring binutils' RL78_Operand_Type.</summary>
    public enum Rl78OpType
    {
        None = 0,
        Imm = 1,
        Reg = 2,
        Ind = 3,
        Bit = 4,
        BitInd = 5,
        PreDec = 6,
        PostInc = 7
    }

    /// <summary>
    /// Semantics id, mirroring binutils' RL78_Opcode_ID.  The mnemonic alone is
    /// not enough: "mov" covers MOV, MOVS, SET1/CLR1/MOV1, PUSH/POP and the
    /// G14 mul/div multiplex.
    /// </summary>
    public enum Rl78Id
    {
        Unknown = 0,
        Add = 1,
        Addc = 2,
        And = 3,
        Branch = 4,
        BranchCond = 5,
        BranchCondClear = 6,
        Break = 7,
        Call = 8,
        Cmp = 9,
        Divhu = 10,
        Divwu = 11,
        Halt = 12,
        Mov = 13,
        Mach = 14,
        Machu = 15,
        Mulu = 16,
        Mulh = 17,
        Mulhu = 18,
        Nop = 19,
        Or = 20,
        Ret = 21,
        Reti = 22,
        Rol = 23,
        Rolc = 24,
        Ror = 25,
        Rorc = 26,
        Sar = 27,
        Sel = 28,
        Shr = 29,
        Shl = 30,
        Skip = 31,
        Stop = 32,
        Sub = 33,
        Subc = 34,
        Xch = 35,
        Xor = 36
    }

    /// <summary>One decoded operand.</summary>
    public struct Rl78Operand
    {
        /// <summary>Imm / Reg / Ind / Bit / BitInd / PreDec / PostInc.</summary>
        public Rl78OpType Type;
        /// <summary>Register, or the base register of [reg+reg2+addend].</summary>
        public Rl78Reg Reg;
        /// <summary>Index register of [HL+B] / [HL+C], else None.</summary>
        public Rl78Reg Reg2;
        /// <summary>Raw addend: immediate value, byte displacement or absolute address.</summary>
        public int Addend;
        /// <summary>Addend encoding (Rl78IsaTable.AK*): how <see cref="Addend"/> was formed.</summary>
        public byte AddKind;
        /// <summary>Effective address for operands the decoder can resolve (SFR/saddr/direct/CALLT).</summary>
        public int Address;
        /// <summary>Bit number of a Bit / BitInd operand.</summary>
        public byte BitNumber;
        /// <summary>Condition code carried by the source operand of a branch / skip.</summary>
        public Rl78Cond Condition;
        /// <summary>Decoded while an ES: prefix was active.</summary>
        public bool Es;
        /// <summary>Byte offset of this operand's data inside the instruction.</summary>
        public int Offset;
        /// <summary>Bytes consumed by this operand's data.</summary>
        public int Size;
    }

    /// <summary>A fully decoded instruction.</summary>
    public sealed class Rl78Decoded
    {
        /// <summary>Table row index.</summary>
        public int Row;
        /// <summary>Total instruction length in bytes, including an 0x11 ES prefix.</summary>
        public int Length;
        /// <summary>True when the instruction was prefixed with 0x11 (ES:).</summary>
        public bool HasEsPrefix;
        /// <summary>Semantics id.</summary>
        public Rl78Id Id;
        /// <summary>PSW flag mask written by the instruction (binutils' Fz/Fc/... masks).</summary>
        public int Flags;
        /// <summary>True for word operations (binutils' W()).</summary>
        public bool Word;
        /// <summary>SYNTAX() template of the row; drives operand printing.</summary>
        public string Syntax;
        /// <summary>Mnemonic of the row.</summary>
        public string Mnemonic;

        public Rl78Operand Op0;
        public Rl78Operand Op1;

        public Rl78Operand Operand(int i) { return i == 0 ? Op0 : Op1; }
    }

    /// <summary>Byte source used by the decoder (implemented by the core over MemoryHub).</summary>
    public interface IRl78ByteSource
    {
        byte ReadByte(uint addr);
    }

    public static class Rl78Isa
    {
        public const int FlagCy = 0x01;   // PSW.0
        public const int FlagIsp0 = 0x02;   // PSW.1
        public const int FlagIsp1 = 0x04;   // PSW.2
        public const int FlagRbs0 = 0x08;   // PSW.3
        public const int FlagAc = 0x10;   // PSW.4
        public const int FlagRbs1 = 0x20;   // PSW.5
        public const int FlagZ = 0x40;   // PSW.6
        public const int FlagIe = 0x80;   // PSW.7

        // Control register SFR addresses (RL78 user's manual, table 3-4).
        public const uint SfrSp = 0xFFFF8;
        public const uint SfrPsw = 0xFFFFA;
        public const uint SfrCs = 0xFFFFC;
        public const uint SfrEs = 0xFFFFD;
        public const uint SfrPmc = 0xFFFFE;
        public const uint SfrMem = 0xFFFFF;

        /// <summary>Condition suffixes indexed by the 3-bit condition field.</summary>
        public static readonly string[] CondNames = new string[] { "t", "f", "c", "nc", "h", "nh", "z", "nz" };

        /// <summary>Short-direct byte address: 0xFFF00 + n for n &lt; 0x20, else 0xFFE00 + n.</summary>
        public static uint ShortDirect(int n)
        {
            if (n < 0x20) return (uint)(0xFFF00 + n);
            return (uint)(0xFFE00 + n);
        }

        /// <summary>Address of SFR number n (binutils' sfr()).</summary>
        public static uint Sfr(int n) { return (uint)(0xFFF00 + n); }

        /// <summary>Lower-case register name, as printed by rl78-dis.c.</summary>
        public static string RegName(Rl78Reg r)
        {
            switch (r)
            {
                case Rl78Reg.X: return "x";
                case Rl78Reg.A: return "a";
                case Rl78Reg.C: return "c";
                case Rl78Reg.B: return "b";
                case Rl78Reg.E: return "e";
                case Rl78Reg.D: return "d";
                case Rl78Reg.L: return "l";
                case Rl78Reg.H: return "h";
                case Rl78Reg.AX: return "ax";
                case Rl78Reg.BC: return "bc";
                case Rl78Reg.DE: return "de";
                case Rl78Reg.HL: return "hl";
                case Rl78Reg.SP: return "sp";
                case Rl78Reg.PSW: return "psw";
                case Rl78Reg.CS: return "cs";
                case Rl78Reg.ES: return "es";
                case Rl78Reg.PMC: return "pmc";
                case Rl78Reg.MEM: return "mem";
                default: return "?";
            }
        }

        /// <summary>True for the four 16-bit register pairs.</summary>
        public static bool IsWordReg(Rl78Reg r)
        {
            return r == Rl78Reg.AX || r == Rl78Reg.BC || r == Rl78Reg.DE || r == Rl78Reg.HL;
        }
    }
}
