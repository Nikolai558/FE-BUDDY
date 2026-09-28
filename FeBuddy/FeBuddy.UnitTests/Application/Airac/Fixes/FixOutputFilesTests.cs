using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.UnitTests.Application.Airac.Fixes;

/// <summary>
/// Covers <see cref="FixOutputFiles"/>: the fixed All-mode file names, the per-group file key
/// format, a combination's group name, which keys count as Fixes GeoJSON files, and reading a
/// per-group key back into its group and kind.
/// </summary>
public sealed class FixOutputFilesTests
{
	[Fact]
	public void the_fixed_names_and_class_are_as_documented()
	{
		Assert.Equal("Fix", FixOutputFiles.AllClass);
		Assert.Equal("Fix_Symbols", FixOutputFiles.Symbols);
		Assert.Equal("Fix_Text", FixOutputFiles.Text);
	}

	[Theory]
	[InlineData("WYPNT", CrcFeatureKind.Symbol, "Fix_WYPNT_Symbols")]
	[InlineData("ENROUTE-LOW", CrcFeatureKind.Text, "Fix_ENROUTE-LOW_Text")]
	[InlineData("NO-CHART", CrcFeatureKind.Symbol, "Fix_NO-CHART_Symbols")]
	public void group_key_combines_the_group_and_the_feature_kind(string group, CrcFeatureKind kind, string expected) =>
		Assert.Equal(expected, FixOutputFiles.GroupKey(group, kind));

	[Theory]
	[InlineData("ENROUTE-LOW", "WYPNT", "ENROUTE-LOW-WYPNT")]
	[InlineData("NO-CHART", "COMPUTER-NAV", "NO-CHART-COMPUTER-NAV")]
	public void combination_group_joins_the_chart_and_fix_use_tokens(string chartToken, string fixUseToken, string expected) =>
		Assert.Equal(expected, FixOutputFiles.CombinationGroup(chartToken, fixUseToken));

	[Theory]
	[InlineData("Fix_Symbols")]
	[InlineData("Fix_Text")]
	[InlineData("fix_symbols")] // ignoring case
	[InlineData("FIX_TEXT")]
	[InlineData("Fix_WYPNT_Symbols")]
	[InlineData("Fix_ENROUTE-LOW_Text")]
	[InlineData("Fix_ENROUTE-LOW-WYPNT_Symbols")] // a combination group
	[InlineData("Fix_NO-CHART_Text")]
	public void is_geojson_key_accepts_the_all_mode_and_per_group_keys(string key) =>
		Assert.True(FixOutputFiles.IsGeojsonKey(key));

	[Theory]
	[InlineData("Fix.txt")] // Fixes has no alias file
	[InlineData("Fixes_Symbols")]
	[InlineData("Fix_WYPNT_Lines")] // Fixes has no Lines file
	[InlineData("Fix__Symbols")] // empty group
	[InlineData("Fix_WYPNT_")]
	[InlineData("junk")]
	public void is_geojson_key_rejects_alias_like_and_malformed_keys(string key) =>
		Assert.False(FixOutputFiles.IsGeojsonKey(key));

	[Theory]
	[InlineData("Fix_WYPNT_Symbols", "WYPNT", CrcFeatureKind.Symbol)]
	[InlineData("Fix_ENROUTE-LOW_Text", "ENROUTE-LOW", CrcFeatureKind.Text)]
	[InlineData("Fix_ENROUTE-LOW-WYPNT_Text", "ENROUTE-LOW-WYPNT", CrcFeatureKind.Text)]
	[InlineData("fix_wypnt_symbols", "WYPNT", CrcFeatureKind.Symbol)] // ignoring case
	public void try_parse_group_key_reads_the_group_upper_cased_and_the_kind(string key, string expectedGroup, CrcFeatureKind expectedKind)
	{
		Assert.True(FixOutputFiles.TryParseGroupKey(key, out string group, out CrcFeatureKind kind));
		Assert.Equal(expectedGroup, group);
		Assert.Equal(expectedKind, kind);
	}

	[Theory]
	[InlineData("Fix_Symbols")] // the All-mode key has no group
	[InlineData("Fix_Text")]
	[InlineData("junk")]
	[InlineData("Fix.txt")]
	public void try_parse_group_key_fails_for_the_all_mode_keys_and_junk(string key)
	{
		Assert.False(FixOutputFiles.TryParseGroupKey(key, out string group, out _));
		Assert.Equal(string.Empty, group);
	}
}
