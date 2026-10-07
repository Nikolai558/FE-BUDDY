using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="MapViewModel"/>: the ROI half - the corner fields, saving, cancelling and
/// clearing, and which typed corners move the box - against a stand-in <see cref="IRoiTarget"/>; and
/// the properties panel a Ctrl + click opens (<see cref="MapViewModel.Inspect"/>, <see cref="MapFeatureCard"/>).
/// </summary>
public sealed class MapViewModelTests
{
	private static readonly RegionOfInterest Saved = new(38.123456789, -104.987654321, 40.5, -100.25);

	/// <summary>A saved ROI shows in the fields with every place it has, so saving it again changes nothing.</summary>
	[Fact]
	public void the_fields_show_a_saved_roi_in_full()
	{
		MapViewModel vm = new(new Target { Current = Saved });

		Assert.Equal("38.123456789", vm.SwLat);
		Assert.Equal("-104.987654321", vm.SwLon);
		Assert.Equal("40.5", vm.NeLat);
		Assert.Equal("-100.25", vm.NeLon);
		Assert.Equal("SW 38.123456789, -104.987654321 / NE 40.5, -100.25", vm.CopyText);
		Assert.Equal("Saved", vm.RoiState);
	}

	[Fact]
	public void saving_an_unchanged_roi_saves_exactly_the_same_roi()
	{
		Target target = new() { Current = Saved };
		MapViewModel vm = new(target) { IsEditingRoi = true };

		vm.SaveRoiCommand.Execute(null);

		Assert.Equal(Saved, Assert.Single(target.Committed));
		Assert.False(vm.IsEditingRoi);
	}

	/// <summary>Only a corner on the globe moves the box: latitudes within ±90, longitudes within ±360.</summary>
	[Theory]
	[InlineData("SwLon", "1e12")]
	[InlineData("SwLon", "-361")]
	[InlineData("NeLon", "400")]
	[InlineData("SwLat", "91")]
	[InlineData("NeLat", "-90.5")]
	[InlineData("NeLat", "abc")]
	public void a_corner_off_the_globe_does_not_move_the_box(string field, string value)
	{
		MapViewModel vm = new(new Target { Current = Saved });
		GeoBounds? before = vm.DraftRoi;

		Type(vm, field, value);

		Assert.Equal(before, vm.DraftRoi);
		Assert.True(vm.IsEditingRoi);
	}

	/// <summary>Typed corners move the box once all four parse - even past 180, as a box drawn over the meridian has.</summary>
	[Fact]
	public void typed_corners_move_the_box()
	{
		// Typed one after the other, as in the fields.
		MapViewModel vm = new(new Target { Current = Saved })
		{
			SwLat = "38",
			NeLon = "190",
		};

		Assert.Equal(new GeoBounds(new GeoPoint(38, -104.987654321), new GeoPoint(40.5, 190)), vm.DraftRoi);
		Assert.Equal("Unsaved", vm.RoiState);
		Assert.Equal("Warn", vm.RoiStateKind);
	}

	/// <summary>A box over the 180th meridian can be drawn, but not saved.</summary>
	[Fact]
	public void a_box_over_the_meridian_is_refused_on_save()
	{
		Target target = new() { Current = Saved };
		MapViewModel vm = new(target) { NeLon = "190" };

		vm.SaveRoiCommand.Execute(null);

		Assert.Contains("180° meridian", vm.RoiError, StringComparison.Ordinal);
		Assert.Empty(target.Committed);
		Assert.True(vm.IsEditingRoi);
	}

	/// <summary>Turning editing off puts every field back, a half-typed one included.</summary>
	[Fact]
	public void turning_editing_off_puts_every_field_back()
	{
		MapViewModel vm = new(new Target { Current = Saved })
		{
			IsEditingRoi = true,
			SwLat = "abc",
			NeLon = "-99",
		};

		vm.IsEditingRoi = false;

		Assert.Equal("38.123456789", vm.SwLat);
		Assert.Equal("-100.25", vm.NeLon);
		Assert.Equal(new GeoBounds(new GeoPoint(Saved.SwLat, Saved.SwLon), new GeoPoint(Saved.NeLat, Saved.NeLon)), vm.DraftRoi);
		Assert.Equal("Saved", vm.RoiState);
	}

	/// <summary>Cancel (and so Esc) only does anything while editing - in a popup it would close the window.</summary>
	[Fact]
	public void cancel_is_only_available_while_editing()
	{
		Target target = new() { Current = Saved };
		MapViewModel vm = new(target);

		Assert.False(vm.CancelRoiCommand.CanExecute(null));

		vm.IsEditingRoi = true;
		Assert.True(vm.CancelRoiCommand.CanExecute(null));
		Assert.Equal("Editing", vm.RoiState);

		vm.CancelRoiCommand.Execute(null);
		Assert.False(vm.IsEditingRoi);
		Assert.Equal(1, target.Cancels);
	}

