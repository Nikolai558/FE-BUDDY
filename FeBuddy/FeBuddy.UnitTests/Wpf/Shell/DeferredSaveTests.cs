using System.Text.Json.Nodes;
using System.Windows.Threading;
using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers <see cref="DeferredSave"/>: the keys that changed are saved together, read from the page when
/// the save is made, off the UI thread, into the profile they were changed in; once the changes stop, and
/// as FE-Buddy closes; a failed save's values go with the next; changes dropped after the settings were
/// read again aren't saved. Each test runs on a WPF thread of its own, against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class DeferredSaveTests : IDisposable
{
	private const string Node = "Services.MapService";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_DeferredSave_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public DeferredSaveTests()
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

	/// <summary>Each key's last value, read when the save is made, goes to the file; nothing is written before.</summary>
	[Fact]
	public void the_changes_are_saved_together_with_their_last_values() => StaThread.Run(() =>
	{
		DeferredSave save = new(Node, "map settings", Dispatcher.CurrentDispatcher);
		int opacity = 30;

		save.Set(Node + ".BaseMapOpacity", () => opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
		save.Set(Node + ".Gridlines", () => "N");
		opacity = 70;

		Assert.True(save.IsWaiting);
		Assert.False(File.Exists(UserConfigFile.ConfigFilePath));

		Assert.True(save.SaveNowAsync().GetAwaiter().GetResult());

		JsonNode map = JsonNode.Parse(File.ReadAllText(UserConfigFile.ConfigFilePath))!["Services"]!["MapService"]!;
		Assert.Equal("70", map["BaseMapOpacity"]!.GetValue<string>());
		Assert.Equal("N", map["Gridlines"]!.GetValue<string>());
		Assert.False(save.IsWaiting);
		Assert.True(save.SaveNowAsync().GetAwaiter().GetResult());
	});

	/// <summary>Changes dropped because the settings were read again (another profile, an import) aren't saved.</summary>
	[Fact]
	public void forgotten_changes_are_not_saved() => StaThread.Run(() =>
	{
		DeferredSave save = new(Node, "map settings", Dispatcher.CurrentDispatcher);
		save.Set(Node + ".Gridlines", () => "N");

		save.Forget();

		Assert.False(save.IsWaiting);
		Assert.True(save.SaveNowAsync().GetAwaiter().GetResult());
		Assert.False(File.Exists(UserConfigFile.ConfigFilePath));
	});

	/// <summary>A save that can't be written says so, and the next change tries again.</summary>
	[Fact]
	public void a_save_that_cannot_be_written_is_reported() => StaThread.Run(() =>
	{
		Directory.CreateDirectory(UserConfigFile.PreviousFilePath);
		DeferredSave save = new(Node, "map settings", Dispatcher.CurrentDispatcher);
		save.Set(Node + ".Gridlines", () => "N");

		Assert.False(save.SaveNowAsync().GetAwaiter().GetResult());
		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith("Could not save the map settings", StringComparison.Ordinal));
	});

	/// <summary>
	/// A change is saved into the profile it was made in: once another is in use, it isn't saved at all - not
	/// into the new one - and the save says it wasn't made.
	/// </summary>
	[Fact]
	public void a_change_made_before_a_switch_is_not_saved_into_the_new_profile() => StaThread.Run(() =>
	{
		UserConfigFile.CreateProfile("ZNY", null);
		DeferredSave save = new(Node, "map settings", Dispatcher.CurrentDispatcher);
		save.Set(Node + ".Gridlines", () => "N");

		UserConfigFile.SwitchProfile("ZNY");

		Assert.False(save.SaveNowAsync().GetAwaiter().GetResult());
		Assert.DoesNotContain("Gridlines", File.ReadAllText(UserConfigFile.ProfileFilePath("ZNY")), StringComparison.Ordinal);
	});

	/// <summary>A failed save's values are saved with the next save, though the next change is to another setting.</summary>
	[Fact]
	public void a_failed_save_is_tried_again_with_the_next_change() => StaThread.Run(() =>
	{
		Directory.CreateDirectory(UserConfigFile.PreviousFilePath);
		DeferredSave save = new(Node, "map settings", Dispatcher.CurrentDispatcher);
		save.Set(Node + ".Gridlines", () => "N");
		Assert.False(save.SaveNowAsync().GetAwaiter().GetResult());

		Directory.Delete(UserConfigFile.PreviousFilePath);
		save.Set(Node + ".BaseMapOpacity", () => "40");

		Assert.True(save.SaveNowAsync().GetAwaiter().GetResult());
		JsonNode map = JsonNode.Parse(File.ReadAllText(UserConfigFile.ConfigFilePath))!["Services"]!["MapService"]!;
		Assert.Equal("N", map["Gridlines"]!.GetValue<string>());
		Assert.Equal("40", map["BaseMapOpacity"]!.GetValue<string>());
	});

	/// <summary>A change is saved on its own once nothing has changed for <see cref="DeferredSave.Delay"/>.</summary>
	[Fact]
	public void a_change_is_saved_once_the_changes_stop() => StaThread.Run(() =>
	{
		DeferredSave save = new(Node, "map settings", Dispatcher.CurrentDispatcher);
		save.Set(Node + ".Gridlines", () => "N");

		// The value is in memory only once the save holding the settings has written it. The round count is
		// only a guard against hanging.
		for (int round = 0; round < 1000 && UserConfigFile.GetValue(Node + ".Gridlines") is null; round++)
		{
			Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
			Thread.Sleep(10);
		}

		Assert.False(save.IsWaiting);
		Assert.Contains("Gridlines", File.ReadAllText(UserConfigFile.ConfigFilePath), StringComparison.Ordinal);
	});

	/// <summary>A change still waiting when FE-Buddy closes is saved then.</summary>
	[Fact]
	public void a_change_waiting_is_saved_as_fe_buddy_closes() => StaThread.Run(() =>
	{
		DeferredSave save = new(Node, "map settings", Dispatcher.CurrentDispatcher);
		save.Set(Node + ".Gridlines", () => "N");

		Dispatcher.CurrentDispatcher.InvokeShutdown();

		Assert.Contains("Gridlines", File.ReadAllText(UserConfigFile.ConfigFilePath), StringComparison.Ordinal);
	});
}
