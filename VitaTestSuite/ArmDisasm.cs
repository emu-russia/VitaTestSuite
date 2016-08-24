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

class ArmDisasm
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

    public class DisasmResult
    {
        public string Name;
        public string Parameters;
        public InstructionType Type;
        public Instruction Instr;
    }

    #region Arm Instruction Set

    public DisasmResult DisasmArm(uint Address, byte[] Stream)
    {

        return null;
    }

    #endregion

    #region Thumb Instruction Set

    public DisasmResult DisasmThumb(uint Address, byte[] Stream)
    {

        return null;
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
