using System.IO;
using System.Text;
using System.Windows.Markup;

using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// The crash report: an exception nothing else caught, written to <c>febuddy-wpf-crash.txt</c> beside
/// the day's log, so it is attached with the log and removed on uninstall.
/// </summary>
internal static class CrashReport
{
	/// <summary>The report's file name.</summary>
	internal const string FileName = "febuddy-wpf-crash.txt";

	/// <summary>
	/// Writes the report, replacing any earlier one, and logs the exception. Written straight to disk
	/// rather than through the log's background writer, which a dying process may never flush. Never throws.
	/// </summary>
	/// <param name="exception">What nothing caught.</param>
	/// <param name="where">Where it was caught, for the log, e.g. <c>on the UI thread</c>.</param>
	/// <param name="directory">The folder to write to; the log folder unless a test gives another.</param>
	internal static void Write(Exception exception, string where, string? directory = null)
	{
		try
		{
			directory ??= AppLog.LogDirectory;
			Directory.CreateDirectory(directory);

			StringBuilder report = new();

			for (Exception? ex = exception; ex is not null; ex = ex.InnerException)
			{
				report.AppendLine(ex.GetType().FullName + ": " + ex.Message);

				if (ex is XamlParseException xaml)
				{
					report.AppendLine($"  BaseUri={xaml.BaseUri}  Line={xaml.LineNumber}  Pos={xaml.LinePosition}");
				}
			}

			report.AppendLine();
			report.AppendLine(exception.ToString());
			File.WriteAllText(Path.Combine(directory, FileName), report.ToString());

			AppLog.Error("App", $"Unhandled exception {where}: {exception.Message}");
		}
		catch
		{
			// Nothing useful to do if even this fails.
		}
	}
}
