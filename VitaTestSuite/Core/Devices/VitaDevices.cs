// Memory-mapped device models for the PS Vita internal buses.
//
// Every register access goes through MemoryHub -> MmioDevice and is recorded in
// MemoryHub.Access (see AccessLog). The models are deliberately "permissive":
// where real hardware would need an external agent (ARM, debugger, eMMC) the
// model exposes a configurable register so a boot image can be walked step by
// step. Use the "devset"/"devget"/"devices" commands to poke them.

using System;
using System.Collections.Generic;
using System.Globalization;
using VitaTestSuite.Core;

namespace VitaTestSuite.Core.Devices
{
    /// <summary>A device built from a plain register file, with optional read/write hooks.</summary>
    public class RegFileDevice : MmioDevice
    {
        protected readonly Dictionary<uint, uint> Regs = new Dictionary<uint, uint>();

        public RegFileDevice(string name, uint baseAddr, uint size)
        {
            Name = name;
            Base = baseAddr;
            Size = size;
        }

        public void Define(uint addr, string name, uint reset)
        {
            RegisterNames[addr] = name;
            Regs[addr] = reset;
        }

        public uint Peek(uint addr)
        {
            uint v;
            return Regs.TryGetValue(addr, out v) ? v : 0;
        }

        public void Poke(uint addr, uint value)
        {
            Regs[addr] = value;
        }

        /// <summary>Set/add bits. addr must be a defined register.</summary>
        public void SetBits(uint addr, uint mask, bool value)
        {
            uint old = Peek(addr);
            Poke(addr, value ? (old | mask) : (old & ~mask));
        }

        public override void Reset()
        {
            // re-run the Define() defaults by remembering them
            foreach (KeyValuePair<uint, uint> kv in Defaults)
                Regs[kv.Key] = kv.Value;
        }

        private readonly Dictionary<uint, uint> Defaults = new Dictionary<uint, uint>();

        /// <summary>Define with a reset value that is remembered by Reset().</summary>
        public void DefineReset(uint addr, string name, uint reset)
        {
            Define(addr, name, reset);
            Defaults[addr] = reset;
        }

        public override uint Read(uint addr, int size)
        {
            uint v = ReadReg(addr, size);
            return v;
        }

        public override void Write(uint addr, int size, uint value)
        {
            WriteReg(addr, size, value);
        }

        protected virtual uint ReadReg(uint addr, int size)
        {
            uint v = Peek(addr);
            switch (size)
            {
                case 1: return v & 0xFF;
                case 2: return v & 0xFFFF;
                default: return v;
            }
        }

        protected virtual void WriteReg(uint addr, int size, uint value)
        {
            Poke(addr, value);
        }

        public override string Summary
        {
            get { return Name + " @ 0x" + Base.ToString("X8") + " size=0x" + Size.ToString("X") + " regs=" + RegisterNames.Count; }
        }
    }

    /// <summary>
    /// CMeP keyring controller (0xE0030000). Key material written through this
    /// window is captured so it can be inspected with the "keyring" command.
    /// </summary>
    public class KeyringDevice : RegFileDevice
    {
        public const uint NewValue0 = 0xE0030000;
        public const uint SetValueTrigger = 0xE0030020;
        public const uint ClearFlags = 0xE0030024;
        public const uint QueryFlagsRequest = 0xE0030028;
        public const uint QueryFlagsResponse = 0xE003002C;

        /// <summary>Value returned by the flags query: 3 means "all flags clear".</summary>
        public uint FlagsResponse = 3;

        /// <summary>Captured 256-bit key material, indexed by key index.</summary>
        public Dictionary<uint, uint[]> Keys = new Dictionary<uint, uint[]>();

        /// <summary>Every clear-flags write, in order (index, raw value).</summary>
        public List<KeyValuePair<uint, uint>> ClearHistory = new List<KeyValuePair<uint, uint>>();

        public long KeyWrites;

        public KeyringDevice() : base("CMeP.Keyring", 0xE0030000, 0x1000)
        {
            DefineReset(0xE0030000, "KeyringNewValue[0]", 0);
            DefineReset(0xE0030004, "KeyringNewValue[1]", 0);
            DefineReset(0xE0030008, "KeyringNewValue[2]", 0);
            DefineReset(0xE003000C, "KeyringNewValue[3]", 0);
            DefineReset(0xE0030010, "KeyringNewValue[4]", 0);
            DefineReset(0xE0030014, "KeyringNewValue[5]", 0);
            DefineReset(0xE0030018, "KeyringNewValue[6]", 0);
            DefineReset(0xE003001C, "KeyringNewValue[7]", 0);
            DefineReset(SetValueTrigger, "KeyringSetValueTrigger", 0);
            DefineReset(ClearFlags, "KeyringClearFlags", 0);
            DefineReset(QueryFlagsRequest, "KeyringQueryFlagsRequest", 0);
            DefineReset(QueryFlagsResponse, "KeyringQueryFlagsResponse", FlagsResponse);
        }

