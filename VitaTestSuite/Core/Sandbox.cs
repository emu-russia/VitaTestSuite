// Sandbox: one place that owns the memory map, the devices and the CPU core.
//
// The GUI and the command processor both drive the sandbox; nothing else needs
// to know how the memory map of a given core looks.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VitaTestSuite.Core.Devices;

namespace VitaTestSuite.Core
{
    public class Sandbox
    {
        public MemoryHub Mem = new MemoryHub();
        public ILogSink Log;
        public CpuCore Cpu;

        public ArmCore Arm;
        public MePCore MeP;
        public Rl78Core Rl78;

        public CpuArch Arch = CpuArch.Unknown;

        /// <summary>Description of the currently loaded image.</summary>
        public string CurrentImage = "";
        public uint CurrentEntry;

        public long Steps;

        /// <summary>Path of the loaded file (for "reload").</summary>
        public string CurrentFile;

        /// <summary>Optional NID database used to annotate imports/exports.</summary>
        public NidDatabase Nids;

        /// <summary>Module import/export tables of the last loaded image (may be null).</summary>
        public VitaModule Module;

        /// <summary>ELF header of the last loaded image (may be null).</summary>
        public ElfImage LastElf;

        /// <summary>Raw bytes of the last loaded image.</summary>
        public byte[] LastData;

        public SceKeys Keys = SceKeys.Default();

        /// <summary>Instruction trace callback, invoked for every executed step (may be null).</summary>
        public Action<StepResult, CpuCore> OnStep;

        public Sandbox(ILogSink log)
        {
            Log = log;
            Mem.OnAccess = null;
        }

        private void Emit(string category, string text)
        {
            if (Log != null) Log.Log(category, text);
        }

        /// <summary>
        /// ARM ELF entry points use bit 0 as the Thumb selector. Returns true when the
        /// core must be switched to Thumb state (apply *after* Cpu.Reset()).
        /// </summary>
        private bool NormalizeEntry(ref uint entry)
        {
            if (Arch == CpuArch.Arm && (entry & 1u) != 0)
            {
                uint plain = entry & ~1u;
                Emit("loader", "entry point 0x" + entry.ToString("X8") + " has bit 0 set: starting in Thumb state at 0x" +
                    plain.ToString("X8"));
                entry = plain;
                return Arm != null;
            }

            return false;
        }

        #region Architecture / memory map

        /// <summary>
        /// Configure the sandbox for an architecture: clears memory, sets up the
        /// default map of that chip and installs its MMIO devices.
        /// </summary>
        public void SetArch(CpuArch arch)
        {
            Arch = arch;
            Cpu = null; Arm = null; MeP = null; Rl78 = null;

            Mem = new MemoryHub();
            Mem.LittleEndian = true;
            Mem.Access.DeviceNames = new string[0];
            DeviceSetup.Install(Mem, arch, Log);

            switch (arch)
            {
                case CpuArch.Arm:
                    // Kermit (Cortex-A9 MPCore) memory map (see the Venezia/physical memory pages of the vita wiki).
                    Mem.AddMemory("arm_rom", 0x10000, 0x00000000, "ARM boot/exception vectors");
                    Mem.AddMemory("arm_sram", 0x40000, 0x1F000000, "on-chip SRAM window");
                    Mem.AddMemory("arm_main", 0x4000000, 0x80000000, "main DRAM (64 MiB window)");
                    Mem.AddMemory("arm_priv", 0x4000000, 0x40000000, "private/protected memory window");
                    Arm = new ArmCore(Mem, Log);
                    Cpu = Arm;
                    break;

                case CpuArch.MeP:
                    // CMeP ("F00D") / Venezia MPE.
                    Mem.AddMemory("cmep_ram", 0x20000, 0x00040000, "CMeP RAM / vector base / next stage");
                    Mem.AddMemory("cmep_rom", 0x4000, 0x0005C000, "first loader window (16 KiB)");
                    Mem.AddMemory("cmep_priv", 0x20000, 0x00800000, "CMeP private RAM (secure kernel / modules)");
                    MeP = new MePCore(Mem, Log);
                    Cpu = MeP;
                    break;

                case CpuArch.Rl78:
                    // Ernie (Renesas RL78 syscon), 1 MiB address space.
                    Mem.AddMemory("Ernie_flash", 0x100000, 0x00000000, "Ernie code/data flash image");
                    Rl78 = new Rl78Core(Mem, Log);
                    Cpu = Rl78;
                    break;
            }

            Mem.ResetDevices();
            Steps = 0;
            Emit("loader", "Sandbox configured for " + arch + " (" + Mem.ranges.Count + " memory regions, " +
                Mem.devices.Count + " devices)");
        }

