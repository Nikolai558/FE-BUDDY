using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>Covers <see cref="AirwayOutputFiles"/>: a GeoJSON file's key read back into its group and kind.</summary>
public sealed class AirwayOutputFilesTests
{
	[Theory]
	[InlineData("Airways_High_Lines", "High", CrcFeatureKind.Line)]
	[InlineData("airways_J_text", "J", CrcFeatureKind.Text)]
	[InlineData("Airways_Low_Symbols", "Low", CrcFeatureKind.Symbol)]
	public void a_key_is_read_back_into_its_group_and_kind(string key, string group, CrcFeatureKind kind)
	{
		Assert.True(AirwayOutputFiles.TryParseGeojsonKey(key, out string gotGroup, out CrcFeatureKind gotKind));
		Assert.Equal(group, gotGroup);
		Assert.Equal(kind, gotKind);
		Assert.Equal(key, AirwayOutputFiles.GeojsonKey(gotGroup, gotKind), ignoreCase: true);
	}

	[Theory]
	[InlineData("Airways.txt")]
	[InlineData("Airways_J1_Lines")]
	[InlineData("Fix_WYPNT_Text")]
	public void anything_else_is_not_an_airways_geojson_key(string key)
	{
		Assert.False(AirwayOutputFiles.TryParseGeojsonKey(key, out string group, out _));
		Assert.Empty(group);
		Assert.Throws<ArgumentNullException>(() => AirwayOutputFiles.TryParseGeojsonKey(null!, out _, out _));
	}
}
