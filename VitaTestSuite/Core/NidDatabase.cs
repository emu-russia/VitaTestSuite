// NidDatabase.cs -- NID -> symbol name database.
//
// Port of the db.yml reader in pup_fiction/vita_loader/vita_loader.py
// (VitaElf.load_nids):
//
//     for line in data:
//         if "0x" in line and "nid: " not in line:
//             name, nid = line.strip().split(":")
//             self.nid_to_name[int(nid, 16)] = name
//
// db.yml is a YAML file whose interesting lines look like
//
//     modules:
//       SceAVConfig:
//         nid: 0x222DDEB1            <- skipped (contains "nid: ")
//         libraries:
//           SceAVConfig: 0x222DDEB1  <- taken: name "SceAVConfig" -> 0x222DDEB1
//
// The real database (434 KB, 8000+ NIDs) is shipped as Docs/nid_db.yml.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace VitaTestSuite.Core
{
    /// <summary>NID (32 bit) to symbol/library name lookup.</summary>
    public class NidDatabase
    {
        private readonly Dictionary<uint, string> map = new Dictionary<uint, string>();
        /// <summary>Path the database was loaded from (null for an empty instance).</summary>
        public string Path;
        /// <summary>Lines that looked like a mapping but could not be parsed.</summary>
        public List<string> Warnings = new List<string>();

        public int Count { get { return map.Count; } }

        /// <summary>Returns the symbol name for <paramref name="nid"/> or null.</summary>
        public string Lookup(uint nid)
        {
            string s;
            if (map.TryGetValue(nid, out s)) return s;
            return null;
        }

        /// <summary>"name (0xNID)" or "0xNID" when unknown; for reports.</summary>
        public string LookupOrHex(uint nid)
        {
            string s = Lookup(nid);
            return s == null ? "0x" + nid.ToString("X8", CultureInfo.InvariantCulture) : s;
        }

        /// <summary>All known NIDs (for diagnostics).</summary>
        public IEnumerable<KeyValuePair<uint, string>> Entries { get { return map; } }

        public static NidDatabase Load(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            NidDatabase db = new NidDatabase();
            db.Path = path;
            string[] lines = File.ReadAllLines(path);
            db.Parse(lines);
            return db;
        }

        private void Parse(string[] lines)
        {
            map.Clear();
            Warnings.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line == null) continue;
                if (line.IndexOf("0x", StringComparison.Ordinal) < 0) continue;   // python: if "0x" in line
                if (line.IndexOf("nid: ", StringComparison.Ordinal) >= 0) continue; // python: and "nid: " not in line

                string[] parts = line.Trim().Split(':');
                if (parts.Length < 2) continue;

                uint nid;
                if (!TryParseHex(parts[1], out nid))
                {
                    // python would raise ValueError; be tolerant and remember it.
                    Warnings.Add("line " + (i + 1) + ": cannot parse NID from '" + line.Trim() + "'");
                    continue;
                }

                string name = parts[0].Trim();
                if (name.Length == 0) continue;
                map[nid] = name;   // later lines win, exactly like the python dict
            }
        }

        private static bool TryParseHex(string s, out uint value)
        {
            value = 0;
            if (s == null) return false;
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (s.Length == 0) return false;
            ulong v;
            if (!ulong.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return false;
            value = (uint)v;
            return true;
        }

        /// <summary>
        /// Find the shipped database: next to the executable, in a Docs
        /// sub-directory next to it, then walking up from both the executable
        /// directory and the current directory (which finds
        /// &lt;repo&gt;\VitaTestSuite\Docs\nid_db.yml from the scratch harness).
        /// Returns null when nothing is found.
        /// </summary>
        public static NidDatabase TryLoadDefault()
        {
            string found = FindDefaultPath();
            if (found == null) return null;
            try
            {
                NidDatabase db = Load(found);
                return db.Count > 0 ? db : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Path of the default database, or null. Does not parse the file.</summary>
        public static string FindDefaultPath()
        {
            List<string> roots = new List<string>();
            try { roots.Add(AppDomain.CurrentDomain.BaseDirectory); } catch (Exception) { }
            try
            {
                string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(loc)) roots.Add(System.IO.Path.GetDirectoryName(loc));
            }
            catch (Exception) { }
            try { roots.Add(Directory.GetCurrentDirectory()); } catch (Exception) { }

            for (int r = 0; r < roots.Count; r++)
            {
                string dir = roots[r];
                if (string.IsNullOrEmpty(dir)) continue;
                for (int up = 0; up < 8 && !string.IsNullOrEmpty(dir); up++)
                {
                    string a = System.IO.Path.Combine(dir, "nid_db.yml");
                    if (File.Exists(a)) return a;
                    string b = System.IO.Path.Combine(System.IO.Path.Combine(dir, "Docs"), "nid_db.yml");
                    if (File.Exists(b)) return b;
                    string c = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(dir, "VitaTestSuite"), "Docs"), "nid_db.yml");
                    if (File.Exists(c)) return c;
                    try { dir = Directory.GetParent(dir) == null ? null : Directory.GetParent(dir).FullName; }
                    catch (Exception) { dir = null; }
                }
            }
            return null;
        }

        public string Describe()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("NidDatabase: ").Append(Count).Append(" entries");
            sb.Append(Path == null ? " (in memory)" : " from " + Path);
            if (Warnings.Count > 0) sb.Append(", ").Append(Warnings.Count).Append(" unparsable line(s)");
            return sb.ToString();
        }
    }
}
