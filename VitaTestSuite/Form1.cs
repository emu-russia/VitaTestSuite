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

namespace VitaTestSuite
{
    public partial class Form1 : Form
    {
        private TestSuiteContext testSuite = new TestSuiteContext();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            testSuite.memoryHub = new MemoryHub();
            testSuite.memoryHub.LittleEndian = true;          // PS Vita is little endian (?)

            testSuite.ReportListView = listView1;

            testSuite.report = new Report();
            testSuite.report.LinkSuite(testSuite);

            testSuite.cmd = new CommandProcessor();
            testSuite.cmd.LinkSuite(testSuite);

            testSuite.DumpMemory = new DumpDelegate(Dump);

            ExecuteBatch("Autoexec.cmd");

            Report("Initialized");
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

            if ( loadDumpDlg.Processed )
            {
                byte[] Data = File.ReadAllBytes(loadDumpDlg.FileName);
                bool Res = testSuite.memoryHub.LoadDump(loadDumpDlg.Address, Data);

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

        private void ExecuteBatch (string Filename)
        {
            string[] lines = File.ReadAllLines(Filename);
            foreach (string str in lines)
            {
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
            if ( e.KeyCode == Keys.Enter )
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

        private void Dump (uint Address, int Size)
        {
            byte[] Data = new byte[Size];

            hexBox1.LineInfoOffset = Address;

            for(int i=0; i<Size; i++)
            {
                Data[i] = testSuite.memoryHub.ReadByte(Address++);
            }

            hexBox1.ByteProvider = new DynamicByteProvider(Data);
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

    }

    public delegate void DumpDelegate(uint Address, int Size);

    public class TestSuiteContext
    {
        public MemoryHub memoryHub;
        public ListView ReportListView;
        public Report report;
        public CommandProcessor cmd;
        public DumpDelegate DumpMemory;
    }

}
