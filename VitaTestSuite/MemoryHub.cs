// Memory engine

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class MemRange
{
    public string name;
    public byte[] Memptr;
    public int Size;
    public uint BaseVAddr;
    public bool Mapped;
}

public class MemoryHub
{
    public bool LittleEndian = false;

    public List<MemRange> ranges = new List<MemRange>();

    public void AddMemory ( string tag, int size )
    {
        MemRange range = new MemRange();
        range.name = tag;
        range.Size = size;
        range.Memptr = new byte[size];
        range.BaseVAddr = 0;
        range.Mapped = false;
        ranges.Add(range);
    }

    public void MapMemory ( uint BaseVAddr, string tag )
    {
        for (int i = 0; i < ranges.Count; i++ )
        {
            if (ranges[i].name == tag)
            {
                ranges[i].Mapped = true;
                ranges[i].BaseVAddr = BaseVAddr;
                break;
            }
        }
    }

    private bool Translate ( uint VAddr, int Size, out uint Offset, out byte [] Memptr )
    {
        Offset = 0;
        Memptr = null;

        foreach ( MemRange range in ranges )
        {
            if (!range.Mapped)
                continue;

            if ( VAddr >= range.BaseVAddr && (VAddr + Size) < (range.BaseVAddr + range.Size) )
            {
                Offset = VAddr - range.BaseVAddr;
                Memptr = range.Memptr;
                return true;
            }
        }

        return false;
    }

    #region Cpu interface

    public byte ReadByte (uint Address )
    {
        uint Offset;
        byte[] Memptr;
        bool Res = Translate(Address, 1, out Offset, out Memptr);

        if (Res)
        {
            return Memptr[Offset];
        }
        else
            return 0xFF;
    }

    public void WriteByte (uint Address, byte val)
    {
        uint Offset;
        byte[] Memptr;
        bool Res = Translate(Address, 1, out Offset, out Memptr);

        if (Res)
        {
            Memptr[Offset] = val;
        }
    }

    public ushort ReadHalf (uint Address)
    {
        uint Offset;
        byte[] Memptr;
        bool Res = Translate(Address, 2, out Offset, out Memptr);

        if (Res)
        {
            ushort hword;

            if (LittleEndian)
            {
                hword = (ushort)((uint)Memptr[Offset + 1] << 8);
                hword |= (ushort)(Memptr[Offset]);
            }
            else
            {
                hword = (ushort)((uint)Memptr[Offset] << 8);
                hword |= (ushort)(Memptr[Offset + 1]);
            }

            return hword;
        }
        else
            return 0xFFFF;
    }

    public void WriteHalf(uint Address, ushort val)
    {
        uint Offset;
        byte[] Memptr;
        bool Res = Translate(Address, 2, out Offset, out Memptr);

        if (Res)
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
        }
    }

    public uint ReadWord(uint Address)
    {
        uint Offset;
        byte[] Memptr;
        bool Res = Translate(Address, 4, out Offset, out Memptr);

        if (Res)
        {
            uint word;

            if (LittleEndian)
            {
                word = (uint)((uint)Memptr[Offset + 3] << 24);
                word |= (uint)((uint)Memptr[Offset + 2] << 16);
                word |= (uint)((uint)Memptr[Offset + 1] << 8);
                word |= (uint)((uint)Memptr[Offset ]);
            }
            else
            {
                word = (uint)((uint)Memptr[Offset] << 24);
                word |= (uint)((uint)Memptr[Offset + 1] << 16);
                word |= (uint)((uint)Memptr[Offset + 2] << 8);
                word |= (uint)((uint)Memptr[Offset + 3] );
            }

            return word;
        }
        else
            return 0xFFFFFFFF;
    }

    public void WriteWord(uint Address, uint val)
    {
        uint Offset;
        byte[] Memptr;
        bool Res = Translate(Address, 4, out Offset, out Memptr);

        if (Res)
        {
            if (LittleEndian)
            {
                Memptr[Offset + 3] = (byte)(val >> 24);
                Memptr[Offset + 2] = (byte)((val >> 16) & 0xff);
                Memptr[Offset + 1] = (byte)((val >> 8) & 0xff);
                Memptr[Offset ] = (byte)(val & 0xff);
            }
            else
            {
                Memptr[Offset] = (byte)(val >> 24);
                Memptr[Offset + 1] = (byte)((val >> 16) & 0xff);
                Memptr[Offset + 2] = (byte)((val >> 8) & 0xff);
                Memptr[Offset + 3] = (byte)(val & 0xff);
            }
        }
    }

    #endregion 

    public bool LoadDump ( uint Address, byte [] Buffer )
    {
        uint Offset;
        byte[] Memptr;
        bool Res = Translate(Address, Buffer.Length, out Offset, out Memptr);

        if (Res)
        {
            Array.Copy(Buffer, Memptr, Buffer.Length);
            return true;
        }
        else
            return false;
    }

}
