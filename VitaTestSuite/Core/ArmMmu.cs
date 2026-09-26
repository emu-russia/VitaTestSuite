// ARMv7-A (Cortex-A9 MPCore) short-descriptor MMU.
//
// Implements the VMSAv7 translation scheme used by the PS Vita's Kermit
// (Cortex-A9) main CPU when the SCTLR.M bit is set:
//
//   * first level descriptors: fault / page table (coarse) / section / supersection
//   * second level descriptors: fault / large page (64K, 4 subpages) / small page (4K)
//   * domains (DACR), AP/APX permissions, XN/XNX execute-never, TEX/CB attributes
//   * access-flag update and fault status (DFSR/IFSR/DFAR/IFAR) generation
//
// Copyright note: this is an original implementation written against the
// ARM Architecture Reference Manual (ARMv7-A, DDI0406C) chapter B3 "VMSAv7".
//
// This file is deliberately free of any WinForms / device dependency: it only
// sees MemoryHub for page table fetches.

using System;
using System.Collections.Generic;
using System.Globalization;
using VitaTestSuite.Core;

namespace VitaTestSuite.Core
{
    /// <summary>Reason a translation attempt failed (maps onto a DFSR/IFSR code).</summary>
    public enum MmFaultKind
    {
        None = 0,
        Alignment,
        Background,
        Section,
        Page,
        Domain,
        Permission
    }

    /// <summary>Result of one MMU translation: everything the core needs to complete the access.</summary>
    public struct MmResult
    {
        public bool Ok;
        public uint PhysAddr;
        /// <summary>True when the mapping is Device or Strongly-ordered (no unaligned support).</summary>
        public bool StronglyOrdered;
        public bool Device;
        /// <summary>Normal memory with the "write-back write-allocate" attribute.</summary>
        public bool Normal;

        public MmFaultKind Fault;
        /// <summary>FSR status code, already shifted into bits [3:0] plus the WnR bit.</summary>
        public uint FsrStatus;
        public uint FsrFull;
    }

    /// <summary>
    /// ARMv7-A short-descriptor MMU. One instance per core (each Cortex-A9 in the
    /// MPCore cluster has its own TTBR/DACR/SCTLR state).
    /// </summary>
    public class ArmMmu
    {
        // ---- CP15 system control registers -------------------------------------

        /// <summary>SCTLR: bit 0 = M (MMU enable), bit 1 = A (strict alignment), bit 2 = C, bit 3 = W, bit 12 = I.</summary>
        public uint Sctlr = 0x00C50078;   // Cortex-A9 reset value (MMU off, caches off, Vectors low)
        public uint Ttbr0;
        public uint Ttbr1;
        public uint Ttbcr;
        public uint Dacr = 0x00000001;    // Cortex-A9 reset: domain 0 = client
        public uint Dfsr;
        public uint Dfar;
        public uint Ifsr;
        public uint Ifar;
        public uint Adfsr;
        public uint Aifsr;
        public uint Prrr = 0x98E8E8E8;
        public uint Nmrr = 0x00009898;
        public uint Vbar;
        public uint ContextIdr;

        /// <summary>Translation table walks performed (statistics for the GUI).</summary>
        public long Walks;

        public ArmMmu(MemoryHub mem)
        {
            Mem = mem;
        }

        public MemoryHub Mem;

        public bool Enabled
        {
            get { return (Sctlr & 1u) != 0; }
        }

        public bool StrictAlignment
        {
            get { return (Sctlr & 2u) != 0; }
        }

        public bool HighVectors
        {
            get { return (Sctlr & (1u << 13)) != 0; }
        }

        public void Reset()
        {
            Sctlr = 0x00C50078;
            Ttbr0 = 0;
            Ttbr1 = 0;
            Ttbcr = 0;
            Dacr = 0x00000001;
            Dfsr = 0; Dfar = 0; Ifsr = 0; Ifar = 0;
            Adfsr = 0; Aifsr = 0;
            Prrr = 0x98E8E8E8;
            Nmrr = 0x00009898;
            Vbar = 0;
            ContextIdr = 0;
            Walks = 0;
        }

