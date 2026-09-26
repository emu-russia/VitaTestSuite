// SceKeys.cs -- the SCE key store, ported from pup_fiction.
//
// Source files (all of them are ported, in this order into SceKeys.Default(),
// which reproduces pup_fiction's behaviour because sceutils.get_segments()
// imports 'from keys import SCE_KEYS' and the store is searched in insertion
// order):
//
//   pup_fiction/keys.py            "SCE_KEYS" -- the table pup_fiction actually uses
//   pup_fiction/keys_external.py   byte-for-byte identical to keys.py (md5
//                                  e61d4d8d2dee00c55a2004c3ffedece9) -- kept as a
//                                  separate source tag so the origin stays visible
//   pup_fiction/keys_internal.py   "internal" (devkit / prototype) key set
//   pup_fiction/keys_proto.py      "prototype" key set + XXX_KEY/XXX_IV
//
// KeyStore semantics ported from scetypes.py:
//   register(keytype, scetype, keyrev, key, iv, minver, maxver, selftype)
//   get(keytype, scetype, sysver, keyrev, selftype) walks the per
//   (keytype, scetype, selftype) list in insertion order and returns the first
//   entry where (sysver < 0 || minver <= sysver <= maxver) && (keyrev < 0 ||
//   keyrev == entry.keyrev).  A missing (keytype, scetype, selftype) triple is
//   an error (KeyError in python, false here).
//
// The AES keys are 16 bytes, the IVs 16 bytes. The "IV" of a CTR segment key
// is used as the initial counter value.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace VitaTestSuite.Core
{
    /// <summary>KeyType (scetypes.py).</summary>
    public static class SceKeyType
    {
        public const int Metadata = 0;
        public const int Npdrm = 1;
    }

    /// <summary>SceType (scetypes.py) -- the ushort at offset 10 of a SCE header.</summary>
    public static class SceContainerType
    {
        public const int Self = 1;
        public const int Srvk = 2;
        public const int Spkg = 3;
        public const int Dev = 0xC0;

        public static string Name(int t)
        {
            switch (t)
            {
                case Self: return "SELF";
                case Srvk: return "SRVK";
                case Spkg: return "SPKG";
                case Dev: return "DEV";
                default: return "SCE_TYPE_0x" + t.ToString("X", CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>SelfType (scetypes.py) -- the 4th dword of the SELF app info.</summary>
    public static class SceSelfType
    {
        public const int None = 0x00;
        public const int Kernel = 0x07;
        public const int App = 0x08;
        public const int Boot = 0x09;
        public const int Secure = 0x0B;
        public const int User = 0x0D;

        public static string Name(int t)
        {
            switch (t)
            {
                case None: return "NONE";
                case Kernel: return "KERNEL";
                case App: return "APP";
                case Boot: return "BOOT";
                case Secure: return "SECURE";
                case User: return "USER";
                default: return "SELF_TYPE_0x" + t.ToString("X", CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>SelfPlatform (scetypes.py); 0xC0 is what the devkit SELFs carry
    /// (python's SelfPlatform enum would raise ValueError on it -- we tolerate it
    /// and report it as DEVKIT, which is the whole point of "port faithfully but
    /// do not crash on real files").</summary>
    public static class ScePlatform
    {
        public const int Ps3 = 0x00;
        public const int Vita = 0x40;
        public const int Devkit = 0xC0;

        public static string Name(int p)
        {
            switch (p)
            {
                case Ps3: return "PS3";
                case Vita: return "VITA";
                case Devkit: return "DEVKIT";
                default: return "PLATFORM_0x" + p.ToString("X2", CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>One registered key/IV pair with its system-version window.</summary>
    public class SceKeyEntry
    {
        public ulong MinVer;
        public ulong MaxVer;
        public int KeyRev;
        public byte[] Key;
        public byte[] Iv;
        public int SelfType;
        /// <summary>Which pup_fiction file this entry came from (kept discoverable).</summary>
        public string Source;
        /// <summary>Line number inside <see cref="Source"/>.</summary>
        public int SourceLine;

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "[{0}:{1}] keyrev={2} minver=0x{3:X} maxver=0x{4:X} key={5}",
                Source, SourceLine, KeyRev, MinVer, MaxVer, BinUtil.ToHex(Key));
        }
    }

    /// <summary>
    /// The SCE key store: metadata keys (AES-CBC over the SELF metadata block)
    /// and NPDRM keys (for APP SELFs, needs the RIF klicense as well).
    /// </summary>
    public class SceKeys
    {
        // ---------------------------------------------------------------- raw
        // Constants that keys.py / keys_internal.py / keys_proto.py define and
        // which pup_fiction uses elsewhere (pup_fiction.enc_decrypt() for the
        // ".enc" SLB2 blobs, and the XX/XXX keys of the prototype key set).
        public static readonly byte[] EncKey = BinUtil.Hex("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        public static readonly byte[] EncIv = BinUtil.Hex("AF5F2CB04AC1751ABF51CEF1C8096210");
        public static readonly byte[] XxxKey = BinUtil.Hex("992EF70868DE1B219EC3618FA79DAEC39067FE5638116C29FC0FF7E2A58FBD9E");
        public static readonly byte[] XxxIv = BinUtil.Hex("00000000000000000000000000000000");

        // keytype -> scetype -> selftype -> ordered entries
        private readonly Dictionary<int, Dictionary<int, Dictionary<int, List<SceKeyEntry>>>> store =
            new Dictionary<int, Dictionary<int, Dictionary<int, List<SceKeyEntry>>>>();

        public SceKeys()
        {
        }

        /// <summary>Number of registered entries (all key types).</summary>
        public int Count
        {
            get
            {
                int n = 0;
                foreach (Dictionary<int, Dictionary<int, List<SceKeyEntry>>> bySce in store.Values)
                    foreach (Dictionary<int, List<SceKeyEntry>> bySelf in bySce.Values)
                        foreach (List<SceKeyEntry> l in bySelf.Values)
                            n += l.Count;
                return n;
            }
        }

        /// <summary>Distinct source files that contributed entries, with entry counts.</summary>
        public List<KeyValuePair<string, int>> SourceCounts()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            List<string> order = new List<string>();
            foreach (Dictionary<int, Dictionary<int, List<SceKeyEntry>>> bySce in store.Values)
                foreach (Dictionary<int, List<SceKeyEntry>> bySelf in bySce.Values)
                    foreach (List<SceKeyEntry> l in bySelf.Values)
                        foreach (SceKeyEntry e in l)
                        {
                            int c;
                            if (!counts.TryGetValue(e.Source, out c)) { counts[e.Source] = 0; order.Add(e.Source); }
                            counts[e.Source] = counts[e.Source] + 1;
                        }
            List<KeyValuePair<string, int>> r = new List<KeyValuePair<string, int>>();
            foreach (string s in order) r.Add(new KeyValuePair<string, int>(s, counts[s]));
            return r;
        }

        /// <summary>Every registered entry, in insertion order (diagnostics).</summary>
        public List<SceKeyEntry> AllEntries()
        {
            List<SceKeyEntry> r = new List<SceKeyEntry>();
            foreach (Dictionary<int, Dictionary<int, List<SceKeyEntry>>> bySce in store.Values)
                foreach (Dictionary<int, List<SceKeyEntry>> bySelf in bySce.Values)
                    foreach (List<SceKeyEntry> l in bySelf.Values)
                        r.AddRange(l);
            return r;
        }

        public void Register(int keyType, int sceType, int keyRev, string keyHex, string ivHex,
                             ulong minVer, ulong maxVer, int selfType, string source, int sourceLine)
        {
            byte[] key = BinUtil.Hex(keyHex);
            byte[] iv = BinUtil.Hex(ivHex);
            if (key == null || iv == null)
                throw new ArgumentException("SceKeys.Register: malformed key/iv hex for " + source + ":" + sourceLine);

            Dictionary<int, Dictionary<int, List<SceKeyEntry>>> bySce;
            if (!store.TryGetValue(keyType, out bySce)) { bySce = new Dictionary<int, Dictionary<int, List<SceKeyEntry>>>(); store[keyType] = bySce; }

            Dictionary<int, List<SceKeyEntry>> bySelf;
            if (!bySce.TryGetValue(sceType, out bySelf)) { bySelf = new Dictionary<int, List<SceKeyEntry>>(); bySce[sceType] = bySelf; }

            List<SceKeyEntry> list;
            if (!bySelf.TryGetValue(selfType, out list)) { list = new List<SceKeyEntry>(); bySelf[selfType] = list; }

            SceKeyEntry e = new SceKeyEntry();
            e.KeyRev = keyRev;
            e.Key = key;
            e.Iv = iv;
            e.MinVer = minVer;
            e.MaxVer = maxVer;
            e.SelfType = selfType;
            e.Source = source;
            e.SourceLine = sourceLine;
            list.Add(e);
        }

        /// <summary>
        /// Faithful port of KeyStore.get(). <paramref name="sysVersion"/> &lt; 0 and
        /// <paramref name="keyRevision"/> &lt; 0 mean "any" (python passes -1 for
        /// both, e.g. self2elf's ignore_sysver).
        /// </summary>
        public bool TryGetKey(int keyType, int sceType, long sysVersion, int keyRevision, int selfType,
                              out byte[] key, out byte[] iv)
        {
            key = null;
            iv = null;

            Dictionary<int, Dictionary<int, List<SceKeyEntry>>> bySce;
            if (!store.TryGetValue(keyType, out bySce)) return false;

            Dictionary<int, List<SceKeyEntry>> bySelf;
            if (!bySce.TryGetValue(sceType, out bySelf)) return false;

            List<SceKeyEntry> list;
            if (!bySelf.TryGetValue(selfType, out list)) return false;

            for (int i = 0; i < list.Count; i++)
            {
                SceKeyEntry e = list[i];
                if (sysVersion >= 0 && !((ulong)sysVersion >= e.MinVer && (ulong)sysVersion <= e.MaxVer)) continue;
                if (keyRevision >= 0 && keyRevision != e.KeyRev) continue;
                key = e.Key;
                iv = e.Iv;
                return true;
            }
            return false;
        }

        // ------------------------------------------------------- spec'd API

        /// <summary>
        /// Every entry that matches (keyType, sceType, selfType) and the
        /// version/key-revision filter, in insertion order and without
        /// duplicates. This is what the SELF metadata decryption uses: because
        /// keys.py, keys_internal.py and keys_proto.py are three *alternative*
        /// key sets that are merged here, more than one key can match the same
        /// (sce type, key revision, self type) triple, and only the correct one
        /// makes the metadata padding check succeed.
        /// <paramref name="ignoreSysVersion"/> reproduces pup_fiction's
        /// self2elf(ignore_sysver=True).
        /// </summary>
        public List<SceKeyEntry> GetCandidates(int keyType, int sceType, long sysVersion, int keyRevision,
                                               int selfType, bool ignoreSysVersion)
        {
            List<SceKeyEntry> r = new List<SceKeyEntry>();

            Dictionary<int, Dictionary<int, List<SceKeyEntry>>> bySce;
            if (!store.TryGetValue(keyType, out bySce)) return r;
            Dictionary<int, List<SceKeyEntry>> bySelf;
            if (!bySce.TryGetValue(sceType, out bySelf)) return r;
            List<SceKeyEntry> list;
            if (!bySelf.TryGetValue(selfType, out list)) return r;

            for (int i = 0; i < list.Count; i++)
            {
                SceKeyEntry e = list[i];
                if (!ignoreSysVersion && sysVersion >= 0 &&
                    !((ulong)sysVersion >= e.MinVer && (ulong)sysVersion <= e.MaxVer)) continue;
                if (keyRevision >= 0 && keyRevision != e.KeyRev) continue;

                bool dup = false;
                for (int j = 0; j < r.Count; j++)
                    if (BinUtil.ToHex(r[j].Key) == BinUtil.ToHex(e.Key) && BinUtil.ToHex(r[j].Iv) == BinUtil.ToHex(e.Iv)) dup = true;
                if (!dup) r.Add(e);
            }
            return r;
        }

        /// <summary>
        /// Metadata key lookup. <paramref name="keyRevision"/> 0xFF means "any
        /// key revision" (the byte API cannot express -1); use
        /// <see cref="TryGetMetadataKeyEx"/> for full control.
        /// </summary>
        public bool TryGetMetadataKey(int sceType, ulong sysVersion, byte keyRevision, int selfType,
                                      out byte[] key, out byte[] iv)
        {
            int rev = keyRevision == 0xFF ? -1 : (int)keyRevision;
            return TryGetKey(SceKeyType.Metadata, sceType, (long)sysVersion, rev, selfType, out key, out iv);
        }

        /// <summary>Same as <see cref="TryGetMetadataKey"/> with signed wildcards (-1 = any).</summary>
        public bool TryGetMetadataKeyEx(int sceType, long sysVersion, int keyRevision, int selfType,
                                        out byte[] key, out byte[] iv)
        {
            return TryGetKey(SceKeyType.Metadata, sceType, sysVersion, keyRevision, selfType, out key, out iv);
        }

        /// <summary>NPDRM key lookup (APP SELFs).</summary>
        public bool TryGetNpdrmKey(int sceType, ulong sysVersion, int keyRevisionIndex, int selfType,
                                   out byte[] key, out byte[] iv)
        {
            return TryGetKey(SceKeyType.Npdrm, sceType, (long)sysVersion, keyRevisionIndex, selfType, out key, out iv);
        }

        /// <summary>Same as <see cref="TryGetNpdrmKey"/> with signed wildcards (-1 = any).</summary>
        public bool TryGetNpdrmKeyEx(int sceType, long sysVersion, int keyRevision, int selfType,
                                     out byte[] key, out byte[] iv)
        {
            return TryGetKey(SceKeyType.Npdrm, sceType, sysVersion, keyRevision, selfType, out key, out iv);
        }

        // ---------------------------------------------------------- building

        /// <summary>
        /// Built-in store: keys.py (== keys_external.py) + keys_internal.py +
        /// keys_proto.py, in that order.
        /// </summary>
        public static SceKeys Default()
        {
            SceKeys k = new SceKeys();
            RegisterKeysPy(k);
            RegisterKeysInternal(k);
            RegisterKeysProto(k);
            return k;
        }

        /// <summary>
        /// Optional external key file. One key per line:
        ///
        ///   # comment
        ///   KEYTYPE SCETYPE KEYREV SELFTYPE MINVER MAXVER KEYHEX IVHEX
        ///
        /// e.g.  "METADATA SELF 1 USER 0x10300000000 0xFFF00000000 &lt;32 hex&gt; &lt;32 hex&gt;"
        ///
        /// KEYTYPE   : metadata | npdrm
        /// SCETYPE   : self | srvk | spkg | dev | decimal
        /// SELFTYPE  : none | kernel | app | boot | secure | user | decimal
        /// KEYREV/MINVER/MAXVER : decimal or 0x-prefixed
        /// MINVER/MAXVER may be "-" meaning 0 / 0xFFFFFFFFFFFFFFFF.
        /// </summary>
        public static SceKeys FromFile(string path)
        {
            SceKeys k = new SceKeys();
            string[] lines = System.IO.File.ReadAllLines(path);
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                int hash = line.IndexOf('#');
                if (hash >= 0) line = line.Substring(0, hash);
                line = line.Trim();
                if (line.Length == 0) continue;

                string[] f = line.Split(new char[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (f.Length < 8)
                    throw new FormatException(path + ":" + (n + 1) + ": expected 8 fields, got " + f.Length);

                int keyType = ParseKeyType(f[0]);
                int sceType = ParseSceType(f[1]);
                int keyRev = ParseInt(f[2]);
                int selfType = ParseSelfType(f[3]);
                ulong minVer = f[4] == "-" ? 0UL : ParseULong(f[4]);
                ulong maxVer = f[5] == "-" ? 0xFFFFFFFFFFFFFFFFUL : ParseULong(f[5]);
                k.Register(keyType, sceType, keyRev, f[6], f[7], minVer, maxVer, selfType,
                           System.IO.Path.GetFileName(path), n + 1);
            }
            return k;
        }

        private static int ParseKeyType(string s)
        {
            switch (s.Trim().ToLowerInvariant())
            {
                case "metadata": case "meta": case "0": return SceKeyType.Metadata;
                case "npdrm": case "1": return SceKeyType.Npdrm;
                default: return ParseInt(s);
            }
        }

        private static int ParseSceType(string s)
        {
            switch (s.Trim().ToLowerInvariant())
            {
                case "self": return SceContainerType.Self;
                case "srvk": return SceContainerType.Srvk;
                case "spkg": return SceContainerType.Spkg;
                case "dev": return SceContainerType.Dev;
                default: return ParseInt(s);
            }
        }

        private static int ParseSelfType(string s)
        {
            switch (s.Trim().ToLowerInvariant())
            {
                case "none": return SceSelfType.None;
                case "kernel": return SceSelfType.Kernel;
                case "app": return SceSelfType.App;
                case "boot": return SceSelfType.Boot;
                case "secure": return SceSelfType.Secure;
                case "user": return SceSelfType.User;
                default: return ParseInt(s);
            }
        }

        private static int ParseInt(string s)
        {
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return (int)ulong.Parse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return int.Parse(s, CultureInfo.InvariantCulture);
        }

        private static ulong ParseULong(string s)
        {
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ulong.Parse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return ulong.Parse(s, CultureInfo.InvariantCulture);
        }

        // --------------------------------------------------------- key tables
        // The huge ones are transcribed verbatim (same order) from the python
        // tables; the SourceLine values are the line of the corresponding
        // SCE_KEYS.register( call in the python file.

        private const ulong MaxVerAll = 0xFFFFFFFFFFFFFFFFUL;

        private static void RegisterKeysPy(SceKeys k)
        {
            const string S = "pup_fiction/keys.py";

            k.Register(SceKeyType.Metadata, SceContainerType.Spkg, 0,
                "2E6F4751D15B06C51F572A9306E52DD7007EA56A31D459EC6D3681AB08625501",
                "B3D541A568751DF8F4833BAB4EFE0537", 0x00000000000, 0xFFF00000000, SceSelfType.None, S, 7);

            k.Register(SceKeyType.Metadata, SceContainerType.Srvk, 0,
                "4648164DB9E67009456C7CA6F2378835FD678539B36B3DE6F1C604B7D4258141",
                "6EC8AD67993DAE75675F0AFFDE5C41F3", 0x10300000000, 0x16920000000, SceSelfType.None, S, 18);

            k.Register(SceKeyType.Metadata, SceContainerType.Srvk, 0,
                "DAE4B0F901E338DEFF3CCDBDEA1E2FDEA9926BB98CB182443CC0C0F7FAE428EE",
                "18D925FA885C7E28A9CFF458C24D8BED", 0x18000000000, 0xFFF00000000, SceSelfType.None, S, 29);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "9D4E4CE92EA1C4576EB9601EC43EC03AAE8EC324ECF6DE01E918E61D2223EE55",
                "CFEA3CCBA454D3279AD7CB0510431434", 0x10300000000, 0x16920000000, SceSelfType.Secure, S, 40);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "B1B6FEB39A8BD7A2AC584D435E150C624F560D3EFB03E745C575E0844569E2D0",
                "89B4E6BAB03B03D49BF0FC927FEA8659", 0x18000000000, 0x36100000000, SceSelfType.Secure, S, 51);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "59AC7F05E115D758201A3F3461BCA0D42BD186F00CFC24263973F622AD9ED30C",
                "A053B00BA4BF880799B4265C6BC064B5", 0x36300000000, 0xFFF00000000, SceSelfType.Secure, S, 62);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "7A7FB1560DCD121CEA5E11B90124B13282752F2D5B95D75036AB3A29BB3BD2AB",
                "6C71642A042A041F1EE3094070B009BE", 0x10300000000, 0x16920000000, SceSelfType.Boot, S, 73);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "B1B936B512F9A16E51B948622B26F15C53680C77AC332EC25846B839520393EC",
                "90D527BAF7296B5B6A576CFA6B54D266", 0x18000000000, 0x36100000000, SceSelfType.Boot, S, 84);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "426FD1D33FEBBFAC560B7957B94F445AE5F1DED2AA70F74DB944645DC439122F",
                "995F1364BB9735FA448B18D886150C85", 0x36300000000, 0xFFF00000000, SceSelfType.Boot, S, 95);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "B4AAF62D48FBD898C240308A9773AFE57B8A18D783F0B37932BB21B51386A9A0",
                "8CD162C5C613376F3E4BEA0B8FD5A3D0", 0x10300000000, 0x16920000000, SceSelfType.Kernel, S, 106);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "849AF7E8DE5B9C28C38CA74963FCF155E0F200FB08185E46CDA87790AAA10D72",
                "88710E219454A3CBF6D382D4BBD22BFC", 0x18000000000, 0x36100000000, SceSelfType.Kernel, S, 117);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "18E26DF712C362769D4F5E70460D28D88B7B991733DE692C2B9463B41FF4B925",
                "5B13077EEA801FC77D492050801FA507", 0x36300000000, 0xFFF00000000, SceSelfType.Kernel, S, 128);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "4769935C3B1CB248C3A88A406B1535D5DC2C0279D5901DE534DC4A11B8F60804",
                "0CE906F746D40105660456D827CEBD25", 0x10300000000, 0x16920000000, SceSelfType.User, S, 139);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "4769935C3B1CB248C3A88A406B1535D5DC2C0279D5901DE534DC4A11B8F60804",
                "0CE906F746D40105660456D827CEBD25", 0x18000000000, 0xFFF00000000, SceSelfType.User, S, 150);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "613AD6EAC63D4E14F51A8C6AF18C66621968323B6F205B5E515C16D77BB06671",
                "ADBDAA5041B2094CF2B359301DE64171", 0x10300000000, 0xFFF00000000, SceSelfType.User, S, 161);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 2,
                "0F2041269B26D6B7EF143E35E83E914629A92F50F3A4CEE14CDFF63AEC641117",
                "07EF64437F0CB6995E6D785E42796C83", 0x18000000000, 0xFFF00000000, SceSelfType.User, S, 172);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 3,
                "3AFADA34660C6515B539EBBBC79C9C0ADA4337C32652CA03C6DD21A1D612D8F4",
                "7F98A137869B91B1EB9604F81FD74C50", 0x18000000000, 0xFFF00000000, SceSelfType.User, S, 183);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 4,
                "8FF491B36713E8AA38DE30B303689657F07AE70A8A8B1D7867441C52DB39C806",
                "D9CC7E26CE99053E48F9BEF1CB93C184", 0x36300000000, 0xFFF00000000, SceSelfType.User, S, 194);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 5,
                "4D71B2FB3D4359FB34445305C88A5E82FA12D34A8308F312AA34B58F6112253A",
                "04A27133FF0205C96B7F45A60D7D417B", 0x36300000000, 0xFFF00000000, SceSelfType.User, S, 205);

            k.Register(SceKeyType.Npdrm, SceContainerType.Self, 0,
                "C10368BF3D2943BC6E5BD05E46A9A7B6",
                "00000000000000000000000000000000", 0x00000000000, MaxVerAll, SceSelfType.App, S, 216);

            k.Register(SceKeyType.Npdrm, SceContainerType.Self, 1,
                "16419DD3BFBE8BDC596929B72CE237CD",
                "00000000000000000000000000000000", 0x00000000000, MaxVerAll, SceSelfType.App, S, 227);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "AAA508FA5E85EAEE597ED2B27804D22287CFADF1DF32EDC7A7C58E8C9AA8BB36",
                "CD1BD3A59200CC67A3B804808DC2AE73", 0x00000000000, 0x16920000000, SceSelfType.App, S, 238);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "5661E5FB20CFD1D1DFF50C1E59A6EA977D0AA5C5770F53B9CDD4E9451FFF55CB",
                "23D02FF79BF430E2D123869BF0CACAA0", 0x18000000000, MaxVerAll, SceSelfType.App, S, 249);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "4181B2DF5F5D94D3C80B7D86EACF1928533A49BA58EDE2B43CDEE7E572568BD4",
                "B1678C0543B6C1997B63A6F4F3C8FD33", 0x00000000000, MaxVerAll, SceSelfType.App, S, 260);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 2,
                "5282582F17F068F89A260AAFB71C58928F45A8D08C681376B07FF9EAB1114226",
                "29672DF43E426F41AF46D42E8437D449", 0x18000000000, MaxVerAll, SceSelfType.App, S, 271);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 3,
                "270CBA370061B87077672ADB5142D18844AAED352A9CCEE63602B0D740594334",
                "1CF2454FBF47D76221B91AFC3B608C28", 0x18000000000, MaxVerAll, SceSelfType.App, S, 282);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 4,
                "A782BC5A9EDDFC49A513FF3E592C4677A8C8920F23C9F11F2558FB9D99A43868",
                "559B5E658559EB65EBF892C274E098A9", 0x35700000000, MaxVerAll, SceSelfType.App, S, 293);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 5,
                "12D64D0172495226010A687DE245A73DE028B3561E25E69BABC325636F3CAE0A",
                "F149EED1757E5A915B24309795BFC380", 0x35700000000, MaxVerAll, SceSelfType.App, S, 304);
        }

        private static void RegisterKeysInternal(SceKeys k)
        {
            const string S = "pup_fiction/keys_internal.py";

            k.Register(SceKeyType.Metadata, SceContainerType.Spkg, 0,
                "23F1D525244266E6DA7A52DA9446318301EE8CC58D54901AE94D93010F7DEE6B",
                "3721F7C05DE5F55ECC39BDDB4A6C585D", 0x00000000000, 0xFFF00000000, SceSelfType.None, S, 7);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "AED9D76EE1E29290002BFF32D4B0656EEE40FBDA4F8B55BE5BE0ED83530F27D2",
                "DB50912F2416B54F7F36227169ECE500", 0x00000000000, 0xFFF00000000, SceSelfType.Secure, S, 18);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "9D3F28DE30DED1D503DB6FA762A571C422A88D0F361899EF36D357059C72EC43",
                "30E43CFB57D418A5A0D32A9939D23501", 0x00000000000, 0xFFF00000000, SceSelfType.Boot, S, 29);

            k.Register(SceKeyType.Metadata, SceContainerType.Srvk, 0,
                "EAB14F9BE15EAEC1603BE63C9FCDE4099D601FB0E9FC4DF250B8DEC635987A1C",
                "30B9E61707993B635D0E182446DB0B8D", 0x00000000000, 0xFFF00000000, SceSelfType.None, S, 40);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "74F6D2A1D2A093AE32B83337E0AE4AD2E6D93B034F5BF3B68DB77131883310D4",
                "926AB55BDADC45DBB610E90E56A0368C", 0x00000000000, 0xFFF00000000, SceSelfType.Kernel, S, 51);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "322D706CB6EBEA14DEF7BFE45F812971347DC95CD7697C16A71EA4B2A1E12C0D",
                "31FA2E606031EDF39665B5616E9F937D", 0x00000000000, 0xFFF00000000, SceSelfType.User, S, 62);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "DA3BE69B77B3A857EA4F6CDC73C0AB0590C0A95E145B8D55D2D3A6447C247F46",
                "A0385383AB31497E3AFB7CCDDB30CA5A", 0x00000000000, 0xFFF00000000, SceSelfType.User, S, 73);

            k.Register(SceKeyType.Npdrm, SceContainerType.Self, 0,
                "C10368BF3D2943BC6E5BD05E46A9A7B6",
                "00000000000000000000000000000000", 0x00000000000, MaxVerAll, SceSelfType.App, S, 84);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "AAA508FA5E85EAEE597ED2B27804D22287CFADF1DF32EDC7A7C58E8C9AA8BB36",
                "CD1BD3A59200CC67A3B804808DC2AE73", 0x00000000000, 0x16920000000, SceSelfType.App, S, 95);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "4181B2DF5F5D94D3C80B7D86EACF1928533A49BA58EDE2B43CDEE7E572568BD4",
                "B1678C0543B6C1997B63A6F4F3C8FD33", 0x00000000000, MaxVerAll, SceSelfType.App, S, 106);
        }

        private static void RegisterKeysProto(SceKeys k)
        {
            const string S = "pup_fiction/keys_proto.py";

            k.Register(SceKeyType.Metadata, SceContainerType.Spkg, 0,
                "FA88E5B5CBB49603DF689F139045E7C3C9C7E33B5923DF54E4C5FE5298B4FD32",
                "5EAA69AB35E737EC22C721A916E00263", 0x00000000000, 0xFFF00000000, SceSelfType.None, S, 9);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "B982589B568CDD4055433747DF19644A8D1B479B17CA44ECE5E82694550FEC74",
                "BECEDF96543939032CC4DD7D95E47720", 0x00000000000, 0xFFF00000000, SceSelfType.Secure, S, 20);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "9EE16CA4AADD77F53BEE0F4AE3D45326D009806D2DE9942CE0836E43DC5DD1CE",
                "CFBA84A87EE29C9A521CA20691485E45", 0x00000000000, 0xFFF00000000, SceSelfType.Boot, S, 31);

            k.Register(SceKeyType.Metadata, SceContainerType.Srvk, 0,
                "A603AA68753CEE3E186C81900A862DCDB13505D39FC59C62BBFAD94C526B8A06",
                "352F596CFB513A148B95F9D78E57E755", 0x00000000000, 0xFFF00000000, SceSelfType.None, S, 42);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "61E7E786BB6F67570A71FC92E73885439CD16B96BC7C37C200EF11D3446FCF69",
                "99E8B68EE784FDAFC3294B8E55F0C529", 0x00000000000, 0xFFF00000000, SceSelfType.Kernel, S, 53);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "DA3BE69B77B3A857EA4F6CDC73C0AB0590C0A95E145B8D55D2D3A6447C247F46",
                "A0385383AB31497E3AFB7CCDDB30CA5A", 0x00000000000, 0xFFF00000000, SceSelfType.User, S, 64);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "8D355E70736EF7AA508D640D8D382B19D9C8747C4A8273A6D5707F227F49592E",
                "BEB4819878915F3025978538693B3EBB", 0x00000000000, 0xFFF00000000, SceSelfType.User, S, 75);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 0,
                "AAA508FA5E85EAEE597ED2B27804D22287CFADF1DF32EDC7A7C58E8C9AA8BB36",
                "CD1BD3A59200CC67A3B804808DC2AE73", 0x00000000000, 0x16920000000, SceSelfType.App, S, 86);

            k.Register(SceKeyType.Metadata, SceContainerType.Self, 1,
                "4181B2DF5F5D94D3C80B7D86EACF1928533A49BA58EDE2B43CDEE7E572568BD4",
                "B1678C0543B6C1997B63A6F4F3C8FD33", 0x00000000000, MaxVerAll, SceSelfType.App, S, 97);

            k.Register(SceKeyType.Npdrm, SceContainerType.Self, 0,
                "C10368BF3D2943BC6E5BD05E46A9A7B6",
                "00000000000000000000000000000000", 0x00000000000, MaxVerAll, SceSelfType.App, S, 108);

            // keys_proto.py registers an *all zero* keyrev 1 NPDRM key; kept as is.
            k.Register(SceKeyType.Npdrm, SceContainerType.Self, 1,
                "00000000000000000000000000000000",
                "00000000000000000000000000000000", 0x00000000000, MaxVerAll, SceSelfType.App, S, 119);
        }
    }
}
