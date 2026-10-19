using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Covers <see cref="UserConfigAreas"/>: the area a tab starts on when it has none saved - its ARTCCs
/// when it lists any, otherwise the ROI when one is on, otherwise everything (Procedures: none).
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
			(UserConfigKeys.DefaultRoiFilter, "Y"));

		Assert.Equal(UserConfigAreas.Artccs, UserConfigAreas.DefaultFor(tab, saved));
	}

	/// <summary>A tab that never saved its ARTCCs starts with the facility from Settings ticked, so it starts on its ARTCCs.</summary>
	[Fact]
	public void a_tab_that_never_saved_its_artccs_starts_on_the_facility()
	{
		Assert.Equal(UserConfigAreas.Artccs, UserConfigAreas.DefaultFor("Arrivals", Saved((UserConfigKeys.Facility, "ZOB"))));
	}

	/// <summary>A saved empty list meant every ARTCC: the ROI if one was on, otherwise everything.</summary>
	[Fact]
	public void a_tab_with_an_empty_artcc_list_starts_on_the_roi_if_one_was_on_or_everything()
	{
		(string, string) emptyList = ($"{Airac}.Departures.ArtccFilter", " , ");

		Assert.Equal(UserConfigAreas.Roi, UserConfigAreas.DefaultFor("Departures", Saved(emptyList, (UserConfigKeys.Facility, "ZOB"), (UserConfigKeys.DefaultRoiFilter, "Y"))));
		Assert.Equal(UserConfigAreas.Roi, UserConfigAreas.DefaultFor("Departures", Saved(emptyList, ($"{Airac}.Departures.Roi.OverrideDefaultRoi", "true"))));
		Assert.Equal(UserConfigAreas.Everything, UserConfigAreas.DefaultFor("Departures", Saved(emptyList, (UserConfigKeys.DefaultRoiFilter, "N"))));
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
		Assert.Equal(UserConfigAreas.Roi, UserConfigAreas.DefaultFor(tab, Saved((UserConfigKeys.DefaultRoiFilter, "Y"), (UserConfigKeys.Facility, "ZOB"))));
		Assert.Equal(UserConfigAreas.Roi, UserConfigAreas.DefaultFor(tab, Saved(($"{Airac}.{tab}.Roi.OverrideDefaultRoi", "Y"))));
		Assert.Equal(UserConfigAreas.Everything, UserConfigAreas.DefaultFor(tab, Saved((UserConfigKeys.Facility, "ZOB"))));
	}

	// ---- Procedures ----

	[Fact]
	public void procedures_starts_on_its_facilities_otherwise_none()
	{
		const string Node = $"{Airac}.Procedures";

		Assert.Equal(UserConfigAreas.Artccs, UserConfigAreas.DefaultFor("Procedures", Saved(($"{Node}.Facilities", "ZOB"))));
		Assert.Equal(UserConfigAreas.Artccs, UserConfigAreas.DefaultFor("Procedures", Saved((UserConfigKeys.Facility, "ZOB"))));

		// The default ROI alone doesn't select the ROI on Procedures.
		Assert.Equal(UserConfigAreas.None, UserConfigAreas.DefaultFor("Procedures", Saved(($"{Node}.Facilities", ""), (UserConfigKeys.DefaultRoiFilter, "Y"))));
	}

	[Fact]
	public void a_tab_with_no_area_is_refused()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => UserConfigAreas.DefaultFor("Telephony", Saved()));

		Assert.Contains("'Telephony'", ex.Message, StringComparison.Ordinal);
		Assert.Throws<ArgumentNullException>(() => UserConfigAreas.DefaultFor("Arrivals", null!));
	}
}
