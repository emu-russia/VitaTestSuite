// Command processor

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
using VitaTestSuite;

public class CommandProcessor
{
    private TestSuiteContext TestSuite;
    private List<Command> cmds = new List<Command>();

    class Command
    {
        public string name;
        public CommandHandler handler;
    }

    public delegate void CommandHandler (string []args);

    public void LinkSuite(TestSuiteContext Ctx)
    {
        TestSuite = Ctx;
    }

    public CommandProcessor ()
    {
        AddCommand("help", new CommandHandler(CmdHelp));
        AddCommand("addmem", new CommandHandler(CmdAddMem));
        AddCommand("mapmem", new CommandHandler(CmdMapMem));
        AddCommand("mem", new CommandHandler(CmdMem));
        AddCommand("dump", new CommandHandler(CmdDump));
        AddCommand("load", new CommandHandler(CmdLoad));
    }

    public void AddCommand(string name, CommandHandler handler)
    {
        Command cmd = new Command();
        cmd.name = name;
        cmd.handler = handler;
        cmds.Add (cmd);
    }

    public void Execute (string cmdline)
    {
        string [] args = cmdline.Split (' ');

        if (args.Length == 0)
            return;

        foreach (Command cmd in cmds)
        {
            if (cmd.name == args[0])
            {
                TestSuite.report.Echo(":" + cmdline);
                cmd.handler(args);
                return;
            }
        }

        TestSuite.report.Echo("Unknown command: " + args[0]);
    }

    private uint Strtoul(string text)
    {
        return text.StartsWith("0x") || text.StartsWith("0X") ?
            Convert.ToUInt32(text, 16) : Convert.ToUInt32(text, 10);
    }

    #region Builtin commands

    private void CmdHelp (string [] args )
    {
        TestSuite.report.Echo("addmem <tag> <size>: Add memory");
        TestSuite.report.Echo("mapmem <base VAddr> <tag>: Map memory at Base virtual address");
        TestSuite.report.Echo("mem: Show memory regions");
        TestSuite.report.Echo("dump <VAddr> [size]: Dump memory (size is 0x1000 if not specified)");
        TestSuite.report.Echo("load <VAddr> <filename>: Load memory dump");
    }

    private void CmdAddMem (string [] args )
    {
        if ( args.Length < 3 )
        {
            TestSuite.report.Echo("addmem <tag> <size>");
            return;
        }

        TestSuite.memoryHub.AddMemory(args[1], (int)Strtoul(args[2]));
    }

    private void CmdMapMem(string[] args)
    {
        if (args.Length < 3)
        {
            TestSuite.report.Echo("mapmem <base VAddr> <tag>");
            return;
        }

        TestSuite.memoryHub.MapMemory(Strtoul(args[1]), args[2]);
    }

    /// <summary>
    /// Show memory ranges
    /// </summary>
    /// <param name="args"></param>
    private void CmdMem(string[] args)
    {
        foreach ( MemRange range in TestSuite.memoryHub.ranges )
        {
            if (range.Mapped)
            {
                TestSuite.report.Echo("Region " + range.name + ": Base=0x" +
                    range.BaseVAddr.ToString("X8") + ", Size=0x" +
                    range.Size.ToString("X") + " bytes");
            }
            else
            {
                TestSuite.report.Echo("Region " + range.name + ": Unmapped, " +
                    "Size=0x" + range.Size.ToString("X") + " bytes");
            }
        }
    }

    private void CmdDump(string[] args)
    {
        if ( args.Length < 2 )
        {
            TestSuite.report.Echo("dump <VAddr> [size]");
            return;
        }

        if (args.Length < 3)
            TestSuite.DumpMemory(Strtoul(args[1]), 0x1000);
        else
            TestSuite.DumpMemory(Strtoul(args[1]), (int)Strtoul(args[2]));
    }

    private void CmdLoad(string[] args)
    {
        if (args.Length == 3 )
        {
            string Filename = args[2];
            uint Address = Strtoul(args[1]);

            byte[] Data = File.ReadAllBytes(Filename);
            bool Res = TestSuite.memoryHub.LoadDump( Address, Data);

            if (Res)
                TestSuite.report.Echo("Loaded dump: " + Filename + ", Address: 0x" + Address.ToString("X8") +
                    " , " + Data.Length.ToString() + " bytes");
            else
                TestSuite.report.Echo("Load dump failed!");
        }
        else
        {
            TestSuite.report.Echo("load <VAddr> <filename>");
        }
    }

    #endregion

}
