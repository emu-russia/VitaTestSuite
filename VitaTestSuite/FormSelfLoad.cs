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
using System.Runtime.InteropServices;
using Be.Windows.Forms;

namespace VitaTestSuite
{
    public partial class FormSelfLoad : Form
    {
        public string SelfPath;
        public bool SelfSelected = false;

        public FormSelfLoad()
        {
            InitializeComponent();
        }

        private void chooseSELFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();

            if ( res == DialogResult.OK )
            {
                SelfPath = openFileDialog1.FileName;
                LoadSelf(SelfPath);
                SelfSelected = true;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelfSelected = false;
            Close();
        }

        private void LoadSelf (string filename)
        {
            FileStream fs = File.Open(filename, FileMode.Open);

            byte[] sceHeaderRaw = new byte[Marshal.SizeOf(typeof(SCEHeader))];
            fs.Read(sceHeaderRaw, 0, sceHeaderRaw.Length);
            GCHandle handle = GCHandle.Alloc(sceHeaderRaw, GCHandleType.Pinned);
            SCEHeader sceHeader = (SCEHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SCEHeader));
            SCEHeaderWrapper sceHeaderWrapper = new SCEHeaderWrapper(sceHeader);
            handle.Free();

            hexBox1.ByteProvider = new DynamicByteProvider(sceHeaderRaw);
            propertyGrid1.SelectedObject = (object)sceHeaderWrapper;
        }

    }


}
