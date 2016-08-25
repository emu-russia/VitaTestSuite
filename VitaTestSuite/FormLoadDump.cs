using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VitaTestSuite
{
    public partial class FormLoadDump : Form
    {
        public bool Processed = false;
        public uint Address = 0xffffffff;
        public string FileName;
        private bool FileNameSpecified = false;
        private bool AddressValid = false;

        public FormLoadDump()
        {
            InitializeComponent();
        }

        private uint Strtoul (string text)
        {
            if (text.Length > 0)
            {
                AddressValid = true;

                return text.StartsWith("0x") || text.StartsWith("0X") ?
                    Convert.ToUInt32(text, 16) : Convert.ToUInt32(text, 10);
            }
            else
                return 0;
        }

        private void FormLoadDump_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }

        /// <summary>
        /// Cancel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Ok
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            Address = Strtoul(textBox2.Text);

            if (AddressValid && FileNameSpecified)
            {
                Processed = true;
            }

            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult Res = openFileDialog1.ShowDialog();

            if ( Res == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                FileName = textBox1.Text;
                FileNameSpecified = true;
            }
        }

    }
}
