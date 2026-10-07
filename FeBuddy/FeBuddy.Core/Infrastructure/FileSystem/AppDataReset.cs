using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.FileSystem.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.FileSystem;

/// <summary>
/// Resets FE-Buddy like new: deletes everything it keeps in <c>%APPDATA%\FE-Buddy</c> -
/// downloaded AIRAC cycles, Telephony and Wx Station data, logs and, unless they are kept, the
/// settings (every profile in <c>User Configurations</c>) - and, when asked, its credentials in
/// Windows Credential Manager.
/// </summary>
/// <remarks>
/// <para>
/// It is done in two halves. <see cref="Request"/> only records what to do, in a small file in the
/// folder; FE-Buddy then restarts, and the new FE-Buddy calls <see cref="RunPending"/> first thing,
/// before it opens a log, reads its settings or loads a cycle - so nothing it deletes is in use,
/// and nothing read into memory outlives it. The request file goes before anything else, so a
/// reset that fails half way is never tried again.
/// </para>
/// <para>
/// The output folder, the temporary folder (<see cref="TempWorkspace"/>, emptied every launch
/// anyway) and FE-Buddy 2.x's <c>%LOCALAPPDATA%\FE-BUDDY</c> are not touched.
/// </para>
/// </remarks>
public static class AppDataReset
{
	private const string LogSource = "Reset";
	private const string RequestFileName = "Reset.pending.json";

	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	private static string? _rootOverride;

	/// <summary>The folder a reset empties: <c>%APPDATA%\FE-Buddy</c>.</summary>
	public static string RootDirectory => _rootOverride ?? AppPaths.AppDataDirectory;

	/// <summary>Where <see cref="Request"/> records a reset for the next launch.</summary>
	public static string RequestFilePath => Path.Combine(RootDirectory, RequestFileName);

	/// <summary>Records a reset for the next launch to carry out. Nothing is deleted yet.</summary>
	/// <param name="request">What the reset keeps and deletes.</param>
	/// <exception cref="IOException">Thrown when the request cannot be written.</exception>
	/// <exception cref="UnauthorizedAccessException">Thrown when the folder cannot be written to.</exception>
	public static void Request(AppDataResetRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);

		Directory.CreateDirectory(RootDirectory);
		File.WriteAllText(RequestFilePath, JsonSerializer.Serialize(request, JsonOptions));

		AppLog.Info(LogSource,
			$"Reset asked for, to happen when FE-Buddy next starts: settings {(request.KeepSettings ? "kept" : "deleted")}, " +
			$"credentials {(request.DeleteCredentials ? "deleted" : "kept")}.");
	}

	/// <summary>
	/// Carries out the reset <see cref="Request"/> recorded, if there is one. Call it first thing at
	/// launch. Never throws: whatever cannot be deleted is named in the result and logged.
	/// </summary>
	/// <param name="credentials">The credential store, for a reset that deletes credentials.</param>
	/// <param name="waitForExit">How long to wait for the FE-Buddy that asked for the reset to close.</param>
	/// <returns>What was done; <see langword="null"/> when no reset was asked for.</returns>
	public static AppDataResetResult? RunPending(CredentialStore credentials, TimeSpan waitForExit)
	{
		ArgumentNullException.ThrowIfNull(credentials);

		if (TakeRequest() is not { } request)
		{
			return null;
		}

		WaitForExit(request.WaitForProcessId, waitForExit);

		// Every settings profile, Shared.json, and an older FE-Buddy's one file not yet moved in.
		string[] settings = [UserConfigFile.ProfilesFolderName, UserConfigFile.LegacyConfigFileName];
		List<string> notDeleted = [];

		foreach (string entry in Entries(RootDirectory))
		{
			string name = Path.GetFileName(entry);

			if (request.KeepSettings && settings.Contains(name, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			try
			{
				if (Directory.Exists(entry))
				{
					Directory.Delete(entry, recursive: true);
				}
				else
				{
					File.Delete(entry);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				notDeleted.Add(name);
				AppLog.Warning(LogSource, $"Could not delete '{entry}': {ex.Message}");
			}
		}

		int? credentialsRemoved = null;

		if (request.DeleteCredentials)
		{
			try
			{
				credentialsRemoved = credentials.DeleteAll();
			}
			catch (Win32Exception ex)
			{
				notDeleted.Add("saved credentials");
				AppLog.Warning(LogSource, $"Could not remove the saved credentials from Windows Credential Manager: {ex.Message}");
			}
		}

		AppLog.Success(LogSource,
			$"FE-Buddy was reset: settings {(request.KeepSettings ? "kept" : "deleted")}" +
			(credentialsRemoved is { } removed ? $", {removed} credential(s) removed" : string.Empty) +
			(notDeleted.Count > 0 ? $"; {notDeleted.Count} item(s) could not be deleted." : "."));

		return new AppDataResetResult(request.KeepSettings, credentialsRemoved, notDeleted);
	}

	/// <summary>Points the folder somewhere else. Unit tests only.</summary>
	/// <param name="root">A throwaway folder, or <see langword="null"/> to restore the default.</param>
	internal static void ConfigureForTesting(string? root) => _rootOverride = root;

	/// <summary>Reads and deletes the request, so it is carried out once whatever happens next.</summary>
	private static AppDataResetRequest? TakeRequest()
	{
		string path = RequestFilePath;

		if (!File.Exists(path))
		{
			return null;
		}

		try
		{
			string json = File.ReadAllText(path);
			File.Delete(path);
			return JsonSerializer.Deserialize<AppDataResetRequest>(json);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
		{
			// Nothing is deleted on a request that cannot be read: a reset must never guess.
			AppLog.Warning(LogSource, $"A reset was asked for, but its request could not be read, so nothing was deleted: {ex.Message}");
			TryDelete(path);
			return null;
		}
	}

	/// <summary>Waits for the FE-Buddy that asked for the reset to close, so none of its files are still open.</summary>
	private static void WaitForExit(int? processId, TimeSpan timeout)
	{
		if (processId is not { } id || id == Environment.ProcessId)
		{
			return;
		}

		try
		{
			using Process process = Process.GetProcessById(id);

			if (!process.WaitForExit(timeout))
			{
				AppLog.Warning(LogSource, "The FE-Buddy that asked for the reset had not closed yet; resetting anyway.");
			}
		}
		catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
		{
			// It has already closed.
		}
	}

	private static IEnumerable<string> Entries(string folder)
	{
		try
		{
			return Directory.Exists(folder) ? [.. Directory.EnumerateFileSystemEntries(folder)] : [];
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Could not read '{folder}': {ex.Message}");
			return [];
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Left for the next launch to report again.
		}
	}
}
