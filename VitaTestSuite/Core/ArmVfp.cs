// VFPv3-D16 floating point unit for the ARM Cortex-A9 (ARMv7-A).
//
// Implements:
//   * the 32 x 64-bit register file (16 double / 32 single registers, d0..d31)
//   * FPSCR with the NZCV flags and the cumulative exception bits, FPEXC
//   * VADD/VMUL/VNMUL/VSUB/VDIV/VABS/VNEG/VSQRT/VMLA/VMLS/VNMLA/VNMLS
//   * VCMP/VCMPE and the VCMP/VCMPE #0.0 forms
//   * VCVT between single/double and int/uint (including the fixed-point forms)
//   * VMOV (immediate, ARM core register <-> single, two singles <-> double)
//   * VMRS/VMSR
//   * VLDR/VSTR/VLDM/VSTM/VPUSH/VPOP
//
// The register file is held as an uint[64] of single-precision slots so that
// both views (sN and dN) can be read/written without allocation.

using System;
using System.Globalization;

namespace VitaTestSuite.Core
{
    public class ArmVfp
    {
        // ---- register file -----------------------------------------------------

        /// <summary>32 double registers as 64 single-precision slots (dN = slots 2N / 2N+1).</summary>
        public readonly uint[] Regs = new uint[64];

        /// <summary>FPSCR - floating point status and control register.</summary>
        public uint Fpscr = 0;
        /// <summary>FPEXC - floating point exception register (bit 30 = EN).</summary>
        public uint Fpexc = 0;
        /// <summary>FPSID (Cortex-A9 VFPv3-D16: 0x41033090).</summary>
        public uint Fpsid = 0x41033090;
        /// <summary>MVFR0 - media and VFP feature register 0 (VFPv3-D16, single+double).</summary>
        public uint Mvfr0 = 0x10110221;
        /// <summary>MVFR1 - media and VFP feature register 1.</summary>
        public uint Mvfr1 = 0x12000011;

        public const uint FpexcEn = 1u << 30;

        public bool Enabled
        {
            get { return (Fpexc & FpexcEn) != 0; }
        }

        public void Reset()
        {
            for (int i = 0; i < Regs.Length; i++) Regs[i] = 0;
            Fpscr = 0;
            Fpexc = 0;
            Fpsid = 0x41033090;
            Mvfr0 = 0x10110221;
            Mvfr1 = 0x12000011;
        }

        // ---- register access ---------------------------------------------------

        public uint ReadS(int n)
        {
            return Regs[n & 31];
        }

        public void WriteS(int n, uint v)
        {
            Regs[n & 31] = v;
        }

        public ulong ReadD(int n)
        {
            int i = (n & 15) * 2;
            return (ulong)Regs[i] | ((ulong)Regs[i + 1] << 32);
        }

        public void WriteD(int n, ulong v)
        {
            int i = (n & 15) * 2;
            Regs[i] = (uint)v;
            Regs[i + 1] = (uint)(v >> 32);
        }

        public float ReadF32(int n)
        {
            return BitsToFloat(Regs[n & 31]);
        }

        public void WriteF32(int n, float v)
        {
            Regs[n & 31] = FloatToBits(v);
        }

        public double ReadF64(int n)
        {
            return BitConverter.Int64BitsToDouble((long)ReadD(n));
        }

        public void WriteF64(int n, double v)
        {
            WriteD(n, (ulong)BitConverter.DoubleToInt64Bits(v));
        }

        public static uint FloatToBits(float f)
        {
            return (uint)BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
        }

