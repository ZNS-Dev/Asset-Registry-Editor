using Asset_Registry_Editor;
using System;
using System.Windows.Forms;

namespace Codes;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (!NewtonsoftJsonBootstrapper.EnsureAvailable())
            return;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (args.Length != 0)
            Application.Run(new AREditor(args[0]));
        else
            Application.Run(new AREditor());
    }
}
