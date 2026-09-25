using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airways.Models;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>
/// Covers the <c>Airways.txt</c> alias file: where it goes (the output folder, or
/// <c>Upload_to_vNAS</c> when marked for vNAS) and the <see cref="AliasRoiScope"/> toggle.
/// </summary>
public sealed class AirwayAliasWriterTests : IDisposable
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

	private AirwaySettings Settings(bool uploadToVnas = false, AliasRoiScope scope = AliasRoiScope.All, RegionOfInterest? roi = null) => new()
	{
		OutputDirectory = _outputDirectory,
		OutputBy = AirwayGeojsonOutputBy.None,
		BufferAirwayWaypoints = false,
		IncludeFebCustomProperties = false,
		FebProperties = [],
		GenerateAliasFile = true,
		SplitAtAntimeridian = true,
		Vnas = uploadToVnas ? new VnasFileChoices([AirwayOutputFiles.Alias], []) : VnasFileChoices.None,
		AliasRoiScope = scope,
		Roi = roi,
	};

	/// <summary>
	/// Builds J1 (near 40, -80) and Q1 (far away near 10, 10). Pass the same ROI the alias
	/// settings use: the builder is what decides whether an airway crosses it.
	/// </summary>
	private static IReadOnlyList<Airway> BuildTwoAirways(RegionOfInterest? roi = null)
	{
		// J1 sits near (40, -80); Q1 sits far away near (10, 10).
		var data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0)],
			awyId: "J1",
			segments: [AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB")]);

		var farData = AirwayTestDataBuilder.Build(
			fixes: [("CCCCC", 10.0, 10.0), ("DDDDD", 11.0, 11.0)],
			awyId: "Q1",
			segments: [AirwayTestDataBuilder.Segment("Q1", 10, "CCCCC", "WP", "DDDDD")]);

		data.Awy!.AwyBase.AddRange(farData.Awy!.AwyBase);
		data.Awy.AwySegAlt.AddRange(farData.Awy.AwySegAlt);
		data.Fix!.FixBase.AddRange(farData.Fix!.FixBase);

		AirwaySettings minimal = new()
		{
			OutputDirectory = @"C:\unused",
			OutputBy = AirwayGeojsonOutputBy.None,
			BufferAirwayWaypoints = false,
			IncludeFebCustomProperties = false,
			FebProperties = [],
			GenerateAliasFile = false,
			SplitAtAntimeridian = true,
			Roi = roi,
		};

		return AirwayBuilder.BuildAll(data, minimal).Airways;
	}

	[Fact]
	public void the_alias_file_is_airways_txt_in_the_output_folder_itself()
	{
		AirwayAliasGenerateResult result = AirwayAliasWriter.Generate(BuildTwoAirways(), Settings());

		Assert.Equal(Path.Combine(_outputDirectory, "Airways.txt"), result.FilePath);
	}

	[Fact]
	public void an_alias_file_marked_for_vnas_goes_in_upload_to_vnas()
	{
		AirwayAliasGenerateResult result = AirwayAliasWriter.Generate(BuildTwoAirways(), Settings(uploadToVnas: true));

		Assert.Equal(Path.Combine(_outputDirectory, "Upload_to_vNAS", "Airways.txt"), result.FilePath);
		Assert.False(File.Exists(Path.Combine(_outputDirectory, "Airways.txt")));
	}

	[Fact]
	public void roi_airways_scope_keeps_only_airways_crossing_the_roi()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0); // contains J1, not Q1

		AirwayAliasGenerateResult result = AirwayAliasWriter.Generate(
			BuildTwoAirways(roi), Settings(scope: AliasRoiScope.RoiAirways, roi: roi));

		string contents = File.ReadAllText(result.FilePath!);
		Assert.Contains(".J1F", contents);
		Assert.DoesNotContain(".Q1F", contents);
		Assert.Equal(1, result.AirwayLineCount);
	}

	[Fact]
	public void the_all_scope_keeps_every_airway_even_with_an_roi_set()
	{
		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		AirwayAliasGenerateResult result = AirwayAliasWriter.Generate(
			BuildTwoAirways(roi), Settings(scope: AliasRoiScope.All, roi: roi));

		string contents = File.ReadAllText(result.FilePath!);
		Assert.Contains(".J1F", contents);
		Assert.Contains(".Q1F", contents);
	}

	[Fact]
	public void roi_airways_scope_keeps_an_airway_whose_line_crosses_the_roi_with_no_waypoint_inside()
	{
		// V9 runs from well west of the ROI to well east of it: neither waypoint is inside,
		// but the line passes straight through - so the GeoJSON draws it, and the alias must
		// include it too, with both of its waypoints.
		var data = AirwayTestDataBuilder.Build(
			fixes: [("WESTT", 40.0, -95.0), ("EASTT", 40.0, -70.0)],
			awyId: "V9",
			segments: [AirwayTestDataBuilder.Segment("V9", 10, "WESTT", "WP", "EASTT")]);

		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		AirwaySettings buildSettings = Settings(scope: AliasRoiScope.RoiAirways, roi: roi);
		IReadOnlyList<Airway> airways = AirwayBuilder.BuildAll(data, buildSettings).Airways;

		AirwayAliasGenerateResult result = AirwayAliasWriter.Generate(airways, buildSettings);

		string contents = File.ReadAllText(result.FilePath!);
		Assert.Contains(".V9F .FF WESTT EASTT", contents);
		Assert.Equal(1, result.AirwayLineCount);
	}
}
