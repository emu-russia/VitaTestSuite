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
using Be.Windows.Forms;
using VitaTestSuite.Core;

namespace VitaTestSuite
{
    public partial class Form1 : Form
    {
        private TestSuiteContext testSuite = new TestSuiteContext();

        // The form already ships three central panes plus the bottom log pane:
        //   left  (tabControl1) : register views, one tab per core
        //   middle(tabControl2) : disassembly views, one tab per core
        //   right (tabControl3) : memory dumps (hexBox1)
        //   bottom(listView1)   : Report, plus an "MMIO log" tab
        private static readonly CpuArch[] CoreSlots = { CpuArch.Arm, CpuArch.MeP, CpuArch.Rl78 };

        private ListView[] regViews = new ListView[3];
        private ListView[] disViews = new ListView[3];
        private TabPage[] regPages = new TabPage[3];
        private TabPage[] disPages = new TabPage[3];

        private ListView listViewMmio;
        private TabControl bottomTabs;
        private ToolStripStatusLabel labelCpuState;
        private OpenFileDialog openImageDialog;
        private int lastSlot = -1;

        public Form1()
        {
            InitializeComponent();
            BuildSandboxUI();
        }

        private static int SlotOf(CpuArch arch)
        {
            if (arch == CpuArch.Arm) return 0;
            if (arch == CpuArch.MeP) return 1;
            if (arch == CpuArch.Rl78) return 2;
            return -1;
        }

        private static string CoreName(CpuArch arch)
        {
            if (arch == CpuArch.Arm) return "ARM Cortex-A9";
            if (arch == CpuArch.MeP) return "Toshiba MeP-c5";
            if (arch == CpuArch.Rl78) return "Renesas RL78";
            return "core";
        }

        /// <summary>
        /// Wire the core panels into the existing three-pane layout and add the
        /// MMIO log / image loading commands.
        /// </summary>
        private void BuildSandboxUI()
        {
            // ---- left pane: one register tab per core -------------------------
            regViews[0] = listView3;                 // "ARM Regs"
            regPages[0] = tabPage1;

            regViews[1] = NewListView("Reg", "Value", "Note");
            regPages[1] = tabPage2;
            tabPage2.Text = "MeP Regs";
            AttachToTab(tabPage2, regViews[1]);

            regViews[2] = NewListView("Reg", "Value", "Note");
            regPages[2] = NewTab("RL78 Regs", regViews[2]);
            tabControl1.TabPages.Add(regPages[2]);

            if (regViews[0].Columns.Count < 3) regViews[0].Columns.Add("Note", 110);

            // ---- middle pane: one disassembly tab per core --------------------
            disViews[0] = listView2;                 // "ARM Core"
            disPages[0] = tabPage3;

            disViews[1] = NewListView("Address", "Bytes", "Instruction", "Parameters");
            disPages[1] = tabPage4;
            tabPage4.Text = "MeP Core";
            AttachToTab(tabPage4, disViews[1]);

            disViews[2] = NewListView("Address", "Bytes", "Instruction", "Parameters");
            disPages[2] = NewTab("RL78 Core", disViews[2]);
            tabControl2.TabPages.Add(disPages[2]);

            for (int i = 0; i < 3; i++)
                disViews[i].SelectedIndexChanged += new EventHandler(listViewDisasm_SelectedIndexChanged);

            // ---- right pane: memory dump --------------------------------------
            tabPage5.Text = "Memory";
            if (tabPage6 != null && tabPage6.Controls.Count == 0)
                tabControl3.TabPages.Remove(tabPage6);

            // ---- bottom pane: Report + MMIO log -------------------------------
            bottomTabs = new TabControl();
            bottomTabs.Dock = DockStyle.Fill;

            TabPage reportPage = new TabPage("Report");
            splitContainer4.Panel1.Controls.Remove(listView1);
            reportPage.Controls.Add(listView1);
            listView1.Dock = DockStyle.Fill;
            bottomTabs.TabPages.Add(reportPage);

            listViewMmio = NewListView("Kind", "Address", "Value", "Device / register", "PC");
            TabPage mmioPage = new TabPage("MMIO log");
            mmioPage.Controls.Add(listViewMmio);
            bottomTabs.TabPages.Add(mmioPage);

            splitContainer4.Panel1.Controls.Add(bottomTabs);

            // ---- execution buttons (wire the toolbar buttons the form ships) ---
            toolStripButton1.Text = "Refresh";
            toolStripButton1.Click += new EventHandler(refreshViewsToolStrip_Click);
            toolStripButton2.Text = "Step";
            toolStripButton2.Click += new EventHandler(buttonStep_Click);
            toolStripButton3.Text = "Step x10";
            toolStripButton3.Click += new EventHandler(buttonStep10_Click);
            toolStripButton4.Text = "Run";
            toolStripButton4.Click += new EventHandler(buttonRun_Click);

            ToolStripButton reset = new ToolStripButton("Reset");
            reset.DisplayStyle = ToolStripItemDisplayStyle.Text;
            reset.Click += new EventHandler(buttonReset_Click);
            toolStrip1.Items.Add(reset);

            labelCpuState = new ToolStripStatusLabel("No core selected");
            statusStrip1.Items.Add(labelCpuState);

            // ---- File menu additions ------------------------------------------
            fileToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem miOpen = new ToolStripMenuItem("Open Image...");
            miOpen.Click += new EventHandler(openImageToolStripMenuItem_Click);
            fileToolStripMenuItem.DropDownItems.Add(miOpen);

            ToolStripMenuItem miFirst = new ToolStripMenuItem("Load CMeP first_loader...");
            miFirst.Click += new EventHandler(loadFirstLoaderToolStripMenuItem_Click);
            fileToolStripMenuItem.DropDownItems.Add(miFirst);

            ToolStripMenuItem miErnie = new ToolStripMenuItem("Load Ernie (RL78) dump...");
            miErnie.Click += new EventHandler(loadErnieToolStripMenuItem_Click);
            fileToolStripMenuItem.DropDownItems.Add(miErnie);

            ToolStripMenuItem miSelf = new ToolStripMenuItem("Load SELF module...");
            miSelf.Click += new EventHandler(loadSelfToolStripMenuItem_Click);
            fileToolStripMenuItem.DropDownItems.Add(miSelf);

            fileToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem miMmio = new ToolStripMenuItem("Show MMIO access log");
            miMmio.Click += new EventHandler(mmioLogToolStripMenuItem_Click);
            fileToolStripMenuItem.DropDownItems.Add(miMmio);

            openImageDialog = new OpenFileDialog();
            openImageDialog.Title = "Open a binary image for the sandbox";
            openImageDialog.Filter = "All files (*.*)|*.*";
        }

