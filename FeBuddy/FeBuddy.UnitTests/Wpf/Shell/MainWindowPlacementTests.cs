using System.Text.Json.Nodes;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers the settings side of <see cref="MainWindowPlacement"/> and the Settings ▸ Main Window card
/// (<see cref="WindowPlacementCardViewModel"/>): remembering is on until turned off; turned off,
/// nothing is kept; the Reset button forgets where the window was and moves it; and both settings
/// live in <c>Shared.json</c>, read straight from it, for every profile. Against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class MainWindowPlacementTests : IDisposable
{
	private static readonly SavedWindowPlacement Placement = new(new PixelRect(2100, 100, 1800, 1170), 144, Maximized: true);

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_WindowPlacement_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public MainWindowPlacementTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(_root);
		UserConfigFile.Write();
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

	private static JsonNode? SharedGeneral() => JsonNode.Parse(File.ReadAllText(UserConfigFile.SharedFilePath))!["General"];

	[Fact]
	public void remembering_is_on_until_turned_off_and_nothing_is_saved_to_start()
	{
		Assert.True(MainWindowPlacement.IsRemembered);
		Assert.Null(MainWindowPlacement.Saved);
	}

	[Fact]
	public void where_the_window_was_is_saved_to_shared_json_and_read_back_from_it()
	{
		MainWindowPlacement.Remember(Placement);

		Assert.Equal(Placement, MainWindowPlacement.Saved);
		Assert.Equal("2100,100,1800,1170,144,Maximized", SharedGeneral()!["MainWindowPlacement"]!.GetValue<string>());
		Assert.DoesNotContain("MainWindowPlacement", File.ReadAllText(UserConfigFile.ConfigFilePath), StringComparison.Ordinal);
	}

	/// <summary>The window opens before the launch reads the settings, so it reads them from the file, not from memory.</summary>
	[Fact]
	public void where_the_window_was_is_read_from_the_file_before_the_settings_are()
	{
		MainWindowPlacement.Remember(Placement);
		UserConfigFile.ConfigureForTesting(_root); // as at launch: nothing read into memory yet

		Assert.Null(UserConfigFile.GetValue(UserConfigKeys.MainWindowPlacement));
		Assert.Equal(Placement, MainWindowPlacement.Saved);
	}

	[Fact]
	public void a_corrupt_saved_value_reads_as_none()
	{
		UserConfigFile.TrySetValue(UserConfigKeys.MainWindowPlacement, "not a placement");
		UserConfigFile.SaveShared(UserConfigKeys.MainWindowPlacement);

		Assert.Null(MainWindowPlacement.Saved);
	}

	[Fact]
	public void turned_off_it_forgets_where_the_window_was_and_keeps_nothing_after()
	{
		MainWindowPlacement.Remember(Placement);

		MainWindowPlacement.SetRemembered(false);
		MainWindowPlacement.Remember(Placement);

		Assert.False(MainWindowPlacement.IsRemembered);
		Assert.Null(MainWindowPlacement.Saved);
		Assert.Equal("N", SharedGeneral()!["RememberWindowPlacement"]!.GetValue<string>());
		Assert.Null(SharedGeneral()!["MainWindowPlacement"]);
	}

	[Fact]
	public void turned_back_on_it_keeps_where_the_window_is_again()
	{
		MainWindowPlacement.SetRemembered(false);
		MainWindowPlacement.SetRemembered(true);
		MainWindowPlacement.Remember(Placement);

		Assert.True(MainWindowPlacement.IsRemembered);
		Assert.Equal(Placement, MainWindowPlacement.Saved);
	}

	[Fact]
	public void a_saved_value_is_ignored_while_remembering_is_off()
	{
		MainWindowPlacement.Remember(Placement);
		UserConfigFile.TrySetValue(UserConfigKeys.RememberWindowPlacement, "N");
		UserConfigFile.SaveShared(UserConfigKeys.RememberWindowPlacement);

		Assert.Null(MainWindowPlacement.Saved);
	}

	[Fact]
	public void forget_clears_where_the_window_was()
	{
		MainWindowPlacement.Remember(Placement);

		MainWindowPlacement.Forget();

		Assert.Null(MainWindowPlacement.Saved);
		Assert.True(MainWindowPlacement.IsRemembered);
		Assert.Null(SharedGeneral()?["MainWindowPlacement"]);
	}

	[Fact]
	public void every_profile_has_the_same_placement()
	{
		MainWindowPlacement.Remember(Placement);

		UserConfigFile.CreateProfile("ZNY", new Dictionary<string, string> { [UserConfigKeys.MainWindowPlacement] = "1,1,1,1,96,Normal" });
		UserConfigFile.SwitchProfile("ZNY");

		Assert.Equal(Placement, MainWindowPlacement.Saved);
		Assert.DoesNotContain("MainWindowPlacement", File.ReadAllText(UserConfigFile.ProfileFilePath("ZNY")), StringComparison.Ordinal);
	}

	// ---- the Settings card ----

	[Fact]
	public void the_card_shows_whether_remembering_is_on()
	{
		Assert.True(new WindowPlacementCardViewModel(() => { }).RememberPlacement);

		MainWindowPlacement.SetRemembered(false);

		Assert.False(new WindowPlacementCardViewModel(() => { }).RememberPlacement);
	}

	[Fact]
	public void unticking_the_card_saves_at_once_and_forgets_where_the_window_was()
	{
		MainWindowPlacement.Remember(Placement);

		_ = new WindowPlacementCardViewModel(() => { }) { RememberPlacement = false };

		Assert.False(MainWindowPlacement.IsRemembered);
		Assert.Null(MainWindowPlacement.Saved);
	}

	[Fact]
	public void the_cards_reset_button_forgets_where_the_window_was_and_moves_it_to_its_default()
	{
		int moves = 0;
		MainWindowPlacement.Remember(Placement);
		WindowPlacementCardViewModel card = new(() => moves++);

		card.ResetCommand.Execute(null);

		Assert.Equal(1, moves);
		Assert.Null(MainWindowPlacement.Saved);
		Assert.True(card.RememberPlacement);
	}
}
