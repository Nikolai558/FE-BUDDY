using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.Launch.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Application.Launch;

/// <summary>
/// Covers the dev launch's choices: <see cref="LaunchOptions"/> (a normal launch runs every step
/// whatever is saved), <see cref="OptionalLaunchSteps"/> (the card's list and the log line), and
/// <see cref="DevLaunchSettings"/> (saved beside the profiles, never in one or in an export).
/// </summary>
[Collection("AppLog")]
public sealed class DevLaunchSettingsTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_DevLaunch_" + Guid.NewGuid().ToString("N"));

	public DevLaunchSettingsTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		DevLaunchSettings.ConfigureForTesting(_root);
		UserConfigFile.ConfigureForTesting(_root);
	}

	public void Dispose()
	{
		UserConfigFile.ConfigureForTesting(null);
		DevLaunchSettings.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch
		{
			// Best-effort.
		}
	}

	// ---- LaunchOptions ----

	[Fact]
	public void a_normal_launch_runs_every_step_whatever_steps_it_is_given()
	{
		LaunchOptions normal = new(IsDevLaunch: false, OptionalLaunchStep.None);

		Assert.All(OptionalLaunchSteps.InCardOrder, step => Assert.True(normal.Runs(step)));
		Assert.All(OptionalLaunchSteps.InCardOrder, step => Assert.True(LaunchOptions.Normal.Runs(step)));
		Assert.False(LaunchOptions.Normal.IsDevLaunch);
	}

	[Fact]
	public void a_dev_launch_runs_only_the_steps_it_was_given()
	{
		LaunchOptions dev = LaunchOptions.DevLaunch(OptionalLaunchStep.ParseAiracData | OptionalLaunchStep.DownloadNews);

		Assert.True(dev.IsDevLaunch);
		Assert.True(dev.Runs(OptionalLaunchStep.ParseAiracData));
		Assert.True(dev.Runs(OptionalLaunchStep.DownloadNews));
		Assert.False(dev.Runs(OptionalLaunchStep.DownloadAiracData));
		Assert.False(dev.Runs(OptionalLaunchStep.WelcomeWindow));
	}

	// ---- OptionalLaunchSteps ----

	[Fact]
	public void the_card_lists_every_optional_step_once_with_a_name_and_a_note()
	{
		Assert.Equal(10, OptionalLaunchSteps.InCardOrder.Count);
		Assert.Equal(OptionalLaunchStep.All, OptionalLaunchSteps.InCardOrder.Aggregate(OptionalLaunchStep.None, (all, step) => all | step));
		Assert.Equal(OptionalLaunchSteps.InCardOrder.Count, OptionalLaunchSteps.InCardOrder.Distinct().Count());

		Assert.All(OptionalLaunchSteps.InCardOrder, step =>
		{
			Assert.False(string.IsNullOrWhiteSpace(OptionalLaunchSteps.Name(step)));
			Assert.False(string.IsNullOrWhiteSpace(OptionalLaunchSteps.Description(step)));
		});

		Assert.Equal("Check the clock and internet connection", OptionalLaunchSteps.Name(OptionalLaunchStep.CheckClockAndInternet));
		Assert.Equal("Remove FE-Buddy 2.x's old install and dead shortcuts", OptionalLaunchSteps.Name(OptionalLaunchStep.RemoveLegacyInstall));
	}

	[Theory]
	[InlineData(OptionalLaunchStep.None)]
	[InlineData(OptionalLaunchStep.All)]
	[InlineData(OptionalLaunchStep.DownloadNews | OptionalLaunchStep.WelcomeWindow)]
	public void a_name_or_note_is_for_one_step_only(OptionalLaunchStep notOneStep)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => OptionalLaunchSteps.Name(notOneStep));
		Assert.Throws<ArgumentOutOfRangeException>(() => OptionalLaunchSteps.Description(notOneStep));
	}

	[Fact]
	public void the_dev_launch_log_line_names_the_steps_it_runs_and_skips()
	{
		Assert.Equal(
			"Dev launch. Running: Download AIRAC data, Parse AIRAC data. Skipping: Check the clock and internet connection, " +
			"Remove old AIRAC cycles from disk, Check for a newer version, Download News, Check GitHub tokens, " +
			"FE-Buddy 2.x GitHub token notice, First-launch welcome window, Remove FE-Buddy 2.x's old install and dead shortcuts.",
			OptionalLaunchSteps.DescribeDevLaunch(OptionalLaunchStep.DownloadAiracData | OptionalLaunchStep.ParseAiracData));

		Assert.StartsWith("Dev launch. Running: none. Skipping: Check the clock", OptionalLaunchSteps.DescribeDevLaunch(OptionalLaunchStep.None), StringComparison.Ordinal);
		Assert.EndsWith("Skipping: none.", OptionalLaunchSteps.DescribeDevLaunch(OptionalLaunchStep.All), StringComparison.Ordinal);
	}

	// ---- DevLaunchSettings ----

	[Fact]
	public void with_nothing_saved_a_dev_launch_skips_every_step()
	{
		Assert.False(File.Exists(DevLaunchSettings.FilePath));
		Assert.Equal(OptionalLaunchStep.None, DevLaunchSettings.Read());
	}

	[Fact]
	public void the_choices_saved_are_the_ones_read_back()
	{
		OptionalLaunchStep chosen = OptionalLaunchStep.CheckClockAndInternet | OptionalLaunchStep.ParseAiracData | OptionalLaunchStep.RemoveLegacyInstall;

		DevLaunchSettings.Save(chosen);

		Assert.Equal(chosen, DevLaunchSettings.Read());
		Assert.Equal(Path.Combine(_root, "DevLaunch.json"), DevLaunchSettings.FilePath);

		string json = File.ReadAllText(DevLaunchSettings.FilePath);
		Assert.Contains("\"ParseAiracData\": true", json, StringComparison.Ordinal);
		Assert.Contains("\"DownloadAiracData\": false", json, StringComparison.Ordinal);
	}

	[Fact]
	public void a_step_the_file_does_not_name_is_off_and_unknown_names_are_ignored()
	{
		Directory.CreateDirectory(_root);
		File.WriteAllText(DevLaunchSettings.FilePath, """{ "DownloadNews": true, "SomethingNew": true }""");

		Assert.Equal(OptionalLaunchStep.DownloadNews, DevLaunchSettings.Read());
	}

	[Fact]
	public void a_file_that_cannot_be_read_skips_every_step_and_says_why()
	{
		Directory.CreateDirectory(_root);
		File.WriteAllText(DevLaunchSettings.FilePath, "{ not json");

		Assert.Equal(OptionalLaunchStep.None, DevLaunchSettings.Read());
		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith("Could not read DevLaunch.json", StringComparison.Ordinal));
	}

	[Fact]
	public void the_choices_are_never_in_a_settings_profile_or_an_export()
	{
		UserConfigFile.TrySetValue("Services.AiracService.UserArtccId", "ZOB");
		UserConfigFile.Write();
		DevLaunchSettings.Save(OptionalLaunchStep.All);

		// Beside the profiles' folder, not in it.
		Assert.Equal(_root, Path.GetDirectoryName(DevLaunchSettings.FilePath));
		Assert.NotEqual(Path.GetDirectoryName(UserConfigFile.ConfigFilePath), Path.GetDirectoryName(DevLaunchSettings.FilePath));

		string profile = File.ReadAllText(UserConfigFile.ConfigFilePath);
		Assert.DoesNotContain("DownloadAiracData", profile, StringComparison.Ordinal);

		string export = Path.Combine(_root, "export.json");
		UserConfigTransfer.Export(export);
		string exported = File.ReadAllText(export);

		Assert.Contains("ZOB", exported, StringComparison.Ordinal);
		Assert.DoesNotContain("DownloadAiracData", exported, StringComparison.Ordinal);
		Assert.DoesNotContain("DevLaunch", exported, StringComparison.Ordinal);
	}
}