        private ListView NewListView(params string[] columns)
        {
            ListView lv = new ListView();
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.HideSelection = false;
            for (int i = 0; i < columns.Length; i++)
                lv.Columns.Add(columns[i], i == 0 ? 80 : 100);
            return lv;
        }

        private TabPage NewTab(string title, Control content)
        {
            TabPage page = new TabPage(title);
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            return page;
        }

        private void AttachToTab(TabPage page, Control content)
        {
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
        }

        private void AddToolButton(string caption, EventHandler handler)
        {
            ToolStripButton b = new ToolStripButton(caption);
            b.DisplayStyle = ToolStripItemDisplayStyle.Text;
            b.Click += handler;
            toolStrip1.Items.Add(b);
        }

        private void refreshViewsToolStrip_Click(object sender, EventArgs e)
        {
            RefreshViews();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            testSuite.ReportListView = listView1;

            testSuite.report = new Report();
            testSuite.report.LinkSuite(testSuite);

            testSuite.sandbox = new Sandbox(testSuite.report);
            testSuite.sandbox.SetArch(CpuArch.Unknown);

            testSuite.cmd = new CommandProcessor();
            testSuite.cmd.LinkSuite(testSuite);

            testSuite.DumpMemory = new DumpDelegate(Dump);
            testSuite.RefreshViews = new Action(RefreshViews);

            // live MMIO view
            testSuite.sandbox.Mem.OnAccess = OnAccess;

            if (File.Exists("Autoexec.cmd"))
                ExecuteBatch("Autoexec.cmd");
            else
                Report("No Autoexec.cmd found; use File/Open Image or 'arch <arm|mep|venezia|rl78>' then 'open <file>'");

            Report("Initialized. Type 'help' for the command list.");
        }

        private void OnAccess(AccessRecord rec, string deviceName)
        {
            if (!testSuite.report.EchoMmio) return;
            testSuite.report.Log("mmio", AccessLog.Describe(rec, deviceName, testSuite.sandbox.Mem.CurrentPC.ToString("X8")));
        }