        // ---- helpers -----------------------------------------------------------

        private uint ReadTable(uint addr)
        {
            Walks++;
            return Mem.ReadWord(addr & 0xFFFFFFFCu);
        }

        /// <summary>TTBR select according to TTBCR.N (ARM ARM B3.5.2).</summary>
        public uint SelectTtbr(uint va, out int ttbrNum)
        {
            uint n = Ttbcr & 7u;
            if (n == 0)
            {
                ttbrNum = 0;
                return Ttbr0 & 0xFFFFC000u;   // TTBR0 must be aligned; low bits ignored
            }
            // Split address: TTBR0 covers [0, 2^(32-N)), TTBR1 covers the top 2^(32-N).
            uint boundary = (uint)(1u << (32 - (int)n));
            if (va < boundary)
            {
                ttbrNum = 0;
                return Ttbr0 & 0xFFFFC000u;
            }
            ttbrNum = 1;
            uint baseAddr = Ttbr1;
            // With TTBCR.N != 0 the TTBR1 base has N low bits ignored (2^(14-N) alignment).
            uint mask = 0xFFFFC000u;
            if (n >= 1) mask &= ~((1u << (14 - (int)n)) - 1u);
            return baseAddr & mask;
        }

        /// <summary>
        /// Build the complete DFSR/IFSR value for a fault (ARM ARM B3.9.5, table B3-26).
        /// Bit 11 is WnR (data aborts only) and bit 10 marks an external abort.
        /// </summary>
        private static uint FsrFor(MmFaultKind kind, bool write, uint domain, bool secondLevel)
        {
            uint status;
            switch (kind)
            {
                case MmFaultKind.Alignment: status = 0x01u; break;              // 0b00001
                case MmFaultKind.Background: status = 0x00u; break;
                case MmFaultKind.Domain: status = 0x09u | ((domain & 0xFu) << 4); break; // 0b01001
                case MmFaultKind.Permission: status = secondLevel ? 0x0Fu : 0x0Du; break;
                case MmFaultKind.Section: status = 0x05u; break;                // 0b00101
                default: status = 0x07u; break;                                 // 0b00111 page
            }
            uint full = status & 0x7FFu;
            if (write && kind != MmFaultKind.Background) full |= 1u << 11;
            return full;
        }

        private static bool DomainFault(uint dacr, uint domain)
        {
            uint v = (dacr >> (int)(domain * 2)) & 3u;
            return v == 0;                    // 0 = no access
        }

        private static bool DomainManager(uint dacr, uint domain)
        {
            return ((dacr >> (int)(domain * 2)) & 3u) == 3u;   // 3 = manager (no permission check)
        }

        /// <summary>AP/APX check. Returns true when the access is allowed.</summary>
        private static bool CheckAp(uint ap, uint apx, uint mode, bool write, bool isPageTableFormat)
        {
            bool privileged = mode != 0x10;      // anything but User mode is privileged

            if (isPageTableFormat)
            {
                // AP[2] (bit 2 of the field) = read-only for privileged modes.
                uint ap2 = (ap >> 2) & 1u;
                uint ap10 = ap & 3u;
                if (write && ap2 != 0) return false;              // read-only for everyone
                if ((ap10 & 1u) == 0)
                {
                    // no user access
                    return privileged;
                }
                if ((ap10 & 2u) != 0)
                {
                    // 0b11: full access for both
                    return true;
                }
                // 0b01: privileged RW, user no access
                return privileged;
            }
            else
            {
                uint ap01 = ap & 3u;
                uint apx2 = apx & 1u;      // acts as AP[2]
                if (write && apx2 != 0) return false;
                switch (ap01)
                {
                    case 0:
                        return privileged;            // privileged RW, user none
                    case 1:
                        return true;                  // RW for all
                    case 2:
                        return privileged && !write;  // privileged RO, user none
                    default:
                        return !write;                // RO for all
                }
            }
        }

