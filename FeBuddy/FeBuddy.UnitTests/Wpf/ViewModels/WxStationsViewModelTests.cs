using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers what the Wx Stations tab says about FE-Buddy's kept copy of the station list (<see cref="WxStationsViewModel"/>):
/// its date or its absence, a look at it that fails, and an older look finishing last - against a throwaway config and log.
/// </summary>
[Collection("AppLog")]
public sealed class WxStationsViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_WxTab_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public WxStationsViewModelTests()
	{
		Directory.CreateDirectory(_root);
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

	/// <summary>The copy's date when there is one, and that the first run needs the internet when there isn't.</summary>
	[Fact]
	public void the_kept_copy_is_described_by_its_date_or_its_absence()
	{
		string copy = Path.Combine(_root, "stations.json");

		Assert.StartsWith("FE-Buddy has no copy yet.", WxStationsViewModel.DescribeStationData(copy), StringComparison.Ordinal);

		File.WriteAllText(copy, "[]");
		File.SetLastWriteTime(copy, new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Local));

		string described = WxStationsViewModel.DescribeStationData(copy);
		Assert.StartsWith("FE-Buddy's copy is from 4 ", described, StringComparison.Ordinal);
		Assert.Contains(" 2026. Every run downloads the latest list first", described, StringComparison.Ordinal);
	}

	/// <summary>
	/// A look at the copy that fails says so, and why, rather than leaving the status empty or stale; an older look
	/// that finishes after it doesn't overwrite it.
	/// </summary>
	[Fact]
	public async Task a_look_that_fails_says_so_and_an_older_one_finishing_last_doesnt_win()
	{
		WxStationsViewModel tab = new();
		using ManualResetEventSlim release = new();
		tab.DescribeStationCopy = _ =>
		{
			release.Wait();
			return "The older look.";
		};
		Task older = tab.RefreshStationDataAsync();

		tab.DescribeStationCopy = _ => throw new IOException("boom");
		await tab.RefreshStationDataAsync();
		release.Set();
		await older;

		Assert.Equal("FE-Buddy couldn't read its copy: boom", tab.BuildPreviewSummary()[0].Rows[0].Value);
	}
}