	[Fact]
	public void clearing_then_saving_saves_no_roi_where_that_is_allowed()
	{
		Target target = new() { Current = Saved, CanClear = true };
		MapViewModel vm = new(target);

		vm.ClearRoiCommand.Execute(null);
		Assert.Null(vm.DraftRoi);
		Assert.True(vm.IsEditingRoi);

		vm.SaveRoiCommand.Execute(null);

		Assert.Null(Assert.Single(target.Committed));
		Assert.Equal("Not set", vm.RoiState);
		Assert.False(vm.ClearRoiCommand.CanExecute(null));
	}

	[Fact]
	public void empty_corners_are_refused_where_no_roi_is_not_allowed()
	{
		Target target = new() { Current = Saved, CanClear = false };
		MapViewModel vm = new(target) { IsEditingRoi = true };
		vm.SwLat = vm.SwLon = vm.NeLat = vm.NeLon = " ";

		vm.SaveRoiCommand.Execute(null);

		Assert.Equal("Draw a box on the map (or type its corners) first.", vm.RoiError);
		Assert.Empty(target.Committed);
		Assert.False(vm.ClearRoiCommand.CanExecute(null));
	}

	/// <summary>A Shift + drag box is saved the moment it is drawn.</summary>
	[Fact]
	public void a_quick_drawn_box_is_saved_at_once()
	{
		Target target = new();
		MapViewModel vm = new(target);

		vm.OnQuickDrawn(new GeoBounds(new GeoPoint(41.2, -82.5), new GeoPoint(42.1, -81.1)));

		Assert.Equal(new RegionOfInterest(41.2, -82.5, 42.1, -81.1), Assert.Single(target.Committed));
		Assert.False(vm.IsEditingRoi);
	}

	/// <summary>A popup opened to pick an ROI starts in editing mode, with the default ROI to compare against.</summary>
	[Fact]
	public void a_picker_starts_editing_with_its_reference()
	{
		MapViewModel vm = new(new Target { StartsEditing = true, Reference = Saved });

		Assert.True(vm.IsEditingRoi);
		Assert.Null(vm.DraftRoi);
		Assert.Equal(Saved.NeLat, vm.ReferenceRoi!.Value.North);
	}

	// ============================ properties ============================

