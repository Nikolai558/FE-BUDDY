using FeBuddy.Core.Infrastructure.Dat;
using FeBuddy.Core.Infrastructure.Dat.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Infrastructure.Dat;

/// <summary>
/// Covers <see cref="DatFileReader"/>: the point of tangency, <c>LINE</c> blocks, the optional
/// hemisphere letters and label, and records that cannot be used.
/// </summary>
public sealed class DatFileReaderTests
{
	private static DatFile Parse(params string[] records) => DatFileReader.Parse(records, "TEST.dat");

	[Fact]
	public void reads_the_point_of_tangency_and_every_line()
	{
		DatFile file = Parse(
			"Filename: TEST.DAT",
			"9900  40 38 23.00 N 073 46 42.00 W",
			"LINE 01",
			" 40 30 00.00 N 073 30 00.00 W",
			" 40 45 00.00 N 073 45 00.00 W",
			"LINE 02",
			" 41 00 00.00 N 074 00 00.00 W",
			" 41 00 30.00 N 074 00 00.00 W",
			" 41 01 00.00 N 074 00 00.00 W");

		Assert.NotNull(file.PointOfTangency);
		Assert.Equal(40 + 38 / 60d + 23 / 3600d, file.PointOfTangency.DecLat, 6);
		Assert.Equal(-(73 + 46 / 60d + 42 / 3600d), file.PointOfTangency.DecLon, 6);

		Assert.Equal(2, file.Lines.Count);
		Assert.Equal([new Coordinate(-73.5, 40.5), new Coordinate(-73.75, 40.75)], file.Lines[0].Coordinates);
		Assert.Equal(3, file.Lines[1].NumPoints);
		Assert.Empty(file.Problems);
		Assert.Equal("TEST.dat", file.SourcePath);
	}

	[Fact]
	public void without_hemisphere_letters_a_point_is_north_and_west()
	{
		Assert.True(DatFileReader.TryParsePoint("40 30 00.00 073 30 00.00", out double lat, out double lon));

		Assert.Equal(40.5, lat, 9);
		Assert.Equal(-73.5, lon, 9);
	}

	[Fact]
	public void south_and_east_hemispheres_are_honoured()
	{
		Assert.True(DatFileReader.TryParsePoint("13 29 00.00 s 144 47 30.00 e", out double lat, out double lon));

		Assert.Equal(-(13 + 29 / 60d), lat, 9);
		Assert.Equal(144 + 47.5 / 60d, lon, 9);
	}

	[Fact]
	public void one_leading_label_is_ignored()
	{
		Assert.True(DatFileReader.TryParsePoint("17 40 30 00.00 N 073 30 00.00 W", out double lat, out double lon));

		Assert.Equal(40.5, lat, 9);
		Assert.Equal(-73.5, lon, 9);
	}

	[Theory]
	[InlineData("")]                                        // nothing at all
	[InlineData("40 30 00.00 N")]                           // latitude only
	[InlineData("A B 40 30 00.00 N 073 30 00.00 W")]        // more than one label
	[InlineData("40 60 00.00 N 073 30 00.00 W")]            // 60 minutes
	[InlineData("40 30 60.00 N 073 30 00.00 W")]            // 60 seconds
	[InlineData("91 00 00.00 N 073 30 00.00 W")]            // latitude past the pole
	[InlineData("40 30 00.00 N 181 00 00.00 W")]            // longitude past 180
	[InlineData("40 30 00.00 N 073 30 xx W")]               // not a number
	[InlineData("40 30 00.00 N 073 -3 00.00 W")]            // negative minutes
	public void rejects_what_is_not_a_point(string text)
	{
		Assert.False(DatFileReader.TryParsePoint(text, out _, out _));
	}

	[Fact]
	public void a_bad_record_is_reported_and_skipped_without_losing_the_line()
	{
		DatFile file = Parse(
			"LINE 01",
			" 40 30 00.00 N 073 30 00.00 W",
			" garbage",
			" 40 45 00.00 N 073 45 00.00 W",
			"",
			"   ");

		LineString line = Assert.Single(file.Lines);
		Assert.Equal(2, line.NumPoints);

		string problem = Assert.Single(file.Problems);
		Assert.StartsWith("Line 3:", problem);
		Assert.Contains("garbage", problem);
	}

	[Fact]
	public void a_line_block_with_fewer_than_two_points_is_reported_and_skipped()
	{
		DatFile file = Parse(
			"LINE 01",
			" 40 30 00.00 N 073 30 00.00 W",
			"LINE 02",
			"LINE 03",
			" 40 30 00.00 N 073 30 00.00 W",
			" 40 45 00.00 N 073 45 00.00 W");

		Assert.Single(file.Lines);
		Assert.Equal(2, file.Problems.Count);
		Assert.StartsWith("Line 1:", file.Problems[0]);
		Assert.Contains("1 point(s)", file.Problems[0]);
		Assert.StartsWith("Line 3:", file.Problems[1]);
		Assert.Contains("0 point(s)", file.Problems[1]);
	}

	[Fact]
	public void a_file_without_a_point_of_tangency_or_lines_reads_as_empty()
	{
		DatFile file = Parse("Filename: EMPTY.DAT", "Some header text");

		Assert.Null(file.PointOfTangency);
		Assert.Empty(file.Lines);
		Assert.Empty(file.Problems);
	}

	[Fact]
	public void an_unreadable_point_of_tangency_is_reported()
	{
		DatFile file = Parse("9900  not a coordinate");

		Assert.Null(file.PointOfTangency);
		Assert.Contains("point of tangency", Assert.Single(file.Problems));
	}

	[Fact]
	public void only_the_first_point_of_tangency_in_the_header_counts()
	{
		DatFile file = Parse(
			"9900  40 00 00.00 N 073 00 00.00 W",
			"9900  41 00 00.00 N 074 00 00.00 W",
			"LINE 01",
			// Seconds that happen to contain 9900 are a point, not a point of tangency.
			" 40 30 09.9900 N 073 30 00.00 W",
			" 40 45 00.00 N 073 45 00.00 W");

		Assert.Equal(40, file.PointOfTangency!.DecLat, 9);
		Assert.Equal(2, Assert.Single(file.Lines).NumPoints);
	}

	[Fact]
	public void read_loads_a_file_from_disk()
	{
		string path = Path.Combine(Path.GetTempPath(), $"FeBuddyTests_{Guid.NewGuid():N}.dat");
		File.WriteAllLines(path, ["LINE 01", " 40 30 00.00 N 073 30 00.00 W", " 40 45 00.00 N 073 45 00.00 W"]);

		try
		{
			DatFile file = DatFileReader.Read(path);

			Assert.Equal(path, file.SourcePath);
			Assert.Single(file.Lines);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentException>(() => DatFileReader.Read(" "));
		Assert.Throws<ArgumentNullException>(() => DatFileReader.Parse(null!, "x"));
	}
}
