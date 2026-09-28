using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Navaids.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Navaids;

/// <summary>
/// Covers <see cref="NavaidBuilder.BuildAll"/>: the SHUTDOWN exclusion, the blank NAV_ID skip, an
/// unrecognized type still being built (with one warning per type), duplicate identifiers never
/// being merged, and the stable output ordering.
/// </summary>
public sealed class NavaidBuilderTests
{
	[Fact]
	public void build_all_throws_when_nav_data_was_never_parsed()
	{
		NasrCsvDataCollection data = new(); // Nav is null

		Assert.Throws<InvalidOperationException>(() => NavaidBuilder.BuildAll(data));
	}

	[Fact]
	public void build_all_rejects_a_null_argument()
	{
		Assert.Throws<ArgumentNullException>(() => NavaidBuilder.BuildAll(null!));
	}

	[Fact]
	public void a_shutdown_navaid_is_excluded_and_reported_as_one_info_message_with_the_count()
	{
		NasrCsvDataCollection data = NavaidTestData.Build(
		[
			NavaidTestData.CgtRow(),
			NavaidTestData.ShutdownRow(),
			NavaidTestData.Row("YYY", "VOR", "ANOTHER RETIRED", navStatus: "SHUTDOWN"),
		]);

		NavaidBuildAllResult result = NavaidBuilder.BuildAll(data);

		Navaid navaid = Assert.Single(result.Navaids);
		Assert.Equal("CGT", navaid.NavId);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Equal("NavaidBuilder", message.Source);
		Assert.Contains("2", message.Text);
		Assert.Contains("SHUTDOWN", message.Text);
	}

	[Fact]
	public void a_blank_nav_id_is_skipped_with_a_warning()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.CgtRow(), NavaidTestData.BlankNavIdRow()]);

		NavaidBuildAllResult result = NavaidBuilder.BuildAll(data);

		Navaid navaid = Assert.Single(result.Navaids);
		Assert.Equal("CGT", navaid.NavId);

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Contains("NO IDENTIFIER", message.Text);
	}

	[Fact]
	public void an_unrecognized_type_is_still_built_with_one_warning_per_type()
	{
		NasrCsvDataCollection data = NavaidTestData.Build(
		[
			NavaidTestData.Row("AAA", "MYSTERY TYPE", "FIRST"),
			NavaidTestData.Row("BBB", "MYSTERY TYPE", "SECOND"),
		]);

		NavaidBuildAllResult result = NavaidBuilder.BuildAll(data);

		Assert.Equal(2, result.Navaids.Count);
		Assert.All(result.Navaids, navaid => Assert.Equal("MYSTERY TYPE", navaid.NavType));

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Contains("MYSTERY TYPE", message.Text);
	}

	[Fact]
	public void duplicate_identifiers_are_never_merged()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.AbqVortacRow(), NavaidTestData.AbqVotRow()]);

		NavaidBuildAllResult result = NavaidBuilder.BuildAll(data);

		Assert.Equal(2, result.Navaids.Count);
		Assert.All(result.Navaids, navaid => Assert.Equal("ABQ", navaid.NavId));
		Assert.Contains(result.Navaids, navaid => navaid.NavType == "VORTAC");
		Assert.Contains(result.Navaids, navaid => navaid.NavType == "VOT");
	}

	[Fact]
	public void navaids_are_ordered_by_id_then_type_then_name_then_latitude_then_longitude()
	{
		NasrCsvDataCollection data = NavaidTestData.Build(
		[
			NavaidTestData.Row("BBB", "VOR", "Z NAME", latitude: 10, longitude: 10),
			NavaidTestData.Row("AAA", "VOR", "B NAME", latitude: 20, longitude: 20),
			NavaidTestData.Row("AAA", "NDB", "A NAME", latitude: 30, longitude: 30),
			NavaidTestData.Row("AAA", "VOR", "A NAME", latitude: 2, longitude: 5),
			NavaidTestData.Row("AAA", "VOR", "A NAME", latitude: 1, longitude: 9),
		]);

		NavaidBuildAllResult result = NavaidBuilder.BuildAll(data);

		Assert.Equal(
			[
				("AAA", "NDB", "A NAME", 30.0),
				("AAA", "VOR", "A NAME", 1.0),
				("AAA", "VOR", "A NAME", 2.0),
				("AAA", "VOR", "B NAME", 20.0),
				("BBB", "VOR", "Z NAME", 10.0),
			],
			result.Navaids.Select(n => (n.NavId, n.NavType, n.Name, n.Latitude)));
	}

	[Fact]
	public void every_field_is_carried_onto_the_built_navaid()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.CgtRow()]);

		Navaid navaid = Assert.Single(NavaidBuilder.BuildAll(data).Navaids);

		Assert.Equal("CGT", navaid.NavId);
		Assert.Equal("VORTAC", navaid.NavType);
		Assert.Equal("CHICAGO HEIGHTS", navaid.Name);
		Assert.Equal(114.2, navaid.Freq);
		Assert.Equal(41.510007, navaid.Latitude);
		Assert.Equal(-87.571546, navaid.Longitude);
		Assert.Equal("ZAU", navaid.LowAltArtccId);
		Assert.Equal("ZAU", navaid.HighAltArtccId);
	}

	[Fact]
	public void a_navaid_type_is_trimmed_and_upper_cased()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.Row("ABC", "  vor  ", "LOWER CASE TYPE")]);

		Navaid navaid = Assert.Single(NavaidBuilder.BuildAll(data).Navaids);

		Assert.Equal("VOR", navaid.NavType);
	}
}
