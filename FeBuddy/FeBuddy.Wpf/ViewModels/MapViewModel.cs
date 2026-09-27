using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Wpf.Map.Models;
using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// One map workspace: the shared layers (<see cref="State"/>) plus the ROI this map edits. The
/// Map page builds one for the saved default ROI; each map popup builds one for whatever opened
/// it (see <see cref="IRoiTarget"/>), so every map in the app is the same screen.
/// <para>
/// ROI editing: <see cref="IsEditingRoi"/> switches the map into drawing mode (draw, move and
/// resize the box, or type its corners) until <b>Save</b> commits it and switches editing off,
/// or <b>Cancel</b> puts the saved box back. <b>Shift + drag</b> on the map skips all that: the
/// box is saved the moment the mouse is let go (<see cref="OnQuickDrawn"/>).
/// </para>
/// </summary>
public sealed class MapViewModel : ObservableObject
{
	private readonly Dispatcher _dispatcher;
	private GeoBounds? _draftRoi;
	private string _neLat = string.Empty;
	private string _neLon = string.Empty;
	private string _swLat = string.Empty;
	private string _swLon = string.Empty;
	private bool _isEditingRoi;
	private string? _roiError;
	private bool _syncing;

	/// <summary>Creates the Map page's workspace, editing the saved default ROI.</summary>
	public MapViewModel()
		: this(new DefaultRoiTarget())
	{
	}

	/// <summary>Creates a workspace that edits <paramref name="target"/>'s ROI.</summary>
	/// <param name="target">Where a saved ROI goes.</param>
	public MapViewModel(IRoiTarget target)
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
		Target = target;
		State = MapLayersState.Shared;

		EditRoiCommand = new RelayCommand(() => IsEditingRoi = true);
		SaveRoiCommand = new RelayCommand(SaveRoi, () => IsEditingRoi);
		CancelRoiCommand = new RelayCommand(CancelRoi);
		ClearRoiCommand = new RelayCommand(ClearRoi, () => Target.CanClear && DraftRoi is not null);
		ZoomToRoiCommand = new RelayCommand(() => FrameRequested?.Invoke(this, DraftRoi!.Value), () => DraftRoi is not null);

		target.CurrentChanged += (_, _) => _dispatcher.BeginInvoke(OnTargetChanged);

