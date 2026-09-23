using FeBuddy.Core.Models.Geojson;
using FeBuddy.Core.Services.General;

namespace FeBuddy.UnitTests.Services.General;

/// <summary>
/// Boundary tests for every CRC ERAM property range documented in CRC_Geojsons.md.
/// </summary>
public class CrcGeojsonPropertyValidatorTests
{
	private static CrcLineProperties Line(int? bcg = 1, IReadOnlyList<int>? filters = null, string? style = null, int? thickness = null) =>
		new() { Bcg = bcg, Filters = filters ?? new[] { 1 }, Style = style, Thickness = thickness };

	private static CrcSymbolProperties Symbol(int? bcg = 1, IReadOnlyList<int>? filters = null, string? style = null, int? size = null) =>
		new() { Bcg = bcg, Filters = filters ?? new[] { 1 }, Style = style, Size = size };

	private static CrcTextProperties Text(int? bcg = 1, IReadOnlyList<int>? filters = null, IReadOnlyList<string>? text = null, int? size = null, int? xOffset = null, int? yOffset = null) =>
		new() { Bcg = bcg, Filters = filters ?? new[] { 1 }, Text = text ?? new[] { "ABC" }, Size = size, XOffset = xOffset, YOffset = yOffset };

	[Theory]
	[InlineData(1, true)]
	[InlineData(40, true)]
	[InlineData(0, false)]
	[InlineData(41, false)]
	public void bcg_boundaries_are_enforced(int bcg, bool expectedValid)
	{
		Assert.Equal(expectedValid, CrcGeojsonPropertyValidator.ValidateLine(Line(bcg: bcg)).IsValid);
	}

	[Fact]
	public void bcg_null_is_valid_because_crc_auto_assigns_it()
	{
		Assert.True(CrcGeojsonPropertyValidator.ValidateLine(Line(bcg: null)).IsValid);
	}

	[Theory]
	[InlineData(0, true)]
	[InlineData(40, true)]
	[InlineData(-1, false)]
	[InlineData(41, false)]
	public void filter_entry_boundaries_are_enforced(int filter, bool expectedValid)
	{
		Assert.Equal(expectedValid, CrcGeojsonPropertyValidator.ValidateLine(Line(filters: new[] { filter })).IsValid);
	}

	[Fact]
	public void empty_filters_is_invalid_because_crc_cannot_auto_assign_it()
	{
		Assert.False(CrcGeojsonPropertyValidator.ValidateLine(Line(filters: Array.Empty<int>())).IsValid);
	}

	[Theory]
	[InlineData("solid", true)]
	[InlineData("shortDashed", true)]
	[InlineData("longDashed", true)]
	[InlineData("longDashShortDash", true)]
	[InlineData("SOLID", false)] // ValidateLine compares case-sensitively; AirwaySettingsParser is what normalizes case before validation
	[InlineData("dotted", false)]
	public void line_style_values_are_enforced(string style, bool expectedValid)
	{
		Assert.Equal(expectedValid, CrcGeojsonPropertyValidator.ValidateLine(Line(style: style)).IsValid);
	}

	[Theory]
	[InlineData(1, true)]
	[InlineData(3, true)]
	[InlineData(0, false)]
	[InlineData(4, false)]
	public void line_thickness_boundaries_are_enforced(int thickness, bool expectedValid)
	{
		Assert.Equal(expectedValid, CrcGeojsonPropertyValidator.ValidateLine(Line(thickness: thickness)).IsValid);
	}

	[Theory]
	[InlineData("vor", true)]
	[InlineData("tacan", true)]
	[InlineData("obstruction1", true)]
	[InlineData("bogus", false)]
	public void symbol_style_values_are_enforced(string style, bool expectedValid)
	{
		Assert.Equal(expectedValid, CrcGeojsonPropertyValidator.ValidateSymbol(Symbol(style: style)).IsValid);
	}

	[Theory]
	[InlineData(1, true)]
	[InlineData(4, true)]
	[InlineData(0, false)]
	[InlineData(5, false)]
	public void symbol_size_boundaries_are_enforced(int size, bool expectedValid)
	{
		Assert.Equal(expectedValid, CrcGeojsonPropertyValidator.ValidateSymbol(Symbol(size: size)).IsValid);
	}

	[Theory]
	[InlineData(0, true)]
	[InlineData(5, true)]
	[InlineData(-1, false)]
	[InlineData(6, false)]
	public void text_size_boundaries_are_enforced(int size, bool expectedValid)
	{
		Assert.Equal(expectedValid, CrcGeojsonPropertyValidator.ValidateText(Text(size: size)).IsValid);
	}

	[Fact]
	public void empty_text_is_invalid_because_crc_cannot_auto_assign_it()
	{
		Assert.False(CrcGeojsonPropertyValidator.ValidateText(Text(text: Array.Empty<string>())).IsValid);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-50)]
	[InlineData(50)]
	public void text_x_offset_accepts_any_integer(int xOffset)
	{
		Assert.True(CrcGeojsonPropertyValidator.ValidateText(Text(xOffset: xOffset)).IsValid);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-50)]
	[InlineData(50)]
	public void text_y_offset_accepts_any_integer(int yOffset)
	{
		Assert.True(CrcGeojsonPropertyValidator.ValidateText(Text(yOffset: yOffset)).IsValid);
	}

	[Fact]
	public void multiple_violations_are_all_reported_not_just_the_first()
	{
		CrcPropertyValidationResult result = CrcGeojsonPropertyValidator.ValidateLine(
			new CrcLineProperties { Bcg = 999, Filters = Array.Empty<int>(), Style = "bogus", Thickness = 999 });

		Assert.False(result.IsValid);
		Assert.True(result.Errors.Count >= 4);
	}

	[Fact]
	public void validate_dispatches_on_feature_kind_and_rejects_mismatched_type()
	{
		Assert.Throws<ArgumentException>(() =>
			CrcGeojsonPropertyValidator.Validate(CrcFeatureKind.Line, Symbol()));
	}
}
