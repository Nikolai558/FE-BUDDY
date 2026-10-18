using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers <see cref="CrashReport"/>, which <c>App</c>'s last-chance handlers write for an exception
/// nothing caught, on whichever thread: every exception in the chain, the stack, and never a throw of
/// its own.
/// </summary>
[Collection("AppLog")]
public sealed class CrashReportTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_CrashReport_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the log at a throwaway folder.</summary>
	public CrashReportTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
	}

	/// <summary>Restores the real log and deletes the folder.</summary>
	public void Dispose()
	{
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	[Fact]
	public void the_report_names_every_exception_in_the_chain_and_keeps_the_stack()
	{
		Exception crash;

		try
		{
			throw new InvalidOperationException("outer", new IOException("inner"));
		}
		catch (InvalidOperationException ex)
		{
			crash = ex;
		}

		CrashReport.Write(crash, "off the UI thread", _root);

		string report = File.ReadAllText(Path.Combine(_root, CrashReport.FileName));
		Assert.StartsWith("System.InvalidOperationException: outer", report, StringComparison.Ordinal);
		Assert.Contains("System.IO.IOException: inner", report, StringComparison.Ordinal);
		Assert.Contains(nameof(the_report_names_every_exception_in_the_chain_and_keeps_the_stack), report, StringComparison.Ordinal);
		Assert.Contains(AppLog.Entries, entry => entry.Message == "Unhandled exception off the UI thread: outer");
	}

	[Fact]
	public void a_report_that_cannot_be_written_never_throws()
	{
		Directory.CreateDirectory(_root);
		string notAFolder = Path.Combine(_root, "file");
		File.WriteAllText(notAFolder, string.Empty);

		CrashReport.Write(new InvalidOperationException("boom"), "on the UI thread", notAFolder);

		Assert.Equal(string.Empty, File.ReadAllText(notAFolder));
	}
}