		ResetDraft();
		_isEditingRoi = target.StartsEditing;
	}

	/// <summary>Raised when the map should frame a box (Zoom to ROI).</summary>
	public event EventHandler<GeoBounds>? FrameRequested;

	/// <summary>The layers every map shares.</summary>
	public MapLayersState State { get; }

	/// <summary>Where a saved ROI goes.</summary>
	public IRoiTarget Target { get; }

	/// <summary>A second box drawn dashed for comparison (the default ROI, when editing an override).</summary>
	public GeoBounds? ReferenceRoi => ToBounds(Target.Reference);

	// ============================= the box ===============================

	/// <summary>
	/// The box on the map. Two-way with the map: drawing, moving or resizing it lands here and
	/// fills the corner fields; typing valid corners moves it.
	/// </summary>
	public GeoBounds? DraftRoi
	{
		get => _draftRoi;
		set
		{
			if (!SetProperty(ref _draftRoi, value))
			{
				return;
			}

			if (!_syncing)
			{
				WriteFields(value);
				if (!IsEditingRoi)
				{
					IsEditingRoi = true;
				}
			}

			RoiError = null;
			RaiseRoiState();
		}
	}

	/// <summary>The north-east latitude field.</summary>
	public string NeLat
	{
		get => _neLat;
		set => SetField(ref _neLat, value);
	}

	/// <summary>The north-east longitude field.</summary>
	public string NeLon
	{
		get => _neLon;
		set => SetField(ref _neLon, value);
	}

	/// <summary>The south-west latitude field.</summary>
	public string SwLat
	{
		get => _swLat;
		set => SetField(ref _swLat, value);
	}

	/// <summary>The south-west longitude field.</summary>
	public string SwLon
	{
		get => _swLon;
		set => SetField(ref _swLon, value);
	}

	/// <summary>
	/// Whether the map is in ROI editing mode. Switching it off by hand is a cancel: an unsaved
	/// box goes back to the saved one.
	/// </summary>
	public bool IsEditingRoi
	{
		get => _isEditingRoi;
		set
		{
			if (_isEditingRoi == value)
			{
				return;
			}

			if (!value && IsRoiDirty)
			{
				ResetDraft();
			}

			SetProperty(ref _isEditingRoi, value);
			RoiError = null;
			RaiseRoiState();
		}
	}

	/// <summary>Why the last Save was refused, or <see langword="null"/>.</summary>
	public string? RoiError
	{
		get => _roiError;
		private set => SetProperty(ref _roiError, value);
	}

	/// <summary>Whether the box differs from the saved one.</summary>
	public bool IsRoiDirty => !Nullable.Equals(DraftRoi, ToBounds(Target.Current));

	/// <summary>A one-word state for the ROI card's chip: Editing, Unsaved, Saved or Not set.</summary>
	public string RoiState => IsEditingRoi
		? IsRoiDirty ? "Unsaved" : "Editing"
		: Target.Current is null ? "Not set" : "Saved";

	/// <summary>The chip colour for <see cref="RoiState"/>: Accent, Warn, Positive or Neutral (see Chip.State).</summary>
	public string RoiStateKind => RoiState switch
	{
		"Editing" => "Accent",
		"Unsaved" => "Warn",
		"Saved" => "Positive",
		_ => "Neutral",
	};

	/// <summary>The four corners on one line, for the copy button; empty when there is no box.</summary>
	public string CopyText => DraftRoi is { } b
		? string.Create(CultureInfo.InvariantCulture, $"SW {b.South:0.####}, {b.West:0.####} / NE {b.North:0.####}, {b.East:0.####}")
		: string.Empty;

	/// <summary>Whether there is a box to copy or zoom to.</summary>
	public bool HasDraftRoi => DraftRoi is not null;

	// ============================ commands ===============================

	/// <summary>Switches ROI editing on.</summary>
	public ICommand EditRoiCommand { get; }

	/// <summary>Validates and saves the box, then leaves editing mode.</summary>
	public ICommand SaveRoiCommand { get; }

	/// <summary>Drops unsaved changes and leaves editing mode (and closes a popup).</summary>
	public ICommand CancelRoiCommand { get; }

	/// <summary>Removes the box, to save "no ROI" (the Map page only).</summary>
	public ICommand ClearRoiCommand { get; }

	/// <summary>Frames the box on the map.</summary>
	public ICommand ZoomToRoiCommand { get; }

	/// <summary>A Shift + drag finished: take the box and save it straight away.</summary>
	/// <param name="box">The box drawn.</param>
	public void OnQuickDrawn(GeoBounds box)
	{
		DraftRoi = box;
		SaveRoi();
	}

	private void SaveRoi()
	{
		RoiError = null;

		if (string.IsNullOrWhiteSpace(SwLat) && string.IsNullOrWhiteSpace(SwLon)
			&& string.IsNullOrWhiteSpace(NeLat) && string.IsNullOrWhiteSpace(NeLon))
		{
			if (!Target.CanClear)
			{
				RoiError = "Draw a box on the map (or type its corners) first.";
				return;
			}

			Finish(null);
			return;
		}

		string swLat = SwLat.Trim(), swLon = SwLon.Trim(), neLat = NeLat.Trim(), neLon = NeLon.Trim();

		if (CrossesAntimeridian(swLon, neLon))
		{
			RoiError = "The box crosses the 180° meridian. An ROI has to sit on one side of it - redraw it there.";
			return;
		}

		if (!RoiFilter.IsCoordinateValidFormat(swLat, swLon, neLat, neLon, out string? formatError))
		{
			RoiError = formatError;
			return;
		}

		double sLat = double.Parse(swLat, CultureInfo.InvariantCulture);
		double sLon = double.Parse(swLon, CultureInfo.InvariantCulture);
		double nLat = double.Parse(neLat, CultureInfo.InvariantCulture);
		double nLon = double.Parse(neLon, CultureInfo.InvariantCulture);

		if (!RoiFilter.IsCoordinatesRelativePositionValid(sLat, sLon, nLat, nLon, out string? positionError))
		{
			RoiError = positionError;
			return;
		}

		Finish(new RegionOfInterest(sLat, sLon, nLat, nLon));
	}

	private void Finish(RegionOfInterest? roi)
	{
		Target.Commit(roi);
		_isEditingRoi = false;
		OnPropertyChanged(nameof(IsEditingRoi));
		ResetDraft();
	}

	private void CancelRoi()
	{
		ResetDraft();
		_isEditingRoi = false;
		OnPropertyChanged(nameof(IsEditingRoi));
		RoiError = null;
		RaiseRoiState();
		Target.Cancel();
	}

	private void ClearRoi()
	{
		DraftRoi = null;
		IsEditingRoi = true;
	}

	// ============================= syncing ===============================

	private void OnTargetChanged()
	{
		if (!IsEditingRoi)
		{
			ResetDraft();
		}

		RaiseRoiState();
	}

	/// <summary>Puts the saved ROI back in the box and the fields.</summary>
	private void ResetDraft()
	{
		_syncing = true;
		DraftRoi = ToBounds(Target.Current);
		WriteFields(DraftRoi);
		_syncing = false;
		RaiseRoiState();
	}

	private void WriteFields(GeoBounds? box)
	{
		bool wasSyncing = _syncing;
		_syncing = true;
		NeLat = box is { } b1 ? Format(b1.North) : string.Empty;
		NeLon = box is { } b2 ? Format(b2.East) : string.Empty;
		SwLat = box is { } b3 ? Format(b3.South) : string.Empty;
		SwLon = box is { } b4 ? Format(b4.West) : string.Empty;
		_syncing = wasSyncing;
	}

	/// <summary>A typed corner: once all four parse, the box on the map follows.</summary>
	private void SetField(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
	{
		if (!SetProperty(ref field, value ?? string.Empty, name) || _syncing)
		{
			return;
		}

		if (!IsEditingRoi)
		{
			IsEditingRoi = true;
		}

		if (TryParse(SwLat, out double swLat) && TryParse(SwLon, out double swLon)
			&& TryParse(NeLat, out double neLat) && TryParse(NeLon, out double neLon))
		{
			_syncing = true;
			DraftRoi = new GeoBounds(
				new GeoPoint(Math.Min(swLat, neLat), Math.Min(swLon, neLon)),
				new GeoPoint(Math.Max(swLat, neLat), Math.Max(swLon, neLon)));
			_syncing = false;
		}

		RaiseRoiState();
	}

	private void RaiseRoiState()
	{
		OnPropertyChanged(nameof(IsRoiDirty));
		OnPropertyChanged(nameof(RoiState));
		OnPropertyChanged(nameof(RoiStateKind));
		OnPropertyChanged(nameof(CopyText));
		OnPropertyChanged(nameof(HasDraftRoi));
		CommandManager.InvalidateRequerySuggested();
	}

	/// <summary>
	/// Whether the corners describe a box drawn across the 180th meridian: the map writes such a
	/// box with one edge past ±180 (e.g. west 170, east 190), which no ROI can hold.
	/// </summary>
	private static bool CrossesAntimeridian(string swLon, string neLon) =>
		(TryParse(swLon, out double west) && west is < -180.0 and >= -360.0)
		|| (TryParse(neLon, out double east) && east is > 180.0 and <= 360.0);

	private static bool TryParse(string text, out double value) =>
		double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

	private static string Format(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

	private static GeoBounds? ToBounds(RegionOfInterest? roi) => roi is { } r
		? new GeoBounds(new GeoPoint(r.SwLat, r.SwLon), new GeoPoint(r.NeLat, r.NeLon))
		: null;
}
