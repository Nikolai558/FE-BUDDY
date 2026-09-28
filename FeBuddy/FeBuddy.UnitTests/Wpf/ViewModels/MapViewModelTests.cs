using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the ROI half of <see cref="MapViewModel"/>: the corner fields, saving, cancelling and
/// clearing, and which typed corners move the box - against a stand-in <see cref="IRoiTarget"/>.
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