        public override void Write(uint addr, int size, uint value)
        {
            if (addr == SetValueTrigger)
            {
                uint index = value & 0x1FFF;
                uint[] key = new uint[8];
                for (int i = 0; i < 8; i++) key[i] = Peek(NewValue0 + (uint)i * 4);
                Keys[index] = key;
                KeyWrites++;
                Poke(SetValueTrigger, value);
                if (Log != null)
                {
                    Log.Log("keyring", string.Format(CultureInfo.InvariantCulture,
                        "keyring: set value index=0x{0:X} (256-bit material captured)", index));
                }
                return;
            }

            if (addr == ClearFlags)
            {
                ClearHistory.Add(new KeyValuePair<uint, uint>(value & 0x1FFF, value));
                Poke(ClearFlags, Peek(ClearFlags) | value);
                return;
            }

            if (addr == QueryFlagsRequest)
            {
                Poke(QueryFlagsRequest, value);
                Poke(QueryFlagsResponse, FlagsResponse);
                return;
            }

            Poke(addr, value);
        }

        public ILogSink Log;
    }

    /// <summary>
    /// CMeP &lt;-&gt; ARM / debugger mailbox block 0xE0000000.
    /// The ARM side is emulated by pre-loading / injecting values.
    /// </summary>
    public class MailboxDevice : RegFileDevice
    {
        public const uint CmepToArmStatus = 0xE0000000;
        public const uint ArmToCmepCommand = 0xE0000010;
        public const uint CmepToDebugger = 0xE0000020;
        public const uint DebuggerToCmep = 0xE0000028;
        public const uint DebuggerToCmep2 = 0xE0000060;

        /// <summary>Last value the emulated core wrote to CmepToArmStatus (for the GUI).</summary>
        public uint LastStatus;

        /// <summary>Set to simulate an ARM command; the CMeP command loop tests bit 0.</summary>
        public bool AutoCommand;

        public MailboxDevice() : base("CMeP.Mailbox", 0xE0000000, 0x100)
        {
            DefineReset(CmepToArmStatus, "MailboxCmepToArm.Status", 0);
            DefineReset(0xE0000004, "MailboxCmepToArm.Status2", 0);
            DefineReset(0xE0000008, "MailboxCmepToArm.Status3", 0);
            DefineReset(0xE000000C, "MailboxCmepToArm.Status4", 0);
            DefineReset(ArmToCmepCommand, "MailboxArmToCmep.Command", 0);
            DefineReset(0xE0000014, "MailboxArmToCmep.Func0", 0);
            DefineReset(0xE0000018, "MailboxArmToCmep.Func1", 0);
            DefineReset(0xE000001C, "MailboxArmToCmep.Func2", 0);
            DefineReset(CmepToDebugger, "MailboxCmepToDebugger", 0);
            DefineReset(0xE0000024, "MailboxCmepToDebugger+0x4", 0);
            DefineReset(DebuggerToCmep, "MailboxDebuggerToCmep", 0);
            DefineReset(0xE000002C, "MailboxDebuggerToCmep+0x4", 0);
            DefineReset(DebuggerToCmep2, "MailboxDebuggerToCmep2", 0);
            DefineReset(0xE0000064, "MailboxDebuggerToCmep2+0x4", 0);
        }

        public override void Write(uint addr, int size, uint value)
        {
            if (addr == CmepToArmStatus) LastStatus = value;
            Poke(addr, value);
        }
    }

    /// <summary>Bigmac (AES/SHA) crypto engine window 0xE0050000.</summary>
    public class BigmacDevice : RegFileDevice
    {
        public const uint Cmd = 0xE0050000;
        public const uint Function = 0xE005000C;
        public const uint Status = 0xE0050024;
        public const uint Result = 0xE005003C;

        /// <summary>Bit 0 = busy. 0 keeps polling loops terminating.</summary>
        public uint StatusValue;

