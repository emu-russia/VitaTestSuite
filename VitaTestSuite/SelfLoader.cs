// Sce ELF Loader

// http://www.vitadevwiki.com/index.php?title=SELF_File_Format

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct SCEHeader
{
    public UInt32 magic;                 /* 53434500 = SCE\0 */
    public UInt32 version;               /* header version 3*/
    public UInt16 sdk_type;
    public UInt16 header_type;           /* 1 self, 2 unknown, 3 pkg */
    public UInt32 metadata_offset;       /* metadata offset */     // Offset to the checksums. Must be at least 20 bytes before the end of the header
    public UInt64 header_len;            /* self header length */
    public UInt64 elf_filesize;          /* ELF file length */
    public UInt64 self_filesize;         /* SELF file length */
    public UInt64 unknown;               /* UNKNOWN */
    public UInt64 self_offset;           /* SELF offset */
    public UInt64 appinfo_offset;        /* app info offset */
    public UInt64 elf_offset;            /* ELF #1 offset */
    public UInt64 phdr_offset;           /* program header offset */
    public UInt64 shdr_offset;           /* section header offset */
    public UInt64 section_info_offset;   /* section info offset */
    public UInt64 sceversion_offset;     /* version offset */
    public UInt64 controlinfo_offset;    /* control info offset */
    public UInt64 controlinfo_size;      /* control info size */
}

public class SCEHeaderWrapper
{
    public UInt32 _magic;
    public UInt32 magic
    {
        get { return _magic; }
        set { _magic = value; }
    }
    public UInt32 version; 
    public UInt16 sdk_type;
    public UInt16 header_type; 
    public UInt32 metadata_offset; 
    public UInt64 header_len; 
    public UInt64 elf_filesize; 
    public UInt64 self_filesize;
    public UInt64 unknown;
    public UInt64 self_offset; 
    public UInt64 appinfo_offset; 
    public UInt64 elf_offset;
    public UInt64 phdr_offset;
    public UInt64 shdr_offset;
    public UInt64 section_info_offset;
    public UInt64 sceversion_offset;
    public UInt64 controlinfo_offset;
    public UInt64 controlinfo_size; 

    public SCEHeaderWrapper ( SCEHeader header)
    {
        magic = header.magic;
    }
}

public class SelfLoader
{

}