        /// <summary>Venezia MPE profile: a MeP-c5 core with the IVC2 coprocessor, SPRAM at 0x80000000.</summary>
        public void SetVenezia()
        {
            SetArch(CpuArch.MeP);
            MeP.Profile = MePProfile.Venezia;
            Mem.AddMemory("vnz_spram", 0x40000, 0x80000000, "Venezia SPRAM / image region");
            Emit("loader", "Venezia MPE profile enabled (IVC2 VLIW recognition on)");
        }

        #endregion

        #region Image loading

        public class LoadResult
        {
            public bool Ok;
            public string Message = "";
            public ImageInfo Info;
        }

        /// <summary>
        /// Identify and load a file into the sandbox. When <paramref name="address"/> is
        /// given (or the image is a raw binary) the bytes are copied verbatim, otherwise
        /// the ELF/SELF/PUP pipeline is used.
        /// </summary>
        public LoadResult LoadFile(string path, long address = -1)
        {
            LoadResult res = new LoadResult();

            if (!File.Exists(path))
            {
                res.Message = "file not found: " + path;
                return res;
            }

            byte[] data = File.ReadAllBytes(path);
            return LoadBytes(data, path, address);
        }

        public LoadResult LoadBytes(byte[] data, string name, long address = -1)
        {
            LoadResult res = new LoadResult();

            ImageInfo info = ImageLoader.Identify(data, name);
            res.Info = info;
            Emit("loader", name + ": " + info.Describe);

            // Raw blobs loaded at an explicit address (the "load" command path).
            bool explicitAddress = address >= 0;

            if (info.Kind == ImageKind.RawBinary || explicitAddress)
            {
                uint addr = explicitAddress ? (uint)address : info.LoadAddress;
                if (addr == 0 && info.Kind == ImageKind.RawBinary && info.Arch != CpuArch.Rl78)
                {
                    // no hint: fall back to the architecture default
                    addr = DefaultRawAddress(Arch);
                }

                Mem.EnsureRamFor(addr, data.Length, "raw");
                if (!Mem.LoadDump(addr, data))
                {
                    res.Message = "could not map 0x" + data.Length.ToString("X") + " bytes at 0x" + addr.ToString("X8");
                    return res;
                }

                uint rawTarget = addr;
                bool rawThumb = NormalizeEntry(ref rawTarget);
                CurrentEntry = rawTarget;
                Cpu.Reset(CurrentEntry);
                if (rawThumb) Arm.Thumb = true;
                LastData = data;
                Module = null;
                LastElf = null;

                if (Arch == CpuArch.MeP)
                    InitMePStack(addr + (uint)data.Length);

                CurrentImage = string.Format(CultureInfo.InvariantCulture,
                    "{0}: raw binary, {1} bytes at 0x{2:X8} ({3})", Path.GetFileName(name), data.Length, addr, Arch);
                res.Ok = true;
                res.Message = CurrentImage;
                CurrentFile = name;
                Emit("loader", "Loaded " + CurrentImage);
                return res;
            }

            // Structured image: let the loader layer map and fill memory.
            uint entry;
            try
            {
                entry = ImageLoader.Load(Mem, data, name, Keys, Log, info);
            }
            catch (Exception ex)
            {
                res.Message = "loader exception: " + ex.Message;
                Emit("loader", res.Message);
                return res;
            }

            if (info.Arch != CpuArch.Unknown && info.Arch != Arch)
            {
                Emit("loader", "image architecture is " + info.Arch + ", switching sandbox from " + Arch);
                SetArch(info.Arch);
                // re-load now that the right devices/map exist
                entry = ImageLoader.Load(Mem, data, name, Keys, Log, info);
            }

            if (entry == 0 && info.EntryPoint == 0)
            {
                res.Message = "loader could not produce an entry point";
                return res;
            }

            uint target = entry != 0 ? entry : info.EntryPoint;
            bool thumb = NormalizeEntry(ref target);
            CurrentEntry = target;
            Cpu.Reset(CurrentEntry);
            if (thumb) Arm.Thumb = true;
            if (Arch == CpuArch.Arm) InitArmStack();
            CurrentImage = string.Format(CultureInfo.InvariantCulture,
                "{0}: {1}, entry 0x{2:X8}", Path.GetFileName(name), info.Kind, CurrentEntry);
            CurrentFile = name;
            LastData = data;
            ParseModuleInfo(data);
            res.Ok = true;
            res.Message = CurrentImage;
            Emit("loader", "Loaded " + CurrentImage);
            return res;
        }

