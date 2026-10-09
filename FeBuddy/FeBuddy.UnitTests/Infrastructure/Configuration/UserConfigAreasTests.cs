using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Covers <see cref="UserConfigAreas"/> (issue #335): the area a tab starts on when it has none
/// saved - exactly what its older settings got where it used one filter or none, its ARTCCs where it
/// used both - and layout 2's step, which saves that area for every tab with settings and drops the
/// settings it replaces.
/// </summary>
public sealed class UserConfigAreasTests
{
	private const string Airac = "Services.AiracService";

	private static Func<string, string?> Saved(params (string Key, string Value)[] values)
	{
		Dictionary<string, string> settings = values.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
		return key => settings.GetValueOrDefault(key);
	}

	/// <summary>The saved values are the names of the area the settings blocks carry.</summary>
	[Fact]
	public void the_saved_values_are_the_names_of_the_areas()
	{
		Assert.Equal(nameof(SubServiceArea.Artccs), UserConfigAreas.Artccs);
		Assert.Equal(nameof(SubServiceArea.Roi), UserConfigAreas.Roi);
		Assert.Equal(nameof(SubServiceArea.Everything), UserConfigAreas.Everything);
		Assert.Equal(nameof(SubServiceArea.None), UserConfigAreas.None);
	}

	// ---- a tab with ARTCCs ----

	[Theory]
	[InlineData("ArtccBoundaries", "LocationFilter")]
	[InlineData("Arrivals", "ArtccFilter")]
	[InlineData("Departures", "ArtccFilter")]
	public void a_tab_with_artccs_listed_starts_on_them_even_with_a_roi(string tab, string listKey)
	{
		Func<string, string?> saved = Saved(
			($"{Airac}.{tab}.{listKey}", "ZOB,ZNY"),
			($"{Airac}.{tab}.Roi.OverrideDefaultRoi", "Y"),
			(UserConfigAreas.DefaultRoiKey, "Y"));

		Assert.Equal(UserConfigAreas.Artccs, UserConfigAreas.DefaultFor(tab, saved));
	}

	/// <summary>A tab that never saved its ARTCCs starts with the facility from Settings ticked, so it starts on its ARTCCs.</summary>
	[Fact]
	public void a_tab_that_never_saved_its_artccs_starts_on_the_facility()
	{
		Assert.Equal(UserConfigAreas.Artccs, UserConfigAreas.DefaultFor("Arrivals", Saved((UserConfigAreas.FacilityKey, "ZOB"))));
	}

	/// <summary>A saved empty list meant every ARTCC: the ROI if one was on, otherwise everything.</summary>
	[Fact]
	public void a_tab_with_an_empty_artcc_list_starts_on_the_roi_if_one_was_on_or_everything()
	{
		(string, string) emptyList = ($"{Airac}.Departures.ArtccFilter", " , ");

		Assert.Equal(UserConfigAreas.Roi, UserConfigAreas.DefaultFor("Departures", Saved(emptyList, (UserConfigAreas.FacilityKey, "ZOB"), (UserConfigAreas.DefaultRoiKey, "Y"))));
		Assert.Equal(UserConfigAreas.Roi, UserConfigAreas.DefaultFor("Departures", Saved(emptyList, ($"{Airac}.Departures.Roi.OverrideDefaultRoi", "true"))));
		Assert.Equal(UserConfigAreas.Everything, UserConfigAreas.DefaultFor("Departures", Saved(emptyList, (UserConfigAreas.DefaultRoiKey, "N"))));
	}

	// ---- a tab whose only area is the ROI ----

	[Theory]
	[InlineData("Airports")]
	[InlineData("Airways")]
	[InlineData("Navaids")]
	[InlineData("Fixes")]
	[InlineData("WxStations")]
	public void a_tab_whose_only_area_is_the_roi_starts_on_it_only_if_one_was_on(string tab)
	{
		Assert.Equal(UserConfigAreas.Roi, UserConfigAreas.DefaultFor(tab, Saved((UserConfigAreas.DefaultRoiKey, "Y"), (UserConfigAreas.FacilityKey, "ZOB"))));
		Assert.Equal(UserConfigAreas.Roi, UserConfigAreas.DefaultFor(tab, Saved(($"{Airac}.{tab}.Roi.OverrideDefaultRoi", "Y"))));
		Assert.Equal(UserConfigAreas.Everything, UserConfigAreas.DefaultFor(tab, Saved((UserConfigAreas.FacilityKey, "ZOB"))));
	}

	// ---- Procedures ----

	[Fact]
	public void procedures_starts_on_its_facilities_then_the_roi_it_brought_in_then_none()
	{
		const string Node = $"{Airac}.Procedures";

		Assert.Equal(UserConfigAreas.Artccs, UserConfigAreas.DefaultFor("Procedures", Saved(($"{Node}.Facilities", "ZOB"), ($"{Node}.IncludeRoiAirports", "Y"))));
		Assert.Equal(UserConfigAreas.Artccs, UserConfigAreas.DefaultFor("Procedures", Saved((UserConfigAreas.FacilityKey, "ZOB"))));
		Assert.Equal(UserConfigAreas.Roi, UserConfigAreas.DefaultFor("Procedures", Saved(($"{Node}.Facilities", ""), ($"{Node}.IncludeRoiAirports", "Y"))));

		// The default ROI alone never brought airports in on Procedures.
		Assert.Equal(UserConfigAreas.None, UserConfigAreas.DefaultFor("Procedures", Saved(($"{Node}.Facilities", ""), (UserConfigAreas.DefaultRoiKey, "Y"))));
	}

	[Fact]
	public void a_tab_with_no_area_is_refused()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => UserConfigAreas.DefaultFor("Telephony", Saved()));

		Assert.Contains("'Telephony'", ex.Message, StringComparison.Ordinal);
		Assert.Throws<ArgumentNullException>(() => UserConfigAreas.DefaultFor("Arrivals", null!));
	}

	// ---- layout 2's step ----

	[Fact]
	public void layout_2_saves_the_area_for_every_tab_with_settings_and_drops_what_it_replaces()
	{
		Dictionary<string, string> layout1 = new(StringComparer.Ordinal)
		{
			[UserConfigAreas.FacilityKey] = "ZOB",
			[UserConfigAreas.DefaultRoiKey] = "Y",
			[$"{Airac}.Arrivals.ArtccFilter"] = "ZNY,ZOB",
			[$"{Airac}.Airports.EmitAirportSymbols"] = "Y",
			[$"{Airac}.Fixes.Area"] = "Everything",
			[$"{Airac}.Procedures.Facilities"] = "",
			[$"{Airac}.Procedures.IncludeRoiAirports"] = "Y",
			[$"{Airac}.Telephony.Roi.OverrideDefaultRoi"] = "N",
			[$"{Airac}.Telephony.Roi.OverrideCorners.SwLat"] = "40",
			[$"{Airac}.Telephony.IncludeVatsimRadarVirtualAirlines"] = "Y",
		};

		// The real steps, without the current layout a test elsewhere may be standing in.
		UserConfigMigrationResult result = UserConfigMigrations.Migrate(layout1, 1, 2, UserConfigMigrations.All);

		Assert.Equal(["Each AIRAC sub-service tab's Area saved; Procedures' IncludeRoiAirports and Telephony's ROI dropped"], result.Applied);
		Assert.Equal("Artccs", result.Values[$"{Airac}.Arrivals.Area"]);
		Assert.Equal("Roi", result.Values[$"{Airac}.Airports.Area"]);
		Assert.Equal("Everything", result.Values[$"{Airac}.Fixes.Area"]);
		Assert.Equal("Roi", result.Values[$"{Airac}.Procedures.Area"]);

		// A tab with no settings yet works its area out when it loads.
		Assert.False(result.Values.ContainsKey($"{Airac}.Departures.Area"));

		Assert.False(result.Values.ContainsKey($"{Airac}.Procedures.IncludeRoiAirports"));
		Assert.DoesNotContain(result.Values.Keys, key => key.StartsWith($"{Airac}.Telephony.Roi", StringComparison.Ordinal));
		Assert.Equal("Y", result.Values[$"{Airac}.Telephony.IncludeVatsimRadarVirtualAirlines"]);
	}
}
