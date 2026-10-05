using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the General tab's table (<see cref="AiracGeneralTabViewModel"/>, <see cref="SubServiceRow"/>):
/// everything included and on to start, the outputs a sub-service doesn't offer always off, the
/// last output kept on, where the choices are saved, and the outputs each tab saved before the
/// table, read until it is first saved - against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class AiracGeneralTabViewModelTests : IDisposable
{
	private const string Node = "Services.AiracService";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_General_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public AiracGeneralTabViewModelTests()
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

	private static SubServiceRow Row(AiracGeneralTabViewModel general, string key) => general.RowFor(key)!;

	[Fact]
	public void with_nothing_saved_every_sub_service_is_in_with_every_output_it_offers_on()
	{
		AiracGeneralTabViewModel general = new();

		Assert.All(general.SubServices, row => Assert.True(row.IsIncluded));
		Assert.All(general.SubServices, row => Assert.Equal(row.Descriptor.Outputs, row.OutputsOn));
		Assert.False(general.IsDirty);
	}

	/// <summary>vNAS Alias Upload makes nothing of its own, so it is not a row; it comes in by itself.</summary>
	[Fact]
	public void the_table_lists_every_sub_service_but_vnas_alias_upload_in_rail_order()
	{
		AiracGeneralTabViewModel general = new();

		Assert.Equal(
			[.. AiracSubServices.All.Where(d => d.Key != AiracSubServices.VnasAliasKey).Select(d => d.DisplayName)],
			general.SubServices.Select(row => row.DisplayName));
		Assert.Null(general.RowFor(AiracSubServices.VnasAliasKey));
	}

	[Fact]
	public void an_output_a_sub_service_does_not_offer_is_always_off_and_greyed_out()
	{
		AiracGeneralTabViewModel general = new();
		SubServiceRow fixes = Row(general, AiracSubServices.FixesKey);

		fixes.Load(included: true, SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson);

		Assert.False(fixes.Alias);
		Assert.False(fixes.CanEditAlias);
		Assert.Equal("Fixes doesn't make an alias file.", fixes.AliasToolTip);
		Assert.Equal("Fixes doesn't make Procedure_Changes.md; only Procedures does.", fixes.ProcedureChangesToolTip);
		Assert.EndsWith("Its settings are on the Fixes tab, in the list to the left.", fixes.GeojsonToolTip);
	}

	[Fact]
	public void the_last_output_of_an_included_sub_service_stays_on()
	{
		AiracGeneralTabViewModel general = new();
		SubServiceRow airports = Row(general, AiracSubServices.AirportsKey);
		SubServiceRow fixes = Row(general, AiracSubServices.FixesKey);

		airports.Geojson = false;
		airports.Alias = false;
		fixes.Geojson = false;

		Assert.True(airports.Alias);
		Assert.False(airports.Geojson);
		Assert.True(fixes.Geojson);
	}

	[Fact]
	public void leaving_a_sub_service_out_greys_out_its_outputs_and_keeps_them()
	{
		AiracGeneralTabViewModel general = new();
		SubServiceRow airports = Row(general, AiracSubServices.AirportsKey);
		int changes = 0;
		general.SubServicesChanged += (_, _) => changes++;

		airports.IsIncluded = false;

		Assert.False(general.IsIncluded(AiracSubServices.AirportsKey));
		Assert.False(airports.CanEditAlias);
		Assert.False(airports.CanEditGeojson);
		Assert.True(airports.Alias);
		Assert.True(airports.Geojson);
		Assert.Equal(1, changes);
		Assert.True(general.IsDirty);
	}

	[Fact]
	public void the_choices_are_saved_under_the_general_tab_and_read_back()
	{
		AiracGeneralTabViewModel general = new();
		Row(general, AiracSubServices.WxStationsKey).IsIncluded = false;
		Row(general, AiracSubServices.AirportsKey).Geojson = false;
		Row(general, AiracSubServices.ProceduresKey).ProceduresJson = false;

		Assert.True(general.Save());

		Assert.DoesNotContain("WxStations", UserConfigFile.GetValue($"{Node}.SelectedSubServices")!, StringComparison.Ordinal);
		Assert.Equal("N", UserConfigFile.GetValue($"{Node}.Outputs.Airports.Geojson"));
		Assert.Equal("Y", UserConfigFile.GetValue($"{Node}.Outputs.Airports.Alias"));
		Assert.Equal("N", UserConfigFile.GetValue($"{Node}.Outputs.Procedures.ProceduresJson"));
		Assert.Null(UserConfigFile.GetValue($"{Node}.Outputs.Fixes.Alias"));

		AiracGeneralTabViewModel reloaded = new();
		Assert.False(reloaded.IsIncluded(AiracSubServices.WxStationsKey));
		Assert.False(Row(reloaded, AiracSubServices.AirportsKey).Geojson);
		Assert.False(Row(reloaded, AiracSubServices.ProceduresKey).ProceduresJson);
		Assert.True(Row(reloaded, AiracSubServices.ProceduresKey).ProcedureChanges);
	}

	/// <summary>Before the table, each tab saved its own outputs - Airways' "None" split was its GeoJSON switch.</summary>
	[Fact]
	public void outputs_saved_by_the_tabs_before_the_table_are_read_until_it_is_saved()
	{
		UserConfigFile.TrySetValue($"{Node}.SelectedSubServices", "Airports,Airways,Procedures,VnasAlias");
		UserConfigFile.TrySetValue($"{Node}.Airports.GenerateGeojson", "N");
		UserConfigFile.TrySetValue($"{Node}.Geojson.Airways.OutputBy", "None");
		UserConfigFile.TrySetValue($"{Node}.Procedures.GenerateChangesDocument", "N");
		UserConfigFile.TrySetValue($"{Node}.Procedures.GenerateAliasFile", "N");

		AiracGeneralTabViewModel general = new();

		Assert.Equal(["Airports", "Airways", "Procedures"], general.IncludedSubServices.Select(row => row.Key));
		Assert.Equal(SubServiceOutputKinds.Alias, Row(general, AiracSubServices.AirportsKey).OutputsOn);
		Assert.Equal(SubServiceOutputKinds.Alias, Row(general, AiracSubServices.AirwaysKey).OutputsOn);
		Assert.Equal(SubServiceOutputKinds.ProceduresJson, Row(general, AiracSubServices.ProceduresKey).OutputsOn);

		// Once the table has its own, the old ones no longer count.
		UserConfigFile.TrySetValue($"{Node}.Outputs.Airports.Geojson", "Y");
		Assert.True(Row(new AiracGeneralTabViewModel(), AiracSubServices.AirportsKey).Geojson);
	}

	/// <summary>Settings edited by hand with every output off would make nothing: they load with every output on.</summary>
	[Fact]
	public void every_output_off_loads_with_every_output_on()
	{
		UserConfigFile.TrySetValue($"{Node}.Outputs.Navaids.Alias", "N");
		UserConfigFile.TrySetValue($"{Node}.Outputs.Navaids.Geojson", "N");

		AiracGeneralTabViewModel general = new();

		Assert.Equal(SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson, Row(general, AiracSubServices.NavaidsKey).OutputsOn);
	}

	[Fact]
	public void the_preview_names_each_included_sub_service_with_its_outputs()
	{
		AiracGeneralTabViewModel general = new();
		foreach (SubServiceRow row in general.SubServices.Where(row => row.Key is not (AiracSubServices.AirportsKey or AiracSubServices.ProceduresKey)))
		{
			row.IsIncluded = false;
		}

		Row(general, AiracSubServices.ProceduresKey).Alias = false;

		string subServices = general.BuildPreviewSummary()[0].Rows.Single(row => row.Label == "Sub-services").Value;

		Assert.Equal("Airports (Alias, GeoJSON), Procedures (Procedure Changes, Procedures JSON)", subServices);
	}

	/// <summary>A tab reads its outputs from its row and follows it, without being marked unsaved.</summary>
	[Fact]
	public void a_tab_follows_its_row()
	{
		AiracGeneralTabViewModel general = new();
		AirportsViewModel airports = new();
		airports.AttachOutputs(Row(general, AiracSubServices.AirportsKey));

		Row(general, AiracSubServices.AirportsKey).Geojson = false;

		Assert.False(airports.GenerateGeojson);
		Assert.Equal("N", airports.BuildSettingsBlock()["GenerateGeojson"]);
		Assert.Equal(["Airports.txt"], airports.OutputFileEntries().Select(file => file.Key));
		Assert.False(airports.IsDirty);
	}
}
