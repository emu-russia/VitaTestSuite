// SceSelf.cs -- SCE/SELF container parsing, metadata decryption and the
// extract-to-ELF pipeline.
//
// Struct definitions are a port of pup_fiction/scetypes.py; the pipeline is a
// port of pup_fiction/self2elf.py + sceutils.py:
//
//   SceHeader (32)  -> SelfHeader (88) -> AppInfo (32) -> SceVersionInfo (16)
//   -> ControlInfo / ControlInfoDigest256 / ControlInfoDRM (NPDRM)
//   -> ELF header (52, copied verbatim into the output)
//   -> e_phnum program headers (32 each) + the same number of SegmentInfo (32)
//   -> [if any SegmentInfo.plaintext == NO] metadata decryption:
//        AES-128-CBC(metadata key, iv) over SceHeader.metadata_offset+48
//        (for APP SELFs the first 64 bytes are first run through the NPDRM
//         klicense double CBC: decrypt(klicense) with the NPDRM key/iv, then
//         that 16 byte result becomes the CBC key for the 64 byte block)
//        -> MetadataInfo(key,iv) -> AES-128-CBC(key,iv) over the rest
//        -> MetadataHeader -> MetadataSection[] -> 16 byte key vault
//        every section with encryption==AES128CTR contributes (key, iv)
//   -> for each phdr: AES-128-CTR the segment with that key/iv, then inflate
//      when SegmentInfo.compressed == YES, and lay the result out at p_offset.
//
// Deviations from the python (all of them are "do not crash on real files",
// documented in the report):
//   * unknown platform / SCE type / SELF type values are kept as raw numbers
//     instead of raising ValueError from the python Enum.
//   * a segment that is encrypted but whose metadata section is missing/not
//     AES128CTR is reported as an explicit error instead of a python KeyError.
//   * a negative p_offset pad is clamped and logged instead of raising
//     RuntimeError("ELF p_offset invalid!").
//   * a repeated segment index is written once (python writes it twice, which
//     corrupts the reconstructed ELF).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace VitaTestSuite.Core
{
    /// <summary>scetypes.py SceHeader (32 bytes, little endian).</summary>
    public class SceHeaderInfo
    {
        public uint Magic;          // 0x00454353 "SCE\0"
        public uint Version;        // 3
        public byte Platform;       // SelfPlatform: 0x40 VITA, 0xC0 in devkit files
        public byte KeyRevision;
        public ushort SceType;      // SceType: 1 SELF, 2 SRVK, 3 SPKG
        public uint MetadataOffset;
        public ulong HeaderLength;
        public ulong DataLength;

        public string PlatformName { get { return ScePlatform.Name(Platform); } }
        public string SceTypeName { get { return SceContainerType.Name(SceType); } }
    }

    /// <summary>scetypes.py SelfHeader (88 bytes, directly after the SceHeader).</summary>
    public class SelfHeaderInfo
    {
        public ulong FileLength;
        /// <summary>Python's SelfHeader.field_8 (unknown).</summary>
        public ulong Field8;
        public ulong SelfOffset;
        public ulong AppInfoOffset;
        public ulong ElfOffset;
        public ulong PhdrOffset;
        public ulong ShdrOffset;
        public ulong SegmentInfoOffset;
        public ulong SceVersionOffset;
        public ulong ControlInfoOffset;
        public ulong ControlInfoLength;
    }

    /// <summary>scetypes.py AppInfoHeader (32 bytes).</summary>
    public class SceAppInfo
    {
        public ulong AuthId;
        public uint VendorId;
        public uint SelfType;       // SelfType
        public ulong SysVersion;
        public ulong Field18;

        public string SelfTypeName { get { return SceSelfType.Name((int)SelfType); } }
    }

    /// <summary>scetypes.py SegmentInfo (32 bytes). Compressed/Plaintext hold the
    /// raw SecureBool values (0 UNUSED, 1 NO, 2 YES).</summary>
    public class SceSegmentInfo
    {
        public ulong Offset;
        public ulong Size;
        public int Compressed;
        public int Plaintext;
        public uint Field14;
        public uint Field1C;

        public bool IsCompressed { get { return Compressed == 2; } }   // SecureBool.YES
        public bool IsPlaintext { get { return Plaintext != 1; } }     // != SecureBool.NO

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "offset=0x{0:X} size=0x{1:X} compressed={2} plaintext={3}",
                Offset, Size, SecureBoolName(Compressed), SecureBoolName(Plaintext));
        }

        public static string SecureBoolName(int v)
        {
            switch (v)
            {
                case 0: return "UNUSED";
                case 1: return "NO";
                case 2: return "YES";
                default: return "0x" + v.ToString("X", CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>scetypes.py MetadataSection (48 bytes).</summary>
    public class SceMetadataSection
    {
        /// <summary>Position of this section inside the metadata section table.</summary>
        public int Index;
        public ulong Offset;
        public ulong Size;
        public int Type;
        /// <summary>Which segment (SegmentInfo/program header index) this section describes.</summary>
        public int SegIdx;
        public int HashType;
        public int HashIdx;
        public int Encryption;
        public int KeyIdx;
        public int IvIdx;
        public int Compression;

        public bool IsAesCtr { get { return Encryption == 3; } }   // EncryptionType.AES128CTR
        public bool IsDeflate { get { return Compression == 2; } } // CompressionType.DEFLATE

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "section[{0}] seg={1} offset=0x{2:X} size=0x{3:X} type=0x{4:X} hash={5}/{6} enc={7} key={8} iv={9} comp={10}",
                Index, SegIdx, Offset, Size, Type, HashType, HashIdx, Encryption, KeyIdx, IvIdx, Compression);
        }
    }

    /// <summary>Internal: one decrypted metadata section plus its CTR key/iv.</summary>
    internal class SceCryptSegment
    {
        public int SectionIndex;
        public ulong Offset;
        public int SegIdx;
        public ulong Size;
        public bool Compressed;
        public bool Encrypted;
        public byte[] Key;
        public byte[] Iv;
    }

    /// <summary>
    /// A SCE container. Only SELF (type 1) can be turned back into an ELF by
    /// <see cref="ExtractElf"/>; SRVK/SPKG are metadata-encrypted blobs that
    /// pup_fiction handles with scedecrypt.py instead.
    /// </summary>
    public class SceSelf
    {
        public const uint SCE_MAGIC = 0x00454353;

        public SceHeaderInfo Sce;
        public SelfHeaderInfo Self;
        public SceAppInfo AppInfo;
        public List<SceSegmentInfo> SegmentInfos = new List<SceSegmentInfo>();
        public List<SceMetadataSection> Sections = new List<SceMetadataSection>();

        // ---- extra, discoverable detail filled in by Parse()/ExtractElf() ----
        public uint SceVersionSubtype;
        public uint SceVersionIsPresent;
        public ulong SceVersionSize;
        public int ControlInfoType = -1;
        public uint ControlInfoSize;
        public int NpdrmType = -1;
        public byte[] ContentId;
        public uint MetadataSignatureType;
        public int MetadataSectionCount;
        public int MetadataKeyCount;
        public List<byte[]> MetadataKeyVault = new List<byte[]>();
        public List<string> Warnings = new List<string>();
        /// <summary>Decompression mode that was actually used ("zlib (2 byte header stripped)" ...).</summary>
        public string LastInflateMode = "";
        /// <summary>Which key table entry verified the metadata decryption ("pup_fiction/keys.py:161 keyrev=1 ...").</summary>
        public string MetadataKeyUsed = "";

        // ------------------------------------------------------------ sniffing

        public static bool IsSce(byte[] data)
        {
            return data != null && data.Length >= 4 &&
                   data[0] == 0x53 && data[1] == 0x43 && data[2] == 0x45 && data[3] == 0x00;
        }

        // ------------------------------------------------------------- parsing

        public static SceSelf Parse(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length < 32) throw new InvalidDataException("file too small for a SCE header (" + data.Length + " bytes)");
            if (!IsSce(data))
                throw new InvalidDataException("bad SCE magic 0x" + BinUtil.U32(data, 0).ToString("X8", CultureInfo.InvariantCulture));

            SceSelf s = new SceSelf();
            s.Sce = new SceHeaderInfo();
            s.Sce.Magic = BinUtil.U32(data, 0);
            s.Sce.Version = BinUtil.U32(data, 4);
            s.Sce.Platform = BinUtil.U8(data, 8);
            s.Sce.KeyRevision = BinUtil.U8(data, 9);
            s.Sce.SceType = BinUtil.U16(data, 10);
            s.Sce.MetadataOffset = BinUtil.U32(data, 12);
            s.Sce.HeaderLength = BinUtil.U64(data, 16);
            s.Sce.DataLength = BinUtil.U64(data, 24);

            if (s.Sce.Version != 3)
                s.Warnings.Add("unknown SCE header version " + s.Sce.Version + " (expected 3)");

            if (BinUtil.InRange(data, 32, 88))
            {
                s.Self = new SelfHeaderInfo();
                s.Self.FileLength = BinUtil.U64(data, 32);
                s.Self.Field8 = BinUtil.U64(data, 40);
                s.Self.SelfOffset = BinUtil.U64(data, 48);
                s.Self.AppInfoOffset = BinUtil.U64(data, 56);
                s.Self.ElfOffset = BinUtil.U64(data, 64);
                s.Self.PhdrOffset = BinUtil.U64(data, 72);
                s.Self.ShdrOffset = BinUtil.U64(data, 80);
                s.Self.SegmentInfoOffset = BinUtil.U64(data, 88);
                s.Self.SceVersionOffset = BinUtil.U64(data, 96);
                s.Self.ControlInfoOffset = BinUtil.U64(data, 104);
                s.Self.ControlInfoLength = BinUtil.U64(data, 112);

                if (BinUtil.InRange(data, (long)s.Self.AppInfoOffset, 32))
                {
                    s.AppInfo = new SceAppInfo();
                    long a = (long)s.Self.AppInfoOffset;
                    s.AppInfo.AuthId = BinUtil.U64(data, a + 0);
                    s.AppInfo.VendorId = BinUtil.U32(data, a + 8);
                    s.AppInfo.SelfType = BinUtil.U32(data, a + 12);
                    s.AppInfo.SysVersion = BinUtil.U64(data, a + 16);
                    s.AppInfo.Field18 = BinUtil.U64(data, a + 24);
                }
                else s.Warnings.Add("app info offset 0x" + s.Self.AppInfoOffset.ToString("X", CultureInfo.InvariantCulture) + " is outside the file");

                if (BinUtil.InRange(data, (long)s.Self.SceVersionOffset, 16))
                {
                    long v = (long)s.Self.SceVersionOffset;
                    s.SceVersionSubtype = BinUtil.U32(data, v + 0);
                    s.SceVersionIsPresent = BinUtil.U32(data, v + 4);
                    s.SceVersionSize = BinUtil.U64(data, v + 8);
                }

                s.ParseControlInfo(data);

                // program headers + segment infos
                s.ParseElfHeaderAndSegments(data);
            }
            else s.Warnings.Add("file too small for a SELF header");

            return s;
        }

        private void ParseElfHeaderAndSegments(byte[] data)
        {
            if (Self.ElfOffset == 0 || !BinUtil.InRange(data, (long)Self.ElfOffset, 52))
            {
                Warnings.Add("inner ELF header at 0x" + Self.ElfOffset.ToString("X", CultureInfo.InvariantCulture) + " is outside the file");
                return;
            }

            long e = (long)Self.ElfOffset;
            if (!(data[e] == 0x7F && data[e + 1] == 0x45 && data[e + 2] == 0x4C && data[e + 3] == 0x46))
            {
                Warnings.Add("no ELF magic at the inner ELF offset (encrypted or unusual container)");
                return;
            }

            ushort phEntSize = BinUtil.U16(data, e + 0x2A);
            ushort phNum = BinUtil.U16(data, e + 0x2C);
            if (phEntSize == 0) phEntSize = 32;

            for (int i = 0; i < phNum; i++)
            {
                long pi = (long)Self.PhdrOffset + (long)i * phEntSize;
                long si = (long)Self.SegmentInfoOffset + (long)i * 32;

                SceSegmentInfo info = new SceSegmentInfo();
                if (BinUtil.InRange(data, si, 32))
                {
                    info.Offset = BinUtil.U64(data, si + 0);
                    info.Size = BinUtil.U64(data, si + 8);
                    info.Compressed = (int)BinUtil.U32(data, si + 16);
                    info.Field14 = BinUtil.U32(data, si + 20);
                    info.Plaintext = (int)BinUtil.U32(data, si + 24);
                    info.Field1C = BinUtil.U32(data, si + 28);
                }
                else Warnings.Add("segment info " + i + " at 0x" + si.ToString("X", CultureInfo.InvariantCulture) + " is outside the file");
                SegmentInfos.Add(info);

                if (!BinUtil.InRange(data, pi, 32))
                    Warnings.Add("program header " + i + " at 0x" + pi.ToString("X", CultureInfo.InvariantCulture) + " is outside the file");
            }
        }

        private void ParseControlInfo(byte[] data)
        {
            if (Self.ControlInfoOffset == 0 || Self.ControlInfoLength < 16) return;
            long baseOff = (long)Self.ControlInfoOffset;
            long off = 0;

            while (off + 16 <= (long)Self.ControlInfoLength)
            {
                long h = baseOff + off;
                if (!BinUtil.InRange(data, h, 16)) break;

                uint type = BinUtil.U32(data, h + 0);
                uint size = BinUtil.U32(data, h + 4);
                if (ControlInfoType < 0) { ControlInfoType = (int)type; ControlInfoSize = size; }

                if (type == 5 && size >= 16 + 0x100 && BinUtil.InRange(data, h + 16, 0x100))
                {
                    // SceControlInfoDRM: npdrm_type at +0x0C of the body
                    NpdrmType = (int)BinUtil.U32(data, h + 16 + 0x0C);
                    ContentId = new byte[0x30];
                    Array.Copy(data, h + 16 + 0x10, ContentId, 0, 0x30);
                }

                if (size < 16 || off + size > (long)Self.ControlInfoLength)
                {
                    // Fall back to the self2elf walk: header, optional 64 byte
                    // digest, header, optional 0x100 byte NPDRM body.
                    if (type == 4) off += 16 + 64;
                    else if (type == 5) off += 16 + 0x100;
                    else off += 16;
                    continue;
                }
                off += size;
            }
        }

        // ----------------------------------------------------------- describe

        public string Describe()
        {
            StringBuilder sb = new StringBuilder();
            if (Sce != null)
            {
                sb.Append("SCE: magic=0x").Append(Sce.Magic.ToString("X8", CultureInfo.InvariantCulture));
                sb.Append(" ver=").Append(Sce.Version);
                sb.Append(" platform=0x").Append(Sce.Platform.ToString("X2", CultureInfo.InvariantCulture))
                  .Append(" (").Append(Sce.PlatformName).Append(')');
                sb.Append(" keyrev=").Append(Sce.KeyRevision);
                sb.Append(" type=").Append(Sce.SceType).Append(" (").Append(Sce.SceTypeName).Append(')');
                sb.Append(" metadata_offset=0x").Append(Sce.MetadataOffset.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" header_len=0x").Append(Sce.HeaderLength.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" data_len=0x").Append(Sce.DataLength.ToString("X", CultureInfo.InvariantCulture));
                sb.Append('\n');
            }
            if (Self != null)
            {
                sb.Append("SELF: file_len=0x").Append(Self.FileLength.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" self_offset=0x").Append(Self.SelfOffset.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" appinfo=0x").Append(Self.AppInfoOffset.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" elf=0x").Append(Self.ElfOffset.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" phdr=0x").Append(Self.PhdrOffset.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" shdr=0x").Append(Self.ShdrOffset.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" seginfo=0x").Append(Self.SegmentInfoOffset.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" sceversion=0x").Append(Self.SceVersionOffset.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" controlinfo=0x").Append(Self.ControlInfoOffset.ToString("X", CultureInfo.InvariantCulture));
                sb.Append("/0x").Append(Self.ControlInfoLength.ToString("X", CultureInfo.InvariantCulture));
                sb.Append('\n');
            }
            if (AppInfo != null)
            {
                sb.Append("APP: auth_id=0x").Append(AppInfo.AuthId.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" vendor_id=0x").Append(AppInfo.VendorId.ToString("X", CultureInfo.InvariantCulture));
                sb.Append(" self_type=0x").Append(AppInfo.SelfType.ToString("X", CultureInfo.InvariantCulture))
                  .Append(" (").Append(AppInfo.SelfTypeName).Append(')');
                sb.Append(" sys_version=0x").Append(AppInfo.SysVersion.ToString("X", CultureInfo.InvariantCulture));
                sb.Append('\n');
            }
            sb.Append("SCE version info: subtype=").Append(SceVersionSubtype)
              .Append(" present=").Append(SceVersionIsPresent)
              .Append(" size=0x").Append(SceVersionSize.ToString("X", CultureInfo.InvariantCulture));
            if (ControlInfoType >= 0)
                sb.Append("  control_info type=").Append(ControlInfoType)
                  .Append(" size=0x").Append(ControlInfoSize.ToString("X", CultureInfo.InvariantCulture));
            if (NpdrmType >= 0) sb.Append(" npdrm_type=").Append(NpdrmType);
            sb.Append('\n');

            for (int i = 0; i < SegmentInfos.Count; i++)
                sb.Append("  seg[").Append(i).Append("] ").Append(SegmentInfos[i]).Append('\n');

            for (int i = 0; i < Sections.Count; i++)
                sb.Append("  ").Append(Sections[i]).Append('\n');

            for (int i = 0; i < Warnings.Count; i++)
                sb.Append("  ! ").Append(Warnings[i]).Append('\n');

            return sb.ToString().TrimEnd('\n');
        }

        // ---------------------------------------------------------- extraction

        public byte[] ExtractElf(byte[] data, SceKeys keys, ILogSink log)
        {
            return ExtractElf(data, keys, log, null);
        }

        /// <summary>
        /// Full decrypt+decompress pipeline. Returns the reconstructed ELF image
        /// or null (with the exact reason logged through <paramref name="log"/>).
        /// <paramref name="klicense"/> is the 16 byte klicensee from a RIF file,
        /// needed only for SELF type APP.
        /// </summary>
        public byte[] ExtractElf(byte[] data, SceKeys keys, ILogSink log, byte[] klicense)
        {
            if (data == null) { Log(log, "extract: null data"); return null; }
            if (keys == null) keys = SceKeys.Default();

            SceSelf s = this;
            if (s.Sce == null || s.Self == null || s.SegmentInfos.Count == 0)
            {
                try { s = Parse(data); }
                catch (Exception ex) { Log(log, "parse failed: " + ex.Message); return null; }
            }

            SceHeaderInfo sce = s.Sce;
            SelfHeaderInfo sh = s.Self;
            if (sce.SceType != SceContainerType.Self)
            {
                Log(log, "SCE type " + sce.SceType + " (" + sce.SceTypeName + ") is not SELF; ExtractElf only handles SELF (SRVK/SPKG use the scedecrypt path)");
                return null;
            }

            long sysVer = s.AppInfo != null ? (long)s.AppInfo.SysVersion : -1;
            int selfType = s.AppInfo != null ? (int)s.AppInfo.SelfType : SceSelfType.None;

            // ---- inner ELF header (copied verbatim into the output) ----
            if (!BinUtil.InRange(data, (long)sh.ElfOffset, 52))
            {
                Log(log, "inner ELF header at 0x" + sh.ElfOffset.ToString("X", CultureInfo.InvariantCulture) + " is outside the file");
                return null;
            }
            byte[] ehdr = new byte[52];
            Array.Copy(data, (long)sh.ElfOffset, ehdr, 0, 52);
            if (!(ehdr[0] == 0x7F && ehdr[1] == 0x45 && ehdr[2] == 0x4C && ehdr[3] == 0x46))
            {
                Log(log, "no ELF magic at the inner ELF offset 0x" + sh.ElfOffset.ToString("X", CultureInfo.InvariantCulture));
                return null;
            }
            ushort phNum = BinUtil.U16(ehdr, 0x2C);
            ushort phEntSize = BinUtil.U16(ehdr, 0x2A);
            if (phEntSize == 0) phEntSize = 32;

            // ---- program headers ----
            List<byte[]> phdrs = new List<byte[]>();
            for (int i = 0; i < phNum; i++)
            {
                long pi = (long)sh.PhdrOffset + (long)i * phEntSize;
                if (!BinUtil.InRange(data, pi, 32))
                {
                    Log(log, "program header " + i + " at 0x" + pi.ToString("X", CultureInfo.InvariantCulture) + " is outside the file");
                    return null;
                }
                byte[] p = new byte[32];
                Array.Copy(data, pi, p, 0, 32);
                phdrs.Add(p);
            }

            bool anyEncrypted = false;
            for (int i = 0; i < s.SegmentInfos.Count; i++)
                if (!s.SegmentInfos[i].IsPlaintext) anyEncrypted = true;

            // ---- metadata ----
            Dictionary<int, SceCryptSegment> crypt = null;
            if (anyEncrypted)
            {
                string reason;
                crypt = s.DecryptMetadata(data, keys, klicense, log, out reason);
                if (crypt == null) { Log(log, "metadata decryption failed: " + reason); return null; }
            }

            // ---- rebuild the ELF ----
            MemoryStream outStream = new MemoryStream();
            outStream.Write(ehdr, 0, 52);
            long at = 52;
            for (int i = 0; i < phdrs.Count; i++)
            {
                outStream.Write(phdrs[i], 0, 32);
                at += 32;
            }

            HashSet<int> alreadyWritten = new HashSet<int>();
            for (int i = 0; i < phdrs.Count; i++)
            {
                int idx = i;
                SceCryptSegment cs = null;
                if (crypt != null)
                {
                    if (!crypt.TryGetValue(i, out cs))
                    {
                        Log(log, "no metadata section for phdr " + i + "; assuming segment index " + i);
                    }
                    else idx = cs.SegIdx;
                }

                if (idx < 0 || idx >= phdrs.Count)
                {
                    Log(log, "segment index " + idx + " (from metadata section " + i + ") is out of range 0.." + (phdrs.Count - 1));
                    return null;
                }

                uint pOff = BinUtil.U32(phdrs[idx], 0x04);
                uint pFilesz = BinUtil.U32(phdrs[idx], 0x10);
                if (pFilesz == 0) continue;

                if (!alreadyWritten.Add(idx))
                {
                    Log(log, "segment " + idx + " is referenced twice; writing it once");
                    continue;
                }

                long padLen = (long)pOff - at;
                if (padLen < 0)
                {
                    Log(log, "p_offset 0x" + pOff.ToString("X", CultureInfo.InvariantCulture) +
                            " of segment " + idx + " is behind the current output offset 0x" +
                            at.ToString("X", CultureInfo.InvariantCulture) + "; padding skipped");
                    padLen = 0;
                }
                for (long z = 0; z < padLen; z++) outStream.WriteByte(0);
                at += padLen;

                ulong segOff = idx < s.SegmentInfos.Count ? s.SegmentInfos[idx].Offset : 0;
                ulong segSize = idx < s.SegmentInfos.Count ? s.SegmentInfos[idx].Size : 0;
                if (!BinUtil.InRange(data, (long)segOff, (int)segSize))
                {
                    Log(log, "segment " + idx + " data 0x" + segOff.ToString("X", CultureInfo.InvariantCulture) +
                            "+0x" + segSize.ToString("X", CultureInfo.InvariantCulture) + " is outside the file");
                    return null;
                }

                byte[] seg = new byte[(int)segSize];
                Array.Copy(data, (long)segOff, seg, 0, (int)segSize);

                bool encrypted = !s.SegmentInfos[idx].IsPlaintext;
                if (encrypted)
                {
                    if (cs == null || cs.Key == null || cs.Iv == null)
                    {
                        Log(log, "segment " + idx + " is encrypted but no AES128CTR key/iv was found in the metadata");
                        return null;
                    }
                    seg = CoreAes.CtrCrypt(cs.Key, cs.Iv, seg);
                }

                if (s.SegmentInfos[idx].IsCompressed)
                {
                    try
                    {
                        string mode;
                        seg = RawDeflate.Inflate(seg, 0, seg.Length, out mode);
                        s.LastInflateMode = mode;
                        Log(log, "segment " + idx + " inflated (" + mode + ") 0x" +
                                segSize.ToString("X", CultureInfo.InvariantCulture) + " -> 0x" +
                                seg.Length.ToString("X", CultureInfo.InvariantCulture));
                    }
                    catch (Exception ex)
                    {
                        Log(log, "segment " + idx + " decompression failed: " + ex.Message);
                        return null;
                    }
                }

                outStream.Write(seg, 0, seg.Length);
                at += seg.Length;
                Log(log, "segment " + idx + " written at 0x" + (at - seg.Length).ToString("X", CultureInfo.InvariantCulture) +
                        " (0x" + seg.Length.ToString("X", CultureInfo.InvariantCulture) + " bytes)");
            }

            byte[] result = outStream.ToArray();
            if (!ElfImage.IsElf(result))
            {
                Log(log, "reconstructed image is not an ELF");
                return null;
            }
            Log(log, "extracted ELF: 0x" + result.Length.ToString("X", CultureInfo.InvariantCulture) + " bytes");
            return result;
        }

        /// <summary>Convenience: parse + ExtractElf + ElfImage.Parse + LoadInto.</summary>
        public uint LoadInto(MemoryHub hub, byte[] data, SceKeys keys, ILogSink log, uint bias)
        {
            byte[] elf = ExtractElf(data, keys, log);
            if (elf == null) return 0;

            ElfImage img;
            try { img = ElfImage.Parse(elf); }
            catch (Exception ex) { Log(log, "extracted ELF does not parse: " + ex.Message); return 0; }

            return img.LoadInto(hub, elf, bias, "self");
        }

        // ------------------------------------------------- metadata decryption

        /// <summary>Port of sceutils.get_segments(). Fills <see cref="Sections"/>
        /// and <see cref="MetadataKeyVault"/> as a side effect.</summary>
        internal Dictionary<int, SceCryptSegment> DecryptMetadata(byte[] data, SceKeys keys, byte[] klicense,
                                                                 ILogSink log, out string reason)
        {
            reason = null;
            SceHeaderInfo sce = Sce;
            if (sce.HeaderLength <= sce.MetadataOffset + 48)
            {
                reason = "metadata region is empty (header_length 0x" + sce.HeaderLength.ToString("X", CultureInfo.InvariantCulture) +
                         " <= metadata_offset 0x" + sce.MetadataOffset.ToString("X", CultureInfo.InvariantCulture) + " + 48)";
                return null;
            }

            long sysVer = AppInfo != null ? (long)AppInfo.SysVersion : -1;
            int selfType = AppInfo != null ? (int)AppInfo.SelfType : SceSelfType.None;

            long datOff = (long)sce.MetadataOffset + 48;
            int datLen = (int)(sce.HeaderLength - sce.MetadataOffset - 48);
            if (!BinUtil.InRange(data, datOff, datLen))
            {
                reason = "metadata blob 0x" + datOff.ToString("X", CultureInfo.InvariantCulture) + "+0x" +
                         datLen.ToString("X", CultureInfo.InvariantCulture) + " is outside the file";
                return null;
            }
            if (datLen < 64)
            {
                reason = "metadata blob is only " + datLen + " bytes";
                return null;
            }

            // ---- find the metadata key -------------------------------------------------
            // The three pup_fiction key tables are *alternatives* that are merged
            // into one store here, so several keys can match the same (sce type,
            // key revision, self type) triple. The decrypted MetadataInfo is
            // self checking (32 of its 64 bytes must be zero), so every candidate
            // is tried until one verifies; the winner is reported.
            List<SceKeyEntry> metaCands = keys.GetCandidates(SceKeyType.Metadata, sce.SceType, sysVer, sce.KeyRevision, selfType, sysVer < 0);
            List<SceKeyEntry> metaFallback = sysVer >= 0
                ? keys.GetCandidates(SceKeyType.Metadata, sce.SceType, sysVer, sce.KeyRevision, selfType, true)
                : new List<SceKeyEntry>();

            int npKeyIndex = sce.KeyRevision >= 2 ? 1 : 0;
            List<SceKeyEntry> npCands = null;
            if (selfType == SceSelfType.App)
            {
                npCands = keys.GetCandidates(SceKeyType.Npdrm, sce.SceType, sysVer, npKeyIndex, selfType, sysVer < 0);
                List<SceKeyEntry> npFallback = sysVer >= 0
                    ? keys.GetCandidates(SceKeyType.Npdrm, sce.SceType, sysVer, npKeyIndex, selfType, true)
                    : new List<SceKeyEntry>();
                for (int i = 0; i < npFallback.Count; i++) npCands.Add(npFallback[i]);

                if (npCands.Count == 0)
                {
                    reason = "no NPDRM key for sce_type=" + sce.SceType + " key_index=" + npKeyIndex + " self_type=APP";
                    return null;
                }
                if (klicense == null || klicense.Length != 16)
                {
                    reason = "SELF type APP is NPDRM encrypted: a 16 byte klicensee (from the RIF) is required";
                    return null;
                }
            }

            if (metaCands.Count == 0 && metaFallback.Count == 0)
            {
                reason = "no metadata key for sce_type=" + sce.SceType + " sys_version=0x" +
                         (sysVer < 0 ? "any" : sysVer.ToString("X", CultureInfo.InvariantCulture)) +
                         " key_revision=" + sce.KeyRevision + " self_type=0x" + selfType.ToString("X", CultureInfo.InvariantCulture) +
                         " (" + SceSelfType.Name(selfType) + ")";
                return null;
            }

            byte[] plainBlock = new byte[64];
            Array.Copy(data, datOff, plainBlock, 0, 64);

            byte[] dec = null;
            SceKeyEntry usedKey = null;
            bool usedWildcard = false;
            string lastFail = "";

            for (int pass = 0; pass < 2 && dec == null; pass++)
            {
                List<SceKeyEntry> cands = pass == 0 ? metaCands : metaFallback;
                for (int i = 0; i < cands.Count && dec == null; i++)
                {
                    SceKeyEntry meta = cands[i];
                    if (npCands == null || npCands.Count == 0)
                    {
                        byte[] tryDec = CoreAes.CbcDecrypt(meta.Key, meta.Iv, plainBlock);
                        if (AllZero(tryDec, 16, 16) && AllZero(tryDec, 48, 16))
                        {
                            dec = tryDec;
                            usedKey = meta;
                            usedWildcard = pass == 1;
                        }
                        else lastFail = meta.Source + ":" + meta.SourceLine;
                    }
                    else
                    {
                        for (int j = 0; j < npCands.Count && dec == null; j++)
                        {
                            SceKeyEntry np = npCands[j];
                            // python: predec = CBC_dec(np_key, np_iv, klictxt); then
                            //         CBC_dec(predec, np_iv, metadata[0:64])
                            byte[] pre = CoreAes.CbcDecrypt(np.Key, np.Iv, klicense);
                            byte[] npKey2 = new byte[16];
                            Array.Copy(pre, 0, npKey2, 0, 16);
                            byte[] npDec = CoreAes.CbcDecrypt(npKey2, np.Iv, plainBlock);
                            byte[] tryDec = CoreAes.CbcDecrypt(meta.Key, meta.Iv, npDec);
                            if (AllZero(tryDec, 16, 16) && AllZero(tryDec, 48, 16))
                            {
                                dec = tryDec;
                                usedKey = meta;
                                usedWildcard = pass == 1;
                            }
                            else lastFail = meta.Source + ":" + meta.SourceLine + " + " + np.Source + ":" + np.SourceLine;
                        }
                    }
                }
            }

            if (dec == null)
            {
                reason = "metadata key verification failed for " + (metaCands.Count + metaFallback.Count) + " candidate key(s)" +
                         (npCands != null ? " x " + npCands.Count + " NPDRM key(s)" : "") +
                         " (keyrev=" + sce.KeyRevision + " self_type=0x" + selfType.ToString("X", CultureInfo.InvariantCulture) +
                         " " + SceSelfType.Name(selfType) + "): the decrypted MetadataInfo padding is not zero" +
                         (lastFail.Length > 0 ? "; last tried " + lastFail : "");
                return null;
            }

            MetadataKeyUsed = usedKey.Source + ":" + usedKey.SourceLine + " keyrev=" + usedKey.KeyRev +
                              " minver=0x" + usedKey.MinVer.ToString("X", CultureInfo.InvariantCulture) +
                              " maxver=0x" + usedKey.MaxVer.ToString("X", CultureInfo.InvariantCulture);
            Log(log, "metadata key: " + MetadataKeyUsed +
                     (usedWildcard ? " [matched with ignore_sysver fallback]" : ""));

            byte[] mKey2 = new byte[16];
            byte[] mIv2 = new byte[16];
            Array.Copy(dec, 0, mKey2, 0, 16);
            Array.Copy(dec, 32, mIv2, 0, 16);

            int bodyLen = datLen - 64;
            byte[] contents = CoreAes.CbcDecrypt(mKey2, mIv2, data, (int)datOff + 64, bodyLen);
            if (contents.Length < 32)
            {
                reason = "decrypted metadata is too small";
                return null;
            }

            MetadataSignatureType = BinUtil.U32(contents, 8);
            int sectionCount = (int)BinUtil.U32(contents, 12);
            int keyCount = (int)BinUtil.U32(contents, 16);
            MetadataSectionCount = sectionCount;
            MetadataKeyCount = keyCount;

            if (sectionCount < 0 || 32L + (long)sectionCount * 48 > contents.Length)
            {
                reason = "metadata section table (" + sectionCount + " x 48) does not fit in 0x" +
                         contents.Length.ToString("X", CultureInfo.InvariantCulture) + " bytes";
                return null;
            }

            long vaultStart = 32 + (long)sectionCount * 48;
            if (keyCount < 0 || vaultStart + (long)keyCount * 16 > contents.Length)
            {
                reason = "metadata key vault (" + keyCount + " keys) does not fit";
                return null;
            }

            Sections.Clear();
            MetadataKeyVault.Clear();
            for (int x = 0; x < keyCount; x++)
            {
                byte[] k = new byte[16];
                Array.Copy(contents, vaultStart + x * 16, k, 0, 16);
                MetadataKeyVault.Add(k);
            }

            Dictionary<int, SceCryptSegment> segs = new Dictionary<int, SceCryptSegment>();
            for (int i = 0; i < sectionCount; i++)
            {
                long so = 32 + (long)i * 48;
                SceMetadataSection ms = new SceMetadataSection();
                ms.Index = i;
                ms.Offset = BinUtil.U64(contents, so + 0);
                ms.Size = BinUtil.U64(contents, so + 8);
                ms.Type = (int)BinUtil.U32(contents, so + 16);
                ms.SegIdx = (int)BinUtil.U32(contents, so + 20);
                ms.HashType = (int)BinUtil.U32(contents, so + 24);
                ms.HashIdx = (int)BinUtil.U32(contents, so + 28);
                ms.Encryption = (int)BinUtil.U32(contents, so + 32);
                ms.KeyIdx = (int)BinUtil.U32(contents, so + 36);
                ms.IvIdx = (int)BinUtil.U32(contents, so + 40);
                ms.Compression = (int)BinUtil.U32(contents, so + 44);
                Sections.Add(ms);

                SceCryptSegment cs = new SceCryptSegment();
                cs.SectionIndex = i;
                cs.Offset = ms.Offset;
                cs.SegIdx = ms.SegIdx;
                cs.Size = ms.Size;
                cs.Compressed = ms.IsDeflate;
                cs.Encrypted = ms.IsAesCtr;
                if (ms.IsAesCtr)
                {
                    if (ms.KeyIdx < 0 || ms.KeyIdx >= MetadataKeyVault.Count || ms.IvIdx < 0 || ms.IvIdx >= MetadataKeyVault.Count)
                    {
                        reason = "metadata section " + i + " references key/iv index " + ms.KeyIdx + "/" + ms.IvIdx +
                                 " but the vault has " + MetadataKeyVault.Count + " entries";
                        return null;
                    }
                    cs.Key = MetadataKeyVault[ms.KeyIdx];
                    cs.Iv = MetadataKeyVault[ms.IvIdx];
                }
                segs[i] = cs;
                Log(log, "metadata " + ms);
            }

            return segs;
        }

        private static bool AllZero(byte[] b, int off, int len)
        {
            for (int i = 0; i < len; i++) if (b[off + i] != 0) return false;
            return true;
        }

        private static void Log(ILogSink log, string text)
        {
            if (log != null) log.Log("self", text);
        }
    }
}
