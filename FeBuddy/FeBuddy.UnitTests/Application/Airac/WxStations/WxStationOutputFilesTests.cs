using FeBuddy.Core.Application.Airac.WxStations;

namespace FeBuddy.UnitTests.Application.Airac.WxStations;

/// <summary>
/// Covers <see cref="WxStationOutputFiles"/>: the fixed file names and class, and which keys count
/// as a Wx Stations GeoJSON file.
/// </summary>
public sealed class WxStationOutputFilesTests
{
	[Fact]
	public void the_fixed_names_and_class_are_as_documented()
	{
		Assert.Equal("Wx", WxStationOutputFiles.AllClass);
		Assert.Equal("Wx_Symbols", WxStationOutputFiles.Symbols);
		Assert.Equal("Wx_Text", WxStationOutputFiles.Text);
	}

	[Theory]
	[InlineData("Wx_Symbols")]
	[InlineData("Wx_Text")]
	[InlineData("wx_symbols")]
	[InlineData("WX_TEXT")]
	public void is_geojson_key_accepts_symbols_and_text_ignoring_case(string key) =>
		Assert.True(WxStationOutputFiles.IsGeojsonKey(key));

	[Theory]
	[InlineData("Wx.txt")]
	[InlineData("Wx_Lines")]
	[InlineData("junk")]
	[InlineData("")]
	public void is_geojson_key_rejects_everything_else(string key) =>
		Assert.False(WxStationOutputFiles.IsGeojsonKey(key));
}