        /// <summary>Refresh the register / disassembly / MMIO panes (after every command).</summary>
        public void RefreshViews()
        {
            Sandbox sb = testSuite.sandbox;

            if (sb == null || sb.Cpu == null)
            {
                labelCpuState.Text = "No core selected";
                return;
            }

            int slot = SlotOf(sb.Arch);

            if (slot >= 0)
            {
                List<RegValue> regs = new List<RegValue>();
                sb.Cpu.GetRegisters(regs);

                ListView lv = regViews[slot];
                lv.BeginUpdate();
                lv.Items.Clear();
                foreach (RegValue r in regs)
                {
                    ListViewItem it = new ListViewItem(r.Name);
                    it.SubItems.Add(r.Value.ToString("X8"));
                    it.SubItems.Add(r.Note ?? "");
                    lv.Items.Add(it);
                }
                lv.EndUpdate();

                if (slot != lastSlot)
                {
                    lastSlot = slot;
                    tabControl1.SelectedTab = regPages[slot];
                    tabControl2.SelectedTab = disPages[slot];
                }

                RefreshDisasm(slot);
            }

            string status = sb.Arch == CpuArch.MeP && sb.MeP != null && sb.MeP.Profile == MePProfile.Venezia
                ? "Venezia (MeP-c5 + IVC2)"
                : CoreName(sb.Arch);

            labelCpuState.Text = status + "  PC=" + sb.Cpu.Pc.ToString("X8") + "  steps=" + sb.Steps +
                "  " + sb.Cpu.StatusLine + (sb.Cpu.Halted ? "  HALTED: " + sb.Cpu.HaltReason : "");

            RefreshMmioLog();
        }

        private void RefreshDisasm(int slot)
        {
            Sandbox sb = testSuite.sandbox;
            ListView lv = disViews[slot];
            uint addr = sb.Cpu.Pc;

            lv.BeginUpdate();
            lv.Items.Clear();

            for (int i = 0; i < 40; i++)
            {
                int len;
                string text;
                try
                {
                    text = sb.Cpu.Disassemble(addr, out len);
                }
                catch (Exception ex)
                {
                    text = "<disasm error: " + ex.Message + ">";
                    len = 4;
                }

                if (len <= 0) len = 4;

                string bytes = "";
                for (int b = 0; b < len && b < 8; b++)
                    bytes += sb.Mem.ReadByte(addr + (uint)b).ToString("X2");

                string mn = text;
                string par = "";
                int sp = text.IndexOf(' ');
                if (sp > 0) { mn = text.Substring(0, sp); par = text.Substring(sp + 1).Trim(); }

                ListViewItem it = new ListViewItem(addr.ToString("X8"));
                it.SubItems.Add(bytes);
                it.SubItems.Add(mn);
                it.SubItems.Add(par);
                if (sb.Cpu.Breakpoints.Contains(addr)) it.BackColor = Color.MistyRose;
                if (addr == sb.Cpu.Pc) it.BackColor = Color.LightYellow;
                lv.Items.Add(it);

                addr += (uint)len;
            }

            lv.EndUpdate();
        }

        /// <summary>Show the most recent MMIO / unmapped accesses in the bottom pane.</summary>
        private void RefreshMmioLog()
        {
            Sandbox sb = testSuite.sandbox;
            if (sb == null || listViewMmio == null) return;

            List<AccessRecord> recs = sb.Mem.Access.Snapshot(300);

            listViewMmio.BeginUpdate();
            listViewMmio.Items.Clear();

            for (int i = recs.Count - 1; i >= 0; i--)
            {
                AccessRecord r = recs[i];
                string kind = r.Kind == AccessKind.Fetch ? "F" : r.Kind == AccessKind.Read ? "R" : "W";

                ListViewItem it = new ListViewItem(kind + (r.Size * 8).ToString());
                it.SubItems.Add(r.Address.ToString("X8"));
                it.SubItems.Add(r.Size == 1 ? (r.Value & 0xFF).ToString("X2")
                    : r.Size == 2 ? (r.Value & 0xFFFF).ToString("X4")
                    : r.Value.ToString("X8"));

                string dev = sb.Mem.Access.DeviceName(r.Device);
                if (r.Unmapped) dev = "[UNMAPPED]";
                else
                {
                    MmioDevice d = sb.Mem.FindDevice(r.Address);
                    if (d != null)
                    {
                        string reg = d.RegisterName(r.Address);
                        if (reg != null) dev = d.Name + "." + reg;
                    }
                }
                it.SubItems.Add(dev);
                it.SubItems.Add(r.Pc.ToString("X8"));
                if (r.Unmapped) it.BackColor = Color.MistyRose;
                listViewMmio.Items.Add(it);
            }

            listViewMmio.EndUpdate();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAbout aboutDialog = new FormAbout();
            aboutDialog.Show();
        }

        private void hexBoxTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[] Data = new byte[256];

            for (int i = 0; i < Data.Length; i++)
                Data[i] = (byte)(i & 0xff);

            hexBox1.LineInfoOffset = 0;
            hexBox1.ByteProvider = new DynamicByteProvider(Data);
        }

