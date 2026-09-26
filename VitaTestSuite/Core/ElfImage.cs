// ElfImage.cs -- ELF32/ELF64 parsing, loading into the MemoryHub sandbox and
// the PS Vita module (SceModuleInfo) import/export NID tables.
//
// ELF parsing is a straight struct decode of the on-disk headers (no
// assumption about the host endianness), plus the Vita specific quirk that
// e_entry of a *module* is not a virtual address but the offset of the
// SceModuleInfo structure from the start of segment 0:
//
//    modinfo_file_offset = segment0.p_offset + e_entry
//
// (verified against real firmware images, see ResolvedEntry below). For every
// other image e_entry is a normal virtual address. Parse() decides which one
// it is by checking whether e_entry falls inside a PT_LOAD segment, and stores
// the answer in ResolvedEntry; LoadInto() returns ResolvedEntry + bias.
//
// The module table reader is a port of pup_fiction/vita_loader/vita_loader.py
// (ELFHeader/ELFphdr/Modinfo/Modexport/Modimport/parse_impexp). The struct
// sizes differ between SDK/firmware generations, so the walk uses the "size"
// field stored in the first bytes of each entry and the field layout is
// validated against segment 0 (see TryReadExportEntry/TryReadImportEntry).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace VitaTestSuite.Core
{
    /// <summary>One program header (PT_LOAD and friends) of an ELF image.</summary>
    public class ElfSegment
    {
        public uint Type;       // p_type  (1 = PT_LOAD)
        public uint Offset;     // p_offset
        public uint VAddr;      // p_vaddr
        public uint PAddr;      // p_paddr
        public uint FileSize;   // p_filesz
        public uint MemSize;    // p_memsz
        public uint Flags;      // p_flags (1=X, 2=W, 4=R)
        public uint Align;      // p_align
        public bool Executable; // (p_flags & 1) != 0

        public bool IsLoad { get { return Type == 1; } }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "type=0x{0:X8} off=0x{1:X8} vaddr=0x{2:X8} paddr=0x{3:X8} filesz=0x{4:X} memsz=0x{5:X} flags=0x{6:X} align=0x{7:X}{8}",
                Type, Offset, VAddr, PAddr, FileSize, MemSize, Flags, Align, Executable ? " X" : "");
        }
    }

    /// <summary>
    /// A parsed ELF image plus the ability to load its PT_LOAD segments into a
    /// <see cref="MemoryHub"/>.
    /// </summary>
    public class ElfImage
    {
        public const ushort EM_ARM = 0x28;
        public const ushort EM_MEP = 0xF00D;

        public bool Is64;
        public ushort Machine;
        public uint Entry;          // raw e_entry
        public uint PhOff, ShOff;
        public ushort PhEntSize, PhNum, ShEntSize, ShNum;

        /// <summary>e_type (2 = ET_EXEC, 0xFE00/0xFE04 = PS Vita module).</summary>
        public ushort ElfType;
        /// <summary>Full 64 bit e_entry (Entry is the truncated 32 bit view).</summary>
        public ulong Entry64;

        public List<ElfSegment> Segments = new List<ElfSegment>();

        /// <summary>
        /// e_entry translated to a virtual address using the Vita module rule
        /// when the raw value is not inside any PT_LOAD segment.
        /// </summary>
        public uint ResolvedEntry;

        /// <summary>Non-fatal problems seen while parsing/loading (empty when clean).</summary>
        public List<string> Warnings = new List<string>();

        // ------------------------------------------------------------ sniffing

        public static bool IsElf(byte[] data)
        {
            return data != null && data.Length >= 16 &&
                   data[0] == 0x7F && data[1] == 0x45 && data[2] == 0x4C && data[3] == 0x46;
        }

        public static string MachineName(ushort machine)
        {
            switch (machine)
            {
                case EM_ARM: return "ARM";
                case EM_MEP: return "MeP";
                case 0x03: return "x86";
                case 0x3E: return "x86-64";
                case 0xB7: return "AArch64";
                case 0x14: return "PowerPC";
                case 0xF3: return "RL78";
                default: return "machine_0x" + machine.ToString("X4", CultureInfo.InvariantCulture);
            }
        }

        public static CpuArch ArchOf(ushort machine)
        {
            switch (machine)
            {
                case EM_ARM: return CpuArch.Arm;
                case EM_MEP: return CpuArch.MeP;
                case 0xF3: return CpuArch.Rl78;
                default: return CpuArch.Unknown;
            }
        }

        public CpuArch Arch { get { return ArchOf(Machine); } }

        // ------------------------------------------------------------- parsing

        public static ElfImage Parse(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length < 52)
                throw new InvalidDataException("ELF too small (" + data.Length + " bytes)");
            if (!IsElf(data))
                throw new InvalidDataException("bad ELF magic (expected 7F 45 4C 46)");

            ElfImage img = new ElfImage();
            byte cls = data[4];
            if (cls == 1) img.Is64 = false;
            else if (cls == 2) img.Is64 = true;
            else throw new InvalidDataException("unknown ELF class " + cls + " (expected 1=ELF32 or 2=ELF64)");

            img.Machine = BinUtil.U16(data, 0x12);
            img.ElfType = BinUtil.U16(data, 0x10);

            if (!img.Is64)
            {
                img.Entry64 = BinUtil.U32(data, 0x18);
                img.PhOff = BinUtil.U32(data, 0x1C);
                img.ShOff = BinUtil.U32(data, 0x20);
                img.PhEntSize = BinUtil.U16(data, 0x2A);
                img.PhNum = BinUtil.U16(data, 0x2C);
                img.ShEntSize = BinUtil.U16(data, 0x2E);
                img.ShNum = BinUtil.U16(data, 0x30);
            }
            else
            {
                img.Entry64 = BinUtil.U64(data, 0x18);
                img.PhOff = BinUtil.U32(data, 0x20);   // low 32 bits (Vita images are 32 bit)
                img.ShOff = BinUtil.U32(data, 0x28);
                img.PhEntSize = BinUtil.U16(data, 0x36);
                img.PhNum = BinUtil.U16(data, 0x38);
                img.ShEntSize = BinUtil.U16(data, 0x3A);
                img.ShNum = BinUtil.U16(data, 0x3C);
            }
            img.Entry = (uint)img.Entry64;

            int phEnt = img.PhEntSize;
            int phOff = (int)img.PhOff;
            if (img.PhNum > 0)
            {
                if (phEnt <= 0)
                {
                    phEnt = img.Is64 ? 56 : 32;
                    img.Warnings.Add("e_phentsize was 0, assuming " + phEnt);
                }
                for (int i = 0; i < img.PhNum; i++)
                {
                    int o = phOff + i * phEnt;
                    if (o < 0 || o + phEnt > data.Length)
                    {
                        img.Warnings.Add("program header " + i + " is outside the file (offset 0x" + o.ToString("X", CultureInfo.InvariantCulture) + ")");
                        break;
                    }
                    ElfSegment s = new ElfSegment();
                    if (!img.Is64)
                    {
                        s.Type = BinUtil.U32(data, o + 0x00);
                        s.Offset = BinUtil.U32(data, o + 0x04);
                        s.VAddr = BinUtil.U32(data, o + 0x08);
                        s.PAddr = BinUtil.U32(data, o + 0x0C);
                        s.FileSize = BinUtil.U32(data, o + 0x10);
                        s.MemSize = BinUtil.U32(data, o + 0x14);
                        s.Flags = BinUtil.U32(data, o + 0x18);
                        s.Align = BinUtil.U32(data, o + 0x1C);
                    }
                    else
                    {
                        s.Type = BinUtil.U32(data, o + 0x00);
                        s.Flags = BinUtil.U32(data, o + 0x04);
                        s.Offset = (uint)BinUtil.U64(data, o + 0x08);
                        s.VAddr = (uint)BinUtil.U64(data, o + 0x10);
                        s.PAddr = (uint)BinUtil.U64(data, o + 0x18);
                        s.FileSize = (uint)BinUtil.U64(data, o + 0x20);
                        s.MemSize = (uint)BinUtil.U64(data, o + 0x28);
                        s.Align = (uint)BinUtil.U64(data, o + 0x30);
                    }
                    s.Executable = (s.Flags & 1) != 0;
                    img.Segments.Add(s);
                }
            }

            img.ResolvedEntry = img.ResolveEntry();
            return img;
        }

        /// <summary>
        /// Raw e_entry is a virtual address when it lands inside a PT_LOAD
        /// segment; otherwise (PS Vita modules) it is an offset from the start
        /// of segment 0, so the address is segment0.p_vaddr + e_entry.
        /// </summary>
        public uint ResolveEntry()
        {
            uint raw = Entry;
            ElfSegment seg0 = FirstLoad();
            for (int i = 0; i < Segments.Count; i++)
            {
                ElfSegment s = Segments[i];
                if (!s.IsLoad || s.MemSize == 0) continue;
                if (raw >= s.VAddr && (ulong)raw < (ulong)s.VAddr + s.MemSize) return raw;
            }
            if (seg0 != null) return seg0.VAddr + raw;
            return raw;
        }

        public ElfSegment FirstLoad()
        {
            for (int i = 0; i < Segments.Count; i++)
                if (Segments[i].IsLoad) return Segments[i];
            return Segments.Count > 0 ? Segments[0] : null;
        }

        /// <summary>True when <paramref name="entryOrOffset"/> equals the raw
        /// e_entry, i.e. the image uses the "module info offset" convention.</summary>
        public bool EntryIsModuleOffset
        {
            get { return Segments.Count > 0 && ResolvedEntry != Entry; }
        }

        /// <summary>File offset of the SceModuleInfo structure, or -1.</summary>
        public long ModuleInfoOffset()
        {
            if (!EntryIsModuleOffset) return -1;
            ElfSegment seg0 = FirstLoad();
            if (seg0 == null) return -1;
            return (long)seg0.Offset + Entry;
        }

        // ----------------------------------------------------------- describe

        public string Describe()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ELF ").Append(Is64 ? "64" : "32");
            sb.Append(" machine=").Append(MachineName(Machine));
            sb.Append(" (0x").Append(Machine.ToString("X4", CultureInfo.InvariantCulture)).Append(')');
            sb.Append(" e_type=0x").Append(ElfType.ToString("X4", CultureInfo.InvariantCulture));
            sb.Append(" arch=").Append(Arch);
            sb.Append(" entry=0x").Append(Entry.ToString("X", CultureInfo.InvariantCulture));
            if (EntryIsModuleOffset)
                sb.Append(" (module-info offset -> 0x").Append(ResolvedEntry.ToString("X", CultureInfo.InvariantCulture)).Append(')');
            sb.Append(" phoff=0x").Append(PhOff.ToString("X", CultureInfo.InvariantCulture));
            sb.Append(" phnum=").Append(PhNum);
            sb.Append(" phdrs=").Append(Segments.Count);
            sb.Append("\n");
            for (int i = 0; i < Segments.Count; i++)
                sb.Append("  ph[").Append(i).Append("] ").Append(Segments[i]).Append('\n');
            for (int i = 0; i < Warnings.Count; i++)
                sb.Append("  ! ").Append(Warnings[i]).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }

        // ------------------------------------------------------------- loading

        private const uint PageMask = 0xFFFFF000u;

        /// <summary>Map [addr, addr+size) in the hub if it is not mapped yet.</summary>
        private void EnsureMapped(MemoryHub hub, uint addr, ulong size, string tag, string note)
        {
            if (size == 0) return;
            uint pageBase = addr & PageMask;
            ulong pageEnd = (addr + size + 0xFFFul) & ~0xFFFul;
            if (pageEnd <= pageBase) pageEnd = (ulong)pageBase + 0x1000;

            int total = (int)(pageEnd - pageBase);
            if (total <= 0) { Warnings.Add("segment at 0x" + addr.ToString("X", CultureInfo.InvariantCulture) + " has an unusable size"); return; }

            if (hub.FindRangeAt(pageBase, total) != null) return;   // fully covered already

            // Partial overlap with an existing range: grow nothing, just report it.
            if (hub.FindRangeAt(addr, 1) != null || hub.FindRangeAt((uint)(pageEnd - 1), 1) != null)
                Warnings.Add("segment at 0x" + addr.ToString("X", CultureInfo.InvariantCulture) + " partially overlaps an existing mapping; using a new range");

            hub.AddMemory(tag, total, pageBase, note);
        }

        /// <summary>
        /// Load every PT_LOAD segment into <paramref name="hub"/>, mapping RAM
        /// as needed, and return the entry point (+ <paramref name="bias"/>).
        /// Zero filled p_memsz &gt; p_filesz tails (BSS) are written explicitly so
        /// a re-used mapping cannot leak stale bytes.
        /// </summary>
        public uint LoadInto(MemoryHub hub, byte[] data, uint bias, string tagPrefix)
        {
            if (hub == null) throw new ArgumentNullException("hub");
            if (data == null) throw new ArgumentNullException("data");
            if (tagPrefix == null) tagPrefix = "elf";

            int n = 0;
            for (int i = 0; i < Segments.Count; i++)
            {
                ElfSegment s = Segments[i];
                if (!s.IsLoad || s.MemSize == 0) continue;

                uint dest = s.VAddr + bias;
                EnsureMapped(hub, dest, s.MemSize, tagPrefix + (n == 0 ? "" : n.ToString(CultureInfo.InvariantCulture)),
                             "ELF PT_LOAD[" + i + "] " + (s.Executable ? "code" : "data"));

                uint fileSize = s.FileSize;
                if (fileSize > 0)
                {
                    if ((ulong)s.Offset + fileSize > (ulong)data.Length)
                    {
                        uint avail = s.Offset < data.Length ? (uint)(data.Length - s.Offset) : 0;
                        Warnings.Add("segment " + i + ": p_filesz 0x" + fileSize.ToString("X", CultureInfo.InvariantCulture) +
                                     " truncated to 0x" + avail.ToString("X", CultureInfo.InvariantCulture) + " (file too short)");
                        fileSize = avail;
                    }
                    if (fileSize > 0)
                    {
                        byte[] chunk = new byte[fileSize];
                        Array.Copy(data, (int)s.Offset, chunk, 0, (int)fileSize);
                        if (!hub.LoadDump(dest, chunk))
                            Warnings.Add("segment " + i + ": LoadDump(0x" + dest.ToString("X", CultureInfo.InvariantCulture) + ", " + fileSize + ") failed");
                    }
                }

                // BSS: zero the part of p_memsz that has no file backing.
                if (s.MemSize > fileSize)
                {
                    uint zeroAt = dest + fileSize;
                    uint zeroLen = s.MemSize - fileSize;
                    ZeroFill(hub, zeroAt, zeroLen);
                }
                n++;
            }

            return ResolvedEntry + bias;
        }

        private static void ZeroFill(MemoryHub hub, uint addr, uint len)
        {
            byte[] zeros = new byte[len < 0x10000 ? len : 0x10000];
            uint done = 0;
            while (done < len)
            {
                uint n = len - done;
                if (n > (uint)zeros.Length) n = (uint)zeros.Length;
                hub.WriteBytes(addr + done, zeros, 0, (int)n);
                done += n;
            }
        }
    }

    // =====================================================================
    //  PS Vita module import/export (NID) tables
    // =====================================================================

    /// <summary>One exported function: (function address, NID) inside a library.</summary>
    public class ModuleExport
    {
        public uint Nid;
        public uint Func;
        public uint LibNid;
        public string LibName;

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "export 0x{0:X8} NID=0x{1:X8} lib={2} (0x{3:X8})",
                Func, Nid, LibName, LibNid);
        }
    }

    /// <summary>One imported function: (stub address, NID) inside a library.</summary>
    public class ModuleImport
    {
        public uint Nid;
        public uint Func;
        public uint LibNid;
        public string LibName;

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "import 0x{0:X8} NID=0x{1:X8} lib={2} (0x{3:X8})",
                Func, Nid, LibName, LibNid);
        }
    }

    /// <summary>
    /// The SceModuleInfo of a PS Vita ELF: module name plus the export and
    /// import tables. Port of vita_loader.py's VitaElf.parse_impexp().
    /// </summary>
    public class VitaModule
    {
        public string ModuleName = "";
        public int ModuleType = -1;
        public ushort ModuleAttribute;
        public uint ExportTop, ExportEnd, ImportTop, ImportEnd;

        public List<ModuleExport> Exports = new List<ModuleExport>();
        public List<ModuleImport> Imports = new List<ModuleImport>();

        /// <summary>Non-fatal problems (unusual struct sizes, clamps, ...).</summary>
        public List<string> Warnings = new List<string>();

        /// <summary>Distinct exported library names, in table order.</summary>
        public List<string> ExportLibraries()
        {
            List<string> r = new List<string>();
            for (int i = 0; i < Exports.Count; i++)
                if (!r.Contains(Exports[i].LibName)) r.Add(Exports[i].LibName);
            return r;
        }

        /// <summary>Distinct imported library names, in table order.</summary>
        public List<string> ImportLibraries()
        {
            List<string> r = new List<string>();
            for (int i = 0; i < Imports.Count; i++)
                if (!r.Contains(Imports[i].LibName)) r.Add(Imports[i].LibName);
            return r;
        }

        public static bool LooksLikeModule(ElfImage elf)
        {
            if (elf == null) return false;
            if (elf.ElfType != 0xFE00 && elf.ElfType != 0xFE04 && elf.ElfType != 0xFE01) return false;
            return elf.ModuleInfoOffset() >= 0;
        }

        /// <summary>
        /// Parse the module table: segment0 + e_entry -&gt; SceModuleInfo -&gt;
        /// export/import tables. Never throws for a plausible ELF; problems are
        /// appended to <see cref="Warnings"/>.
        /// </summary>
        public static VitaModule Parse(byte[] elfData, ElfImage elf)
        {
            if (elfData == null) throw new ArgumentNullException("elfData");
            if (elf == null) throw new ArgumentNullException("elf");

            VitaModule m = new VitaModule();
            ElfSegment seg0 = elf.FirstLoad();
            if (seg0 == null)
            {
                m.Warnings.Add("ELF has no PT_LOAD segment");
                return m;
            }

            long seg0Off = seg0.Offset;
            uint seg0Va = seg0.VAddr;
            long seg0End = seg0Off + seg0.FileSize;
            long modOff = elf.ModuleInfoOffset();
            if (modOff < 0)
            {
                // e_entry already looked like a VA; fall back to the raw value
                // (vita_loader always does seg0_off + e_entry).
                modOff = seg0Off + elf.Entry;
                m.Warnings.Add("e_entry 0x" + elf.Entry.ToString("X", CultureInfo.InvariantCulture) +
                               " is a virtual address; trying seg0.p_offset + e_entry anyway");
            }

            if (!BinUtil.InRange(elfData, modOff, 0x34))
            {
                m.Warnings.Add("module info at 0x" + modOff.ToString("X", CultureInfo.InvariantCulture) + " is outside the file");
                return m;
            }

            m.ModuleAttribute = BinUtil.U16(elfData, modOff + 0x00);
            m.ModuleName = BinUtil.CStr(elfData, modOff + 4, 27);
            m.ModuleType = BinUtil.U8(elfData, modOff + 0x1F);
            m.ExportTop = BinUtil.U32(elfData, modOff + 0x24);
            m.ExportEnd = BinUtil.U32(elfData, modOff + 0x28);
            m.ImportTop = BinUtil.U32(elfData, modOff + 0x2C);
            m.ImportEnd = BinUtil.U32(elfData, modOff + 0x30);

            ParseTable(elfData, m, seg0Off, seg0Va, seg0End, m.ExportTop, m.ExportEnd, true);
            ParseTable(elfData, m, seg0Off, seg0Va, seg0End, m.ImportTop, m.ImportEnd, false);
            return m;
        }

        private static void ParseTable(byte[] data, VitaModule m, long seg0Off, uint seg0Va, long seg0End,
                                       uint top, uint end, bool export)
        {
            if (top == 0 || end == 0 || end <= top) return;
            if ((ulong)top > (ulong)seg0End - (ulong)seg0Off)
            {
                m.Warnings.Add((export ? "export" : "import") + " table 0x" + top.ToString("X", CultureInfo.InvariantCulture) + " is outside segment 0");
                return;
            }

            long cur = top;
            int guard = 0;
            while ((ulong)cur < end && guard++ < 4096)
            {
                long o = seg0Off + cur;
                if (!BinUtil.InRange(data, o, export ? 0x20 : 0x18))
                {
                    m.Warnings.Add((export ? "export" : "import") + " entry at 0x" + cur.ToString("X", CultureInfo.InvariantCulture) + " runs past the end of the file");
                    break;
                }

                uint numFuncs, libNid, libNamePtr, nidTable, entryTable;
                int advance;

                if (export)
                {
                    // Modexport (vita_loader offsets), size byte at +0
                    advance = BinUtil.U8(data, o + 0);
                    numFuncs = BinUtil.U16(data, o + 6);
                    libNid = BinUtil.U32(data, o + 16);
                    libNamePtr = BinUtil.U32(data, o + 20);
                    nidTable = BinUtil.U32(data, o + 24);
                    entryTable = BinUtil.U32(data, o + 28);
                }
                else
                {
                    // Two known Modimport layouts:
                    //   0x24 (SDK/devkit)  num_funcs@6  libnid@12 libname@16 nid@20 entry@24
                    //   0x34 (firmware)    num_funcs@4  libnid@16 libname@20 nid@28 entry@32
                    int size = BinUtil.U16(data, o + 0);
                    if (size >= 0x34)
                    {
                        advance = size;
                        numFuncs = BinUtil.U16(data, o + 4);
                        libNid = BinUtil.U32(data, o + 0x10);
                        libNamePtr = BinUtil.U32(data, o + 0x14);
                        nidTable = BinUtil.U32(data, o + 0x1C);
                        entryTable = BinUtil.U32(data, o + 0x20);
                    }
                    else
                    {
                        advance = size > 0 ? size : 0x24;
                        numFuncs = BinUtil.U16(data, o + 6);
                        libNid = BinUtil.U32(data, o + 12);
                        libNamePtr = BinUtil.U32(data, o + 16);
                        nidTable = BinUtil.U32(data, o + 20);
                        entryTable = BinUtil.U32(data, o + 24);
                    }
                }

                string libName = "";
                if (libNamePtr != 0)
                {
                    long no = seg0Off + (long)libNamePtr - seg0Va;
                    if (BinUtil.InRange(data, no, 1)) libName = BinUtil.CStr(data, no, 256);
                    else m.Warnings.Add("library name pointer 0x" + libNamePtr.ToString("X", CultureInfo.InvariantCulture) + " is outside segment 0");
                }
                if (libName.Length == 0) libName = "noname";

                if (numFuncs > 0 && nidTable != 0 && entryTable != 0)
                {
                    long nidOff = seg0Off + (long)nidTable - seg0Va;
                    long entOff = seg0Off + (long)entryTable - seg0Va;
                    for (uint x = 0; x < numFuncs; x++)
                    {
                        long nn = nidOff + x * 4;
                        long ne = entOff + x * 4;
                        if ((ulong)nn + 4 > (ulong)seg0End || (ulong)ne + 4 > (ulong)seg0End)
                        {
                            m.Warnings.Add(libName + ": table truncated after " + x + " of " + numFuncs + " entries");
                            break;
                        }
                        uint nid = BinUtil.U32(data, nn);
                        uint func = BinUtil.U32(data, ne);
                        if (export)
                        {
                            ModuleExport e = new ModuleExport();
                            e.Nid = nid; e.Func = func; e.LibNid = libNid; e.LibName = libName;
                            m.Exports.Add(e);
                        }
                        else
                        {
                            ModuleImport e = new ModuleImport();
                            e.Nid = nid; e.Func = func; e.LibNid = libNid; e.LibName = libName;
                            m.Imports.Add(e);
                        }
                    }
                }
                else if (numFuncs != 0)
                {
                    m.Warnings.Add((export ? "export" : "import") + " " + libName + ": numFuncs=" + numFuncs +
                                   " but nid/entry table is null (nid=0x" + nidTable.ToString("X", CultureInfo.InvariantCulture) +
                                   " entry=0x" + entryTable.ToString("X", CultureInfo.InvariantCulture) + ")");
                }

                if (advance <= 0)
                {
                    m.Warnings.Add((export ? "export" : "import") + " entry at 0x" + cur.ToString("X", CultureInfo.InvariantCulture) + " has size 0; stopping");
                    break;
                }
                cur += advance;
            }
        }

        /// <summary>Human readable report; every unknown NID is resolved through
        /// <paramref name="db"/> when one is supplied.</summary>
        public string Describe(NidDatabase db)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("module '").Append(ModuleName).Append("' type=0x")
              .Append(ModuleType.ToString("X2", CultureInfo.InvariantCulture))
              .Append(" exports=").Append(Exports.Count)
              .Append(" imports=").Append(Imports.Count).Append('\n');

            sb.Append("  export libs: ");
            List<string> el = ExportLibraries();
            if (el.Count == 0) sb.Append("(none)");
            else for (int i = 0; i < el.Count; i++) { if (i > 0) sb.Append(", "); sb.Append(el[i]); }
            sb.Append('\n');

            sb.Append("  import libs: ");
            List<string> il = ImportLibraries();
            if (il.Count == 0) sb.Append("(none)");
            else for (int i = 0; i < il.Count; i++) { if (i > 0) sb.Append(", "); sb.Append(il[i]); }
            sb.Append('\n');

            int shown = 0;
            for (int i = 0; i < Exports.Count && shown < 10; i++, shown++)
                sb.Append("  exp ").Append(NidName(db, Exports[i].Nid)).Append(' ').Append(Exports[i]).Append('\n');
            shown = 0;
            for (int i = 0; i < Imports.Count && shown < 10; i++, shown++)
                sb.Append("  imp ").Append(NidName(db, Imports[i].Nid)).Append(' ').Append(Imports[i]).Append('\n');

            for (int i = 0; i < Warnings.Count; i++)
                sb.Append("  ! ").Append(Warnings[i]).Append('\n');

            return sb.ToString().TrimEnd('\n');
        }

        private static string NidName(NidDatabase db, uint nid)
        {
            if (db == null) return "";
            string n = db.Lookup(nid);
            return n == null ? "" : n;
        }
    }
}