        public BigmacDevice() : base("CMeP.Bigmac", 0xE0050000, 0x100)
        {
            DefineReset(Cmd, "Bigmac cmd", 0);
            DefineReset(0xE0050004, "Bigmac cmd+0x4", 0);
            DefineReset(0xE0050008, "Bigmac cmd+0x8", 0);
            DefineReset(Function, "Bigmac function", 0);
            DefineReset(0xE0050010, "Bigmac function+0x4", 0);
            DefineReset(0xE0050014, "Bigmac function+0x8", 0);
            DefineReset(0xE005001C, "Bigmac function+0x10", 0);
            DefineReset(Status, "Bigmac status (bit0 busy)", 0);
            DefineReset(0xE0050028, "Bigmac ctx", 0);
            DefineReset(0xE0050030, "Bigmac ctx+0x8", 0);
            DefineReset(Result, "Bigmac result", 0);
            DefineReset(0xE0050200, "Bigmac context", 0);
        }

        public override uint Read(uint addr, int size)
        {
            if (addr == Status) return StatusValue;
            return base.Read(addr, size);
        }

        public override void Write(uint addr, int size, uint value)
        {
            if (addr == Status) { StatusValue = value; Poke(Status, value); return; }
            base.Write(addr, size, value);
        }
    }

    /// <summary>Bignum / RSA engine window 0xE0040800.</summary>
    public class BignumDevice : RegFileDevice
    {
        public BignumDevice() : base("CMeP.Bignum", 0xE0040800, 0x800)
        {
            DefineReset(0xE0040800, "BignumStatus", 0);
            DefineReset(0xE0040804, "Bignum[0804] status", 0);
            DefineReset(0xE0040808, "Bignum[0808] result", 0);
            DefineReset(0xE0040900, "Bignum output", 0);
            DefineReset(0xE0040B00, "Bignum operand", 0);
            DefineReset(0xE0040C00, "Bignum operand2", 0);
            DefineReset(0xE0041000, "Bignum exponent", 0);
            DefineReset(0xE0041080, "Bignum modulus", 0);
        }
    }

    /// <summary>eMMC crypto window 0xE0070000.</summary>
    public class EmmcCryptoDevice : RegFileDevice
    {
        public EmmcCryptoDevice() : base("CMeP.EmmcCrypto", 0xE0070000, 0x100)
        {
            DefineReset(0xE0070000, "EmmcCryptoToggle", 0);
            DefineReset(0xE0070008, "EmmcCrypto keyring indexes", 0);
        }
    }

    /// <summary>CMeP internal flags / timers 0xE0020000.</summary>
    public class CmepFlagsDevice : RegFileDevice
    {
        public CmepFlagsDevice() : base("CMeP.Flags", 0xE0020000, 0x1000)
        {
            DefineReset(0xE0020000, "Cmep flags (arm2cry)", 0);
            DefineReset(0xE0020020, "Cmep timer/work state", 0);
        }
    }

    /// <summary>CMeP strap / JIG detect window 0xE0062000.</summary>
    public class CmepStrapDevice : RegFileDevice
    {
        public CmepStrapDevice() : base("CMeP.Strap", 0xE0060000, 0x10000)
        {
            DefineReset(0xE0062020, "JP strap / JIG detect", 0);
            DefineReset(0xE0064060, "MMIO_E0064060", 0);
        }
    }

    /// <summary>
    /// GPIO / external-agent handshake 0xE20A0000. Bit 4 of +0x04 models the
    /// debugger handshake line: it is set on reset and clears itself after the
    /// first read so that handshake polling loops terminate.
    /// </summary>
    public class GpioDevice : RegFileDevice
    {
        public const uint Set = 0xE20A0008;
        public const uint Clear = 0xE20A000C;

        public GpioDevice() : base("CMeP.GPIO", 0xE20A0000, 0x100)
        {
            DefineReset(0xE20A0000, "GPIO data", 0);
            DefineReset(0xE20A0004, "GPIO handshake/state", 0x10);
            DefineReset(Set, "GPIO set", 0);
            DefineReset(Clear, "GPIO clear", 0);
        }

        public override uint Read(uint addr, int size)
        {
            if (addr == 0xE20A0004)
            {
                uint v = Peek(addr);
                // model the external debugger releasing the handshake line
                if ((v & 0x10) != 0) Poke(addr, v & ~0x10u);
                return v;
            }
            return base.Read(addr, size);
        }

        public override void Write(uint addr, int size, uint value)
        {
            if (addr == Set) { Poke(0xE20A0000, Peek(0xE20A0000) | value); Poke(Set, value); return; }
            if (addr == Clear) { Poke(0xE20A0000, Peek(0xE20A0000) & ~value); Poke(Clear, value); return; }
            base.Write(addr, size, value);
        }
    }

