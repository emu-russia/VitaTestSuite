// CoreAes.cs -- low level binary helpers shared by the VitaTestSuite loader layer.
//
// This file is deliberately self contained: it holds
//
//   1. BinUtil    - explicit little-endian readers (the whole SCE/SELF/PUP/ELF
//                   world is little-endian; System.BitConverter is host-endian
//                   so we do not rely on it).
//   2. CoreAes    - AES-128 CBC decryption and AES-CTR.
//                   Both are built on the BCL primitive only:
//                        Aes.Create() + CipherMode.ECB/CBC + PaddingMode.None
//                   `AesManaged` is NOT used because it does not exist on
//                   .NET (Core) 8, and this library has to compile both on
//                   .NET Framework 4.8 (the GUI) and on the net8.0 scratch
//                   harness.  Aes.Create() is available on both.
//                   AES-CTR is *not* in the BCL, so it is implemented here on
//                   top of the ECB block primitive, exactly matching
//                   PyCrypto's Counter.new(128, initial_value=<iv as big
//                   endian int>): the 16 byte counter block is the IV itself
//                   and the whole 128 bit value is incremented big-endian.
//   3. RawDeflate - zlib/raw DEFLATE decompression for SELF segments and for
//                   PUP package segments (see the comment on Inflate()).
//
// No WinForms, no Console: the file is usable head-less.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace VitaTestSuite.Core
{
    /// <summary>
    /// Explicit little-endian primitive readers over byte[]. Out of range reads
    /// return 0 so that container parsers can be defensive without throwing;
    /// callers that need to distinguish "0" from "missing" use
    /// <see cref="InRange"/> first.
    /// </summary>
    public static class BinUtil
    {
        public static bool InRange(byte[] d, long off, int len)
        {
            return d != null && off >= 0 && len >= 0 && off + len <= d.Length;
        }

        public static byte U8(byte[] d, long off)
        {
            if (!InRange(d, off, 1)) return 0;
            return d[off];
        }

        public static ushort U16(byte[] d, long off)
        {
            if (!InRange(d, off, 2)) return 0;
            return (ushort)(d[off] | (d[off + 1] << 8));
        }

        public static uint U32(byte[] d, long off)
        {
            if (!InRange(d, off, 4)) return 0;
            uint v = d[off];
            v |= (uint)d[off + 1] << 8;
            v |= (uint)d[off + 2] << 16;
            v |= (uint)d[off + 3] << 24;
            return v;
        }

        public static ulong U64(byte[] d, long off)
        {
            if (!InRange(d, off, 8)) return 0;
            return (ulong)U32(d, off) | ((ulong)U32(d, off + 4) << 32);
        }

        /// <summary>NUL terminated string, decoded as Latin-1 (never throws).</summary>
        public static string CStr(byte[] d, long off, int max)
        {
            if (d == null || off < 0 || off >= d.Length) return "";
            StringBuilder sb = new StringBuilder();
            for (long i = off; i < d.Length && i < off + max; i++)
            {
                if (d[i] == 0) break;
                sb.Append((char)d[i]);
            }
            return sb.ToString();
        }

        /// <summary>Hex string (no separators) -> bytes. Returns null on bad input.</summary>
        public static byte[] Hex(string hex)
        {
            if (hex == null) return null;
            StringBuilder sb = new StringBuilder(hex.Length);
            for (int i = 0; i < hex.Length; i++)
            {
                char c = hex[i];
                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')) sb.Append(c);
            }
            if ((sb.Length & 1) != 0) return null;
            byte[] r = new byte[sb.Length / 2];
            for (int i = 0; i < r.Length; i++)
                r[i] = byte.Parse(sb.ToString(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return r;
        }

        public static string ToHex(byte[] b, int offset, int count)
        {
            if (b == null) return "";
            if (offset < 0) offset = 0;
            if (count < 0 || offset + count > b.Length) count = b.Length - offset;
            StringBuilder sb = new StringBuilder(count * 2);
            for (int i = 0; i < count; i++) sb.Append(b[offset + i].ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        public static string ToHex(byte[] b)
        {
            return b == null ? "" : ToHex(b, 0, b.Length);
        }

        /// <summary>Classic hexdump ("00000000  00 11 ..  |ascii|"), for logs and the harness.</summary>
        public static string Dump(byte[] data, long offset, int count)
        {
            if (data == null) return "";
            if (offset < 0) offset = 0;
            if (offset > data.Length) return "";
            if (count < 0 || offset + count > data.Length) count = (int)(data.Length - offset);
            StringBuilder sb = new StringBuilder();
            for (int row = 0; row < count; row += 16)
            {
                int n = Math.Min(16, count - row);
                sb.Append((offset + row).ToString("x8", CultureInfo.InvariantCulture));
                sb.Append("  ");
                for (int i = 0; i < 16; i++)
                {
                    if (i < n) sb.Append(data[offset + row + i].ToString("x2", CultureInfo.InvariantCulture));
                    else sb.Append("  ");
                    sb.Append(i == 7 ? "  " : " ");
                }
                sb.Append(" |");
                for (int i = 0; i < n; i++)
                {
                    byte b = data[offset + row + i];
                    sb.Append(b >= 32 && b < 127 ? (char)b : '.');
                }
                sb.Append("|");
                if (row + 16 < count) sb.Append('\n');
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// AES helpers. CBC is used by the SCE metadata decryption, CTR by the
    /// SELF segment decryption (SCE "AES128CTR" encryption type).
    ///
    /// NOTE ON KEY SIZES: the SCE metadata keys in pup_fiction's tables are
    /// *32 byte* (AES-256) values -- e.g. keys.py's SELF/SECURE key
    /// 9D4E4CE92EA1C4576EB9601EC43EC03AAE8EC324ECF6DE01E918E61D2223EE55 is
    /// 64 hex characters -- while the NPDRM "keys" and the per-segment key vault
    /// entries are 16 byte (AES-128). Both must be passed to the block cipher
    /// unchanged; truncating a 32 byte key to 16 silently produces a wrong
    /// AES-128 decryption. Only the *IV* is normalised to 16 bytes.
    /// </summary>
    public static class CoreAes
    {
        public const int BlockSize = 16;

        /// <summary>Pad (or truncate) an IV to exactly 16 bytes.</summary>
        public static byte[] Normalize16(byte[] b)
        {
            byte[] r = new byte[16];
            if (b != null)
            {
                int n = b.Length < 16 ? b.Length : 16;
                Array.Copy(b, 0, r, 0, n);
            }
            return r;
        }

        /// <summary>
        /// Validate a key for the BCL AES implementation: 16, 24 and 32 byte
        /// keys are passed through unchanged (AES-128/192/256); anything else is
        /// zero padded/truncated to 16 bytes so a malformed table entry cannot
        /// throw deep inside the crypto stack.
        /// </summary>
        public static byte[] NormalizeKey(byte[] key)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (key.Length == 16 || key.Length == 24 || key.Length == 32) return key;
            return Normalize16(key);
        }

        /// <summary>
        /// Raw AES-ECB single block encryption (no padding). This is the
        /// primitive that AES-CTR is built on.
        /// </summary>
        public static void EcbEncryptBlock(ICryptoTransform ecbEncryptor, byte[] input, int inOff, byte[] output, int outOff)
        {
            ecbEncryptor.TransformBlock(input, inOff, BlockSize, output, outOff);
        }

        /// <summary>
        /// One AES-ECB block encryption with a fresh transform (used by the
        /// diagnostics; TransformFinalBlock is safe to call once per transform).
        /// </summary>
        public static byte[] EcbEncryptBlock(byte[] key, byte[] block16)
        {
            byte[] output = new byte[16];
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.Key = NormalizeKey(key);
                using (ICryptoTransform enc = aes.CreateEncryptor())
                {
                    enc.TransformBlock(block16, 0, BlockSize, output, 0);
                }
            }
            return output;
        }

        /// <summary>AES-CTR: keystream = E(counter), counter starts at <paramref name="iv"/>
        /// interpreted as a 128 bit big-endian integer and increments big-endian.</summary>
        public static byte[] CtrCrypt(byte[] key, byte[] iv, byte[] data)
        {
            return CtrCrypt(key, iv, data, 0, data == null ? 0 : data.Length);
        }

        public static byte[] CtrCrypt(byte[] key, byte[] iv, byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (count < 0 || offset < 0 || offset + count > data.Length)
                throw new ArgumentOutOfRangeException("count", "CTR region outside the buffer");

            byte[] output = new byte[count];
            if (count == 0) return output;

            // Build every counter block up front, then encrypt them in one
            // TransformBlock call (and consume its return value: a transform is
            // allowed to write fewer bytes than the input length, so the count is
            // checked and a per block fallback is used when it does not match).
            int blocks = (count + BlockSize - 1) / BlockSize;
            byte[] counters = new byte[blocks * BlockSize];
            byte[] counter = Normalize16(iv);
            for (int i = 0; i < blocks; i++)
            {
                Array.Copy(counter, 0, counters, i * BlockSize, BlockSize);
                IncrementBigEndian(counter);
            }

            byte[] keystream = new byte[blocks * BlockSize];
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.Key = NormalizeKey(key);

                int written = 0;
                using (ICryptoTransform enc = aes.CreateEncryptor())
                {
                    try { written = enc.TransformBlock(counters, 0, counters.Length, keystream, 0); }
                    catch (CryptographicException) { written = 0; }
                }

                if (written != counters.Length)
                {
                    // Fallback: one finalised transform per block always produces
                    // the full block, whatever the platform implementation does.
                    for (int i = 0; i < blocks; i++)
                    {
                        using (ICryptoTransform one = aes.CreateEncryptor())
                        {
                            byte[] b = one.TransformFinalBlock(counters, i * BlockSize, BlockSize);
                            Array.Copy(b, 0, keystream, i * BlockSize, BlockSize);
                        }
                    }
                }
            }

            for (int i = 0; i < count; i++)
                output[i] = (byte)(data[offset + i] ^ keystream[i]);
            return output;
        }

        /// <summary>Increment a 16 byte big-endian counter (carry propagates right to left).</summary>
        public static void IncrementBigEndian(byte[] counter)
        {
            for (int i = counter.Length - 1; i >= 0; i--)
            {
                if (++counter[i] != 0) break;
            }
        }

        /// <summary>
        /// AES-128-CBC decryption of <paramref name="count"/> bytes starting at
        /// <paramref name="offset"/>. A trailing partial block is zero padded
        /// (PyCrypto would raise instead; a SCE metadata blob is always a
        /// whole number of blocks, so this only matters for corrupt input).
        /// </summary>
        public static byte[] CbcDecrypt(byte[] key, byte[] iv, byte[] data)
        {
            return CbcDecrypt(key, iv, data, 0, data == null ? 0 : data.Length);
        }

        public static byte[] CbcDecrypt(byte[] key, byte[] iv, byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (count < 0 || offset < 0 || offset + count > data.Length)
                throw new ArgumentOutOfRangeException("count", "CBC region outside the buffer");

            int padded = ((count + BlockSize - 1) / BlockSize) * BlockSize;
            byte[] input = new byte[padded];
            Array.Copy(data, offset, input, 0, count);
            byte[] output = new byte[padded];

            if (padded == 0) return output;

            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = NormalizeKey(key);
                aes.IV = Normalize16(iv);
                using (ICryptoTransform dec = aes.CreateDecryptor())
                {
                    dec.TransformBlock(input, 0, padded, output, 0);
                }
            }
            return output;
        }
    }

    /// <summary>
    /// DEFLATE decompression for SCE segments.
    ///
    /// The SELF segments compressed by the SCE tools are zlib streams (RFC1950:
    /// a 2 byte header 78 xx followed by a raw DEFLATE stream and an Adler-32
    /// trailer). System.IO.Compression.DeflateStream only understands the *raw*
    /// DEFLATE stream, so the 2 byte zlib header has to be skipped; the trailing
    /// Adler-32 is simply left unread by DeflateStream.
    ///
    /// Inflate() auto-detects the header and, if the detected mode fails,
    /// retries both ways, so it cannot be broken by a stream that turns out to
    /// be raw DEFLATE after all. `mode` reports what was actually used, which
    /// the harness prints.
    /// </summary>
    public static class RawDeflate
    {
        /// <summary>zlib header check: CM==8 (deflate) and (CMF*256+FLG)%31==0.</summary>
        public static bool HasZlibHeader(byte[] data, int offset, int count)
        {
            if (data == null || count < 2 || offset < 0 || offset + 2 > data.Length) return false;
            int cmf = data[offset];
            int flg = data[offset + 1];
            return (cmf & 0x0F) == 8 && (((cmf << 8) | flg) % 31) == 0;
        }

        /// <summary>Inflate with zlib/raw auto detection. Throws InvalidDataException when both fail.</summary>
        public static byte[] Inflate(byte[] data, int offset, int count)
        {
            string mode;
            return Inflate(data, offset, count, out mode);
        }

        public static byte[] Inflate(byte[] data, int offset, int count, out string mode)
        {
            bool zlib = HasZlibHeader(data, offset, count);
            byte[] first = TryInflate(data, offset + (zlib ? 2 : 0), count - (zlib ? 2 : 0));
            if (first != null)
            {
                mode = zlib ? "zlib (2 byte header stripped)" : "raw deflate";
                return first;
            }

            // Fall back to the other interpretation (also covers a 0 byte result).
            byte[] alt = TryInflate(data, offset, count);
            if (alt != null)
            {
                mode = "raw deflate (zlib autodetect retry)";
                return alt;
            }
            alt = TryInflate(data, offset + 2, count - 2);
            if (alt != null)
            {
                mode = "zlib (2 byte header stripped, autodetect retry)";
                return alt;
            }

            mode = "failed";
            throw new InvalidDataException("deflate stream could not be decompressed (neither zlib nor raw)");
        }

        private static byte[] TryInflate(byte[] data, int offset, int count)
        {
            if (count <= 0 || offset < 0 || offset + count > data.Length) return null;
            try
            {
                using (MemoryStream ms = new MemoryStream(data, offset, count, false))
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                using (MemoryStream outp = new MemoryStream())
                {
                    ds.CopyTo(outp);
                    if (outp.Length == 0) return null;
                    return outp.ToArray();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
