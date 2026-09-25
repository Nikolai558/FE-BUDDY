using FeBuddy.Core.Infrastructure.Sct;
using FeBuddy.Core.Infrastructure.Sct.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Infrastructure.Sct;

/// <summary>
/// Covers <see cref="SctFileReader"/>: sections, DMS and named coordinates, comments, the SID/STAR
/// and REGIONS layouts, labels, and records that cannot be used.
/// </summary>
public sealed class SctFileReaderTests
{
	private const string A = "N040.00.00.000 W075.00.00.000";
	private const string B = "N040.30.00.000 W075.00.00.000";
	private const string C = "N040.30.00.000 W074.30.00.000";

	private static SctFile Parse(params string[] records) => SctFileReader.Parse(records, "TEST.sct2");

	[Theory]
	[InlineData("N040.30.00.000", true, 40.5)]
	[InlineData("n40.30.00", true, 40.5)]
	[InlineData("S013.29.00.000", true, -(13 + 29 / 60d))]
	[InlineData("W073.30.00.000", false, -73.5)]
	[InlineData("E144.47.30.5", false, 144 + 47 / 60d + 30.5 / 3600)]
	public void reads_dms_with_or_without_leading_zeros_and_fractions(string text, bool isLatitude, double expected)
	{
		Assert.True(SctFileReader.TryParseDms(text, isLatitude, out double degrees));
		Assert.Equal(expected, degrees, 9);
	}

	[Theory]
	[InlineData("W073.30.00.000", true)]    // a longitude where a latitude belongs
	[InlineData("N040.30.00.000", false)]   // and the other way round
	[InlineData("N040.60.00.000", true)]    // 60 minutes
	[InlineData("N040.30.60.000", true)]    // 60 seconds
	[InlineData("N091.00.00.000", true)]    // past the pole
	[InlineData("E181.00.00.000", false)]   // past 180
	[InlineData("KJFK", true)]              // a name, not DMS
	[InlineData("40.5", true)]              // decimal degrees are not sector-file DMS
	public void rejects_what_is_not_dms_of_the_right_kind(string text, bool isLatitude)
	{
		Assert.False(SctFileReader.TryParseDms(text, isLatitude, out _));
	}

	[Fact]
	public void reads_every_line_section_by_name_and_geo_without_one()
	{
		SctFile file = Parse(
			"#define COASTLINE 16777215",
			"[INFO]",
			"Test sector",
			"[ARTCC]",
			$"ZOB {A} {B}",
			"[ARTCC HIGH]",
			$"ZOB_HI {A} {B}",
			"[ARTCC LOW]",
			$"ZOB_LO {A} {B}",
			"[LOW AIRWAY]",
			$"V14 {A} {B}",
			"[HIGH AIRWAY]",
			$"J60 {A} {B}",
			"[GEO]",
			$"{A} {B} COASTLINE");

		Assert.Equal(6, file.Lines.Count);
		Assert.Equal("ZOB", Assert.Single(file.Lines[SctLineSection.Artcc]).Name);
		Assert.Equal("ZOB_HI", file.Lines[SctLineSection.ArtccHigh][0].Name);
		Assert.Equal("ZOB_LO", file.Lines[SctLineSection.ArtccLow][0].Name);
		Assert.Equal("V14", file.Lines[SctLineSection.LowAirway][0].Name);
		Assert.Equal("J60", file.Lines[SctLineSection.HighAirway][0].Name);

		SctSegment geo = Assert.Single(file.Lines[SctLineSection.Geo]);
		Assert.Equal(string.Empty, geo.Name);
		Assert.Equal(new Coordinate(-75, 40), geo.Start);
		Assert.Equal(new Coordinate(-75, 40.5), geo.End);
		Assert.Empty(file.Problems);
		Assert.Equal("TEST.sct2", file.SourcePath);
	}