    /// <summary>
    /// CMeP "SC" (secure controller / Syscon bridge) register window 0xE3100000.
    /// Plain storage: the interesting part is the access log.
    /// </summary>
    public class SecureControllerDevice : RegFileDevice
    {
        public SecureControllerDevice() : base("CMeP.SecureCtl", 0xE3100000, 0x10000)
        {
            DefineReset(0xE3100124, "SC 0xE3100124", 0);
            DefineReset(0xE31010A0, "SC 0xE31010A0 req", 0);
            DefineReset(0xE31010A4, "SC 0xE31010A4 ack", 0);
            DefineReset(0xE3101100, "SC 0xE3101100", 0);
            DefineReset(0xE3101190, "SC 0xE3101190", 0);
            DefineReset(0xE31020A0, "SC 0xE31020A0 req", 0);
            DefineReset(0xE31020A4, "SC 0xE31020A4 ack", 0);
            DefineReset(0xE3102100, "SC 0xE3102100", 0);
            DefineReset(0xE3103040, "SC 0xE3103040", 0);
            DefineReset(0xE3103050, "SC 0xE3103050", 0);
        }
    }

    /// <summary>
    /// Ernie (Renesas RL78 syscon) peripheral area. The RL78 uses a 16-bit
    /// address space; SFRs live at 0xFF00-0xFFFF and are reached through the
    /// "SFR" addressing mode with mirroring, so the hub maps them at 0xFFFFF000
    /// (the top of the 32-bit space) as well.
    /// </summary>
    public class ErnieSfrDevice : RegFileDevice
    {
        public ErnieSfrDevice(uint baseAddr, uint size, string name) : base(name, baseAddr, size)
        {
            // generic SFR block: names are filled in on demand
        }

        public override uint Read(uint addr, int size)
        {
            uint v = Peek(addr);
            if (size == 1) return v & 0xFF;
            if (size == 2) return v & 0xFFFF;
            return v;
        }

        public override void Write(uint addr, int size, uint value)
        {
            Poke(addr, value);
        }
    }

    /// <summary>Generic logging window used to observe unknown MMIO areas.</summary>
    public class SpyDevice : RegFileDevice
    {
        public SpyDevice(string name, uint baseAddr, uint size) : base(name, baseAddr, size) { }

        public override uint Read(uint addr, int size)
        {
            uint v = Peek(addr);
            if (size == 1) return v & 0xFF;
            if (size == 2) return v & 0xFFFF;
            return v;
        }
    }

    public static class DeviceSetup
    {
        /// <summary>
        /// Install the devices that belong to the given architecture. Returns the
        /// installed list (also available through hub.devices).
        /// </summary>
        public static List<MmioDevice> Install(MemoryHub hub, CpuArch arch, ILogSink log)
        {
            List<MmioDevice> added = new List<MmioDevice>();

            if (arch == CpuArch.MeP)
            {
                KeyringDevice keyring = new KeyringDevice();
                keyring.Log = log;
                added.Add(keyring);
                added.Add(new MailboxDevice());
                added.Add(new BigmacDevice());
                added.Add(new BignumDevice());
                added.Add(new EmmcCryptoDevice());
                added.Add(new CmepFlagsDevice());
                added.Add(new CmepStrapDevice());
                added.Add(new GpioDevice());
                added.Add(new SecureControllerDevice());
            }
            else if (arch == CpuArch.Arm)
            {
                // The ARM side sees the SoC MMIO. The CMeP windows are mirrored,
                // so install them too: writes there are what the Secure Kernel does.
                added.Add(new MailboxDevice());
                added.Add(new SecureControllerDevice());
                added.Add(new SpyDevice("ARM.Pervasive", 0xE3200000, 0x1000));
                added.Add(new SpyDevice("ARM.SysconBridge", 0xE2000000, 0x1000));
                added.Add(new SpyDevice("ARM.Uart", 0xE2030000, 0x100));
            }
            else if (arch == CpuArch.Rl78)
            {
                // RL78 SFR area is the last 256 bytes of the 1 MiB address space.
                added.Add(new ErnieSfrDevice(0x000FFF00, 0x100, "Ernie.SFR"));
                added.Add(new ErnieSfrDevice(0xFFFFF00, 0x100, "Ernie.SFR.mirror"));
            }

            foreach (MmioDevice d in added) hub.AddDevice(d);
            return added;
        }
    }
}
