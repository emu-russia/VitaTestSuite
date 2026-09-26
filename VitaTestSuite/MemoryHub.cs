// Memory engine: RAM ranges + MMIO device bus + access logging.

using System;
using System.Collections.Generic;
using System.Globalization;
using VitaTestSuite.Core;

public class MemRange
{
    public string name;
    public byte[] Memptr;
    public int Size;
    public uint BaseVAddr;
    public bool Mapped;
    /// <summary>Free-form note shown by the "mem" command (e.g. "CMeP private RAM").</summary>
    public string Note = "";

    public uint EndVAddr { get { return BaseVAddr + (uint)Size; } }
}

/// <summary>
/// Base class of every memory mapped device. Implementations decode register
/// accesses and return/produce values; the hub does all the logging.
/// </summary>
public abstract class MmioDevice
{
    public string Name = "device";
    public uint Base;
    public uint Size;

    /// <summary>Register map: address -> name. Used for the access log.</summary>
    public Dictionary<uint, string> RegisterNames = new Dictionary<uint, string>();

    public virtual bool Handles(uint addr)
    {
        return addr >= Base && addr < Base + Size;
    }

    /// <summary>Human readable register name for logs. Returns null when unknown.</summary>
    public virtual string RegisterName(uint addr)
    {
        string n;
        if (RegisterNames.TryGetValue(addr, out n)) return n;
        return null;
    }

    public abstract uint Read(uint addr, int size);
    public abstract void Write(uint addr, int size, uint value);

    /// <summary>Called when the core is reset, so devices can return to power-on state.</summary>
    public virtual void Reset() { }

    /// <summary>One-line summary of the device state, shown by the "devices" command.</summary>
    public virtual string Summary { get { return Name + " @ 0x" + Base.ToString("X8") + " (0x" + Size.ToString("X") + ")"; } }
}

public class MemoryHub
{
    public bool LittleEndian = true;

    public delegate byte ReadByteDelegate(uint Address);
    public delegate void WriteByteDelegate(uint Address, byte val);
    public delegate ushort ReadHalfDelegate(uint Address);
    public delegate void WriteHalfDelegate(uint Address, ushort val);
    public delegate uint ReadWordDelegate(uint Address);
    public delegate void WriteWordDelegate(uint Address, uint val);

    public List<MemRange> ranges = new List<MemRange>();
    public List<MmioDevice> devices = new List<MmioDevice>();

    public AccessLog Access = new AccessLog(200000);

    /// <summary>PC of the core performing the access (set by the cores every step).</summary>
    public uint CurrentPC;
    /// <summary>Name of the core performing the access.</summary>
    public string CurrentCore = "";

    /// <summary>Emitted (through OnMmioAccess) for every logged access; the GUI uses it for live view.</summary>
    public Action<AccessRecord, string> OnAccess;

    /// <summary>Set when an access missed every RAM range and every device.</summary>
    public bool LastAccessUnmapped;

    #region Memory ranges

    public MemRange AddMemory(string tag, int size)
    {
        MemRange range = new MemRange();
        range.name = tag;
        range.Size = size;
        range.Memptr = new byte[size];
        range.BaseVAddr = 0;
        range.Mapped = false;
        ranges.Add(range);
        return range;
    }

    /// <summary>Add a range and map it immediately.</summary>
    public MemRange AddMemory(string tag, int size, uint baseVAddr, string note)
    {
        MemRange r = AddMemory(tag, size);
        r.BaseVAddr = baseVAddr;
        r.Mapped = true;
        r.Note = note ?? "";
        return r;
    }

