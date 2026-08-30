using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace FeBuddy.Wpf;

/// <summary>
/// Application entry point. No host, no DI, no services - this project is a UI
/// shell. The only logic here is a last-chance handler that writes an unhandled
/// exception to disk so a crash on a user's machine leaves a trace.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnUnhandled;
        base.OnStartup(e);
    }

    private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "febuddy-wpf-crash.txt");
            var sb = new System.Text.StringBuilder();
            for (Exception? ex = e.Exception; ex is not null; ex = ex.InnerException)
            {
                sb.AppendLine(ex.GetType().FullName + ": " + ex.Message);
                if (ex is System.Windows.Markup.XamlParseException xpe)
                {
                    sb.AppendLine($"  BaseUri={xpe.BaseUri}  Line={xpe.LineNumber}  Pos={xpe.LinePosition}");
                }
            }
            sb.AppendLine();
            sb.AppendLine(e.Exception.ToString());
            File.WriteAllText(path, sb.ToString());
        }
        catch
        {
            // Nothing useful to do if even logging fails.
        }
    }
}
