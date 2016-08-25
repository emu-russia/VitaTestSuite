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
    public partial class FormEnterValue : Form
    {
        public bool Processed = false;
        public uint Value;

        public FormEnterValue()
        {
            InitializeComponent();
        }

        private uint Strtoul(string text)
        {
            return text.StartsWith("0x") || text.StartsWith("0X") ?
                Convert.ToUInt32(text, 16) : Convert.ToUInt32(text, 10);
        }

        private void FormEnterValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
            else if (e.KeyCode == Keys.Enter)
                Process();
        }

        private void Process ()
        {
            if ( textBox1.Text.Length > 0 )
            {
                Value = Strtoul(textBox1.Text);
                Processed = true;
            }
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process();
        }

        public void SetLabel (string text)
        {
            label1.Text = text;
        }
    }
}