        /// <summary>
        /// Large page AP check. For a large page the second level AP field selects
        /// one of four subpage permission sets (ARM ARM B3.7.3, table B3-16):
        ///   AP=00: subpage 0 privileged RW, subpages 1-3 inaccessible
        ///   AP=01: subpage 1 privileged RW, subpage 0 inaccessible, 2-3 user read-only
        ///   AP=10: subpages 0-1 privileged RW, subpages 2-3 user RW
        ///   AP=11: all subpages read-only for privileged and user
        /// </summary>
        private static bool CheckLargeAp(uint ap, uint subpage, uint mode, bool write)
        {
            bool privileged = mode != 0x10;
            switch (ap & 3u)
            {
                case 0u:
                    return subpage == 0u && privileged;
                case 1u:
                    if (subpage == 0u) return false;
                    if (subpage == 1u) return privileged;
                    return !write;                       // subpages 2,3 user read-only
                case 2u:
                    return privileged || !write || (subpage >= 2u);
                default:
                    return !write;                       // read-only for everyone
            }
        }

        /// <summary>
        /// Translate a virtual address. Returns false on fault with a human readable reason.
        /// This is the entry point used by the GUI/tests.
        /// </summary>
        public bool Translate(uint va, bool write, bool fetch, out uint pa, out string fault)
        {
            MmResult r = TranslateEx(va, write, fetch, 0x13);
            pa = r.Ok ? r.PhysAddr : 0;
            if (r.Ok)
            {
                fault = null;
                return true;
            }
            fault = FaultText(r.Fault, va, write, fetch);
            return false;
        }

        public string FaultText(MmFaultKind kind, uint va, bool write, bool fetch)
        {
            string s;
            switch (kind)
            {
                case MmFaultKind.Alignment: s = "alignment fault"; break;
                case MmFaultKind.Background: s = "translation fault (no TTBR entry)"; break;
                case MmFaultKind.Domain: s = "domain fault"; break;
                case MmFaultKind.Permission: s = "permission fault"; break;
                case MmFaultKind.Section: s = "section translation fault"; break;
                default: s = "page translation fault"; break;
            }
            return string.Format(CultureInfo.InvariantCulture, "{0} @ VA 0x{1:X8} ({2})", s, va, fetch ? "fetch" : (write ? "write" : "read"));
        }

