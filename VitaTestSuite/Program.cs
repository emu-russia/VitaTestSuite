using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

using VitaTestSuite.Core;

namespace VitaTestSuite
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        private const int STD_OUTPUT_HANDLE = -11;

        /// <summary>
        /// The main entry point for the application.
        ///
        /// GUI mode (no arguments): the WinForms test bench.
        /// Headless mode:  VitaTestSuite.exe -script file.cmd [-log out.txt]
        ///                 VitaTestSuite.exe -cmd "arch mep" "loadfirstloader x.bin" ...
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            List<string> commands = new List<string>();
            string script = null;
            string logFile = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-uicheck":
                        return UiCheck();
                    case "-script":
                    case "--script":
                    case "-s":
                        if (i + 1 < args.Length) script = args[++i];
                        break;
                    case "-log":
                    case "--log":
                        if (i + 1 < args.Length) logFile = args[++i];
                        break;
                    case "-cmd":
                    case "--cmd":
                    case "-c":
                        while (i + 1 < args.Length && !args[i + 1].StartsWith("-")) commands.Add(args[++i]);
                        break;
                    default:
                        commands.Add(args[i]);
                        break;
                }
            }

            if (script == null && commands.Count == 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                // Never die silently: log unhandled exceptions to crash.log.
                Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
                {
                    LogCrash(e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
                {
                    LogCrash(e.ExceptionObject as Exception);
                };
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                Application.Run(new Form1());
                return 0;
            }

            return RunHeadless(script, commands, logFile);
        }

        /// <summary>
        /// Diagnostic: build the main window, let it initialise, print the pane/tab
        /// layout and exit. Used to verify the GUI structure without a human looking
        /// at it.
        /// </summary>
        private static int UiCheck()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (Form1 f = new Form1())
            {
                f.Show();
                Application.DoEvents();
                DumpControl(Console.Out, f, 0);
                Application.DoEvents();
                f.Close();
            }

            Console.Out.Flush();
            return 0;
        }

        private static void DumpControl(TextWriter w, Control c, int depth)
        {
            string indent = new string(' ', depth * 2);
            string text = "";
            TabPage tp = c as TabPage;
            if (tp != null) text = tp.Text;
            ListView lv = c as ListView;
            if (lv != null)
            {
                text = "columns=";
                for (int i = 0; i < lv.Columns.Count; i++)
                    text += (i > 0 ? "," : "") + lv.Columns[i].Text;
            }
            ToolStrip tstrip = c as ToolStrip;
            if (tstrip != null)
            {
                text = "items=";
                for (int i = 0; i < tstrip.Items.Count; i++)
                    text += (i > 0 ? "," : "") + tstrip.Items[i].Text;
            }

            w.WriteLine(indent + c.GetType().Name + " " + c.Name + " \"" + text + "\"");

            if (depth > 9) return;
            foreach (Control child in c.Controls)
                DumpControl(w, child, depth + 1);
        }

        private static void LogCrash(Exception ex)        {
            try
            {
                using (StreamWriter w = new StreamWriter("crash.log", true))
                {
                    w.WriteLine("==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ====");
                    w.WriteLine(ex != null ? ex.ToString() : "(no exception object)");
                    w.WriteLine();
                }
            }
            catch
            {
                // ignore
            }
        }

        private static int RunHeadless(string script, List<string> commands, string logFile)
        {
            // A WinExe has no console of its own; attach to the parent console so that
            // interactive use prints something. Do NOT attach when stdout has already
            // been redirected to a file or a pipe - that would hijack the redirect.
            try
            {
                IntPtr h = GetStdHandle(STD_OUTPUT_HANDLE);
                bool haveStdout = h != IntPtr.Zero && h != new IntPtr(-1);
                if (!haveStdout)
                    AttachConsole(ATTACH_PARENT_PROCESS);
            }
            catch
            {
                // not fatal
            }

            TextWriter writer = Console.Out;

            Report report = new Report();
            report.Headless = true;
            report.Writer = writer;

            if (logFile != null)
                report.FileWriter = new StreamWriter(logFile, false);

            TestSuiteContext ctx = new TestSuiteContext();
            ctx.report = report;
            report.LinkSuite(ctx);

            ctx.sandbox = new Sandbox(report);
            ctx.sandbox.SetArch(CpuArch.Unknown);

            ctx.cmd = new CommandProcessor();
            ctx.cmd.LinkSuite(ctx);

            ctx.DumpMemory = delegate(uint addr, int size)
            {
                DumpToText(report, ctx.sandbox, addr, size);
            };

            try
            {
                if (script != null)
                {
                    if (!File.Exists(script))
                    {
                        report.EchoError("No such script: " + script);
                    }
                    else
                    {
                        foreach (string line in File.ReadAllLines(script))
                        {
                            string s = line.Trim();
                            if (s.Length == 0 || s.StartsWith("#") || s.StartsWith(";")) continue;
                            ctx.cmd.Execute(s);
                        }
                    }
                }

                foreach (string c in commands)
                    ctx.cmd.Execute(c);
            }
            finally
            {
                if (report.FileWriter != null)
                {
                    report.FileWriter.Flush();
                    report.FileWriter.Close();
                }
            }

            return report.ErrorCount != 0 ? 1 : 0;
        }

        private static void DumpToText(Report report, Sandbox sb, uint addr, int size)
        {
            for (int row = 0; row < size; row += 16)
            {
                System.Text.StringBuilder sbHex = new System.Text.StringBuilder();
                System.Text.StringBuilder sbAsc = new System.Text.StringBuilder();

                for (int i = 0; i < 16 && row + i < size; i++)
                {
                    byte b = sb.Mem.ReadByte(addr + (uint)(row + i));
                    sbHex.Append(b.ToString("X2")).Append(' ');
                    sbAsc.Append(b >= 32 && b < 127 ? (char)b : '.');
                }

                report.Echo((addr + (uint)row).ToString("X8") + ": " + sbHex.ToString().PadRight(48) + sbAsc.ToString());
            }
        }
    }
}
