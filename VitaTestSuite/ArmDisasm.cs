// ARMv7 ISA Disassembler

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class ArmDisasm
{
    /// <summary>
    /// Specify endianness mode.
    /// </summary>
    public bool LittleEndian = false;

    /// <summary>
    /// Instruction by encoding
    /// Chapter A4. ARM DII 0406C.b
    /// </summary>
    public enum Instruction
    {
        UNDEFINED = 0,
        // Branch
        B, CBNZ, CBZ, BL, BLX, BX, BXJ, TBB, TBH,
        // Data processing
        ADC, ADD, ADR, AND, BIC, CMN, CMP, EOR, MOV, MVN, ORN, ORR, RSB, RSC, SBC, SUB, TEQ, TST,
        ASR, LSL, LSR, ROR, RRX,
        MLA, MLS, MUL,
        SMLABB, SMLABT, SMLATB, SMLATT, SMLAD, SMLAL, SMLALBB, SMLALBT, SMLALTB, SMLALTT, SMLALD,
            SMLAWB, SMLAWT, SMLSD, SMLSLD, SMMLA, SMMLS, SMMUL, SMUAD, SMULBB, SMULBT, SMULTB, SMULTT,
            SMULL, SMULWB, SMULWT, SMUSD,
        UMAAL, UMLAL, UMULL,
        SSAT, SSAT16, USAT, USAT16,
        QADD, QSUB, QDADD, QDSUB,
        PKH, SXTAB, SXTAB16, SXTAH, SXTB, SXTB16, SXTH, UXTAB, UXTAB16, UXTAH, UXTB, UXTB16, UXTH,
        ADD16_Group, ASX_Group, SAX_Group, SUB16_Group, ADD8_Group, SUB8_Group,
        SDIV, UDIV,
        BFC, BFI, CLZ, MOVT, RBIT, REV, REV16, REVSH, SBFX, SEL, UBFX, USAD8, USADA8,
        // StatusRegisterAccess
        MRS, MSR, CPS, 
        // LoadStore
        LDR, STR, LDRT, STRT, LDREX, STREX,
        STRH, STRHT, STREXH,
        LRDH, LDRHT, LDREXH,
        LDRSH, LDRSHT,
        STRB, STRBT, STREXB,
        LDRB, LDRBT, LDREXB,
        LDRSB, LDRSBT,
        LDRD, STRD, LDREXD, STREXD,
        // LoadStoreMultiple
        LDM, LDMIA, LDMFD, LDMDA, LDMFA, LDMDB, LDMEA, LDMIB, LDMED, POP, PUSH,
        STM, STMIA, STMEA, STMDA, STMED, STMDB, STMFD, STMIB, STMFA,
        // Misc
        CLREX, DBG, DMB, DSB, ISB, IT, NOP, PLD, PLDW, 
        PLI, 
        SETEND, SEV, SWP, SWPB, WFE, WFI, YIELD,
        // ExceptionRelated
        SVC, BKPT, SMC, HVC, SRS, RFE, SUBS_PC_LR, ERET, LDM_Except,
        // Coprocessor
        MCR, MCR2, MCRR, MCRR2, MRC, MRC2, MRRC, MRRC2, LDC, LDC2, STC, STC2,
        // FloatLoadStore
        VLDM, VLDR, VSTM, VSTR, VLD1, VLD2, VLD3, VLD4, VST1, VST2, VST3, VST4,
        // FloatRegisterTransfer
        VDUP_Fp, VMOV_Fp, VMRS_Fp, VMSR_Fp,
        // SIMDDataProcessing
        VADD_Simd, VADDHN, VADDL, VADDW, VHADD, VHSUB, VPADAL, VPADD, VPADDL, VRADDHN, VRHADD,
        VRSUBHN, VQADD, VQSUB, VSUB_Simd, VSUBHN, VSUBL, VSUBW,
        VAND, VBIC, VEOR, VBIF, VBIT, VBSL, VMOV_Simd, VMVN, VORR, VORN, 
        VACGE, VACGT, VACLE, VACLT, VCEQ, VCGE, VCGT, VCLE, VCLT, VTST,
        VQRSHL, VQRSHRN, VQRSHRUN, VQSHL, VQSHLU, VQSHRN, VQSHRUN, VRSHL, VRSHR, VRSRA, VRSHRN,
        VSHL, VSHLL, VSHR, VSHRN, VSLI, VSRA, VSRI,
        VMLA_Simd, VMLAL, VMLS_Simd, VMLSL, VMUL, VMULL, VMDA, VMS, VQDMLAL, VQDMLSL, VQSMULH, VQRDMULH, VQDMULL,
        VABA, VABAL, VABD, VABDL, VABS, VCVT_Simd, VCLS, VCLZ, VCNT, VDUP_Simd, VEXT, VMOVN, VMOVL, VMAX, VMIN, VNEG_Simd,
        VPMAX, VPMIN, VRECPE, VRECPS, VRSQRTE, VRSQRTS, VREV16, VREV32, VREV64, VQABS,
        VQMOVN, VQMOVUN, VQNEG, VSWP, VTBL, VTBX, VTRN, VUZP, VZIP,
        // FloatDataProcessing
        VABS_Fp, VADD_Fp, VCMP, VCMPE, VCVT_Fp, VCVTR, VCVTB, VCVTT, VDIV, VMLA_Fp, VMLS_Fp, VFMA, VFMS, VNEG_Fp,
        VNMLA, VNMLS, VNMUL, VFNMA, VFNMS, VSQRT, VSUB_Fp,
    }

    /// <summary>
    /// Instruction by category (type)
    /// Chapter A4. ARM DII 0406C.b
    /// </summary>
    public enum InstructionType
    {
        Undefined = 0,
        Branch,
        DataProcessing,
        StatusRegisterAccess,
        LoadStore,
        LoadStoreMultiple,
        Misc,
        ExceptionRelated,
        Coprocessor,
        FloatLoadStore,
        FloatRegisterTransfer,
        SIMDDataProcessing,
        FloatDataProcessing,
    }

    public enum Condition
    {
        Equal = 0,
        NotEqual,
        CarrySet,
        CarryClear,
        Minus,
        Plus,
        Overflow,
        NotOverflow,
        UnsignedHigher,
        UnsignedLowerOrSame,
        SignedGreaterOrEqual,
        SignedLess,
        SignedGreater,
        SignedLessOrEqual,
        Always,             // 0xe
        Unknown,        // 0xf
    }

    public class DisasmResult
    {
        public string Name;
        public string Parameters;
        public InstructionType Type;
        public Instruction Instr;
        public Condition Cond;
    }

    private delegate void InstrDelegate(uint instr, string encoding, ref DisasmResult res);

    class InstrSpec
    {
        public string encoding;
        public InstrDelegate instrHandler;
        public InstrSpec(string e, InstrDelegate d) { encoding = e; instrHandler = d; }
    }

    /// <summary>
    /// Extract specified bits from encoding string (Arm/Thumb)
    /// </summary>
    /// <param name="instr"></param>
    /// <param name="encoding"></param>
    /// <param name="spec"></param>
    /// <returns></returns>
    private static uint Extract (uint instr, string encoding, char spec)
    {
        uint res = 0;
        bool foundOnce = false;

        for (int i = 0; i < encoding.Length; i++ )
        {
            if ( encoding[i] == spec )
            {
                if ((instr & (1 << (encoding.Length - 1 - i))) != 0)
                    res |= 1;
                res <<= 1;
                foundOnce = true;
            }
        }

        if (!foundOnce)
            throw new Exception();

        return res;
    }

    private static string Cond(uint cond)
    {
        switch(cond)
        {
            case 0: return "eq";
            case 1: return "ne";
            case 2: return "cs";
            case 3: return "cc";
            case 4: return "mi";
            case 5: return "pl";
            case 6: return "vs";
            case 7: return "vc";
            case 8: return "hi";
            case 9: return "ls";
            case 0xa: return "ge";
            case 0xb: return "lt";
            case 0xc: return "gt";
            case 0xd: return "le";
            case 0xe: return "";
        }
        return "???";
    }

    private static string Imm(uint imm)
    {
        return "#0x" + imm.ToString("X");
    }

    #region Arm Instruction Set

    private InstrSpec[] armSpecs = new InstrSpec[] {
        new InstrSpec ( "cccc0010101snnnnddddiiiiiiiiiiii", new InstrDelegate(arm_ADC_Immediate) ),     // pp 300

    };

    public DisasmResult DisasmArm(uint Address, byte[] Stream)
    {
        DisasmResult res = new DisasmResult();

        res.Instr = Instruction.UNDEFINED;
        res.Type = InstructionType.Undefined;
        res.Name = "";
        res.Parameters = "";
        res.Cond = Condition.Unknown;

        uint instr = FetchWord(Stream);

        //
        // TODO: Do it Parallel.ForEach
        //

        foreach ( InstrSpec spec in armSpecs )
        {
            uint mask = 0;
            uint enc = 0;

            for (int i=0; i<spec.encoding.Length; i++)
            {
                char c = spec.encoding[i];

                if (c == '1')
                    enc |= (uint)(1 << (spec.encoding.Length - 1 - i));

                if (c == '1' || c == '0')
                    mask |= (uint)(1 << (spec.encoding.Length - 1 - i));
            }

            if ((instr & mask) == enc)
            {
                spec.instrHandler(instr, spec.encoding, ref res);
                break;
            }
        }

        return res;
    }

    private static void arm_ADC_Immediate (uint instr, string encoding, ref DisasmResult res)
    {
        // If Rd == 1111 and S == 1 then see SUBS_PC_LR and related instructions

        uint cond = Extract(instr, encoding, 'c');
        uint s = Extract(instr, encoding, 's');
        uint Rn = Extract(instr, encoding, 'n');
        uint Rd = Extract(instr, encoding, 'd');
        uint imm = Extract(instr, encoding, 'i');

        if ((imm & 0x800) != 0)
            imm |= 0xfffff000;

        res.Type = InstructionType.DataProcessing;
        res.Instr = Instruction.ADC;
        res.Cond = (Condition)cond;

        res.Name = "adc";
        if (s != 0)
            res.Name += "s";
        res.Name += Cond(cond);

        res.Parameters = "r" + Rd.ToString() + ", r" + Rn.ToString() + ", " + Imm(imm);
    }

    #endregion

    #region Thumb Instruction Set

    public DisasmResult DisasmThumb(uint Address, byte[] Stream)
    {
        DisasmResult res = new DisasmResult();

        res.Instr = Instruction.UNDEFINED;
        res.Type = InstructionType.Undefined;
        res.Name = "";
        res.Parameters = "";
        res.Cond = Condition.Unknown;

        return res;
    }

    #endregion

    #region Fetch

    private uint SwapWord(uint val)
    {
        return (val >> 24) |
                ((val >> 8) & 0xff00) |
                ((val << 8) & 0xff0000) |
                (val << 24);
    }

    private ushort SwapHalf(ushort val)
    {
        return (ushort)(((uint)val << 8) | ((uint)val >> 8));
    }

    private uint FetchWord(byte[] Stream)
    {
        uint word = ((uint)Stream[0] << 24) |
                    ((uint)Stream[1] << 16) |
                    ((uint)Stream[2] << 8) |
                    (uint)Stream[3];

        if (LittleEndian)
            word = SwapWord(word);

        return word;
    }

    private ushort FetchHalf(byte[] Stream)
    {
        ushort hword = (ushort)(((uint)Stream[0] << 8) |
                        (uint)Stream[1]);

        if (LittleEndian)
            hword = SwapHalf(hword);

        return hword;
    }

    #endregion 

}