        /// <summary>Full translation returning the fault details the core needs to raise an abort.</summary>
        public MmResult TranslateEx(uint va, bool write, bool fetch, uint mode)
        {
            MmResult res = new MmResult();

            if (!Enabled)
            {
                res.Ok = true;
                res.PhysAddr = va;
                res.Normal = true;
                return res;
            }

            int ttbrNum;
            uint ttbr = SelectTtbr(va, out ttbrNum);

            if (ttbrNum == 1 && (Ttbcr & 7u) == 0)
            {
                // TTBR1 is not in use: everything goes through TTBR0.
                ttbr = Ttbr0 & 0xFFFFC000u;
            }

            uint l1Index = (va >> 20) & 0xFFFu;
            uint l1Addr = ttbr | (l1Index << 2);
            uint l1 = ReadTable(l1Addr);

            uint type = l1 & 3u;
            if (type == 0u || type == 3u)
            {
                res.Fault = MmFaultKind.Section;
                res.FsrFull = FsrFor(MmFaultKind.Section, write, 0, false);
                res.FsrStatus = res.FsrFull;
                return res;
            }

            if (type == 2u)
            {
                // Section (1MB) or supersection (16MB).
                bool supersection = (l1 & (1u << 18)) != 0;
                uint ns = (l1 >> 19) & 1u;
                bool isSection = !supersection;
                uint domain = (l1 >> 5) & 0xFu;
                uint ap = (l1 >> 10) & 3u;
                uint apx = (l1 >> 15) & 1u;
                uint tex = (l1 >> 12) & 7u;
                uint c = (l1 >> 3) & 1u;
                uint b = (l1 >> 2) & 1u;
                uint xn = (l1 >> 4) & 1u;

                if (DomainFault(Dacr, domain))
                {
                    res.Fault = MmFaultKind.Domain;
                    res.FsrFull = FsrFor(MmFaultKind.Domain, write, domain, false);
                    res.FsrStatus = res.FsrFull;
                    return res;
                }

                bool manager = DomainManager(Dacr, domain);
                if (!manager && !CheckAp(ap, apx, mode, write, false))
                {
                    res.Fault = MmFaultKind.Permission;
                    res.FsrFull = FsrFor(MmFaultKind.Permission, write, domain, false);
                    res.FsrStatus = res.FsrFull;
                    return res;
                }

                if (fetch && !manager && xn != 0)
                {
                    res.Fault = MmFaultKind.Permission;
                    res.FsrFull = FsrFor(MmFaultKind.Permission, write, domain, false) | (1u << 3); // XN
                    res.FsrStatus = res.FsrFull;
                    return res;
                }

                uint physAddr;
                if (supersection)
                {
                    physAddr = (l1 & 0xFF000000u) | (va & 0x00FFFFFFu);
                    if (ns == 1) { res.Fault = MmFaultKind.Section; res.FsrFull = 0x05; return res; }
                }
                else
                {
                    physAddr = (l1 & 0xFFF00000u) | (va & 0x000FFFFFu);
                }

                // First level TEX[0],C,B / TEX[2:1] encoding (ARM ARM B3.7, table B3-13).
                uint texCb = ((tex & 1u) << 3) | (c << 2) | (b << 1);
                ClassifyMemory(texCb, tex, ref res);

                // Access flag update: mark the section as accessed.
                if ((l1 & (1u << 8)) == 0 && !manager)
                {
                    uint nl1 = l1 | (1u << 8);
                    Mem.WriteWord(l1Addr, nl1);
                }

                res.Ok = true;
                res.PhysAddr = physAddr;
                return res;
            }

            // type == 1: coarse page table.
            uint l2Base = l1 & 0xFFFFFC00u;
            uint l2Index = (va >> 12) & 0xFFu;
            uint l2Addr = l2Base | (l2Index << 2);
            uint l2 = ReadTable(l2Addr);

            uint t2 = l2 & 3u;
            if (t2 == 0u)
            {
                res.Fault = MmFaultKind.Page;
                res.FsrFull = FsrFor(MmFaultKind.Page, write, 0, true);
                res.FsrStatus = res.FsrFull;
                return res;
            }

            uint domain2 = (l2 >> 5) & 0xFu;
            if (DomainFault(Dacr, domain2))
            {
                res.Fault = MmFaultKind.Domain;
                res.FsrFull = FsrFor(MmFaultKind.Domain, write, domain2, true);
                res.FsrStatus = res.FsrFull;
                return res;
            }
            bool manager2 = DomainManager(Dacr, domain2);

            bool large = t2 == 1u;
            uint subpage = large ? ((va >> 14) & 3u) : 0u;

            uint apP, texP, cP, bP, xnP;
            uint pa;

            if (large)
            {
                // Large page: 64KB, 4 x 16KB subpages.
                // AP[1:0] at descriptor bits [5:4]; XN is bit 4 of the *subpage*
                // permission field, which for large pages is nested in AP - we use
                // the simple "AP selects the subpage permission set" model.
                apP = (l2 >> 4) & 3u;
                texP = (l2 >> 6) & 7u;
                cP = (l2 >> 3) & 1u;
                bP = (l2 >> 2) & 1u;
                xnP = (l2 >> 15) & 1u;
                pa = (l2 & 0xFFFF0000u) | (va & 0x0000FFFFu);
            }
            else
            {
                // Small page: 4KB. AP[2:0] = {descriptor[9], descriptor[5:4]}.
                uint apField = (l2 >> 4) & 3u;
                uint ap2 = (l2 >> 9) & 1u;
                apP = apField | (ap2 << 2);
                texP = (l2 >> 6) & 7u;
                cP = (l2 >> 3) & 1u;
                bP = (l2 >> 2) & 1u;
                xnP = (l2 >> 15) & 1u;
                pa = (l2 & 0xFFFFF000u) | (va & 0x00000FFFu);
            }

            bool allowed = large ? CheckLargeAp(apP, subpage, mode, write)
                                 : CheckAp(apP, 0u, mode, write, true);
            if (!manager2 && !allowed)
            {
                res.Fault = MmFaultKind.Permission;
                res.FsrFull = FsrFor(MmFaultKind.Permission, write, domain2, true);
                res.FsrStatus = res.FsrFull;
                return res;
            }

            if (fetch && !manager2 && xnP != 0)
            {
                res.Fault = MmFaultKind.Permission;
                res.FsrFull = FsrFor(MmFaultKind.Permission, write, domain2, true) | (1u << 3);
                res.FsrStatus = res.FsrFull;
                return res;
            }

            uint texCb2 = ((texP & 1u) << 3) | (cP << 2) | (bP << 1);
            ClassifyMemory(texCb2, texP, ref res);

            if ((l2 & (1u << 8)) == 0 && !manager2)
            {
                Mem.WriteWord(l2Addr, l2 | (1u << 8));
            }

            res.Ok = true;
            res.PhysAddr = pa;
            return res;
        }