	[Fact]
	public void names_with_spaces_and_geo_with_a_leading_name_still_read()
	{
		SctFile file = Parse(
			"[ARTCC]",
			$"ZOB NORTH SECTOR {A} {B}",
			"[GEO]",
			$"LAKE_ERIE {A} {B} COASTLINE");

		Assert.Equal("ZOB NORTH SECTOR", Assert.Single(file.Lines[SctLineSection.Artcc]).Name);
		Assert.Single(file.Lines[SctLineSection.Geo]);
		Assert.Empty(file.Problems);
	}

	[Fact]
	public void coordinates_can_be_named_points_defined_anywhere_in_the_file()
	{
		SctFile file = Parse(
			"[LOW AIRWAY]",
			"V14 DJB DJB KCLE KCLE",
			"V15 BADXX BADXX DJB DJB",
			"[VOR]",
			"DJB 116.400 N041.21.28.000 W082.09.43.000",
			"[NDB]",
			"XYZ 400 N041.00.00.000 W082.00.00.000",
			"[AIRPORT]",
			"KCLE 000.000 N041.24.42.000 W081.51.00.000 C",
			"[FIXES]",
			$"BADXX {A}",
			"DJB N000.00.00.000 E000.00.00.000",
			"SHORT N040.00.00.000",
			"BADLON N040.00.00.000 X075.00.00.000");

		IReadOnlyList<SctSegment> airways = file.Lines[SctLineSection.LowAirway];

		Assert.Equal(2, airways.Count);
		Assert.Equal(41 + 21 / 60d + 28 / 3600d, airways[0].Start.Y, 9);
		Assert.Equal(41 + 24 / 60d + 42 / 3600d, airways[0].End.Y, 9);

		// The first definition of a name wins: DJB is the VOR, not the later fix.
		Assert.Equal(new Coordinate(-75, 40), airways[1].Start);
		Assert.Equal(airways[0].Start, airways[1].End);
	}

	[Fact]
	public void comments_and_blank_lines_are_ignored_and_records_before_any_section_skipped()
	{
		SctFile file = Parse(
			$"ZOB {A} {B}",
			"; a whole-line comment",
			"[ARTCC] ; section comment",
			"",
			$"ZOB {A} {B} ; trailing",
			$"ZOB {B} {C} // also trailing",
			$"ZOB {C} {A} // both ; kinds",
			"[SOMETHING NEW]",
			"anything at all");

		Assert.Equal(3, file.Lines[SctLineSection.Artcc].Count);
		Assert.Empty(file.Problems);
	}

	[Fact]
	public void an_unreadable_line_record_is_reported_and_skipped()
	{
		SctFile file = Parse(
			"[ARTCC]",
			$"ZOB {A} {B}",
			"ZOB N040.00.00.000 W075.00.00.000 NOWHERE NOWHERE",
			"[GEO]",
			"too short");

		Assert.Single(file.Lines[SctLineSection.Artcc]);
		Assert.False(file.Lines.ContainsKey(SctLineSection.Geo));
		Assert.Equal(2, file.Problems.Count);
		Assert.StartsWith("Line 3: [ARTCC]", file.Problems[0]);
		Assert.StartsWith("Line 5: [GEO]", file.Problems[1]);
	}

	[Fact]
	public void diagrams_read_with_the_name_in_the_first_26_columns_and_indented_continuations()
	{
		SctFile file = Parse(
			"[SID]",
			$"{"KCLE RWY 24L DEPARTURE",-26}{A} {B} RED",
			$"                          {B} {C}",
			$"{"KCLE RWY 6R",-26}{C} {A}",
			"[STAR]",
			$"{"DJB ARRIVAL",-26}{A} {C}");

		Assert.Equal(3, file.Sids.Count);
		Assert.Equal("KCLE RWY 24L DEPARTURE", file.Sids[0].Name);
		Assert.Equal("KCLE RWY 24L DEPARTURE", file.Sids[1].Name);
		Assert.Equal("KCLE RWY 6R", file.Sids[2].Name);
		Assert.Equal("DJB ARRIVAL", Assert.Single(file.Stars).Name);
		Assert.Empty(file.Problems);
	}

