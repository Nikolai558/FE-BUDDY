using System.Text.Json;

using FeBuddy.Core.Application.Launch.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Application.Launch;

/// <summary>
/// The launch steps a dev launch runs, as chosen on Settings' Developer card, kept in
/// <c>%APPDATA%\FE-Buddy\DevLaunch.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// The file is beside <c>User Configurations</c>, not in it, so it belongs to no settings profile, and
/// Import and Export Settings never see it. A normal launch never reads it. A reset that keeps the
/// settings keeps it too (see <c>AppDataReset</c>).
/// </para>
/// <para>
/// Each step is saved by name as <see langword="true"/> or <see langword="false"/>. A step that isn't
/// in the file, or a file that can't be read, is off: a dev launch skips every step until told otherwise.
/// </para>
/// </remarks>
public static class DevLaunchFile
{
	/// <summary>The file's name, in <c>%APPDATA%\FE-Buddy</c>.</summary>
	public const string FileName = AppDataReset.DevLaunchFileName;

	private const string LogSource = "DevLaunch";

	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	private static string? _directoryOverride;

	/// <summary>The file's full path.</summary>
	public static string FilePath => Path.Combine(_directoryOverride ?? AppPaths.AppDataDirectory, FileName);

	/// <summary>The steps a dev launch runs. Never throws: a missing or unreadable file means none.</summary>
	/// <returns>The steps.</returns>
	public static OptionalLaunchStep Read()
	{
		try
		{
			if (!File.Exists(FilePath))
			{
				return OptionalLaunchStep.None;
			}

			Dictionary<string, bool>? saved = JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(FilePath));
			OptionalLaunchStep steps = OptionalLaunchStep.None;

			foreach (OptionalLaunchStep step in OptionalLaunchSteps.InCardOrder)
			{
				if (saved?.GetValueOrDefault(step.ToString()) == true)
				{
					steps |= step;
				}
			}

			return steps;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
		{
			AppLog.Warning(LogSource, $"Could not read {FileName}, so a dev launch skips every optional step: {ex.Message}");
			return OptionalLaunchStep.None;
		}
	}

	/// <summary>Saves the steps a dev launch runs.</summary>
	/// <param name="steps">The steps.</param>
	/// <exception cref="IOException">The file couldn't be written.</exception>
	/// <exception cref="UnauthorizedAccessException">The folder can't be written to.</exception>
	public static void Save(OptionalLaunchStep steps)
	{
		Dictionary<string, bool> saved = OptionalLaunchSteps.InCardOrder.ToDictionary(step => step.ToString(), step => (steps & step) == step);

		Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
		File.WriteAllText(FilePath, JsonSerializer.Serialize(saved, JsonOptions));
	}

	/// <summary>Points the file at a throwaway folder. Unit tests only.</summary>
	/// <param name="directory">The folder, or <see langword="null"/> to restore <c>%APPDATA%\FE-Buddy</c>.</param>
	internal static void ConfigureForTesting(string? directory) => _directoryOverride = directory;
}