        /// <summary>Classify a TEX/C/B combination into Normal / Device / Strongly-ordered.</summary>
        private static void ClassifyMemory(uint texCb, uint tex, ref MmResult res)
        {
            uint main = texCb & 7u;      // TEX[0],C,B
            uint texHigh = (tex >> 1) & 3u;
            if (texHigh != 0u)
            {
                // TEX[2:1] != 00 -> Normal memory (outer attributes); 0b001 is reserved.
                if (texHigh == 1u)
                {
                    res.StronglyOrdered = true;
                    return;
                }
                res.Normal = true;
                return;
            }

            switch (main)
            {
                case 0: res.StronglyOrdered = true; break;
                case 1: res.Device = true; break;      // shared device
                case 2: res.Device = true; break;      // non-shared device (TEX=0b010)
                case 3: res.Normal = true; break;      // write-back, no write allocate
                case 6: res.Device = true; break;      // TEX=0b001, C=1,B=0
                case 7: res.Normal = true; break;      // write-back, write allocate
                default: res.Normal = true; break;
            }
        }

        /// <summary>Update DFSR/DFAR (or IFSR/IFAR) and return the vector offset to take.</summary>
        public void ReportDataAbort(ref MmResult r, uint va, bool write)
        {
            Dfsr = (r.FsrFull & 0xFFFu);
            if (r.Fault == MmFaultKind.Alignment)
            {
                // Alignment faults are always DFSR=0b00001 with the WnR bit set for
                // an unaligned store (ARM ARM B3.9.4: fs[10] and WnR both possible).
                Dfsr = 0x01u | (write ? (1u << 11) : 0u);
                Dfar = va;
            }
            else
            {
                if (write) Dfsr |= 1u << 11; else Dfsr &= ~(1u << 11);
                if ((Dfsr & 0xFu) != 0x00) Dfar = va;    // external/background aborts do not set FAR
            }
            Adfsr = Dfsr;
        }

        public void ReportPrefetchAbort(ref MmResult r, uint va, bool write)
        {
            Ifsr = (r.FsrFull & 0xFFFu);
            if (r.Fault == MmFaultKind.Permission && (r.FsrFull & (1u << 3)) != 0)
                Ifsr = 0x0Fu | (1u << 3);     // section/page permission fault, XN
            Ifar = va;
            Aifsr = Ifsr;
        }

        /// <summary>One-line summary for the GUI status line / "mmu" command.</summary>
        public string Describe()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "MMU {0}  TTBR0=0x{1:X8} TTBR1=0x{2:X8} TTBCR=0x{3:X} DACR=0x{4:X8} walks={5}",
                Enabled ? "on" : "off", Ttbr0, Ttbr1, Ttbcr, Dacr, Walks);
        }
    }
}
