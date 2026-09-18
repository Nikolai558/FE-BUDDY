using FeBuddy.Core.Models.Services.Airac.Airways;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.Airac.Airways;

using FeBuddy.UnitTests.Services.Airac.Airways.Fixtures;

namespace FeBuddy.UnitTests.Services.Airac.Airways;

/// <summary>
/// Covers the alias file's rename to <c>Airways.txt</c>, the <see cref="AliasRoiScope"/>
/// toggle, and the "Add FE-Buddy_Output folder" preference (remediation plan 3.5 / 3.7).
/// </summary>
public sealed class AirwayAliasServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_Alias_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_outputDirectory))
			{
				Directory.Delete(_outputDirectory, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	private AirwaySettings Settings(bool addWrapper = true, AliasRoiScope scope = AliasRoiScope.All, RegionOfInterest? roi = null) => new()
	{
		OutputDirectory = _outputDirectory,
		OutputBy = AirwayGeojsonOutputBy.None,
		BufferAirwayWaypoints = false,
		IncludeFebCustomProperties = false,
		IncludeAirwayWaypointIds = false,
		GenerateAliasFile = true,
		SplitAtAntimeridian = true,
		IncludeCrcEramPropertyDefaults = false,
		AddFeBuddyOutputFolder = addWrapper,
		AliasRoiScope = scope,
		Roi = roi,
	};

	private static IReadOnlyList<Airway> BuildTwoAirways()
	{
		// J1 sits near (40, -80); Q1 sits far away near (10, 10).
		var data = AirwayTestDataBuilder.Build(
			fixes: new[] { ("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0) },
			awyId: "J1",
			segments: new[] { AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB") });

		var farData = AirwayTestDataBuilder.Build(
			fixes: new[] { ("CCCCC", 10.0, 10.0), ("DDDDD", 11.0, 11.0) },
			awyId: "Q1",
			segments: new[] { AirwayTestDataBuilder.Segment("Q1", 10, "CCCCC", "WP", "DDDDD") });

		data.Awy!.AwyBase.AddRange(farData.Awy!.AwyBase);
		data.Awy.AwySegAlt.AddRange(farData.Awy.AwySegAlt);
		data.Fix!.FixBase.AddRange(farData.Fix!.FixBase);

		AirwaySettings minimal = new()
		{
			OutputDirectory = @"C:\unused",
			OutputBy = AirwayGeojsonOutputBy.None,
			BufferAirwayWaypoints = false,
			IncludeFebCustomProperties = false,
			IncludeAirwayWaypointIds = false,
			GenerateAliasFile = false,
			SplitAtAntimeridian = true,
			IncludeCrcEramPropertyDefaults = false,
		};

		return AirwayBuilder.BuildAll(data, minimal).Airways;
	}

	[Fact]
	public void the_alias_file_is_named_Airways_txt_under_the_febuddy_output_wrapper()
	{
		AirwayAliasGenerateResult result = AirwayAliasService.Generate(BuildTwoAirways(), Settings());

		Assert.NotNull(result.FilePath);
		Assert.Equal("Airways.txt", Path.GetFileName(result.FilePath));
		Assert.Contains(Path.Combine("FE-Buddy_Output", "Airways", "Alias"), result.FilePath!);
	}

	[Fact]
	public void turning_off_the_wrapper_writes_straight_into_the_output_directory()
	{
		AirwayAliasGenerateResult result = AirwayAliasService.Generate(BuildTwoAirways(), Settings(addWrapper: false));

		Assert.DoesNotContain("FE-Buddy_Output", result.FilePath!);
		Assert.Equal(Path.Combine(_outputDirectory, "Airways", "Alias", "Airways.txt"), result.FilePath);
	}

	[Fact]
	public void roi_airways_scope_keeps_only_airways_with_a_waypoint_inside_the_roi()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0); // contains J1, not Q1

		AirwayAliasGenerateResult result = AirwayAliasService.Generate(
			BuildTwoAirways(), Settings(scope: AliasRoiScope.RoiAirways, roi: roi));

		string contents = File.ReadAllText(result.FilePath!);
		Assert.Contains(".J1F", contents);
		Assert.DoesNotContain(".Q1F", contents);
		Assert.Equal(1, result.AirwayLineCount);
	}

	[Fact]
	public void the_all_scope_keeps_every_airway_even_with_an_roi_set()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		AirwayAliasGenerateResult result = AirwayAliasService.Generate(
			BuildTwoAirways(), Settings(scope: AliasRoiScope.All, roi: roi));

		string contents = File.ReadAllText(result.FilePath!);
		Assert.Contains(".J1F", contents);
		Assert.Contains(".Q1F", contents);
	}
}
