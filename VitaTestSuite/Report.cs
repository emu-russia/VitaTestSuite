// Debug reports

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using VitaTestSuite;

public class Report
{
    private TestSuiteContext TestSuite;

    public void LinkSuite ( TestSuiteContext Ctx )
    {
        TestSuite = Ctx;
    }

    public void Echo (string Text)
    {
        string stamp = DateTime.Now.ToString("HH:mm:ss.fff");
        ListViewItem item = new ListViewItem(stamp);
        item.SubItems.Add(Text);
        ListViewItem added = TestSuite.ReportListView.Items.Add(item);
        TestSuite.ReportListView.EnsureVisible(added.Index);
    }

}