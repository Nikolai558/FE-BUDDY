using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Guards the saved-settings layout (<see cref="UserConfigVersion"/>), so testers never have to wipe
/// their settings for a new version. Each layout FE-Buddy has released has a sample
/// <c>UserConfig.json</c> in <c>Fixtures\UserConfig</c>, which never changes. Each sample is brought up
/// to today's layout (<see cref="UserConfigMigrations"/>) and read by every settings page, and what
/// the pages would save must keep every setting in it, with the same value. A setting renamed,
/// moved, re-formatted or dropped without a migration step fails here.
/// </summary>
/// <remarks>
/// A new setting with a default needs nothing: a file without it gets the default. Changing the
/// layout needs a new <see cref="UserConfigVersion.Current"/>, its step in
/// <see cref="UserConfigMigrations.All"/>, and a sample of the new layout (with its fingerprint in
/// <see cref="SampleFingerprints"/>).
/// </remarks>
[Collection("AppLog")]
public sealed class UserConfigLayoutTests : IDisposable
{
	/// <summary>
	/// Each sample's settings, fingerprinted (<see cref="Fingerprint"/>): a sample is the file its layout
	/// was released with, so it must never change, or it would stop guarding the files testers have.
	/// </summary>
	private static readonly Dictionary<int, string> SampleFingerprints = new()
	{
		[1] = "DBFE66FA9DAECE3CB37C161017294B8738C55FA2012E375C1CFC8D3A7BCD47D7",
		[2] = "4FF1F465DCE3FC23E03C96F9562F3267FC781B6E5396D3509FEC9ABCD667A33E",
	};