    public void MapMemory(uint BaseVAddr, string tag)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            if (ranges[i].name == tag)
            {
                ranges[i].Mapped = true;
                ranges[i].BaseVAddr = BaseVAddr;
                return;
            }
        }
    }

    public MemRange FindRange(string tag)
    {
        for (int i = 0; i < ranges.Count; i++)
            if (ranges[i].name == tag) return ranges[i];
        return null;
    }

    public MemRange FindRangeAt(uint addr, int size)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            MemRange r = ranges[i];
            if (!r.Mapped) continue;
            if (addr >= r.BaseVAddr && (ulong)addr + (ulong)size <= (ulong)r.BaseVAddr + (ulong)r.Size)
                return r;
        }
        return null;
    }

    /// <summary>
    /// Make sure [addr, addr+size) is backed by writable RAM, allocating a new
    /// 64 KiB-aligned range when nothing covers it. Returns the (possibly new) range.
    /// </summary>
    public MemRange EnsureRamFor(uint addr, int size, string tagPrefix)
    {
        MemRange r = FindRangeAt(addr, size);
        if (r != null) return r;

        const uint page = 0x10000;
        uint lo = addr & ~(page - 1);
        uint hi = ((uint)(addr + (uint)size) + page - 1) & ~(page - 1);
        int len = (int)(hi - lo);
        if (len <= 0) len = (int)page;

        MemRange nr = AddMemory(tagPrefix + "_auto@" + lo.ToString("X8"), len);
        nr.BaseVAddr = lo;
        nr.Mapped = true;
        nr.Note = "auto-mapped";
        return nr;
    }

    public bool IsMapped(uint addr, int size = 1)
    {
        if (FindRangeAt(addr, size) != null) return true;
        return FindDevice(addr) != null;
    }

    private bool Translate(uint VAddr, int Size, out uint Offset, out byte[] Memptr)
    {
        Offset = 0;
        Memptr = null;
        MemRange r = FindRangeAt(VAddr, Size);
        if (r == null) return false;
        Offset = VAddr - r.BaseVAddr;
        Memptr = r.Memptr;
        return true;
    }

    #endregion

    #region MMIO devices

    public void AddDevice(MmioDevice dev)
    {
        devices.Add(dev);
        string[] names = new string[devices.Count];
        for (int i = 0; i < devices.Count; i++) names[i] = devices[i].Name;
        Access.DeviceNames = names;
    }

    public MmioDevice FindDevice(uint addr)
    {
        // Smallest matching device wins, so that a sub-device can override a big window.
        MmioDevice best = null;
        for (int i = 0; i < devices.Count; i++)
        {
            MmioDevice d = devices[i];
            if (!d.Handles(addr)) continue;
            if (best == null || d.Size < best.Size) best = d;
        }
        return best;
    }

    public int DeviceIndex(MmioDevice dev)
    {
        return devices.IndexOf(dev);
    }

    public void ResetDevices()
    {
        foreach (MmioDevice d in devices) d.Reset();
    }

    #endregion

    #region Logging helpers

    private void Note(AccessKind kind, uint addr, int size, uint value, MmioDevice dev, bool unmapped)
    {
        AccessRecord rec = new AccessRecord();
        rec.Kind = kind;
        rec.Size = (byte)size;
        rec.Address = addr;
        rec.Value = value;
        rec.Pc = CurrentPC;
        rec.Device = dev != null ? DeviceIndex(dev) : -1;
        rec.Unmapped = unmapped;

        Access.Add(ref rec);

        Action<AccessRecord, string> cb = OnAccess;
        if (cb != null)
        {
            string reg = dev != null ? dev.RegisterName(addr) : null;
            string name = dev != null ? dev.Name : (unmapped ? "unmapped" : "RAM");
            cb(rec, reg != null ? name + "." + reg : name);
        }
    }

    #endregion

    #region Cpu interface

    public byte ReadByte(uint Address)
    {
        MmioDevice dev = FindDevice(Address);
        if (dev != null)
        {
            uint v = dev.Read(Address, 1);
            Access.MmioAccesses++;
            Note(AccessKind.Read, Address, 1, v, dev, false);
            return (byte)v;
        }

        uint Offset; byte[] Memptr;
        if (Translate(Address, 1, out Offset, out Memptr))
        {
            Access.RamAccesses++;
            if (Access.TraceRam) Note(AccessKind.Read, Address, 1, Memptr[Offset], null, false);
            return Memptr[Offset];
        }

        Access.UnmappedAccesses++;
        LastAccessUnmapped = true;
        Note(AccessKind.Read, Address, 1, 0xFF, null, true);
        return 0xFF;
    }

    public void WriteByte(uint Address, byte val)
    {
        MmioDevice dev = FindDevice(Address);
        if (dev != null)
        {
            Access.MmioAccesses++;
            Note(AccessKind.Write, Address, 1, val, dev, false);
            dev.Write(Address, 1, val);
            return;
        }

        uint Offset; byte[] Memptr;
        if (Translate(Address, 1, out Offset, out Memptr))
        {
            Access.RamAccesses++;
            if (Access.TraceRam) Note(AccessKind.Write, Address, 1, val, null, false);
            Memptr[Offset] = val;
            return;
        }

        Access.UnmappedAccesses++;
        LastAccessUnmapped = true;
        Note(AccessKind.Write, Address, 1, val, null, true);
    }

    public ushort ReadHalf(uint Address)
    {
        MmioDevice dev = FindDevice(Address);
        if (dev != null)
        {
            uint v = dev.Read(Address, 2);
            Access.MmioAccesses++;
            Note(AccessKind.Read, Address, 2, v, dev, false);
            return (ushort)v;
        }

        uint Offset; byte[] Memptr;
        if (Translate(Address, 2, out Offset, out Memptr))
        {
            ushort hword;
            if (LittleEndian)
                hword = (ushort)((uint)Memptr[Offset + 1] << 8 | Memptr[Offset]);
            else
                hword = (ushort)((uint)Memptr[Offset] << 8 | Memptr[Offset + 1]);
            Access.RamAccesses++;
            if (Access.TraceRam) Note(AccessKind.Read, Address, 2, hword, null, false);
            return hword;
        }

        Access.UnmappedAccesses++;
        LastAccessUnmapped = true;
        Note(AccessKind.Read, Address, 2, 0xFFFF, null, true);
        return 0xFFFF;
    }

    public void WriteHalf(uint Address, ushort val)
    {
        MmioDevice dev = FindDevice(Address);
        if (dev != null)
        {
            Access.MmioAccesses++;
            Note(AccessKind.Write, Address, 2, val, dev, false);
            dev.Write(Address, 2, val);
            return;
        }

        uint Offset; byte[] Memptr;
        if (Translate(Address, 2, out Offset, out Memptr))
        {
            if (LittleEndian)
            {
                Memptr[Offset] = (byte)(val & 0xff);
                Memptr[Offset + 1] = (byte)(val >> 8);
            }
            else
            {
                Memptr[Offset] = (byte)(val >> 8);
                Memptr[Offset + 1] = (byte)(val & 0xff);
            }
            Access.RamAccesses++;
            if (Access.TraceRam) Note(AccessKind.Write, Address, 2, val, null, false);
            return;
        }

        Access.UnmappedAccesses++;
        LastAccessUnmapped = true;
        Note(AccessKind.Write, Address, 2, val, null, true);
    }

    public uint ReadWord(uint Address)
    {
        MmioDevice dev = FindDevice(Address);
        if (dev != null)
        {
            uint v = dev.Read(Address, 4);
            Access.MmioAccesses++;
            Note(AccessKind.Read, Address, 4, v, dev, false);
            return v;
        }

        uint Offset; byte[] Memptr;
        if (Translate(Address, 4, out Offset, out Memptr))
        {
            uint word;
            if (LittleEndian)
            {
                word = (uint)Memptr[Offset + 3] << 24;
                word |= (uint)Memptr[Offset + 2] << 16;
                word |= (uint)Memptr[Offset + 1] << 8;
                word |= Memptr[Offset];
            }
            else
            {
                word = (uint)Memptr[Offset] << 24;
                word |= (uint)Memptr[Offset + 1] << 16;
                word |= (uint)Memptr[Offset + 2] << 8;
                word |= Memptr[Offset + 3];
            }
            Access.RamAccesses++;
            if (Access.TraceRam) Note(AccessKind.Read, Address, 4, word, null, false);
            return word;
        }

        Access.UnmappedAccesses++;
        LastAccessUnmapped = true;
        Note(AccessKind.Read, Address, 4, 0xFFFFFFFF, null, true);
        return 0xFFFFFFFF;
    }

    public void WriteWord(uint Address, uint val)
    {
        MmioDevice dev = FindDevice(Address);
        if (dev != null)
        {
            Access.MmioAccesses++;
            Note(AccessKind.Write, Address, 4, val, dev, false);
            dev.Write(Address, 4, val);
            return;
        }

        uint Offset; byte[] Memptr;
        if (Translate(Address, 4, out Offset, out Memptr))
        {
            if (LittleEndian)
            {
                Memptr[Offset + 3] = (byte)(val >> 24);
                Memptr[Offset + 2] = (byte)((val >> 16) & 0xff);
                Memptr[Offset + 1] = (byte)((val >> 8) & 0xff);
                Memptr[Offset] = (byte)(val & 0xff);
            }
            else
            {
                Memptr[Offset] = (byte)(val >> 24);
                Memptr[Offset + 1] = (byte)((val >> 16) & 0xff);
                Memptr[Offset + 2] = (byte)((val >> 8) & 0xff);
                Memptr[Offset + 3] = (byte)(val & 0xff);
            }
            Access.RamAccesses++;
            if (Access.TraceRam) Note(AccessKind.Write, Address, 4, val, null, false);
            return;
        }

        Access.UnmappedAccesses++;
        LastAccessUnmapped = true;
        Note(AccessKind.Write, Address, 4, val, null, true);
    }

    /// <summary>Instruction fetch: same as ReadWord but recorded as a fetch.</summary>
    public uint FetchWord(uint Address)
    {
        MmioDevice dev = FindDevice(Address);
        if (dev != null)
        {
            uint v = dev.Read(Address, 4);
            Access.Fetches++;
            Note(AccessKind.Fetch, Address, 4, v, dev, false);
            return v;
        }

        uint Offset; byte[] Memptr;
        if (Translate(Address, 4, out Offset, out Memptr))
        {
            uint word;
            if (LittleEndian)
            {
                word = (uint)Memptr[Offset + 3] << 24;
                word |= (uint)Memptr[Offset + 2] << 16;
                word |= (uint)Memptr[Offset + 1] << 8;
                word |= Memptr[Offset];
            }
            else
            {
                word = (uint)Memptr[Offset] << 24;
                word |= (uint)Memptr[Offset + 1] << 16;
                word |= (uint)Memptr[Offset + 2] << 8;
                word |= Memptr[Offset + 3];
            }
            Access.Fetches++;
            return word;
        }

        Access.UnmappedAccesses++;
        LastAccessUnmapped = true;
        Note(AccessKind.Fetch, Address, 4, 0xFFFFFFFF, null, true);
        return 0xFFFFFFFF;
    }

    #endregion

    #region Bulk access

    public void ReadBytes(uint Address, byte[] buffer, int offset, int length)
    {
        for (int i = 0; i < length; i++)
            buffer[offset + i] = ReadByte(Address + (uint)i);
    }

    public byte[] ReadBytes(uint Address, int length)
    {
        byte[] b = new byte[length];
        ReadBytes(Address, b, 0, length);
        return b;
    }

    public void WriteBytes(uint Address, byte[] buffer, int offset, int length)
    {
        for (int i = 0; i < length; i++)
            WriteByte(Address + (uint)i, buffer[offset + i]);
    }

    public void WriteBytes(uint Address, byte[] buffer)
    {
        WriteBytes(Address, buffer, 0, buffer.Length);
    }

    #endregion

    /// <summary>
    /// Load a raw dump into mapped memory. The destination must already be mapped
    /// (or must fit inside an unmapped region with BaseVAddr == 0, in which case it
    /// is mapped automatically).
    /// </summary>
    public bool LoadDump(uint Address, byte[] Buffer)
    {
        uint Offset; byte[] Memptr;
        bool Res = Translate(Address, Buffer.Length, out Offset, out Memptr);

        if (!Res)
        {
            // Try auto-mapping a free range that is big enough.
            MemRange free = null;
            for (int i = 0; i < ranges.Count; i++)
            {
                if (ranges[i].Mapped) continue;
                if (ranges[i].Size >= Buffer.Length) { free = ranges[i]; break; }
            }
            if (free == null) return false;
            free.BaseVAddr = Address;
            free.Mapped = true;
            Res = Translate(Address, Buffer.Length, out Offset, out Memptr);
            if (!Res) return false;
        }

        Array.Copy(Buffer, 0, Memptr, (int)Offset, Buffer.Length);
        return true;
    }
}