	/// <summary>A Ctrl + click opens the panel: one card per shape, and the shapes highlighted.</summary>
	[Fact]
	public void a_ctrl_click_shows_a_card_for_each_shape()
	{
		MapViewModel vm = new(new Target());
		MapLayer layer = Layer(
			new MapGeometry(MapGeometryKind.Line, [[new GeoPoint(40, -100), new GeoPoint(41, -99), new GeoPoint(42, -98)]])
			{
				Feature = MapFeature.FromGeoJson("LineString", 152, """{"style":"dashed"}"""),
			},
			new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(41.4, -81.85)], [new GeoPoint(41.5, -81.9)]], "CLE")
			{
				Feature = MapFeature.FromData("Airport", [new("FAA ID", "CLE")]),
			});
		MapHit line = new(layer, layer.Geometries[0], null);
		MapHit airport = new(layer, layer.Geometries[1], new GeoPoint(41.4, -81.85));

		vm.Inspect(new MapInspection(new GeoPoint(41.4, -81.85), [line, airport]));

		Assert.True(vm.IsInspecting);
		Assert.Equal("2 objects", vm.InspectTitle);
		Assert.Equal("At 41.40000, -81.85000.", vm.InspectNote);
		Assert.Equal([line, airport], vm.Highlighted);

		MapFeatureCard first = vm.InspectedFeatures[0];
		Assert.Equal("ZOB_Test.geojson", first.Title);
		Assert.Equal("LineString · 3 points · feature 152 in the file", first.Summary);
		Assert.Equal([new MapProperty("style", "dashed")], first.Properties);
		Assert.True(first.CanCopy);
		Assert.Equal("Copy the properties as JSON", first.CopyToolTip);

		MapFeatureCard second = vm.InspectedFeatures[1];
		Assert.Equal("Airport · 41.40000, -81.85000", second.Summary);
		Assert.Equal("Copy the properties", second.CopyToolTip);
		Assert.Equal("FAA ID: CLE", second.CopyText);
	}

	/// <summary>A shape with nothing known beyond itself says so, and has nothing to copy.</summary>
	[Fact]
	public void a_shape_with_no_properties_says_so()
	{
		MapLayer layer = Layer(new MapGeometry(MapGeometryKind.Polygon, [[new GeoPoint(0, 0), new GeoPoint(1, 0), new GeoPoint(1, 1)], [new GeoPoint(5, 5), new GeoPoint(6, 5), new GeoPoint(6, 6)]]));

		MapFeatureCard card = new(new MapHit(layer, layer.Geometries[0], null));

		Assert.Equal("Polygon · 2 parts, 6 points", card.Summary);
		Assert.False(card.HasProperties);
		Assert.False(card.CanCopy);
		Assert.Throws<ArgumentNullException>(() => new MapFeatureCard(null!));
	}

	/// <summary>A click on nothing still opens the panel, to say so and how to use it.</summary>
	[Fact]
	public void a_click_on_nothing_says_how_to_use_it()
	{
		MapViewModel vm = new(new Target());

		vm.Inspect(new MapInspection(new GeoPoint(40, -100), []));

		Assert.True(vm.IsInspecting);
		Assert.Equal("Nothing here", vm.InspectTitle);
		Assert.Equal("At 40.00000, -100.00000. Ctrl + click on a line, a dot or a label to see its properties.", vm.InspectNote);
		Assert.Empty(vm.InspectedFeatures);
		Assert.Throws<ArgumentNullException>(() => vm.Inspect(null!));
	}

	/// <summary>The panel shows the shapes drawn on top, and says when there were more.</summary>
	[Theory]
	[InlineData(30, "30 objects")]
	[InlineData(MapInspection.MaxHits, "100 or more objects")]
	public void many_shapes_show_the_ones_on_top(int count, string title)
	{
		MapLayer layer = Layer(new MapGeometry(MapGeometryKind.Point, [[new GeoPoint(40, -100)]]));
		MapViewModel vm = new(new Target());

		vm.Inspect(new MapInspection(new GeoPoint(40, -100), [.. Enumerable.Repeat(new MapHit(layer, layer.Geometries[0], null), count)]));

		Assert.Equal(title, vm.InspectTitle);
		Assert.Equal(MapViewModel.MaxCards, vm.InspectedFeatures.Count);
		Assert.Equal(MapViewModel.MaxCards, vm.Highlighted!.Count);
		Assert.EndsWith("Showing the 25 drawn on top.", vm.InspectNote, StringComparison.Ordinal);
	}

	/// <summary>Esc closes the panel first; with it closed, Esc cancels an ROI edit as before.</summary>
	[Fact]
	public void escape_closes_the_panel_then_cancels_the_edit()
	{
		Target target = new() { Current = Saved };
		MapViewModel vm = new(target) { IsEditingRoi = true };
		vm.Inspect(new MapInspection(new GeoPoint(40, -100), []));

		vm.EscapeCommand.Execute(null);

		Assert.False(vm.IsInspecting);
		Assert.Null(vm.Highlighted);
		Assert.True(vm.IsEditingRoi);
		Assert.Equal(0, target.Cancels);

		vm.EscapeCommand.Execute(null);

		Assert.False(vm.IsEditingRoi);
		Assert.Equal(1, target.Cancels);
		Assert.False(vm.EscapeCommand.CanExecute(null));
		Assert.False(vm.CloseInspectCommand.CanExecute(null));
	}

	private static MapLayer Layer(params MapGeometry[] geometries) => new("ZOB_Test.geojson", geometries, System.Windows.Media.Brushes.Orange);

	private static void Type(MapViewModel vm, string field, string value)
	{
		switch (field)
		{
			case "SwLat": vm.SwLat = value; break;
			case "SwLon": vm.SwLon = value; break;
			case "NeLat": vm.NeLat = value; break;
			default: vm.NeLon = value; break;
		}
	}

	/// <summary>A stand-in for where the ROI goes: remembers what was saved and cancelled.</summary>
	private sealed class Target : IRoiTarget
	{
		public event EventHandler? CurrentChanged;

		public string Title => "Test ROI";

		public string Description => "Test";

		public string SaveLabel => "Save";

		public bool CanClear { get; init; } = true;

		public bool StartsEditing { get; init; }

		public RegionOfInterest? Current { get; set; }

		public RegionOfInterest? Reference { get; init; }

		public List<RegionOfInterest?> Committed { get; } = [];

		public int Cancels { get; private set; }

		public void Commit(RegionOfInterest? roi)
		{
			Committed.Add(roi);
			Current = roi;
			CurrentChanged?.Invoke(this, EventArgs.Empty);
		}

		public void Cancel() => Cancels++;
	}
}
