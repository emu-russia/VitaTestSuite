// PupPackage.cs -- PSP2UPDAT ("SCEUF") firmware update package parsing.
//
// Port of pup_fiction/pup_fiction.py:
//
//   SCEUF_HEADER_SIZE = 0x80
//   SCEUF_FILEREC_SIZE = 0x20
//
//   header[0:5] == "SCEUF"
//   u32(header, 0x08) = PUP version
//   u32(header, 0x10) = firmware version
//   u32(header, 0x14) = build number
//   u32(header, 0x18) = number of files
//   for x in range(cnt):
//       filetype, offset, length, flags = unpack("<QQQQ", filerec at 0x80 + x*0x20)
//
//   the file name comes from the pup_types table, otherwise from
//   make_filename(): if the payload starts with a SCE header
//   (magic 0x454353, version 3, flags 0x30040 == platform 0x40 | keyrev 0 |
//   type 3 (SPKG)) then u8(sce_header + header_length + 4) indexes FSTYPE and
//   the name becomes "<fstype>-<nn>.pkg".
//
// Parse(byte[]) keeps the whole package in memory (fine for the small TOC
// samples); ParseFile(string) only reads the header, the TOC and the 0x1000
// byte SCE headers needed for naming, so a 148 MB PUP costs ~64 KB of RAM.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace VitaTestSuite.Core
{
    /// <summary>One entry of the PUP table of contents.</summary>
    public class PupSegment
    {
        /// <summary>Resolved file name (pup_types table, FSTYPE derived or "unknown-0xNN.pkg").</summary>
        public string Name;
        /// <summary>Low 32 bits of the record's flags field.</summary>
        public uint Flags;
        /// <summary>Low 32 bits of the record's offset field (PUP files are &lt; 4 GB).</summary>
        public uint Offset;
        /// <summary>Segment length in bytes.</summary>
        public ulong Size;

        // ---- extra detail (kept 64 bit so nothing is lost) ----
        /// <summary>Record file type (e.g. 0x200 = psp2swu.self).</summary>
        public ulong FileType;
        /// <summary>Full 64 bit record offset.</summary>
        public ulong Offset64;
        /// <summary>Full 64 bit record flags.</summary>
        public ulong Flags64;
        /// <summary>True when the segment lies inside the file that was parsed.</summary>
        public bool InFile;

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0,-24} type=0x{1:X} offset=0x{2:X} size=0x{3:X} ({3} bytes) flags=0x{4:X}{5}",
                Name, FileType, Offset64, Size, Flags64, InFile ? "" : " [outside file]");
        }
    }

    /// <summary>A PSP2UPDAT package: header plus the table of contents.</summary>
    public class PupPackage
    {
        public const int HeaderSize = 0x80;
        public const int FileRecSize = 0x20;

        public uint Version;
        public uint FirmwareVersion;
        public uint BuildNumber;
        public uint FileCount;
        public List<PupSegment> Segments = new List<PupSegment>();
        public List<string> Warnings = new List<string>();
        /// <summary>Total size of the parsed file (0 when only the TOC was read from a stream).</summary>
        public long FileSize;

        /// <summary>PUP types (pup_fiction.pup_types).</summary>
        public static readonly Dictionary<ulong, string> PupTypes = BuildPupTypes();

        /// <summary>File system / partition names (pup_fiction.FSTYPE).</summary>
        public static readonly string[] FsType = new string[]
        {
            "unknown0", "os0", "unknown2", "unknown3", "vs0_chmod", "unknown5", "unknown6", "unknown7",
            "pervasive8", "boot_slb2", "vs0", "devkit_cp", "motionC", "bbmc", "unknownE", "motionF",
            "touch10", "touch11", "syscon12", "syscon13", "pervasive14", "unknown15", "vs0_tarpatch",
            "sa0", "pd0", "pervasive19", "unknown1A", "psp_emulist"
        };

        private static Dictionary<ulong, string> BuildPupTypes()
        {
            Dictionary<ulong, string> d = new Dictionary<ulong, string>();
            d[0x100] = "version.txt";
            d[0x101] = "license.xml";
            d[0x200] = "psp2swu.self";
            d[0x204] = "cui_setupper.self";
            d[0x400] = "package_scewm.wm";
            d[0x401] = "package_sceas.as";
            d[0x2005] = "UpdaterES1.CpUp";
            d[0x2006] = "UpdaterES2.CpUp";
            return d;
        }

        // ------------------------------------------------------------ sniffing

        /// <summary>
        /// True for a PSP2UPDAT package. pup_fiction.py checks the 5 byte
        /// "SCEUF" magic at offset 0; the ASCII form "PSP2UPDAT" (which only
        /// ever appears in the file name) is accepted as a defensive extra.
        /// </summary>
        public static bool IsPup(byte[] data)
        {
            if (data == null || data.Length < 0x20) return false;
            if (data[0] == 0x53 && data[1] == 0x43 && data[2] == 0x45 && data[3] == 0x55 && data[4] == 0x46)
                return true;   // "SCEUF"
            string head = Encoding.ASCII.GetString(data, 0, 8);
            return head.StartsWith("PSP2UPDAT", StringComparison.Ordinal);
        }

        // ------------------------------------------------------------- parsing

        public static PupPackage Parse(byte[] data, ILogSink log)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (!IsPup(data))
                throw new InvalidDataException("not a PSP2UPDAT package (expected the 5 byte 'SCEUF' magic)");

            PupPackage p = new PupPackage();
            p.FileSize = data.Length;
            p.ReadHeader(data, log);

            Dictionary<int, int> typeCount = new Dictionary<int, int>();
            for (uint x = 0; x < p.FileCount; x++)
            {
                long recOff = HeaderSize + (long)x * FileRecSize;
                if (!BinUtil.InRange(data, recOff, FileRecSize))
                {
                    p.Warnings.Add("TOC record " + x + " is outside the file");
                    break;
                }
                p.AddSegment(data, recOff, log, typeCount);
            }
            return p;
        }

        /// <summary>
        /// Memory friendly variant for multi-hundred-megabyte PUPs: reads only
        /// the header, the TOC and (for unnamed types) the 0x1000 byte payload
        /// header of each entry.
        /// </summary>
        public static PupPackage ParseFile(string path, ILogSink log)
        {
            if (path == null) throw new ArgumentNullException("path");

            PupPackage p = new PupPackage();
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                p.FileSize = fs.Length;
                byte[] header = new byte[HeaderSize];
                if (fs.Read(header, 0, HeaderSize) != HeaderSize)
                    throw new InvalidDataException("file is smaller than a PUP header");
                if (!IsPup(header))
                    throw new InvalidDataException("not a PSP2UPDAT package (expected the 5 byte 'SCEUF' magic)");
                p.ReadHeader(header, log);

                long recOff = HeaderSize;
                Dictionary<int, int> typeCount = new Dictionary<int, int>();
                for (uint x = 0; x < p.FileCount; x++)
                {
                    byte[] rec = new byte[FileRecSize];
                    if (fs.Read(rec, 0, FileRecSize) != FileRecSize)
                    {
                        p.Warnings.Add("TOC record " + x + " is outside the file");
                        break;
                    }
                    p.AddSegmentFromFile(fs, rec, log, typeCount);
                    recOff += FileRecSize;
                }
            }
            return p;
        }

        private void ReadHeader(byte[] header, ILogSink log)
        {
            Version = BinUtil.U32(header, 0x08);
            FirmwareVersion = BinUtil.U32(header, 0x10);
            BuildNumber = BinUtil.U32(header, 0x14);
            FileCount = BinUtil.U32(header, 0x18);
            Log(log, "PUP header: version=" + Version + " firmware=0x" + FirmwareVersion.ToString("X8", CultureInfo.InvariantCulture) +
                     " build=" + BuildNumber + " files=" + FileCount);
        }

        private void AddSegment(byte[] data, long recOff, ILogSink log, Dictionary<int, int> typeCount)
        {
            PupSegment s = new PupSegment();
            s.FileType = BinUtil.U64(data, recOff + 0x00);
            s.Offset64 = BinUtil.U64(data, recOff + 0x08);
            s.Size = BinUtil.U64(data, recOff + 0x10);
            s.Flags64 = BinUtil.U64(data, recOff + 0x18);
            s.Offset = (uint)s.Offset64;
            s.Flags = (uint)s.Flags64;
            s.InFile = s.Offset64 + s.Size <= (ulong)data.Length;

            s.Name = NameFor(data, (long)s.Offset64, s.FileType, typeCount);
            Segments.Add(s);
            Log(log, "  " + s);
        }

        private void AddSegmentFromFile(FileStream fs, byte[] rec, ILogSink log, Dictionary<int, int> typeCount)
        {
            PupSegment s = new PupSegment();
            s.FileType = BinUtil.U64(rec, 0x00);
            s.Offset64 = BinUtil.U64(rec, 0x08);
            s.Size = BinUtil.U64(rec, 0x10);
            s.Flags64 = BinUtil.U64(rec, 0x18);
            s.Offset = (uint)s.Offset64;
            s.Flags = (uint)s.Flags64;
            s.InFile = s.Offset64 + s.Size <= (ulong)fs.Length;

            byte[] hdr = null;
            long save = fs.Position;
            if (!PupTypes.ContainsKey(s.FileType) && s.Offset64 + 0x1000 <= (ulong)fs.Length)
            {
                hdr = new byte[0x1000];
                fs.Position = (long)s.Offset64;
                if (fs.Read(hdr, 0, 0x1000) != 0x1000) hdr = null;
                fs.Position = save;
            }

            s.Name = hdr != null ? NameForHeader(hdr, s.FileType, typeCount) : NameFor(null, (long)s.Offset64, s.FileType, typeCount);
            Segments.Add(s);
            Log(log, "  " + s);
        }

        private string NameFor(byte[] data, long offset, ulong fileType, Dictionary<int, int> typeCount)
        {
            string known;
            if (PupTypes.TryGetValue(fileType, out known)) return known;

            if (data != null && BinUtil.InRange(data, offset, 0x1000))
            {
                byte[] hdr = new byte[0x1000];
                Array.Copy(data, offset, hdr, 0, 0x1000);
                return NameForHeader(hdr, fileType, typeCount);
            }
            return "unknown-0x" + fileType.ToString("x", CultureInfo.InvariantCulture) + ".pkg";
        }

        /// <summary>Port of pup_fiction.make_filename().</summary>
        private string NameForHeader(byte[] hdr, ulong fileType, Dictionary<int, int> typeCount)
        {
            uint magic = BinUtil.U32(hdr, 0);
            uint version = BinUtil.U32(hdr, 4);
            uint flags = BinUtil.U32(hdr, 8);
            ulong metaOffs = BinUtil.U64(hdr, 16);

            if (magic == 0x454353 && version == 3 && flags == 0x30040)
            {
                long meta = (long)metaOffs;
                if (BinUtil.InRange(hdr, meta + 4, 1))
                {
                    int t = BinUtil.U8(hdr, meta + 4);
                    if (t >= 0 && t < FsType.Length)
                    {
                        int n;
                        if (!typeCount.TryGetValue(t, out n)) n = 0;
                        typeCount[t] = n + 1;
                        return FsType[t] + "-" + n.ToString("00", CultureInfo.InvariantCulture) + ".pkg";
                    }
                }
            }
            return "unknown-0x" + fileType.ToString("x", CultureInfo.InvariantCulture) + ".pkg";
        }

        /// <summary>
        /// Read one segment out of an in-memory package. Returns null when the
        /// segment is outside the buffer (e.g. the header_and_toc.bin sample).
        /// </summary>
        public byte[] ReadSegment(byte[] data, PupSegment seg)
        {
            if (data == null || seg == null) return null;
            if (seg.Offset64 + seg.Size > (ulong)data.Length) return null;
            byte[] r = new byte[seg.Size];
            Array.Copy(data, (long)seg.Offset64, r, 0, (int)seg.Size);
            return r;
        }

        /// <summary>Read one segment from the file on disk (streaming, no full load).</summary>
        public byte[] ReadSegmentFromFile(string path, PupSegment seg)
        {
            if (seg == null) return null;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (seg.Offset64 + seg.Size > (ulong)fs.Length) return null;
                fs.Position = (long)seg.Offset64;
                byte[] r = new byte[seg.Size];
                int got = 0;
                while (got < r.Length)
                {
                    int n = fs.Read(r, got, r.Length - got);
                    if (n <= 0) break;
                    got += n;
                }
                if (got != r.Length) return null;
                return r;
            }
        }

        public string Describe()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PUP: version=").Append(Version);
            sb.Append(" firmware=0x").Append(FirmwareVersion.ToString("X8", CultureInfo.InvariantCulture));
            sb.Append(" build=").Append(BuildNumber);
            sb.Append(" files=").Append(FileCount);
            sb.Append(" file_size=").Append(FileSize);
            sb.Append('\n');
            for (int i = 0; i < Segments.Count; i++)
                sb.Append("  ").Append(Segments[i]).Append('\n');
            for (int i = 0; i < Warnings.Count; i++)
                sb.Append("  ! ").Append(Warnings[i]).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }

        private static void Log(ILogSink log, string text)
        {
            if (log != null) log.Log("pup", text);
        }
    }
}