        /// <summary>
        /// Parse the Vita module import/export tables of an ELF or SELF image so that
        /// the "modinfo" command can annotate them with names from the NID database.
        /// </summary>
        private void ParseModuleInfo(byte[] data)
        {
            Module = null;
            LastElf = null;

            try
            {
                byte[] elf = data;

                if (SceSelf.IsSce(data))
                {
                    SceSelf self = SceSelf.Parse(data);
                    elf = self.ExtractElf(data, Keys, Log);
                }

                if (elf == null || !ElfImage.IsElf(elf)) return;

                ElfImage img = ElfImage.Parse(elf);
                LastElf = img;
                Module = VitaModule.Parse(elf, img);

                long miOff = img.ModuleInfoOffset();
                if (Module != null && miOff >= 0 && img.Segments.Count > 0)
                {
                    uint miAddr = img.Segments[0].VAddr + (uint)miOff;
                    if (miAddr == CurrentEntry)
                    {
                        Emit("loader", "NOTE: entry 0x" + CurrentEntry.ToString("X8") +
                            " is the SceModuleInfo structure, not executable code. Use 'modinfo' to list the " +
                            "exported functions and 'setreg pc <address>' (or 'dis <address>') to explore them.");
                    }
                }
            }
            catch (Exception ex)
            {
                Emit("loader", "module table parsing failed: " + ex.Message);
            }
        }

        private static uint DefaultRawAddress(CpuArch arch)
        {
            switch (arch)
            {
                case CpuArch.MeP: return 0x5C000;      // first loader window
                case CpuArch.Arm: return 0x80000000;   // main DRAM
                case CpuArch.Rl78: return 0x00000000;  // flash base
                default: return 0x80000000;
            }
        }

        /// <summary>
        /// A loaded ARM module expects its caller to provide a stack. In the sandbox
        /// there is no caller, so the first PUSH would target unmapped memory; seed SP
        /// with the top of the main DRAM window when it is still zero.
        /// </summary>
        public void InitArmStack()
        {
            if (Arm == null) return;
            if (Arm.R[13] != 0) return;

            uint sp = 0x84000000u - 0x100u;
            Mem.EnsureRamFor(sp - 0x1000, 0x1000, "arm_stack");
            Arm.R[13] = sp;
            Emit("loader", "ARM stack pointer (SP/r13) initialised to 0x" + sp.ToString("X8"));
        }

        /// <summary>
        /// The CMeP first loader expects $0 (the stack pointer) to be preloaded by the
        /// reset hardware ("mov $sp,$0" at 0x5C024). The sandbox models that by placing
        /// the stack at the top of the loaded image region when $0 is still zero.
        /// </summary>
        public void InitMePStack(uint imageEnd)
        {
            if (MeP == null) return;
            if (MeP.R[0] != 0) return;

            uint sp = (imageEnd + 0xFFF) & ~0xFFFu;
            if (sp == 0) return;

            Mem.EnsureRamFor(sp - 0x100, 0x100, "cmep_stack");
            MeP.R[0] = sp;
            Emit("loader", "CMeP stack pointer ($0 / $sp) initialised to 0x" + sp.ToString("X8"));
        }

        #endregion

        #region Execution

        public void Reset()
        {
            if (Cpu == null) return;
            Mem.ResetDevices();
            Cpu.Reset(CurrentEntry);
            Steps = 0;
            Emit("cpu", "Reset to 0x" + Cpu.Pc.ToString("X8"));
        }

        public StepResult StepOne()
        {
            if (Cpu == null) return null;
            StepResult sr = Cpu.Step();
            Steps++;
            if (OnStep != null) OnStep(sr, Cpu);
            return sr;
        }

        /// <summary>Run up to <paramref name="count"/> instructions; stop at a breakpoint or on halt.</summary>
        public int Run(int count, bool stopOnBreakpoint = true)
        {
            if (Cpu == null) return 0;

            int done = 0;
            while (done < count && !Cpu.Halted)
            {
                if (stopOnBreakpoint && Cpu.Breakpoints.Count != 0 && Cpu.Breakpoints.Contains(Cpu.Pc))
                {
                    Emit("cpu", "Breakpoint hit at 0x" + Cpu.Pc.ToString("X8"));
                    break;
                }

                StepOne();
                done++;

                if (Cpu.UndefinedInstruction) break;
            }

            return done;
        }

        #endregion

        /// <summary>
        /// Disassemble one instruction, optionally overriding the instruction set
        /// (mode: -1 = the core's current state, 0 = ARM, 1 = Thumb). Only the ARM
        /// core supports the override.
        /// </summary>
        public string DisassembleAt(uint address, int mode, out int length)
        {
            if (mode >= 0 && Arm != null)
                return ArmDisasm.Disassemble(Arm, address, mode == 1, out length);

            return Cpu.Disassemble(address, out length);
        }

        /// <summary>Compact register dump used by the "regs" command.</summary>
        public string RegisterDump()
        {
            if (Cpu == null) return "(no core)";

            List<RegValue> regs = new List<RegValue>();
            Cpu.GetRegisters(regs);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string group = null;
            foreach (RegValue r in regs)
            {
                if (r.Group != group)
                {
                    if (group != null) sb.AppendLine();
                    sb.Append(r.Group).Append(": ");
                    group = r.Group;
                }
                else sb.Append("  ");

                sb.Append(r.Name).Append('=').Append(r.Value.ToString("X8"));
                if (r.Note != null) sb.Append('(').Append(r.Note).Append(')');
            }

            return sb.ToString();
        }
    }
}
