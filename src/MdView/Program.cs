using Avalonia;
using System;

namespace MdView;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Silence WebView2/Chromium INFO+ERROR logs (e.g. the benign
        // "Failed to unregister class Chrome_WidgetWin_0" 1412 message at exit).
        if (Environment.GetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS") is null)
        {
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--log-level=3");
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .AfterSetup(_ => App.RegisterActivatableLifetime());
}
