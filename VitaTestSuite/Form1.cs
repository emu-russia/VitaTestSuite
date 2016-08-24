using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Be.Windows.Forms;

namespace VitaTestSuite
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
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
    }
}
