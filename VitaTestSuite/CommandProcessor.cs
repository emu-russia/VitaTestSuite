// Command processor: the scripting interface of VitaTestSuite.
//
// Works identically in the GUI (command box / Autoexec.cmd / "Execute script")
// and in headless mode (VitaTestSuite.exe -script file.cmd).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

using VitaTestSuite;
using VitaTestSuite.Core;
using VitaTestSuite.Core.Devices;

public class CommandProcessor
{
    private TestSuiteContext TestSuite;
    private List<Command> cmds = new List<Command>();

    class Command
    {
        public string name;
        public string usage;
        public string help;
        public CommandHandler handler;
    }

    public delegate void CommandHandler(string[] args);

    public void LinkSuite(TestSuiteContext Ctx)
    {
        TestSuite = Ctx;
    }

    public CommandProcessor()
    {
        AddCommand("help", "help [cmd]", "Show help", new CommandHandler(CmdHelp));

        // --- sandbox / architecture ---
        AddCommand("arch", "arch <arm|mep|venezia|rl78|status>", "Select the emulated core", new CommandHandler(CmdArch));
        AddCommand("info", "info", "Show the currently loaded image", new CommandHandler(CmdInfo));
        AddCommand("reset", "reset [addr]", "Reset the core (optionally to an address)", new CommandHandler(CmdReset));

        // --- memory ---
        AddCommand("addmem", "addmem <tag> <size>", "Add a memory region", new CommandHandler(CmdAddMem));
        AddCommand("mapmem", "mapmem <base VAddr> <tag>", "Map a memory region", new CommandHandler(CmdMapMem));
        AddCommand("mem", "mem", "Show memory regions and devices", new CommandHandler(CmdMem));
        AddCommand("dump", "dump <VAddr> [size]", "Dump memory into the hex view", new CommandHandler(CmdDump));
        AddCommand("peek", "peek <VAddr> [count]", "Read words from memory", new CommandHandler(CmdPeek));
        AddCommand("poke", "poke <VAddr> <value>", "Write a word to memory", new CommandHandler(CmdPoke));
        AddCommand("load", "load <VAddr> <filename>", "Load a raw dump at an address", new CommandHandler(CmdLoad));

        // --- image loading ---
        AddCommand("open", "open <filename> [address]", "Auto-detect and load any image", new CommandHandler(CmdOpen));
        AddCommand("loadelf", "loadelf <filename> [bias]", "Load an ELF image", new CommandHandler(CmdLoadElf));
        AddCommand("loadself", "loadself <filename>", "Decrypt and load a SELF module", new CommandHandler(CmdLoadSelf));
        AddCommand("loadfirstloader", "loadfirstloader <filename>", "Load a CMeP first_loader / bootrom image", new CommandHandler(CmdLoadFirstLoader));
        AddCommand("loadernie", "loadernie <filename>", "Load an Ernie (RL78 syscon) dump", new CommandHandler(CmdLoadErnie));
        AddCommand("nids", "nids <db.yml>", "Load the NID -> name database", new CommandHandler(CmdNids));
        AddCommand("modinfo", "modinfo", "Show the imports/exports of the loaded module", new CommandHandler(CmdModInfo));
        AddCommand("pup", "pup <file.pup>", "List the segments of a PUP package", new CommandHandler(CmdPup));
        AddCommand("pupget", "pupget <file.pup> <index> <outfile>", "Extract one PUP segment", new CommandHandler(CmdPupGet));

        // --- execution ---
        AddCommand("step", "step [count]", "Execute N instructions (default 1)", new CommandHandler(CmdStep));
        AddCommand("run", "run [count]", "Execute up to N instructions (default 1000)", new CommandHandler(CmdRun));
        AddCommand("cont", "cont", "Run until a breakpoint, halt or the step limit", new CommandHandler(CmdCont));
        AddCommand("regs", "regs", "Show the register file", new CommandHandler(CmdRegs));
        AddCommand("setreg", "setreg <name> <value>", "Set a register (setreg pc <addr> to jump)", new CommandHandler(CmdSetReg));
        AddCommand("dis", "dis [addr] [count] [arm|thumb]", "Disassemble", new CommandHandler(CmdDis));
        AddCommand("coverage", "coverage <start> <end>", "Linear disassembly coverage of a range", new CommandHandler(CmdCoverage));
        AddCommand("bp", "bp <addr> | bp list | bp clear [addr]", "Manage breakpoints", new CommandHandler(CmdBp));
        AddCommand("trace", "trace <on|off>", "Echo every executed instruction", new CommandHandler(CmdTrace));

        // --- hardware access log ---
        AddCommand("mmiolog", "mmiolog <on|off|tail [n]|unmapped [n]|clear|stats|ram on|ram off>", "MMIO access log control", new CommandHandler(CmdMmioLog));
        AddCommand("devices", "devices", "List MMIO devices and their registers", new CommandHandler(CmdDevices));
        AddCommand("devset", "devset <addr> <value>", "Write an MMIO register directly", new CommandHandler(CmdDevSet));
        AddCommand("devget", "devget <addr>", "Read an MMIO register directly", new CommandHandler(CmdDevGet));
        AddCommand("keyring", "keyring", "Show captured CMeP keyring material", new CommandHandler(CmdKeyring));
    }

