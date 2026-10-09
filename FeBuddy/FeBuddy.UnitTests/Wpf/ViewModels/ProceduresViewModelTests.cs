using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Procedures tab's Airports card taking a list (issue #309): every ID typed or pasted
/// is added at once, in order, up to <see cref="ProceduresViewModel.MaxAirports"/>; what can't be
/// added stays in the box with a line per reason - against a throwaway config and a few NASR airports.
/// </summary>
[Collection("AppLog")]
public sealed class ProceduresViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Procedures_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public ProceduresViewModelTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	[Fact]
	public void a_pasted_list_adds_every_airport_in_order_by_its_faa_id()
	{
		ProceduresViewModel procedures = Loaded();

		procedures.NewAirportText = "KCLE, cak\r\nKDTW;ORD  MDW";
		procedures.AddAirportCommand.Execute(null);

		Assert.Equal(["CLE", "CAK", "DTW", "ORD", "MDW"], procedures.Airports);
		Assert.Equal(string.Empty, procedures.NewAirportText);
		Assert.Equal(string.Empty, procedures.NewAirportHint);
		Assert.True(procedures.IsDirty);
	}

	[Fact]
	public void what_cannot_be_added_stays_in_the_box_with_a_line_per_reason()
	{
		ProceduresViewModel procedures = Loaded();
		procedures.Airports.Add("PIT");
		procedures.Area = SubServiceArea.Artccs;
		procedures.Facilities.Single(f => f.Artcc == "ZOB").IsSelected = true;

		procedures.NewAirportText = "KPIT KXYZ ORD CLE KORD CAK";

		Assert.Equal(
			"Not an airport in this cycle: KXYZ\nAlready in the list: KPIT\nAlready included by ZOB, a facility ticked on the Area card: CLE, CAK",
			procedures.NewAirportHint);

		procedures.AddAirportCommand.Execute(null);

		Assert.Equal(["PIT", "ORD"], procedures.Airports);
		Assert.Equal("KPIT, KXYZ, CLE, CAK", procedures.NewAirportText);
	}

	[Fact]
	public void an_airport_inside_the_roi_area_is_already_included()
	{
		ProceduresViewModel procedures = Loaded();
		procedures.Area = SubServiceArea.Roi;
		procedures.OverrideRoi = true;
		procedures.SwLat = "40";
		procedures.SwLon = "-83";
		procedures.NeLat = "42";
		procedures.NeLon = "-80";

		procedures.NewAirportText = "CAK ORD";

		Assert.Equal("Already included by the ROI specific to the Procedures sub-service: CAK", procedures.NewAirportHint);
	}

	/// <summary>Only the area chosen counts: a facility ticked while the area is the ROI includes nothing.</summary>
	[Fact]
	public void a_facility_ticked_includes_nothing_while_the_area_is_something_else()
	{
		ProceduresViewModel procedures = Loaded();
		procedures.Area = SubServiceArea.None;
		procedures.Facilities.Single(f => f.Artcc == "ZOB").IsSelected = true;

		procedures.NewAirportText = "CLE";

		Assert.Equal(string.Empty, procedures.NewAirportHint);
	}

	[Fact]
	public void the_list_stops_at_its_limit()
	{
		ProceduresViewModel procedures = Loaded(extra: ProceduresViewModel.MaxAirports);

		for (int i = 0; i < ProceduresViewModel.MaxAirports - 1; i++)
		{
			procedures.Airports.Add(Extra(i));
		}

		procedures.NewAirportText = "ORD MDW";
		procedures.AddAirportCommand.Execute(null);

		Assert.Equal(ProceduresViewModel.MaxAirports, procedures.Airports.Count);
		Assert.Equal("MDW", procedures.NewAirportText);
		Assert.Equal($"Over the {ProceduresViewModel.MaxAirports}-airport limit: MDW", procedures.NewAirportHint);
		Assert.False(procedures.AddAirportCommand.CanExecute(null));
	}

	[Fact]
	public void add_is_on_while_anything_in_the_box_can_be_added_and_clear_empties_it()
	{
		ProceduresViewModel procedures = Loaded();

		procedures.NewAirportText = "KXYZ";
		Assert.False(procedures.AddAirportCommand.CanExecute(null));

		procedures.NewAirportText = "KXYZ, KCLE KCLE";
		Assert.True(procedures.AddAirportCommand.CanExecute(null));

		procedures.AddAirportCommand.Execute(null);
		Assert.Equal(["CLE"], procedures.Airports);
		Assert.Equal("KXYZ", procedures.NewAirportText);

		procedures.ClearAirportTextCommand.Execute(null);
		Assert.Equal(string.Empty, procedures.NewAirportText);
		Assert.False(procedures.ClearAirportTextCommand.CanExecute(null));
	}

	/// <summary>A Procedures tab with a few airports' NASR data: four in ZOB, two in ZAU, and <paramref name="extra"/> more in ZNY.</summary>
	private static ProceduresViewModel Loaded(int extra = 0)
	{
		List<AptCsvDataModel.AptBase> airports =
		[
			Airport("CLE", "KCLE", "ZOB", 41.41, -81.85),
			Airport("CAK", "KCAK", "ZOB", 40.92, -81.44),
			Airport("DTW", "KDTW", "ZOB", 42.21, -83.35),
			Airport("PIT", "KPIT", "ZOB", 40.49, -80.23),
			Airport("ORD", "KORD", "ZAU", 41.98, -87.90),
			Airport("MDW", "KMDW", "ZAU", 41.79, -87.75),
		];

		for (int i = 0; i < extra; i++)
		{
			airports.Add(Airport(Extra(i), null, "ZNY", 40, -74));
		}

		ProceduresViewModel procedures = new();
		procedures.LoadCycleDependentLists(new NasrCsvDataCollection { Apt = new AptCsvDataCollection { AptBase = airports } });
		return procedures;
	}

	private static string Extra(int i) => $"X{i:000}";

	private static AptCsvDataModel.AptBase Airport(string faaId, string? icao, string artcc, double latitude, double longitude) => new()
	{
		ArptId = faaId,
		IcaoId = icao,
		RespArtccId = artcc,
		BaseLatDecimal = latitude,
		BaseLongDecimal = longitude,
	};
}