        private void loadDumpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormLoadDump loadDumpDlg = new FormLoadDump();
            loadDumpDlg.FormClosed += loadDumpDlg_FormClosed;
            loadDumpDlg.ShowDialog();
        }

        void loadDumpDlg_FormClosed(object sender, FormClosedEventArgs e)
        {
            FormLoadDump loadDumpDlg = (FormLoadDump)sender;

            if (loadDumpDlg.Processed)
            {
                byte[] Data = File.ReadAllBytes(loadDumpDlg.FileName);
                testSuite.sandbox.Mem.EnsureRamFor(loadDumpDlg.Address, Data.Length, "dump");
                bool Res = testSuite.sandbox.Mem.LoadDump(loadDumpDlg.Address, Data);

                if (Res)
                    Report("Loaded dump: " + loadDumpDlg.FileName + ", Address: 0x" + loadDumpDlg.Address.ToString("X8") +
                        " , " + Data.Length.ToString() + " bytes");
                else
                    Report("Load dump failed!");
            }
        }

        private void Report(string text)
        {
            testSuite.report.Echo(text);
        }

        private void printMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Report("Foo");
        }

        private void ExecuteBatch(string Filename)
        {
            string[] lines = File.ReadAllLines(Filename);
            foreach (string str in lines)
            {
                if (str.Trim().Length == 0) continue;
                if (str.TrimStart().StartsWith("#")) continue;
                testSuite.cmd.Execute(str);
            }
        }

        private void executeCmdScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();

            if (res == DialogResult.OK)
            {
                ExecuteBatch(openFileDialog1.FileName);
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (textBox1.Text.Length > 0)
                {
                    testSuite.cmd.Execute(textBox1.Text);
                    textBox1.Text = "";
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                testSuite.cmd.Execute(textBox1.Text);
                textBox1.Text = "";
            }
        }

        private void Dump(uint Address, int Size)
        {
            byte[] Data = new byte[Size];

            hexBox1.LineInfoOffset = Address;

            for (int i = 0; i < Size; i++)
                Data[i] = testSuite.sandbox.Mem.ReadByte(Address++);

            hexBox1.ByteProvider = new DynamicByteProvider(Data);
            tabControl3.SelectedTab = tabPage5;
        }

        private void memoryPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormEnterValue enterValue = new FormEnterValue();
            enterValue.SetLabel("Address:");
            enterValue.FormClosed += enterValue_FormClosed;
            enterValue.ShowDialog();
        }

        void enterValue_FormClosed(object sender, FormClosedEventArgs e)
        {
            FormEnterValue enterValue = (FormEnterValue)sender;

            if (enterValue.Processed)
                Dump(enterValue.Value, 0x1000);
        }

        private void loadSceELFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSelfLoad selfLoader = new FormSelfLoad();
            selfLoader.FormClosed += selfLoader_FormClosed;
            selfLoader.Show();
        }

        void selfLoader_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        // ---- execution / loading ------------------------------------------

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = openImageDialog.ShowDialog();

            if (res == DialogResult.OK)
                testSuite.cmd.Execute("open \"" + openImageDialog.FileName + "\"");
        }

        private void buttonStep_Click(object sender, EventArgs e)
        {
            testSuite.cmd.Execute("step 1");
        }

        private void buttonStep10_Click(object sender, EventArgs e)
        {
            testSuite.cmd.Execute("step 10");
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            testSuite.cmd.Execute("run 100000");
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            testSuite.cmd.Execute("reset");
        }

        private void loadFirstLoaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = openImageDialog.ShowDialog();
            if (res == DialogResult.OK)
                testSuite.cmd.Execute("loadfirstloader \"" + openImageDialog.FileName + "\"");
        }

        private void loadErnieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = openImageDialog.ShowDialog();
            if (res == DialogResult.OK)
                testSuite.cmd.Execute("loadernie \"" + openImageDialog.FileName + "\"");
        }

        private void loadSelfToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = openImageDialog.ShowDialog();
            if (res == DialogResult.OK)
                testSuite.cmd.Execute("loadself \"" + openImageDialog.FileName + "\"");
        }

        private void mmioLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            testSuite.cmd.Execute("mmiolog tail 200");
            if (bottomTabs != null && bottomTabs.TabPages.Count > 1)
                bottomTabs.SelectedIndex = 1;
        }

        private void listViewDisasm_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView lv = sender as ListView;
            if (lv == null || lv.SelectedItems.Count == 0) return;

            string addrText = lv.SelectedItems[0].Text;
            uint addr;
            if (uint.TryParse(addrText, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out addr))
            {
                Dump(addr, 0x100);
            }
        }
    }

    public delegate void DumpDelegate(uint Address, int Size);

    public class TestSuiteContext
    {
        public Sandbox sandbox;
        public Report report;
        public CommandProcessor cmd;
        public DumpDelegate DumpMemory;
        public ListView ReportListView;
        public Action RefreshViews;

        public MemoryHub memoryHub
        {
            get { return sandbox != null ? sandbox.Mem : null; }
        }
    }
}