    public void AddCommand(string name, string usage, string help, CommandHandler handler)
    {
        Command cmd = new Command();
        cmd.name = name;
        cmd.usage = usage;
        cmd.help = help;
        cmd.handler = handler;
        cmds.Add(cmd);
    }

    public void Execute(string cmdline)
    {
        string[] args = Tokenize(cmdline);

        if (args.Length == 0)
            return;

        foreach (Command cmd in cmds)
        {
            if (string.Equals(cmd.name, args[0], StringComparison.OrdinalIgnoreCase))
            {
                TestSuite.report.Echo(":" + cmdline);
                try
                {
                    cmd.handler(args);
                }
                catch (Exception ex)
                {
                    TestSuite.report.EchoError("Error: " + ex.Message);
                }
                if (TestSuite.RefreshViews != null) TestSuite.RefreshViews();
                return;
            }
        }

        TestSuite.report.Echo("Unknown command: " + args[0] + " (try 'help')");
    }

    /// <summary>Split a command line, honouring double quotes for paths with spaces.</summary>
    public static string[] Tokenize(string cmdline)
    {
        List<string> parts = new List<string>();
        StringBuilder cur = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < cmdline.Length; i++)
        {
            char c = cmdline[i];

            if (c == '"') { inQuotes = !inQuotes; continue; }

            if (!inQuotes && (c == ' ' || c == '\t'))
            {
                if (cur.Length > 0) { parts.Add(cur.ToString()); cur.Length = 0; }
                continue;
            }

            cur.Append(c);
        }