	[Fact]
	public void a_tab_separated_diagram_header_falls_back_to_the_last_four_coordinates()
	{
		SctFile file = Parse(
			"[SID]",
			$"KCLE\tSID\t{A} {B}",
			$"MY DIAGRAM\t{A} {B} RED");

		Assert.Equal(["KCLE SID", "MY DIAGRAM"], file.Sids.Select(s => s.Name));
		Assert.Empty(file.Problems);
	}

	[Fact]
	public void diagram_records_that_cannot_be_placed_are_reported()
	{
		SctFile file = Parse(
			"[STAR]",
			$"   {A} {B}",
			"UNREADABLE HEADER",
			$"   {A} {B}",
			$"{"GOOD",-26}{A} {B}",
			"   N040.00.00.000 W075.00.00.000 NOWHERE NOWHERE");

		Assert.Equal("GOOD", Assert.Single(file.Stars).Name);
		Assert.Equal(4, file.Problems.Count);
		Assert.Contains("no diagram has started", file.Problems[0]);
		Assert.Contains("could not be read", file.Problems[1]);
		Assert.Contains("no diagram has started", file.Problems[2]);
		Assert.Contains("could not be read", file.Problems[3]);
	}

	[Fact]
	public void labels_keep_their_text_even_with_a_semicolon_inside_the_quotes()
	{
		SctFile file = Parse(
			"[LABELS]",
			$"\"KCLE; TWR\" {A} WHITE",
			$"\"J60\" DJB DJB ; comment after",
			"\"NO POSITION\"",
			"not quoted at all",
			"[VOR]",
			"DJB 116.400 N041.21.28.000 W082.09.43.000");

		Assert.Equal(["KCLE; TWR", "J60"], file.Labels.Select(l => l.Text));
		Assert.Equal(new Coordinate(-75, 40), file.Labels[0].Position);
		Assert.Equal(2, file.Problems.Count);
	}

	[Fact]
	public void regions_start_with_a_colour_and_point_or_a_name_and_continue_one_point_a_line()
	{
		SctFile file = Parse(
			"[REGIONS]",
			$"GRASS {A}",
			$"  {B}",
			$"  {C}",
			"RUNWAY SURFACE",
			$"{A}",
			$"{B}",
			$"{C}");

		Assert.Equal(2, file.Regions.Count);
		Assert.Equal("GRASS", file.Regions[0].Name);
		Assert.Equal(3, file.Regions[0].Points.Count);
		Assert.Equal("RUNWAY SURFACE", file.Regions[1].Name);
		Assert.Equal(3, file.Regions[1].Points.Count);
		Assert.Empty(file.Problems);
	}

	[Fact]
	public void region_records_that_cannot_be_used_are_reported()
	{
		SctFile file = Parse(
			"[REGIONS]",
			$"{A}",
			$"THIN {A}",
			$"  {B}",
			"  not a point here",
			$"  {A}");

		Assert.Empty(file.Regions);
		Assert.Equal(3, file.Problems.Count);
		Assert.Contains("no region has started", file.Problems[0]);
		Assert.Contains("could not be read", file.Problems[1]);
		Assert.Contains("'THIN' has fewer than three points", file.Problems[2]);
	}

	[Fact]
	public void read_loads_a_file_from_disk()
	{
		string path = Path.Combine(Path.GetTempPath(), $"FeBuddyTests_{Guid.NewGuid():N}.sct2");
		File.WriteAllLines(path, ["[ARTCC]", $"ZOB {A} {B}"]);

		try
		{
			SctFile file = SctFileReader.Read(path);

			Assert.Equal(path, file.SourcePath);
			Assert.Single(file.Lines[SctLineSection.Artcc]);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentException>(() => SctFileReader.Read(" "));
		Assert.Throws<ArgumentNullException>(() => SctFileReader.Parse(null!, "x"));
	}
}
