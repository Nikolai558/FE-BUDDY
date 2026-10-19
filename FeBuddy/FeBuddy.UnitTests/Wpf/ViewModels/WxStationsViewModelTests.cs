using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>Covers what the Wx Stations tab says about FE-Buddy's kept copy of the station list (<see cref="WxStationsViewModel"/>).</summary>
public sealed class WxStationsViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_WxTab_" + Guid.NewGuid().ToString("N"));

	public WxStationsViewModelTests() => Directory.CreateDirectory(_root);

	public void Dispose()
	{
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, recursive: true);
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
}