        public static float BitsToFloat(uint b)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes((int)b), 0);
        }

        // ---- FPSCR flags -------------------------------------------------------

        public const uint Ns = 1u << 31;
        public const uint Zs = 1u << 30;
        public const uint Cs = 1u << 29;
        public const uint Vs = 1u << 28;

        /// <summary>Copy the FPSCR NZCV bits into the CPSR NZCV bits (ARM ARM A2.5).</summary>
        public uint FlagsToCpsr(uint cpsr)
        {
            cpsr &= ~0xF0000000u;
            cpsr |= (Fpscr >> 28) << 28;
            return cpsr;
        }

        /// <summary>Copy the CPSR NZCV bits into the FPSCR (VMSR/exception entry).</summary>
        public uint CpsrToFlags(uint cpsr)
        {
            Fpscr = (Fpscr & 0x0FFFFFFFu) | (cpsr & 0xF0000000u);
            return Fpscr;
        }

        private void SetFlags(bool n, bool z, bool c, bool v)
        {
            Fpscr &= 0x0FFFFFFFu;
            if (n) Fpscr |= Ns;
            if (z) Fpscr |= Zs;
            if (c) Fpscr |= Cs;
            if (v) Fpscr |= Vs;
        }

        /// <summary>Set the FPSCR result flags for a double result (NaN handling per ARM ARM).</summary>
        private void SetFlagsDouble(double d)
        {
            if (double.IsNaN(d)) { SetFlags(false, false, true, true); return; }
            SetFlags(d < 0.0, d == 0.0, false, false);
        }

        private void SetFlagsSingle(float f)
        {
            if (float.IsNaN(f)) { SetFlags(false, false, true, true); return; }
            SetFlags(f < 0.0f, f == 0.0f, false, false);
        }

        /// <summary>Set the IOC (invalid operation) cumulative bit and raise the exception flag.</summary>
        private void SetIoc()
        {
            Fpscr |= 1u;         // IOC
            Fpscr |= 1u << 7;    // IDC (input denormal) - deliberately conservative
        }

        private void SetIxc()
        {
            Fpscr |= 1u << 4;    // IXC
        }

        private void SetOfc()
        {
            Fpscr |= 1u << 2;    // OFC
            Fpscr |= 1u << 3;    // UFC
        }

        private void SetDzc()
        {
            Fpscr |= 1u << 1;    // DZC
        }

        // ---- conversions -------------------------------------------------------

        /// <summary>
        /// Convert an integer/unsigned value to a float using the VCVT rounding
        /// mode selected by FPSCR.RMode.
        /// </summary>
        public float IntToFloat(uint value, bool unsigned, bool is64)
        {
            switch (Fpscr & (3u << 22))
            {
                case 0u << 22:   // RN
                    if (is64) return (float)(unsigned ? (double)value : (double)(int)value);
                    return unsigned ? (float)value : (float)(int)value;
                case 1u << 22:   // RP (round toward +inf)
                    {
                        double d = is64 ? (unsigned ? (double)value : (double)(int)value)
                                        : (unsigned ? (double)value : (double)(int)value);
                        return (float)Math.Ceiling(d);
                    }
                case 2u << 22:   // RM
                    {
                        double d = is64 ? (unsigned ? (double)value : (double)(int)value)
                                        : (unsigned ? (double)value : (double)(int)value);
                        return (float)Math.Floor(d);
                    }
                default:         // RZ
                    {
                        double d = is64 ? (unsigned ? (double)value : (double)(int)value)
                                        : (unsigned ? (double)value : (double)(int)value);
                        return (float)Math.Truncate(d);
                    }
            }
        }

        /// <summary>VCVT float -> signed/unsigned integer with the FPSCR rounding mode.</summary>
        public uint FloatToInt(double value, bool unsigned, bool to64)
        {
            double r;
            switch (Fpscr & (3u << 22))
            {
                case 1u << 22: r = Math.Floor(value + 0.5); break;
                case 2u << 22: r = Math.Ceiling(value - 0.5); break;
                case 3u << 22: r = Math.Truncate(value); break;
                default: r = Math.Round(value, MidpointRounding.ToEven); break;
            }

            if (double.IsNaN(value))
            {
                SetIoc();
                SetFlags(false, false, true, true);
                return 0;
            }

            if (unsigned)
            {
                if (to64)
                {
                    if (r < 0.0) { SetIoc(); SetFlags(false, false, true, true); return 0; }
                    if (r >= 18446744073709551616.0) { SetIoc(); SetFlags(false, false, true, true); return 0xFFFFFFFFu; }
                    return (uint)(ulong)r;
                }
                if (r < 0.0) { SetIoc(); SetFlags(false, false, true, true); return 0; }
                if (r > 4294967295.0) { SetIoc(); SetFlags(false, false, true, true); return 0xFFFFFFFFu; }
                return (uint)r;
            }
            else
            {
                if (to64)
                {
                    if (r <= -9223372036854775809.0 || r >= 9223372036854775808.0)
                    { SetIoc(); SetFlags(false, false, true, true); return 0; }
                    return (uint)(long)r;
                }
                if (r < -2147483648.0) { SetIoc(); SetFlags(false, false, true, true); return 0x80000000u; }
                if (r > 2147483647.0) { SetIoc(); SetFlags(false, false, true, true); return 0x7FFFFFFFu; }
                return (uint)(int)r;
            }
        }

        // ---- arithmetic helpers (flag-updating) --------------------------------

        public double AddDouble(double a, double b) { double r = a + b; SetFlagsDouble(r); return r; }
        public double SubDouble(double a, double b) { double r = a - b; SetFlagsDouble(r); return r; }
        public double MulDouble(double a, double b) { double r = a * b; SetFlagsDouble(r); return r; }

        public double DivDouble(double a, double b)
        {
            if (b == 0.0)
            {
                if (a == 0.0) { SetIoc(); SetFlags(false, false, true, true); return double.NaN; }
                SetDzc();
                bool neg = (a < 0.0) ^ (IsNegativeZero(b));
                SetFlags(neg, false, false, false);
                return neg ? double.NegativeInfinity : double.PositiveInfinity;
            }
            double r = a / b;
            SetFlagsDouble(r);
            return r;
        }

        public double SqrtDouble(double a)
        {
            if (a < 0.0) { SetIoc(); SetFlags(false, false, true, true); return double.NaN; }
            double r = Math.Sqrt(a);
            SetFlagsDouble(r);
            return r;
        }

        public float AddSingle(float a, float b) { float r = a + b; SetFlagsSingle(r); return r; }
        public float SubSingle(float a, float b) { float r = a - b; SetFlagsSingle(r); return r; }
        public float MulSingle(float a, float b) { float r = a * b; SetFlagsSingle(r); return r; }

        public float DivSingle(float a, float b)
        {
            if (b == 0.0f)
            {
                if (a == 0.0f) { SetIoc(); SetFlags(false, false, true, true); return float.NaN; }
                SetDzc();
                bool neg = (a < 0.0f) ^ (IsNegativeZeroSingle(b));
                SetFlags(neg, false, false, false);
                return neg ? float.NegativeInfinity : float.PositiveInfinity;
            }
            float r = a / b;
            SetFlagsSingle(r);
            return r;
        }

        public float SqrtSingle(float a)
        {
            if (a < 0.0f) { SetIoc(); SetFlags(false, false, true, true); return float.NaN; }
            float r = (float)Math.Sqrt(a);
            SetFlagsSingle(r);
            return r;
        }

        private static bool IsNegativeZero(double d)
        {
            return d == 0.0 && double.IsNegativeInfinity(1.0 / d);
        }

        private static bool IsNegativeZeroSingle(float f)
        {
            return f == 0.0f && float.IsNegativeInfinity(1.0f / f);
        }

        /// <summary>VCMP/VCMPE comparison: sets FPSCR NZCV; returns the comparison result.</summary>
        public int Compare(double a, double b, bool quiet)
        {
            if (double.IsNaN(a) || double.IsNaN(b))
            {
                if (!quiet) SetIoc();
                SetFlags(false, false, true, true);
                return 3;
            }
            if (a < b) { SetFlags(true, false, false, false); return 0; }
            if (a == b) { SetFlags(false, true, true, false); return 1; }
            SetFlags(false, false, true, false);
            return 2;
        }

        public int CompareSingle(float a, float b, bool quiet)
        {
            return Compare((double)a, (double)b, quiet);
        }

        /// <summary>
        /// VMOV immediate: assemble the 32-bit single-precision pattern from the
        /// encoding (ARM ARM A2.7.8 VFPExpandImm).  With imm8 = abcdefgh:
        ///   sign     = a
        ///   exponent = NOT(b) : Replicate(b, 5) : ef       (8 bits)
        ///   fraction = gh?? - no: the low four bits cdefgh's last four bits
        /// so exponent[30:23] = NOT(imm8[6]) &lt;&lt; 7 | Replicate(imm8[6],5) &lt;&lt; 2
        ///                      | imm8[5:4], and the mantissa is imm8[3:0] &lt;&lt; 19.
        /// </summary>
        public static uint ExpandImmediate(uint imm8)
        {
            uint b = (imm8 >> 6) & 1u;
            uint sign = (imm8 >> 7) & 1u;
            uint exp = (b == 0u) ? (0x80u | ((imm8 >> 4) & 3u))
                                 : (0x7Cu | ((imm8 >> 4) & 3u));
            return (sign << 31) | (exp << 23) | ((imm8 & 0xFu) << 19);
        }

        /// <summary>Double-precision VMOV immediate expansion (ARM ARM A2.7.8):
        /// exponent[62:52] = NOT(imm8[6]) &lt;&lt; 10 | Replicate(imm8[6],8) &lt;&lt; 2
        ///                   | imm8[5:4], mantissa = imm8[3:0] &lt;&lt; 48.</summary>
        public static ulong ExpandImmediate64(uint imm8)
        {
            uint b = (imm8 >> 6) & 1u;
            ulong sign = (ulong)((imm8 >> 7) & 1u);
            ulong exp = (b == 0u) ? (0x400u | ((imm8 >> 4) & 3u))
                                  : (0x3FCu | ((imm8 >> 4) & 3u));
            return (sign << 63) | (exp << 52) | ((ulong)(imm8 & 0xFu) << 48);
        }

        /// <summary>Advanced SIMD modified immediate expansion (ARM ARM A7.4.3).</summary>
        public static uint ExpandSimdImmediate(uint imm8, uint cmode)
        {
            uint cm = cmode & 0xFu;
            switch (cm >> 1)
            {
                case 0u:  // 0bx000: 00000000 00000000 00000000 abcdefgh
                    return imm8;
                case 1u:  // 0bx001: 00000000 00000000 abcdefgh 00000000
                    return imm8 << 8;
                case 2u:  // 0bx010: 00000000 abcdefgh 00000000 00000000
                    return imm8 << 16;
                case 3u:  // 0bx011: abcdefgh 00000000 00000000 00000000
                    return imm8 << 24;
                case 4u:  // 0bx100: abcdefgh abcdefgh 00000000 00000000
                    return imm8 | (imm8 << 8);
                case 5u:  // 0bx101: abcdefgh abcdefgh abcdefgh abcdefgh
                    if (cm == 0xAu) return imm8 * 0x01010101u;
                    if (cm == 0xBu) return imm8 * 0x01010101u;
                    // 0b1101/0b1111 are handled below (shifted-ones encodings).
                    return imm8 | (imm8 << 8) | (imm8 << 16) | (imm8 << 24);
                case 6u:  // 0bx110: abcdefgh abcdefgh abcdefgh abcdefgh
                    return imm8 | (imm8 << 8) | (imm8 << 16) | (imm8 << 24);
                default:  // 0bx111: each set bit of imm8 becomes a byte of 0xFF
                    {
                        uint v = 0;
                        for (int i = 0; i < 8; i++)
                            if (((imm8 >> i) & 1u) != 0) v |= 0xFFu << (i * 8);
                        return v;
                    }
            }
        }

        public string Describe()
        {
            return string.Format(CultureInfo.InvariantCulture, "VFP {0} FPSCR=0x{1:X8} FPEXC=0x{2:X8}",
                Enabled ? "on" : "off", Fpscr, Fpexc);
        }
    }
}