	private static readonly string SamplesFolder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "UserConfig");

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_UserConfigLayout_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public UserConfigLayoutTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		UserConfigFile.ConfigureForTesting(null);
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

	/// <summary>Every layout from the oldest FE-Buddy brings forward to the current one.</summary>
	private static readonly int[] LayoutVersions =
		[.. Enumerable.Range(UserConfigVersion.Oldest, UserConfigVersion.Current - UserConfigVersion.Oldest + 1)];

	/// <summary><see cref="LayoutVersions"/>, for the theories.</summary>
	public static TheoryData<int> every_layout() => [.. LayoutVersions];

	private static string SamplePath(int version) => Path.Combine(SamplesFolder, $"UserConfig.v{version}.json");

	/// <summary>A sample's settings, by dotted path.</summary>
	private static Dictionary<string, string> SampleSettings(int version)
	{
		JsonObject root = JsonNode.Parse(File.ReadAllText(SamplePath(version)))!.AsObject();
		UserConfigFile.TakeVersion(root, SamplePath(version));

		Dictionary<string, string> values = new(StringComparer.Ordinal);
		UserConfigFile.FlattenInto(root, prefix: string.Empty, values);
		return values;
	}

	/// <summary>A sample's settings in a form that doesn't depend on how the file is laid out or its line endings.</summary>
	private static string Fingerprint(int version)
	{
		string settings = string.Join('\n', SampleSettings(version)
			.OrderBy(entry => entry.Key, StringComparer.Ordinal)
			.Select(entry => $"{entry.Key}={entry.Value}"));

		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(settings)));
	}

	/// <summary>
	/// Every settings page, the way the app builds it, read from the saved settings. No cycle is
	/// loaded, so a page keeps every saved value as it is; the option rows a cycle adds (an ARTCC's
	/// boundary lines, a chart's fixes) aren't offered, and aren't in the samples. They are saved by
	/// the same code, under the same pattern, as the fixed rows the samples have.
	/// </summary>
	private static List<SubServiceSettingsViewModel> EveryPage() =>
	[
		.. typeof(SubServiceSettingsViewModel).Assembly.GetTypes()
			.Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(SubServiceSettingsViewModel)))
			.OrderBy(type => type.FullName, StringComparer.Ordinal)
			.Select(NewPage),
	];

	/// <summary>A page: with no parameters, or - for one that reads saved credentials - an empty credential store.</summary>
	private static SubServiceSettingsViewModel NewPage(Type type) =>
		type == typeof(ConcatenateAliasesViewModel)
			? new ConcatenateAliasesViewModel(new CredentialStore(new InMemoryCredentialVault()))
			: (SubServiceSettingsViewModel)Activator.CreateInstance(type)!;

	/// <summary>
	/// The settings saved outside the settings pages' own sections - Settings itself, the default
	/// ROI, the map, launch and update state - as the code that reads them names them.
	/// </summary>
	private static HashSet<string> OtherSettings() =>
	[
		.. typeof(UserConfigKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(field => field.IsLiteral && field.FieldType == typeof(string))
			.Select(field => (string)field.GetRawConstantValue()!),
		SettingsViewModel.ArtccKey,
		DefaultRoiStore.FilterKey,
		DefaultRoiStore.SwLatKey,
		DefaultRoiStore.SwLonKey,
		DefaultRoiStore.NeLatKey,
		DefaultRoiStore.NeLonKey,
		BaseMapSettings.GridlinesKey,
		BaseMapSettings.LayersKey,
		BaseMapSettings.OpacityKey,
		MapLayersState.AiracKey,
		MapLayersState.HomeKey,
	];

	[Fact]
	public void every_layout_has_a_sample_and_a_fingerprint_and_nothing_else_does()
	{
		Assert.Equal(
			LayoutVersions.Select(version => $"UserConfig.v{version}.json"),
			Directory.GetFiles(SamplesFolder).Select(Path.GetFileName).Order(StringComparer.Ordinal));
		Assert.Equal(LayoutVersions, SampleFingerprints.Keys.Order());
	}

	/// <summary>A released layout's sample is what testers' files look like; changing it would hide a setting they could lose.</summary>
	[Theory]
	[MemberData(nameof(every_layout))]
	public void a_released_layouts_sample_never_changes(int version)
	{
		Assert.True(
			Fingerprint(version) == SampleFingerprints[version],
			$"UserConfig.v{version}.json changed (fingerprint {Fingerprint(version)}). It is the settings file layout {version} was released with: " +
			"leave it as it is. To change the layout, add a new one (see UserConfigMigrations).");
	}

	/// <summary>
	/// The guard itself: every setting in every layout's sample, brought forward, is still saved by a
	/// settings page with the same value - or is one of the settings saved elsewhere.
	/// </summary>
	[Theory]
	[MemberData(nameof(every_layout))]
	public void every_setting_of_every_layout_is_still_read_and_saved_the_same_way(int version)
	{
		List<string> problems = Problems(File.ReadAllText(SamplePath(version)));

		Assert.DoesNotContain(AppLog.Entries, entry => entry.Level >= LogLevel.Warning && entry.Source == "UserConfig");
		Assert.True(
			problems.Count == 0,
			$"Settings in a layout {version} file would be lost or changed. A setting that is renamed, moved, re-formatted or dropped " +
			$"needs a new layout and a migration step (see UserConfigMigrations):\n{string.Join('\n', problems)}");
	}

	/// <summary>The guard sees a setting today's code doesn't read, and one it would save differently.</summary>
	[Fact]
	public void the_guard_names_a_setting_no_longer_read_and_one_saved_differently()
	{
		JsonObject sample = JsonNode.Parse(File.ReadAllText(SamplePath(UserConfigVersion.Current)))!.AsObject();
		JsonObject airways = sample["Services"]!["AiracService"]!["Airways"]!.AsObject();
		airways.Remove("FixBufferNm");
		airways["FixBufferMiles"] = "1.5";
		sample["Services"]!["AiracService"]!["Departures"]!["IncludeObstacleDepartures"] = "no";

		Assert.Equal(
			[
				"Services.AiracService.Airways.FixBufferMiles: no longer read or saved",
				"Services.AiracService.Departures.IncludeObstacleDepartures: saved as 'N', not 'no'",
			],
			Problems(sample.ToJsonString()));
	}

	/// <summary>
	/// Reads a settings file the way launch does (bringing it forward), then lists every setting in it
	/// that no settings page would save with the same value and that isn't saved elsewhere.
	/// </summary>
	private static List<string> Problems(string json)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(UserConfigFile.ConfigFilePath)!);
		File.WriteAllText(UserConfigFile.ConfigFilePath, json);
		UserConfigFile.ReadAll();
		IReadOnlyDictionary<string, string> brought = UserConfigFile.SnapshotValues();

		Dictionary<string, string> saved = new(StringComparer.Ordinal);

		foreach (SubServiceSettingsViewModel page in EveryPage())
		{
			foreach ((string key, string value) in page.CaptureCurrentValues())
			{
				saved[$"{page.NodePath}.{key}"] = value;
			}
		}

		HashSet<string> elsewhere = OtherSettings();
		List<string> problems = [];

		foreach ((string key, string value) in brought.OrderBy(entry => entry.Key, StringComparer.Ordinal))
		{
			if (saved.TryGetValue(key, out string? now))
			{
				if (now != value)
				{
					problems.Add($"{key}: saved as '{now}', not '{value}'");
				}
			}
			else if (!elsewhere.Contains(key))
			{
				problems.Add($"{key}: no longer read or saved");
			}
		}

		return problems;
	}
}
