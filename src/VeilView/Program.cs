using System;
using System.Windows.Forms;

namespace VeilView;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        WebView2NativeBootstrap.RegisterResolver();

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var settings = AppSettings.Load();
        var options = AppOptions.Parse(args, settings);

        Application.Run(new OverlayBrowserForm(options, settings));
    }
}