        if (cur.Length > 0) parts.Add(cur.ToString());
        return parts.ToArray();
    }

    private static uint Strtoul(string text)
    {
        if (text.StartsWith("0x") || text.StartsWith("0X"))
            return Convert.ToUInt32(text.Substring(2), 16);
        if (text.StartsWith("$"))
            return Convert.ToUInt32(text.Substring(1), 16);
        return Convert.ToUInt32(text, 10);
    }

    private Sandbox Sb { get { return TestSuite.sandbox; } }

    private static string Hex(uint v)
    {
        return "0x" + v.ToString("X8");
    }

    #region Help

    private void CmdHelp(string[] args)
    {
        if (args.Length > 1)
        {
            foreach (Command c in cmds)
            {
                if (c.name == args[1])
                {
                    TestSuite.report.Echo(c.usage + " - " + c.help);
                    return;
                }
            }
            TestSuite.report.Echo("No such command: " + args[1]);
            return;
        }

        foreach (Command c in cmds)
            TestSuite.report.Echo(string.Format("{0,-46} {1}", c.usage, c.help));
    }

    #endregion

    #region Sandbox / architecture

    private void CmdArch(string[] args)
    {
        if (args.Length < 2 || args[1] == "status")
        {
            TestSuite.report.Echo("Architecture: " + Sb.Arch + (Sb.Cpu != null ? " (" + Sb.Cpu.Name + ")" : ""));
            if (Sb.Cpu != null)
                TestSuite.report.Echo("PC=0x" + Sb.Cpu.Pc.ToString("X8") + "  steps=" + Sb.Steps +
                    (Sb.Cpu.Halted ? "  HALTED: " + Sb.Cpu.HaltReason : "") +
                    (Sb.Cpu.StatusLine.Length != 0 ? "  " + Sb.Cpu.StatusLine : ""));
            if (Sb.MeP != null) TestSuite.report.Echo("MeP profile: " + Sb.MeP.Profile);
            return;
        }

        string a = args[1].ToLowerInvariant();

        if (a == "arm" || a == "a9" || a == "cortex-a9")
            Sb.SetArch(CpuArch.Arm);
        else if (a == "mep" || a == "cmep" || a == "c5" || a == "f00d")
            Sb.SetArch(CpuArch.MeP);
        else if (a == "venezia" || a == "vnz" || a == "mpe")
            Sb.SetVenezia();
        else if (a == "rl78" || a == "Ernie" || a == "syscon")
            Sb.SetArch(CpuArch.Rl78);
        else
        {
            TestSuite.report.Echo("arch <arm|mep|venezia|rl78|status>");
            return;
        }

        TestSuite.report.Echo("Architecture set to " + Sb.Arch);
    }

    private void CmdInfo(string[] args)
    {
        TestSuite.report.Echo("Image: " + (Sb.CurrentImage.Length != 0 ? Sb.CurrentImage : "(none)"));
        TestSuite.report.Echo("Entry: " + Hex(Sb.CurrentEntry) + "  PC: " + (Sb.Cpu != null ? Hex(Sb.Cpu.Pc) : "n/a") +
            "  steps: " + Sb.Steps);
        if (Sb.Cpu != null)
        {
            TestSuite.report.Echo("Core: " + Sb.Cpu.Name + "  instr=" + Sb.Cpu.InstructionCount +
                " cycles=" + Sb.Cpu.CycleCount + "  " + Sb.Cpu.StatusLine);
            if (Sb.Cpu.Halted) TestSuite.report.Echo("HALTED: " + Sb.Cpu.HaltReason);
        }
        TestSuite.report.Echo("MMIO accesses: " + Sb.Mem.Access.MmioAccesses +
            ", unmapped: " + Sb.Mem.Access.UnmappedAccesses +
            ", RAM: " + Sb.Mem.Access.RamAccesses);
    }

    private void CmdReset(string[] args)
    {
        if (args.Length >= 2)
        {
            uint addr = Strtoul(args[1]);
            Sb.CurrentEntry = addr;
        }
        Sb.Reset();
        TestSuite.report.Echo("Reset. PC=" + Hex(Sb.Cpu.Pc));
    }

    #endregion

    #region Memory

    private void CmdAddMem(string[] args)
    {
        if (args.Length < 3) { TestSuite.report.Echo("addmem <tag> <size>"); return; }
        Sb.Mem.AddMemory(args[1], (int)Strtoul(args[2]));
    }

    private void CmdMapMem(string[] args)
    {
        if (args.Length < 3) { TestSuite.report.Echo("mapmem <base VAddr> <tag>"); return; }
        Sb.Mem.MapMemory(Strtoul(args[1]), args[2]);
    }

    private void CmdMem(string[] args)
    {
        foreach (MemRange range in Sb.Mem.ranges)
        {
            if (range.Mapped)
            {
                TestSuite.report.Echo("Region " + range.name + ": Base=0x" + range.BaseVAddr.ToString("X8") +
                    ", Size=0x" + range.Size.ToString("X") + " bytes" +
                    (range.Note.Length != 0 ? "  (" + range.Note + ")" : ""));
            }
            else
            {
                TestSuite.report.Echo("Region " + range.name + ": Unmapped, Size=0x" + range.Size.ToString("X") + " bytes");
            }
        }

        if (Sb.Mem.devices.Count != 0)
        {
            TestSuite.report.Echo("Devices:");
            foreach (MmioDevice d in Sb.Mem.devices)
                TestSuite.report.Echo("  " + d.Summary);
        }
    }

    private void CmdDump(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("dump <VAddr> [size]"); return; }
        int size = args.Length < 3 ? 0x1000 : (int)Strtoul(args[2]);
        TestSuite.DumpMemory(Strtoul(args[1]), size);
    }

    private void CmdPeek(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("peek <VAddr> [count]"); return; }
        uint addr = Strtoul(args[1]);
        int count = args.Length < 3 ? 16 : (int)Strtoul(args[2]);

        for (int i = 0; i < count; i++)
        {
            uint a = addr + (uint)(i * 4);
            TestSuite.report.Echo(a.ToString("X8") + ": " + Sb.Mem.ReadWord(a).ToString("X8"));
        }
    }

    private void CmdPoke(string[] args)
    {
        if (args.Length < 3) { TestSuite.report.Echo("poke <VAddr> <value>"); return; }
        uint addr = Strtoul(args[1]);
        uint val = Strtoul(args[2]);
        Sb.Mem.WriteWord(addr, val);
        TestSuite.report.Echo("[" + Hex(addr) + "] = " + Hex(val));
    }

    private void CmdLoad(string[] args)
    {
        if (args.Length < 3) { TestSuite.report.Echo("load <VAddr> <filename>"); return; }

        uint address = Strtoul(args[1]);
        string filename = args[2];

        if (!File.Exists(filename)) { TestSuite.report.Echo("No such file: " + filename); return; }

        byte[] data = File.ReadAllBytes(filename);
        Sb.Mem.EnsureRamFor(address, data.Length, "load");

        if (Sb.Mem.LoadDump(address, data))
        {
            Sb.CurrentImage = Path.GetFileName(filename) + ": raw binary, " + data.Length + " bytes at 0x" +
                address.ToString("X8");
            Sb.CurrentFile = filename;
            Sb.CurrentEntry = address;
            Sb.Cpu.Reset(address);
            TestSuite.report.Echo("Loaded dump: " + filename + ", Address: 0x" + address.ToString("X8") +
                " , " + data.Length + " bytes");
        }
        else
            TestSuite.report.EchoError("Load dump failed (no memory region covers 0x" + address.ToString("X8") + ")");
    }

    #endregion

    #region Image loading

    private void CmdOpen(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("open <filename> [address]"); return; }
        long addr = args.Length >= 3 ? (long)Strtoul(args[2]) : -1;

        Sandbox.LoadResult r = Sb.LoadFile(args[1], addr);
        if (r.Ok) TestSuite.report.Echo("OK: " + r.Message);
        else TestSuite.report.EchoError("FAILED: " + r.Message);
    }

    private void CmdLoadElf(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("loadelf <filename> [bias]"); return; }
        CmdOpen(new string[] { "open", args[1] });
    }

    private void CmdLoadSelf(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("loadself <filename>"); return; }
        CmdOpen(new string[] { "open", args[1] });
    }

    private void CmdLoadFirstLoader(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("loadfirstloader <filename>"); return; }

        Sb.SetArch(CpuArch.MeP);

        byte[] data = File.ReadAllBytes(args[1]);
        uint baseAddr = 0x5C000;

        // An ELF (e_machine 0xF00D) first loader is loaded through the ELF path.
        if (ElfImage.IsElf(data))
        {
            Sandbox.LoadResult er = Sb.LoadBytes(data, args[1]);
            if (er.Ok) TestSuite.report.Echo("OK: " + er.Message);
            else TestSuite.report.EchoError("FAILED: " + er.Message);
            return;
        }

        Sb.Mem.EnsureRamFor(baseAddr, data.Length, "first_loader");
        if (!Sb.Mem.LoadDump(baseAddr, data))
        {
            TestSuite.report.EchoError("FAILED: no memory at 0x5C000");
            return;
        }

        Sb.CurrentImage = Path.GetFileName(args[1]) + ": CMeP first_loader, " + data.Length + " bytes at 0x5C000";
        Sb.CurrentFile = args[1];
        Sb.CurrentEntry = baseAddr;
        Sb.Cpu.Reset(baseAddr);
        Sb.LastData = data;
        Sb.InitMePStack(baseAddr + (uint)data.Length);

        TestSuite.report.Echo("Loaded CMeP first_loader " + Path.GetFileName(args[1]) + " at 0x5C000, entry " + Hex(baseAddr));
        TestSuite.report.Echo("First instructions:");
        DumpDisasm(baseAddr, 8);
    }

    private void CmdLoadErnie(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("loadernie <filename>"); return; }

        Sb.SetArch(CpuArch.Rl78);

        byte[] data = File.ReadAllBytes(args[1]);
        Sb.Mem.EnsureRamFor(0, data.Length, "Ernie");

        if (!Sb.Mem.LoadDump(0, data))
        {
            TestSuite.report.EchoError("FAILED: could not map the dump");
            return;
        }

        // RL78 reset vector: the word stored at 0x0000.
        uint vector = (uint)(Sb.Mem.ReadByte(0) | (Sb.Mem.ReadByte(1) << 8));

        Sb.CurrentImage = Path.GetFileName(args[1]) + ": Ernie (RL78) dump, " + data.Length + " bytes at 0x00000000";
        Sb.CurrentFile = args[1];
        Sb.CurrentEntry = vector;
        Sb.Cpu.Reset(vector);

        TestSuite.report.Echo("Loaded Ernie dump " + Path.GetFileName(args[1]) + ", reset vector = " + Hex(vector));
        TestSuite.report.Echo("First instructions:");
        DumpDisasm(vector, 8);
    }

    private void CmdNids(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("nids <db.yml>"); return; }
        try
        {
            Sb.Nids = NidDatabase.Load(args[1]);
            TestSuite.report.Echo("Loaded " + Sb.Nids.Count + " NID names from " + args[1]);
        }
        catch (Exception ex)
        {
            TestSuite.report.EchoError("Failed: " + ex.Message);
        }
    }

    private void CmdModInfo(string[] args)
    {
        if (Sb.LastElf == null)
        {
            TestSuite.report.Echo("No ELF module loaded (use 'open <file.self|file.elf>')");
            return;
        }

        TestSuite.report.Echo(Sb.LastElf.Describe());

        if (Sb.Module == null)
        {
            TestSuite.report.Echo("No Vita module table found in this image.");
            return;
        }

        TestSuite.report.Echo(Sb.Module.Describe(Sb.Nids));
        TestSuite.report.Echo("Export libraries: " + string.Join(", ", Sb.Module.ExportLibraries().ToArray()));
        TestSuite.report.Echo("Import libraries: " + string.Join(", ", Sb.Module.ImportLibraries().ToArray()));
    }

    private void CmdPup(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("pup <file.pup>"); return; }

        try
        {
            PupPackage pup = PupPackage.ParseFile(args[1], TestSuite.report);
            TestSuite.report.Echo(pup.Describe());
            for (int i = 0; i < pup.Segments.Count; i++)
            {
                PupSegment s = pup.Segments[i];
                TestSuite.report.Echo(string.Format("[{0,3}] {1,-32} flags=0x{2:X8} offset=0x{3:X} size=0x{4:X}",
                    i, s.Name, s.Flags, s.Offset, s.Size));
            }
        }
        catch (Exception ex)
        {
            TestSuite.report.EchoError("Failed: " + ex.Message);
        }
    }

    private void CmdPupGet(string[] args)
    {
        if (args.Length < 4) { TestSuite.report.Echo("pupget <file.pup> <index> <outfile>"); return; }

        try
        {
            PupPackage pup = PupPackage.ParseFile(args[1], TestSuite.report);
            int idx = (int)Strtoul(args[2]);
            if (idx < 0 || idx >= pup.Segments.Count)
            {
                TestSuite.report.EchoError("Segment index out of range (0.." + (pup.Segments.Count - 1) + ")");
                return;
            }

            byte[] seg = pup.ReadSegmentFromFile(args[1], pup.Segments[idx]);
            File.WriteAllBytes(args[3], seg);
            TestSuite.report.Echo("Wrote " + seg.Length + " bytes of " + pup.Segments[idx].Name + " to " + args[3]);
        }
        catch (Exception ex)
        {
            TestSuite.report.EchoError("Failed: " + ex.Message);
        }
    }

    #endregion

    #region Execution
    private void CmdStep(string[] args)
    {
        if (Sb.Cpu == null) { TestSuite.report.Echo("No core selected (use 'arch')"); return; }
        int count = args.Length < 2 ? 1 : (int)Strtoul(args[1]);

        for (int i = 0; i < count; i++)
        {
            if (Sb.Cpu.Halted) break;

            // Disassemble BEFORE executing: instructions such as BLX/BX/POP {pc}
            // change the instruction set, so decoding afterwards would be wrong.
            int preLen;
            uint preAddr = Sb.Cpu.Pc;
            string preText = Sb.Cpu.Disassemble(preAddr, out preLen);

            StepResult sr = Sb.StepOne();

            string text = !string.IsNullOrEmpty(preText) ? preText
                : (sr != null ? sr.Text : "");
            if (string.IsNullOrEmpty(text) && sr != null)
            {
                int len;
                text = Sb.Cpu.Disassemble(sr.Address, out len);
            }

            TestSuite.report.Echo(Hex(preAddr) + ": " + text +
                (sr != null && sr.Faulted ? "   <-- " + sr.Fault : ""));
            if (Sb.Cpu.UndefinedInstruction) break;
        }

        if (Hb()) TestSuite.report.Echo("PC=" + Hex(Sb.Cpu.Pc) + "  " + Sb.Cpu.StatusLine +
            (Sb.Cpu.Halted ? " HALTED: " + Sb.Cpu.HaltReason : ""));
    }

    private bool Hb()
    {
        return true;
    }

    private void CmdRun(string[] args)
    {
        if (Sb.Cpu == null) { TestSuite.report.Echo("No core selected (use 'arch')"); return; }
        int count = args.Length < 2 ? 1000 : (int)Strtoul(args[1]);

        DateTime t0 = DateTime.Now;
        int done = Sb.Run(count);
        double ms = (DateTime.Now - t0).TotalMilliseconds;

        TestSuite.report.Echo("Executed " + done + " instructions in " + ms.ToString("F1", CultureInfo.InvariantCulture) + " ms" +
            ", PC=" + Hex(Sb.Cpu.Pc) + (Sb.Cpu.Halted ? ", HALTED: " + Sb.Cpu.HaltReason : ""));
    }

    private void CmdCont(string[] args)
    {
        if (Sb.Cpu == null) { TestSuite.report.Echo("No core selected (use 'arch')"); return; }

        int limit = args.Length < 2 ? 20000000 : (int)Strtoul(args[1]);
        DateTime t0 = DateTime.Now;
        int done = Sb.Run(limit);
        double ms = (DateTime.Now - t0).TotalMilliseconds;

        TestSuite.report.Echo("Executed " + done + " instructions in " + ms.ToString("F1", CultureInfo.InvariantCulture) + " ms" +
            ", PC=" + Hex(Sb.Cpu.Pc) + (Sb.Cpu.Halted ? ", HALTED: " + Sb.Cpu.HaltReason : ""));
    }

    private void CmdRegs(string[] args)
    {
        TestSuite.report.Echo(Sb.RegisterDump());
    }

    private void CmdSetReg(string[] args)
    {
        if (args.Length < 3) { TestSuite.report.Echo("setreg <name> <value>"); return; }
        uint val = Strtoul(args[2]);
        if (Sb.Cpu != null && Sb.Cpu.SetRegister(args[1], val))
            TestSuite.report.Echo(args[1] + " = " + Hex(val));
        else
            TestSuite.report.Echo("Unknown register: " + args[1]);
    }

    private void DumpDisasm(uint addr, int count, int mode = -1)
    {
        for (int i = 0; i < count; i++)
        {
            int len;
            string text = Sb.DisassembleAt(addr, mode, out len);
            if (len <= 0) len = 4;
            TestSuite.report.Echo(Hex(addr) + ": " + text);
            addr += (uint)len;
        }
    }

    private void CmdDis(string[] args)
    {
        if (Sb.Cpu == null) { TestSuite.report.Echo("No core selected (use 'arch')"); return; }

        uint addr = Sb.Cpu.Pc;
        int count = 16;
        int mode = -1;

        int positional = 0;
        for (int i = 1; i < args.Length; i++)
        {
            string a = args[i].ToLowerInvariant();
            if (a == "arm") { mode = 0; continue; }
            if (a == "thumb") { mode = 1; continue; }

            if (positional == 0) { addr = Strtoul(args[i]); positional++; }
            else if (positional == 1) { count = (int)Strtoul(args[i]); positional++; }
        }

        if (mode >= 0 && Sb.Arm == null)
            TestSuite.report.Echo("Note: instruction-set override is only meaningful for the ARM core");

        DumpDisasm(addr, count, mode);
    }

    private void CmdCoverage(string[] args)
    {
        if (Sb.Cpu == null) { TestSuite.report.Echo("No core selected (use 'arch')"); return; }
        if (args.Length < 3) { TestSuite.report.Echo("coverage <start> <end>"); return; }

        uint start = Strtoul(args[1]);
        uint end = Strtoul(args[2]);
        if (end <= start) { TestSuite.report.Echo("end must be greater than start"); return; }

        Dictionary<string, int> hist = new Dictionary<string, int>();
        uint addr = start;
        int insns = 0, unknown = 0, unknownBytes = 0;
        uint firstUnknown = 0;

        while (addr < end)
        {
            int len;
            string text = Sb.Cpu.Disassemble(addr, out len);
            if (len <= 0) len = 1;

            bool bad = string.IsNullOrEmpty(text) || text.StartsWith("??") || text.StartsWith(".word") ||
                       text.StartsWith("<");
            if (bad)
            {
                if (unknown == 0) firstUnknown = addr;
                unknown++;
                unknownBytes += len;
            }

            string mn = text;
            int sp = mn.IndexOf(' ');
            if (sp > 0) mn = mn.Substring(0, sp);
            int count;
            hist.TryGetValue(mn, out count);
            hist[mn] = count + 1;

            insns++;
            addr += (uint)len;
        }

        uint total = end - start;
        TestSuite.report.Echo(string.Format(CultureInfo.InvariantCulture,
            "0x{0:X8}..0x{1:X8}: {2} instructions, {3} bytes, {4} unknown ({5:F2}% known), {6} unknown bytes",
            start, end, insns, total, unknown, insns == 0 ? 0.0 : 100.0 * (insns - unknown) / insns, unknownBytes));
        if (unknown != 0) TestSuite.report.Echo("first unknown instruction at 0x" + firstUnknown.ToString("X8"));

        List<KeyValuePair<string, int>> sorted = new List<KeyValuePair<string, int>>(hist);
        sorted.Sort(delegate(KeyValuePair<string, int> a, KeyValuePair<string, int> b) { return b.Value.CompareTo(a.Value); });

        StringBuilder sb = new StringBuilder("mnemonics:");
        for (int i = 0; i < sorted.Count && i < 24; i++)
            sb.Append(' ').Append(sorted[i].Key).Append('=').Append(sorted[i].Value);
        TestSuite.report.Echo(sb.ToString());
    }

    private void CmdBp(string[] args)
    {
        if (Sb.Cpu == null) { TestSuite.report.Echo("No core selected"); return; }

        if (args.Length < 2 || args[1] == "list")
        {
            if (Sb.Cpu.Breakpoints.Count == 0) TestSuite.report.Echo("No breakpoints");
            foreach (uint a in Sb.Cpu.Breakpoints) TestSuite.report.Echo("bp " + Hex(a));
            return;
        }

        if (args[1] == "clear")
        {
            if (args.Length >= 3) Sb.Cpu.Breakpoints.Remove(Strtoul(args[2]));
            else Sb.Cpu.Breakpoints.Clear();
            TestSuite.report.Echo("Breakpoints cleared");
            return;
        }

        uint addr = Strtoul(args[1]);
        Sb.Cpu.Breakpoints.Add(addr);
        TestSuite.report.Echo("Breakpoint set at " + Hex(addr));
    }

    private void CmdTrace(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("trace <on|off>"); return; }
        bool on = args[1] == "on" || args[1] == "1";
        TestSuite.report.EchoTrace = on;
        TestSuite.report.SetCategory("trace", on);

        if (Sb.Arm != null) Sb.Arm.TraceInstructions = on;
        if (Sb.MeP != null) Sb.MeP.TraceInstructions = on;
        if (Sb.Rl78 != null) Sb.Rl78.TraceInstructions = on;

        TestSuite.report.Echo("Instruction trace " + (on ? "on" : "off"));
    }

    #endregion

    #region Hardware access log

    private void CmdMmioLog(string[] args)
    {
        if (args.Length < 2)
        {
            TestSuite.report.Echo("mmiolog <on|off|tail [n]|unmapped [n]|clear|stats|ram on|ram off>");
            return;
        }

        switch (args[1])
        {
            case "on": TestSuite.report.EchoMmio = true; Sb.Mem.Access.Enabled = true; break;
            case "off": TestSuite.report.EchoMmio = false; break;
            case "clear": Sb.Mem.Access.Clear(); TestSuite.report.Echo("Access log cleared"); break;
            case "ram":
                if (args.Length >= 3)
                {
                    Sb.Mem.Access.TraceRam = args[2] == "on";
                    TestSuite.report.Echo("RAM access tracing " + (Sb.Mem.Access.TraceRam ? "on (very verbose)" : "off"));
                }
                break;
            case "tail":
                {
                    int n = args.Length >= 3 ? (int)Strtoul(args[2]) : 64;
                    List<AccessRecord> recs = Sb.Mem.Access.Snapshot(n);
                    foreach (AccessRecord r in recs)
                        TestSuite.report.Echo(AccessLog.Describe(r, AccessDeviceName(r), FormatPc(r.Pc)));
                    TestSuite.report.Echo("(" + recs.Count + " entries, " + Sb.Mem.Access.Count + " in the ring buffer)");
                }
                break;
            case "unmapped":
                {
                    int n = args.Length >= 3 ? (int)Strtoul(args[2]) : 32;
                    List<AccessRecord> recs = Sb.Mem.Access.Snapshot();
                    int shown = 0;
                    for (int i = recs.Count - 1; i >= 0 && shown < n; i--)
                    {
                        if (!recs[i].Unmapped) continue;
                        TestSuite.report.Echo(AccessLog.Describe(recs[i], "unmapped", FormatPc(recs[i].Pc)));
                        shown++;
                    }
                    TestSuite.report.Echo("(" + shown + " unmapped accesses shown, " + Sb.Mem.Access.UnmappedAccesses + " total)");
                }
                break;
            case "stats":
                TestSuite.report.Echo("MMIO accesses: " + Sb.Mem.Access.MmioAccesses);
                TestSuite.report.Echo("RAM accesses:  " + Sb.Mem.Access.RamAccesses);
                TestSuite.report.Echo("Fetches:       " + Sb.Mem.Access.Fetches);
                TestSuite.report.Echo("Unmapped:      " + Sb.Mem.Access.UnmappedAccesses);
                TestSuite.report.Echo("Ring buffer:   " + Sb.Mem.Access.Count + " / " + Sb.Mem.Access.Capacity +
                    (Sb.Mem.Access.TraceRam ? "  (RAM tracing on)" : ""));
                break;
            default:
                TestSuite.report.Echo("mmiolog <on|off|tail [n]|clear|stats|ram on|ram off>");
                break;
        }
    }

    private static string FormatPc(uint pc)
    {
        return pc.ToString("X8");
    }

    /// <summary>"Device.Register" for an access record, for the log output.</summary>
    private string AccessDeviceName(AccessRecord r)
    {
        if (r.Unmapped) return "unmapped";

        MmioDevice d = r.Device >= 0 && r.Device < Sb.Mem.devices.Count ? Sb.Mem.devices[r.Device] : null;
        if (d == null)
        {
            d = Sb.Mem.FindDevice(r.Address);
            if (d == null) return "RAM";
        }

        string reg = d.RegisterName(r.Address);
        return reg != null ? d.Name + "." + reg : d.Name;
    }

    private void CmdDevices(string[] args)
    {
        if (Sb.Mem.devices.Count == 0) { TestSuite.report.Echo("No devices installed"); return; }

        foreach (MmioDevice d in Sb.Mem.devices)
        {
            TestSuite.report.Echo(d.Summary);
            foreach (KeyValuePair<uint, string> kv in d.RegisterNames)
            {
                uint v = Sb.Mem.FindDevice(kv.Key) == d ? PeekRegister(d, kv.Key) : 0;
                TestSuite.report.Echo("    " + kv.Key.ToString("X8") + "  " + kv.Value.PadRight(28) + " = " + v.ToString("X8"));
            }
        }
    }

    private uint PeekRegister(MmioDevice d, uint addr)
    {
        RegFileDevice rf = d as RegFileDevice;
        if (rf != null) return rf.Peek(addr);
        return d.Read(addr, 4);
    }

    private void CmdDevSet(string[] args)
    {
        if (args.Length < 3) { TestSuite.report.Echo("devset <addr> <value>"); return; }
        uint addr = Strtoul(args[1]);
        uint val = Strtoul(args[2]);
        MmioDevice d = Sb.Mem.FindDevice(addr);

        if (d == null) { TestSuite.report.Echo("No device at " + Hex(addr)); return; }

        d.Write(addr, 4, val);
        TestSuite.report.Echo(d.Name + "." + (d.RegisterName(addr) ?? Hex(addr)) + " = " + Hex(val));
    }

    private void CmdDevGet(string[] args)
    {
        if (args.Length < 2) { TestSuite.report.Echo("devget <addr>"); return; }
        uint addr = Strtoul(args[1]);
        MmioDevice d = Sb.Mem.FindDevice(addr);

        if (d == null) { TestSuite.report.Echo("No device at " + Hex(addr)); return; }

        TestSuite.report.Echo(d.Name + "." + (d.RegisterName(addr) ?? Hex(addr)) + " = " +
            Hex(d.Read(addr, 4)));
    }

    private void CmdKeyring(string[] args)
    {
        KeyringDevice kr = null;
        foreach (MmioDevice d in Sb.Mem.devices)
        {
            kr = d as KeyringDevice;
            if (kr != null) break;
        }

        if (kr == null) { TestSuite.report.Echo("Keyring device is not installed (use 'arch mep')"); return; }

        TestSuite.report.Echo("Keyring writes: " + kr.KeyWrites + ", captured slots: " + kr.Keys.Count);

        foreach (KeyValuePair<uint, uint[]> kv in kr.Keys)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("  slot 0x").Append(kv.Key.ToString("X4")).Append(": ");
            for (int i = 0; i < kv.Value.Length; i++) sb.Append(kv.Value[i].ToString("X8")).Append(' ');
            TestSuite.report.Echo(sb.ToString());
        }

        TestSuite.report.Echo("Clear/flags history (" + kr.ClearHistory.Count + "):");
        int start = kr.ClearHistory.Count > 32 ? kr.ClearHistory.Count - 32 : 0;
        for (int i = start; i < kr.ClearHistory.Count; i++)
            TestSuite.report.Echo("  value=0x" + kr.ClearHistory[i].Value.ToString("X8") +
                "  keyring=0x" + kr.ClearHistory[i].Key.ToString("X"));
    }

    #endregion
}
