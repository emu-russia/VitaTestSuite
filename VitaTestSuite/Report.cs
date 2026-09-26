// Debug reports / logging.
//
// The same Report object is used by the GUI (writes into a ListView) and by the
// headless script mode (writes to stdout and/or a log file).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

using VitaTestSuite;
using VitaTestSuite.Core;

public class Report : ILogSink
{
    private TestSuiteContext TestSuite;

    /// <summary>True when there is no ListView (console mode).</summary>
    public bool Headless;

    /// <summary>Where headless output goes (default: Console.Out).</summary>
    public TextWriter Writer = Console.Out;

    /// <summary>Optional log file: everything is duplicated there.</summary>
    public TextWriter FileWriter;

    /// <summary>Echo MMIO device accesses into the report (very verbose).</summary>
    public bool EchoMmio = true;

    /// <summary>Echo every executed instruction (extremely verbose).</summary>
    public bool EchoTrace;

    /// <summary>Maximum number of items kept in the GUI ListView.</summary>
    public int MaxViewItems = 5000;

    /// <summary>Per-category enable flags. Unknown categories default to enabled.</summary>
    private readonly Dictionary<string, bool> categories = new Dictionary<string, bool>();

    public Report()
    {
        categories["mmio"] = true;
        categories["cpu"] = true;
        categories["loader"] = true;
        categories["keyring"] = true;
        categories["trace"] = false;
        categories["mmu"] = false;
    }

    public void LinkSuite(TestSuiteContext Ctx)
    {
        TestSuite = Ctx;
    }

    public void SetCategory(string category, bool enabled)
    {
        categories[category] = enabled;
    }

    public bool IsEnabled(string category)
    {
        if (category == null) return true;

        bool v;
        if (categories.TryGetValue(category, out v)) return v;
        return true;
    }

    public void Echo(string Text)
    {
        Write(Text);
    }

    /// <summary>Number of error/failure messages emitted (used as the headless exit code).</summary>
    public int ErrorCount;

    /// <summary>Report a failure; counted so scripted runs can fail with a non-zero exit code.</summary>
    public void EchoError(string Text)
    {
        ErrorCount++;
        Write(Text);
    }

    public void Log(string category, string text)
    {
        if (category == "mmio" && !EchoMmio) return;
        if (category == "trace" && !EchoTrace) return;
        if (!IsEnabled(category)) return;

        Write("[" + category + "] " + text);
    }

    private void Write(string text)
    {
        string stamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

        if (FileWriter != null)
        {
            FileWriter.Write(stamp);
            FileWriter.Write(' ');
            FileWriter.WriteLine(text);
            FileWriter.Flush();
        }

        if (Headless || TestSuite == null || TestSuite.ReportListView == null)
        {
            TextWriter w = Writer ?? Console.Out;
            w.Write(stamp);
            w.Write(' ');
            w.WriteLine(text);
            return;
        }

        ListViewItem item = new ListViewItem(stamp);
        item.SubItems.Add(text);
        ListViewItem added = TestSuite.ReportListView.Items.Add(item);

        if (TestSuite.ReportListView.Items.Count > MaxViewItems)
            TestSuite.ReportListView.Items.RemoveAt(0);

        TestSuite.ReportListView.EnsureVisible(added.Index);
    }
}
