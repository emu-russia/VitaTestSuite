// Access log: records every memory / MMIO access made by an emulated core.
//
// MMIO accesses (device registers) are always recorded; plain RAM accesses are
// recorded when TraceRam is enabled (they are very frequent).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VitaTestSuite.Core
{
    public enum AccessKind : byte
    {
        Fetch = 0,
        Read = 1,
        Write = 2,
        Execute = 3
    }

    public struct AccessRecord
    {
        public AccessKind Kind;
        public byte Size;        // 1, 2, 4, ...
        public uint Address;
        public uint Value;
        public uint Pc;
        public int Device;       // index into the device table, -1 for RAM
        public bool Unmapped;
    }

    /// <summary>
    /// Ring buffer of memory accesses plus per-device statistics.
    /// Thread safety: the emulator is single threaded, but the GUI may read the
    /// statistics; all mutating calls are locked.
    /// </summary>
    public class AccessLog
    {
        private readonly object sync = new object();

        private AccessRecord[] ring;
        private int head;      // next write position
        private int count;     // valid entries

        public bool Enabled = true;
        public bool TraceRam = false;

        /// <summary>Total number of MMIO accesses seen since the last Clear().</summary>
        public long MmioAccesses;
        /// <summary>Total number of unmapped accesses seen since the last Clear().</summary>
        public long UnmappedAccesses;
        /// <summary>Total number of RAM accesses seen (only counted, never logged, unless TraceRam).</summary>
        public long RamAccesses;
        /// <summary>Total number of code fetches.</summary>
        public long Fetches;

        public AccessLog(int capacity)
        {
            Capacity = capacity < 64 ? 64 : capacity;
            ring = new AccessRecord[Capacity];
        }

        public int Capacity { get; private set; }

        public string[] DeviceNames = new string[0];

        public void Clear()
        {
            lock (sync)
            {
                head = 0;
                count = 0;
                MmioAccesses = 0;
                UnmappedAccesses = 0;
                RamAccesses = 0;
                Fetches = 0;
            }
        }

        public int Count { get { lock (sync) { return count; } } }

        public void Add(ref AccessRecord rec)
        {
            if (!Enabled) return;

            lock (sync)
            {
                ring[head] = rec;
                head = (head + 1) % ring.Length;
                if (count < ring.Length) count++;
            }
        }

        /// <summary>Snapshot of the log, oldest first.</summary>
        public List<AccessRecord> Snapshot()
        {
            lock (sync)
            {
                List<AccessRecord> list = new List<AccessRecord>(count);
                int start = (head - count + ring.Length) % ring.Length;
                for (int i = 0; i < count; i++)
                    list.Add(ring[(start + i) % ring.Length]);
                return list;
            }
        }

        /// <summary>Snapshot of the last <paramref name="n"/> entries, oldest first.</summary>
        public List<AccessRecord> Snapshot(int n)
        {
            lock (sync)
            {
                int take = n < count ? n : count;
                List<AccessRecord> list = new List<AccessRecord>(take);
                int start = (head - take + ring.Length) % ring.Length;
                for (int i = 0; i < take; i++)
                    list.Add(ring[(start + i) % ring.Length]);
                return list;
            }
        }

        public string DeviceName(int index)
        {
            if (index < 0 || index >= DeviceNames.Length) return "RAM";
            return DeviceNames[index];
        }

        public static string Describe(AccessRecord r, string deviceName, string pcTag)
        {
            string kind;
            switch (r.Kind)
            {
                case AccessKind.Fetch: kind = "FETCH"; break;
                case AccessKind.Read: kind = "R"; break;
                case AccessKind.Write: kind = "W"; break;
                default: kind = "X"; break;
            }

            StringBuilder sb = new StringBuilder(96);
            sb.Append(kind);
            sb.Append(r.Size == 1 ? "8 " : r.Size == 2 ? "16" : r.Size == 4 ? "32" : r.Size == 8 ? "64" : (r.Size * 8).ToString());
            sb.Append(' ');
            sb.Append(r.Address.ToString("X8", CultureInfo.InvariantCulture));
            if (r.Kind != AccessKind.Fetch)
            {
                sb.Append(" = ");
                if (r.Size == 1) sb.Append((r.Value & 0xFF).ToString("X2"));
                else if (r.Size == 2) sb.Append((r.Value & 0xFFFF).ToString("X4"));
                else sb.Append(r.Value.ToString("X8"));
            }
            sb.Append("  ");
            sb.Append(deviceName ?? "?");
            if (r.Unmapped) sb.Append(" [UNMAPPED]");
            if (pcTag != null)
            {
                sb.Append("  @");
                sb.Append(pcTag);
            }
            return sb.ToString();
        }
    }
}
