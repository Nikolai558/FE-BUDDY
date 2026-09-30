using System.ComponentModel;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.FileSystem.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Infrastructure.FileSystem;

/// <summary>
/// Covers <see cref="AppDataReset"/>: a reset asked for is carried out once, at the next launch;
/// it empties FE-Buddy's folder but for the settings when they are kept; it removes the
/// credentials only when asked; and it names whatever it could not delete - against a throwaway
/// folder and an in-memory credential vault.
/// </summary>
[Collection("AppLog")]
public sealed class AppDataResetTests : IDisposable
{
	private static readonly TimeSpan NoWait = TimeSpan.Zero;

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Reset_" + Guid.NewGuid().ToString("N"));
	private readonly string _appData;
	private readonly InMemoryCredentialVault _vault = new();
	private readonly CredentialStore _credentials;

	/// <summary>Points the reset and the log at a throwaway folder, and fills it the way FE-Buddy would.</summary>
	public AppDataResetTests()
	{
		_appData = Path.Combine(_root, "FE-Buddy");
		AppLog.ConfigureForTesting(Path.Combine(_root, "test-logs"));
		AppDataReset.ConfigureForTesting(_appData);
		_credentials = new CredentialStore(_vault);

		Write("UserConfig.json", "{}");
		Write("UserConfig.previous.json", "{}");
		Write("UserConfig.before-import.json", "{}");
		Write(@"AiracCycles\2610\APT_BASE.csv", "x");
		Write(@"Logs\FE-Buddy_2026-09-29.log", "x");
		Write(@"Telephony\telephony.html", "x");
		Write(@"WxStations\stations.xml", "x");

		_credentials.Save(new CredentialDraft(null, "ZOB GitHub", CredentialKind.GitHubToken, null, "token", CredentialHosts.GitHubDefaults));
	}

	/// <summary>Restores the defaults and deletes the folder.</summary>
	public void Dispose()
	{
		AppDataReset.ConfigureForTesting(null);
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
	public void with_no_reset_asked_for_nothing_is_deleted()
	{
		Assert.Null(AppDataReset.RunPending(_credentials, NoWait));

		Assert.Equal(7, Directory.EnumerateFiles(_appData, "*", SearchOption.AllDirectories).Count());
		Assert.Single(_credentials.List());
	}

	/// <summary>Asking only records the reset: nothing goes until the next launch carries it out.</summary>
	[Fact]
	public void asking_for_a_reset_deletes_nothing_yet()
	{
		AppDataReset.Request(new AppDataResetRequest(KeepSettings: false, DeleteCredentials: true));

		Assert.True(File.Exists(AppDataReset.RequestFilePath));
		Assert.True(Directory.Exists(Path.Combine(_appData, "AiracCycles")));
		Assert.Single(_credentials.List());
	}

	[Fact]
	public void keeping_the_settings_empties_everything_else()
	{
		AppDataReset.Request(new AppDataResetRequest(KeepSettings: true, DeleteCredentials: false, WaitForProcessId: Environment.ProcessId));

		AppDataResetResult? result = AppDataReset.RunPending(_credentials, NoWait);

		Assert.NotNull(result);
		Assert.True(result.SettingsKept);
		Assert.Null(result.CredentialsRemoved);
		Assert.Empty(result.NotDeleted);
		Assert.Equal(["UserConfig.json"], Names(_appData));
		Assert.Single(_credentials.List());
	}

	[Fact]
	public void deleting_the_settings_and_credentials_leaves_nothing()
	{
		_credentials.Save(new CredentialDraft(null, "Other", CredentialKind.Token, null, "abc", ["example.org"]));
		AppDataReset.Request(new AppDataResetRequest(KeepSettings: false, DeleteCredentials: true, WaitForProcessId: int.MaxValue));

		AppDataResetResult? result = AppDataReset.RunPending(_credentials, NoWait);

		Assert.NotNull(result);
		Assert.False(result.SettingsKept);
		Assert.Equal(2, result.CredentialsRemoved);
		Assert.Empty(Directory.EnumerateFileSystemEntries(_appData));
		Assert.Empty(_credentials.List());
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Success && e.Message.StartsWith("FE-Buddy was reset: settings deleted, 2 credential(s) removed", StringComparison.Ordinal));
	}

	[Fact]
	public void a_reset_is_carried_out_once()
	{
		AppDataReset.Request(new AppDataResetRequest(KeepSettings: true, DeleteCredentials: false));
		Assert.NotNull(AppDataReset.RunPending(_credentials, NoWait));

		Write(@"Logs\FE-Buddy_2026-09-30.log", "x");

		Assert.Null(AppDataReset.RunPending(_credentials, NoWait));
		Assert.True(File.Exists(Path.Combine(_appData, "Logs", "FE-Buddy_2026-09-30.log")));
	}

	/// <summary>A file still in use keeps its folder; it is named, and everything else still goes.</summary>
	[Fact]
	public void what_is_in_use_is_named_and_the_rest_still_goes()
	{
		AppDataReset.Request(new AppDataResetRequest(KeepSettings: false, DeleteCredentials: false));

		AppDataResetResult? result;
		using (new FileStream(Path.Combine(_appData, "Logs", "FE-Buddy_2026-09-29.log"), FileMode.Open, FileAccess.Read, FileShare.None))
		{
			result = AppDataReset.RunPending(_credentials, NoWait);
		}

		Assert.Equal(["Logs"], result!.NotDeleted);
		Assert.Equal(["Logs"], Names(_appData));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.StartsWith("Could not delete", StringComparison.Ordinal));
	}

	[Fact]
	public void credentials_that_cannot_be_removed_are_named()
	{
		_vault.Failure = new Win32Exception(5);
		AppDataReset.Request(new AppDataResetRequest(KeepSettings: true, DeleteCredentials: true));

		AppDataResetResult? result = AppDataReset.RunPending(_credentials, NoWait);

		Assert.Null(result!.CredentialsRemoved);
		Assert.Equal(["saved credentials"], result.NotDeleted);
	}

	/// <summary>A reset must never guess: a request it cannot read deletes nothing, and is not tried again.</summary>
	[Fact]
	public void a_request_that_cannot_be_read_deletes_nothing()
	{
		File.WriteAllText(AppDataReset.RequestFilePath, "not a request");

		Assert.Null(AppDataReset.RunPending(_credentials, NoWait));
		Assert.False(File.Exists(AppDataReset.RequestFilePath));
		Assert.True(File.Exists(Path.Combine(_appData, "UserConfig.json")));
		Assert.True(Directory.Exists(Path.Combine(_appData, "AiracCycles")));
		Assert.Contains(AppLog.Entries, e => e.Message.Contains("could not be read, so nothing was deleted", StringComparison.Ordinal));
	}

	[Fact]
	public void bad_arguments_are_refused()
	{
		Assert.Throws<ArgumentNullException>(() => AppDataReset.Request(null!));
		Assert.Throws<ArgumentNullException>(() => AppDataReset.RunPending(null!, NoWait));
	}

	[Fact]
	public void by_default_the_folder_is_fe_buddys_app_data()
	{
		AppDataReset.ConfigureForTesting(null);

		Assert.Equal(AppPaths.AppDataDirectory, AppDataReset.RootDirectory);
		Assert.Equal(Path.Combine(AppPaths.AppDataDirectory, "Reset.pending.json"), AppDataReset.RequestFilePath);
	}

	private static string[] Names(string folder) =>
		[.. Directory.EnumerateFileSystemEntries(folder).Select(path => Path.GetFileName(path))];

	private void Write(string relativePath, string text)
	{
		string path = Path.Combine(_appData, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, text);
	}
}
