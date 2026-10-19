using System.Diagnostics;

namespace FeBuddy.UnitTests.Repository;

/// <summary>
/// Runs <c>FeBuddy.Installer\Get-InstallerVersion.ps1</c> under Windows PowerShell 5.1, as <c>build.cmd</c> does
/// (O1-23): the counter it advances is written as UTF-8 without a byte-order mark, laid out exactly as the tracked
/// file is, so a build changes one number and <see cref="FileEncodingTests"/> still passes.
/// </summary>
public sealed class InstallerVersionScriptTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_InstallerVersion_" + Guid.NewGuid().ToString("N"));

	/// <summary>Deletes the throwaway folder.</summary>
	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	private static string Counter(int major, int minor, int build) =>
		$"{{\r\n  \"major\": {major},\r\n  \"counterMinor\": {minor},\r\n  \"counterBuild\": {build}\r\n}}\r\n";

	/// <summary>Runs the script with Windows PowerShell on a copy of the counter, returning what it prints.</summary>
	private string Run(string realVersion, string counterPath)
	{
		string script = Path.Combine(FindRepositoryRoot(), "FeBuddy", "FeBuddy.Installer", "Get-InstallerVersion.ps1");
		string powershell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");

		ProcessStartInfo start = new(powershell)
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = _root,
		};
		foreach (string argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script, "-RealVersion", realVersion, "-CounterFilePath", counterPath })
		{
			start.ArgumentList.Add(argument);
		}

		using Process process = Process.Start(start)!;
		string output = process.StandardOutput.ReadToEnd();
		string error = process.StandardError.ReadToEnd();
		process.WaitForExit();

		Assert.True(process.ExitCode == 0, $"Get-InstallerVersion.ps1 failed ({process.ExitCode}): {error}");
		return output.Trim();
	}

	/// <summary>A release advances the build number, and the file is the tracked layout with no byte-order mark.</summary>
	[Fact]
	public void the_counter_is_advanced_and_written_without_a_byte_order_mark()
	{
		Directory.CreateDirectory(_root);
		string counter = Path.Combine(_root, "installer-version-counter.json");
		File.WriteAllText(counter, Counter(3, 0, 17));

		Assert.Equal("3.0.18", Run("3.0.0-beta.13", counter));
		Assert.Equal(System.Text.Encoding.UTF8.GetBytes(Counter(3, 0, 18)), File.ReadAllBytes(counter));
	}

	/// <summary>A new major version starts its counter over; a counter that isn't there yet starts at 0.0.</summary>
	[Fact]
	public void a_new_major_or_a_missing_counter_starts_over()
	{
		Directory.CreateDirectory(_root);
		string counter = Path.Combine(_root, "installer-version-counter.json");
		File.WriteAllText(counter, Counter(3, 4, 65535));

		Assert.Equal("4.0.0", Run("4.0.0", counter));
		Assert.Equal(Counter(4, 0, 0), File.ReadAllText(counter));

		File.Delete(counter);
		Assert.Equal("4.0.0", Run("4.0.0", "installer-version-counter.json"));
		Assert.Equal(Counter(4, 0, 0), File.ReadAllText(counter));
	}

	/// <summary>The folder holding <c>FeBuddy\FeBuddy.sln</c>, found by walking up from the test's own folder.</summary>
	private static string FindRepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "FeBuddy", "FeBuddy.sln")))
			{
				return folder.FullName;
			}
		}

		throw new DirectoryNotFoundException($"No repository root (holding FeBuddy\\FeBuddy.sln) above {AppContext.BaseDirectory}.");
	}
}
