// ImageLoader.cs -- architecture / container auto-detection and the
// "load this file into the sandbox" entry points.
//
// Detection rules (as specified for VitaTestSuite):
//   * 7F 45 4C 46                 -> ELF; e_machine 0x28 ARM, 0xF00D MeP
//   * 53 43 45 00 ("SCE\0")       -> SELF; the *inner* ELF's e_machine decides
//   * "SCEUF"                     -> PSP2UPDAT package
//   * otherwise a raw binary, decided from the file name:
//        *bootrom* *first_loader* *secure_kernel* *second_loader* -> MeP
//        USS-*  *ernie*  *IRT-*                                  -> RL78
//        *.bin whose first words look like ARM (LDR literal / B / BL / PUSH)
//                                                                -> ARM
//        anything else                                            -> Unknown

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace VitaTestSuite.Core
{
    public enum ImageKind
    {
        Unknown = 0,
        RawBinary,
        Elf32,
        Elf64,
        SceSelf,
        PupPackage
    }

    /// <summary>Result of <see cref="ImageLoader.Identify"/>.</summary>
    public class ImageInfo
    {
        public ImageKind Kind;
        public CpuArch Arch;
        /// <summary>Entry point (a virtual address; for Vita modules the
        /// e_entry offset has already been converted, see ElfImage.ResolvedEntry).
        /// 0 when the image has no entry point.</summary>
        public uint EntryPoint;
        /// <summary>Lowest PT_LOAD virtual address (0 for raw binaries/containers).</summary>
        public uint LoadAddress;
        /// <summary>One line human readable summary.</summary>
        public string Describe;
        /// <summary>Suggested tag for MemoryHub.AddMemory().</summary>
        public string SuggestedMemoryTag;
        /// <summary>Suggested RAM size in bytes for the whole image.</summary>
        public int SuggestedMemorySize;
        /// <summary>Why the arch is Unknown, when it is.</summary>
        public string ArchReason;
    }

    public static class ImageLoader
    {
        // ------------------------------------------------------------ sniffing

        /// <summary>Sniff a file: ELF (32/64, ARM 0x28 / MeP 0xF00D), SCE 0x00454353, PUP "SCEUF", raw.</summary>
        public static ImageInfo Identify(byte[] data, string filename)
        {
            ImageInfo info = new ImageInfo();
            info.Kind = ImageKind.Unknown;
            info.Arch = CpuArch.Unknown;
            info.SuggestedMemoryTag = "image";
            info.ArchReason = "";

            if (data == null || data.Length == 0)
            {
                info.Describe = "empty file";
                info.ArchReason = "no data";
                return info;
            }

            if (ElfImage.IsElf(data))
            {
                return IdentifyElf(data);
            }

            if (SceSelf.IsSce(data))
            {
                return IdentifySce(data, filename);
            }

            if (PupPackage.IsPup(data))
            {
                info.Kind = ImageKind.PupPackage;
                info.Arch = CpuArch.Unknown;
                info.SuggestedMemoryTag = "pup";
                info.SuggestedMemorySize = 0;
                info.ArchReason = "PUP is a container; the arch of each segment differs";
                string desc = "PSP2UPDAT package";
                try
                {
                    PupPackage p = PupPackage.Parse(data, null);
                    info.LoadAddress = 0;
                    info.EntryPoint = 0;
                    desc = "PSP2UPDAT package version=" + p.Version + " firmware=0x" +
                           p.FirmwareVersion.ToString("X8", CultureInfo.InvariantCulture) +
                           " files=" + p.FileCount;
                }
                catch (Exception ex)
                {
                    desc = "PSP2UPDAT package (TOC parse failed: " + ex.Message + ")";
                }
                info.Describe = desc;
                return info;
            }

            return IdentifyRaw(data, filename);
        }

        private static ImageInfo IdentifyElf(byte[] data)
        {
            ImageInfo info = new ImageInfo();
            info.SuggestedMemoryTag = "elf";
            info.Kind = data.Length > 4 && data[4] == 2 ? ImageKind.Elf64 : ImageKind.Elf32;
            try
            {
                ElfImage img = ElfImage.Parse(data);
                info.Arch = img.Arch;
                info.EntryPoint = img.ResolvedEntry;
                if (info.Arch == CpuArch.Unknown)
                    info.ArchReason = "unsupported e_machine 0x" + img.Machine.ToString("X4", CultureInfo.InvariantCulture);
                else info.ArchReason = "";

                uint low = 0xFFFFFFFFu;
                ulong high = 0;
                int loads = 0;
                for (int i = 0; i < img.Segments.Count; i++)
                {
                    ElfSegment s = img.Segments[i];
                    if (!s.IsLoad || s.MemSize == 0) continue;
                    loads++;
                    if (s.VAddr < low) low = s.VAddr;
                    if ((ulong)s.VAddr + s.MemSize > high) high = (ulong)s.VAddr + s.MemSize;
                }
                info.LoadAddress = loads > 0 ? low : 0;
                info.SuggestedMemorySize = loads > 0 ? RoundUp((int)Math.Min((ulong)int.MaxValue, high - low)) : RoundUp(data.Length);
                info.Describe = "ELF" + (img.Is64 ? "64" : "32") + " " + ElfImage.MachineName(img.Machine) +
                                " e_type=0x" + img.ElfType.ToString("X4", CultureInfo.InvariantCulture) +
                                " entry=0x" + img.ResolvedEntry.ToString("X", CultureInfo.InvariantCulture) +
                                " (raw e_entry=0x" + img.Entry.ToString("X", CultureInfo.InvariantCulture) + ")" +
                                " phnum=" + img.PhNum + " loadaddr=0x" + info.LoadAddress.ToString("X", CultureInfo.InvariantCulture) +
                                " span=0x" + ((ulong)high - low).ToString("X", CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                info.Describe = "ELF (parse failed: " + ex.Message + ")";
                info.ArchReason = "ELF header parse failed: " + ex.Message;
            }
            return info;
        }

        private static ImageInfo IdentifySce(byte[] data, string filename)
        {
            ImageInfo info = new ImageInfo();
            info.Kind = ImageKind.SceSelf;
            info.SuggestedMemoryTag = "self";
            info.SuggestedMemorySize = RoundUp(data.Length);
            try
            {
                SceSelf s = SceSelf.Parse(data);
                string desc = "SCE " + (s.Sce != null ? s.Sce.SceTypeName : "?");
                if (s.Sce != null)
                {
                    desc += " platform=0x" + s.Sce.Platform.ToString("X2", CultureInfo.InvariantCulture) +
                            " keyrev=" + s.Sce.KeyRevision +
                            " header_len=0x" + s.Sce.HeaderLength.ToString("X", CultureInfo.InvariantCulture);
                }
                if (s.AppInfo != null)
                {
                    desc += " self_type=0x" + s.AppInfo.SelfType.ToString("X", CultureInfo.InvariantCulture) +
                            " (" + s.AppInfo.SelfTypeName + ")" +
                            " sys_version=0x" + s.AppInfo.SysVersion.ToString("X", CultureInfo.InvariantCulture);
                }

                // The inner ELF header is stored in the clear even in encrypted
                // SELFs, so the arch can be determined without any key.
                if (s.Self != null && BinUtil.InRange(data, (long)s.Self.ElfOffset, 52) &&
                    data[s.Self.ElfOffset] == 0x7F && data[s.Self.ElfOffset + 1] == 0x45)
                {
                    ushort machine = BinUtil.U16(data, (long)s.Self.ElfOffset + 0x12);
                    info.Arch = ElfImage.ArchOf(machine);
                    uint entry = BinUtil.U32(data, (long)s.Self.ElfOffset + 0x18);
                    ushort phNum = BinUtil.U16(data, (long)s.Self.ElfOffset + 0x2C);
                    desc += " inner=ELF" + ElfImage.MachineName(machine) + " entry=0x" + entry.ToString("X", CultureInfo.InvariantCulture) +
                            " phnum=" + phNum;
                    if (s.SegmentInfos.Count > 0)
                    {
                        ulong lo = ulong.MaxValue, hi = 0;
                        int phEntSize = BinUtil.U16(data, (long)s.Self.ElfOffset + 0x2A);
                        if (phEntSize == 0) phEntSize = 32;
                        for (int i = 0; i < phNum; i++)
                        {
                            long p = (long)s.Self.PhdrOffset + (long)i * phEntSize;
                            if (!BinUtil.InRange(data, p, 32)) break;
                            uint type = BinUtil.U32(data, p);
                            if (type != 1) continue;
                            uint va = BinUtil.U32(data, p + 8);
                            uint msz = BinUtil.U32(data, p + 20);
                            if (va < lo) lo = va;
                            if ((ulong)va + msz > hi) hi = (ulong)va + msz;
                        }
                        if (hi > lo)
                        {
                            info.LoadAddress = (uint)lo;
                            // e_entry of a Vita image is a segment relative offset,
                            // but tolerate a real virtual address as well.
                            info.EntryPoint = (entry >= lo && (ulong)entry < hi) ? entry : (uint)lo + entry;
                            info.SuggestedMemorySize = RoundUp((int)Math.Min((ulong)int.MaxValue, hi - lo));
                            desc += " loadaddr=0x" + lo.ToString("X", CultureInfo.InvariantCulture);
                        }
                    }
                    if (info.Arch == CpuArch.Unknown)
                        info.ArchReason = "unsupported inner e_machine 0x" + machine.ToString("X4", CultureInfo.InvariantCulture);
                }
                else
                {
                    info.ArchReason = "inner ELF header not present in the clear";
                }
                info.Describe = desc;
            }
            catch (Exception ex)
            {
                info.Describe = "SCE container (parse failed: " + ex.Message + ")";
                info.ArchReason = "SCE header parse failed: " + ex.Message;
            }
            return info;
        }

        /// <summary>Raw binary heuristics: name/path clues first, then ARM opcode words.</summary>
        public static ImageInfo IdentifyRaw(byte[] data, string filename)
        {
            ImageInfo info = new ImageInfo();
            info.Kind = ImageKind.RawBinary;
            info.SuggestedMemoryTag = "raw";
            info.SuggestedMemorySize = RoundUp(data == null ? 0 : data.Length);
            info.EntryPoint = 0;
            info.LoadAddress = 0;

            string lower = filename == null ? "" : filename.ToLowerInvariant();
            string name = lower;
            int slash = name.LastIndexOfAny(new char[] { '\\', '/' });
            if (slash >= 0) name = name.Substring(slash + 1);

            string reason;
            if (ContainsAny(lower, new string[] { "bootrom", "first_loader", "secure_kernel", "second_loader" }))
            {
                info.Arch = CpuArch.MeP;
                info.ArchReason = "MeP: file name/path matches a MeP internal-core image";
                info.Describe = "raw MeP image (name hint)";
                return info;
            }
            if (name.StartsWith("uss-", StringComparison.Ordinal) ||
                ContainsAny(lower, new string[] { "ernie", "irt-" }))
            {
                info.Arch = CpuArch.Rl78;
                info.ArchReason = "RL78: file name/path matches the ErnIE syscon (USS-*/IRT-*/ernie)";
                info.Describe = "raw RL78 image (name hint)";
                return info;
            }

            string armPattern;
            if (LooksLikeArm(data, out armPattern))
            {
                info.Arch = CpuArch.Arm;
                info.ArchReason = "ARM: found a " + armPattern + " word in the first bytes";
                info.Describe = "raw ARM image (" + armPattern + ")";
                return info;
            }

            info.Arch = CpuArch.Unknown;
            reason = "no name hint and no ARM opcode pattern";
            info.ArchReason = reason;
            info.Describe = "raw binary (arch unknown: " + reason + ")";
            return info;
        }
        /// <summary>
        /// Looks for the ARM words that show up at the start of a real ARM boot
        /// image: an LDR from the literal pool (e.g. 18 F0 9F E5), a B/BL, or a
        /// PUSH {..., lr}.
        /// </summary>
        public static bool LooksLikeArm(byte[] data, out string what)
        {
            what = null;
            if (data == null || data.Length < 4) return false;
            int limit = Math.Min(data.Length & ~3, 0x40);
            for (int o = 0; o < limit; o += 4)
            {
                uint w = BinUtil.U32(data, o);
                if (w == 0xE59FF018u) { what = "18 F0 9F E5 (ldr pc, [pc, #0x18])"; return true; }
                if ((w & 0xFFFFF000u) == 0xE59F0000u) { what = "ldr rX, [pc, #imm] (0x" + w.ToString("X8", CultureInfo.InvariantCulture) + ")"; return true; }
                if ((w & 0x0F000000u) == 0x0A000000u) { what = "b (0x" + w.ToString("X8", CultureInfo.InvariantCulture) + ")"; return true; }
                if ((w & 0x0F000000u) == 0x0B000000u) { what = "bl (0x" + w.ToString("X8", CultureInfo.InvariantCulture) + ")"; return true; }
                if ((w & 0xFFFF0000u) == 0xE92D0000u) { what = "push {...} (0x" + w.ToString("X8", CultureInfo.InvariantCulture) + ")"; return true; }
            }
            return false;
        }

        private static bool ContainsAny(string haystack, string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
                if (haystack.IndexOf(needles[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        private static int RoundUp(int n)
        {
            if (n <= 0) return 0;
            return (int)(((long)n + 0xFFF) & ~0xFFFL);
        }

        // ------------------------------------------------------------- loading

        /// <summary>
        /// Sniff (unless <paramref name="info"/> is given) and load into the hub,
        /// mapping RAM as needed. Returns the entry point, or 0 on failure
        /// (failures are reported through <paramref name="log"/>).
        /// </summary>
        public static uint Load(MemoryHub hub, byte[] data, string filename, SceKeys keys, ILogSink log, ImageInfo info)
        {
            if (hub == null) throw new ArgumentNullException("hub");
            if (data == null || data.Length == 0)
            {
                Log(log, "load: empty file " + filename);
                return 0;
            }
            if (info == null) info = Identify(data, filename);
            if (keys == null) keys = SceKeys.Default();

            switch (info.Kind)
            {
                case ImageKind.Elf32:
                case ImageKind.Elf64:
                    {
                        try
                        {
                            ElfImage img = ElfImage.Parse(data);
                            uint entry = img.LoadInto(hub, data, 0, "elf");
                            for (int i = 0; i < img.Warnings.Count; i++) Log(log, "elf: " + img.Warnings[i]);
                            Log(log, "loaded ELF " + filename + " entry=0x" + entry.ToString("X", CultureInfo.InvariantCulture));
                            return entry;
                        }
                        catch (Exception ex)
                        {
                            Log(log, "ELF load failed for " + filename + ": " + ex.Message);
                            return 0;
                        }
                    }

                case ImageKind.SceSelf:
                    {
                        try
                        {
                            SceSelf s = SceSelf.Parse(data);
                            uint entry = s.LoadInto(hub, data, keys, log, 0);
                            if (entry == 0) Log(log, "SELF load produced no entry point for " + filename);
                            return entry;
                        }
                        catch (Exception ex)
                        {
                            Log(log, "SELF load failed for " + filename + ": " + ex.Message);
                            return 0;
                        }
                    }

                case ImageKind.PupPackage:
                    Log(log, filename + " is a PSP2UPDAT container: extract its segments first (PupPackage.Parse)");
                    return 0;

                default:
                    return LoadRaw(hub, data, info.LoadAddress, "raw", log);
            }
        }

        /// <summary>
        /// Load a raw (headerless) binary at <paramref name="baseAddr"/>.
        /// Returns <paramref name="baseAddr"/> (a raw image has no entry point,
        /// but the load address is the only meaningful "start").
        /// </summary>
        public static uint LoadRaw(MemoryHub hub, byte[] data, uint baseAddr, string tag, ILogSink log)
        {
            if (hub == null) throw new ArgumentNullException("hub");
            if (data == null || data.Length == 0) return 0;
            if (tag == null) tag = "raw";

            int size = RoundUp(data.Length);
            if (hub.FindRangeAt(baseAddr, 1) == null)
            {
                hub.AddMemory(tag, size, baseAddr, "raw binary, " + data.Length + " bytes");
            }
            if (!hub.LoadDump(baseAddr, data))
            {
                Log(log, "raw load: LoadDump(0x" + baseAddr.ToString("X", CultureInfo.InvariantCulture) + ", " + data.Length + ") failed");
                return 0;
            }
            Log(log, "loaded raw binary at 0x" + baseAddr.ToString("X", CultureInfo.InvariantCulture) +
                     " (" + data.Length + " bytes)");
            return baseAddr;
        }

        private static void Log(ILogSink log, string text)
        {
            if (log != null) log.Log("load", text);
        }
    }
}
