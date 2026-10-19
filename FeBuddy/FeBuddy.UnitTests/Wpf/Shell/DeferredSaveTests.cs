using System.Text.Json.Nodes;
using System.Windows.Threading;
using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers <see cref="DeferredSave"/>: the keys that changed are saved together, read from the page when
/// the save is made, off the UI thread; changes dropped after the settings were read again aren't saved.
/// Each test runs on a WPF thread of its own, against a throwaway config.
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
}
