using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>ARTCC Boundaries</b> sub-service tab inside the AIRAC Service screen: how the boundary
/// GeoJSON is laid out, which ARTCCs to include, whether lines are split at the antimeridian, the
/// optional region of interest, which FE-Buddy properties and the CRC ERAM defaults.
/// </summary>
/// <remarks>
/// Unlike every other AIRAC sub-service, ARTCC Boundaries writes GeoJSON Lines only: there is no
/// output toggle, no file-kind choice and no alias file. Save, Undo and navigation come from the
/// tab host's action bar; the run is launched by <b>Run AIRAC Service</b> on the Preview Settings
/// tab, and its results are shown on the Review tab, described by this tab through
/// <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class ArtccBoundariesViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.ArtccBoundaries";

	private ArtccBoundaryOutputBy _outputBy = ArtccBoundaryOutputBy.HighLow;
	private bool _splitAtAntimeridian = true;
	private HashSet<string> _savedLocationFilter = new(StringComparer.OrdinalIgnoreCase);
	private bool _suppressLocationChanges;
	private IReadOnlyList<(string LocationId, string Altitude)> _locationAltitudePairs = [];

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public ArtccBoundariesViewModel()
	{
		FebProperties = FebPropertyToggle.ListFor(ArtccBoundaryFebPropertyOptions.All, MarkDirty);
		LineDefaults =
		[
			new(ArtccBoundaryOutputFiles.HighClass, EramFieldKind.Line, MarkDirty),
			new(ArtccBoundaryOutputFiles.LowClass, EramFieldKind.Line, MarkDirty),
			new(ArtccBoundaryOutputFiles.UnlimitedClass, EramFieldKind.Line, MarkDirty),
		];

		Locations.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasLocations));
		ClearLocationsCommand = new RelayCommand(ClearLocations, () => Locations.Any(l => l.IsSelected));

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "ARTCC Boundaries";

	// ================= outputs =================

	/// <inheritdoc />
	protected override int EnabledOutputCount => 1;

	/// <inheritdoc />
	protected override string NoDefaultRoiHint =>
		"No default ROI is set, so every boundary is drawn in full. Set one in Settings, or override it here.";

	/// <inheritdoc />
	/// <remarks>ARTCC Boundaries has no file choices: it always writes Lines only.</remarks>
	protected override (string? Lines, string? Symbols, string? Text) EmitKeys => (null, null, null);

	/// <inheritdoc />
	/// <remarks>ARTCC Boundaries has no alias file.</remarks>
	protected override bool HasAliasFile => false;

	// ================= file layout =================

	/// <summary>Whether the GeoJSON is one High file and one Low file, an UNLIMITED ring going into both.</summary>
	public bool OutputHighLow
	{
		get => _outputBy == ArtccBoundaryOutputBy.HighLow;
		set { if (value) SetOutputBy(ArtccBoundaryOutputBy.HighLow); }
	}

	/// <summary>Whether the GeoJSON is one file each for High, Low and Unlimited rings.</summary>
	public bool OutputHighLowUnlimited
	{
		get => _outputBy == ArtccBoundaryOutputBy.HighLowUnlimited;
		set { if (value) SetOutputBy(ArtccBoundaryOutputBy.HighLowUnlimited); }
	}

	/// <summary>Whether the GeoJSON is one file per ARTCC and altitude present, e.g. <c>ZOB-HIGH</c>.</summary>
	public bool OutputByArtccAltitude
	{
		get => _outputBy == ArtccBoundaryOutputBy.ArtccAltitude;
		set { if (value) SetOutputBy(ArtccBoundaryOutputBy.ArtccAltitude); }
	}

	/// <summary>Whether a boundary line that crosses 180 degrees longitude is split in two there.</summary>
	public bool SplitAtAntimeridian { get => _splitAtAntimeridian; set { if (SetProperty(ref _splitAtAntimeridian, value)) MarkDirty(); } }

	// ================= ARTCCs =================

	/// <summary>
	/// One toggle per ARTCC in the selected cycle's <c>ARB_SEG</c>. The selected set is the
	/// filter; none selected means every ARTCC. Empty until the cycle's data is loaded.
	/// </summary>
	public ObservableCollection<ArtccToggle> Locations { get; } = [];

	/// <summary>Whether the ARTCC list has been built from the selected cycle yet.</summary>
	public bool HasLocations => Locations.Count > 0;

	/// <summary>Deselects every ARTCC, which means every ARTCC is included.</summary>
	public ICommand ClearLocationsCommand { get; }

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>
	/// Builds the ARTCC toggle list and the (LocationId, Altitude) pairs from the cycle's
	/// <c>ARB_SEG</c>, then adds a CRC defaults row for each pair (used by ArtccAltitude mode).
	/// </remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
		_savedLocationFilter = ParseList(Get("LocationFilter"));

		string[] locationIds = [.. (data.Arb?.ArbSeg ?? [])
			.Select(s => (s.LocationId ?? string.Empty).Trim().ToUpperInvariant())
			.Where(id => id.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)];

		_suppressLocationChanges = true;
		try
		{
			Locations.Clear();
			foreach (string locationId in locationIds)
			{
				Locations.Add(new ArtccToggle(locationId, _savedLocationFilter.Contains(locationId), OnLocationToggled));
			}
		}
		finally
		{
			_suppressLocationChanges = false;
		}

		_locationAltitudePairs = [.. (data.Arb?.ArbSeg ?? [])
			.Select(s => (
				LocationId: (s.LocationId ?? string.Empty).Trim().ToUpperInvariant(),
				Altitude: (s.Altitude ?? string.Empty).Trim().ToUpperInvariant()))
			.Where(p => p.LocationId.Length > 0 && p.Altitude is "HIGH" or "LOW" or "UNLIMITED")
			.Distinct()
			.OrderBy(p => p.LocationId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(p => AltitudeOrder(p.Altitude))];

		AddCrcRows(_locationAltitudePairs.Select(p => new EramClassDefault($"{p.LocationId}-{p.Altitude}", EramFieldKind.Line, MarkDirty)));

		// The per-ARTCC files exist only now; list them even when the tab has unsaved edits (the
		// resync below then does nothing).
		RefreshVnasFiles();

		// The lists were empty when this tab snapshotted itself at construction; re-take the
		// snapshot now they reflect what is actually saved.
		ResyncSavedState();
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		if (result.ArtccBoundaries is not { } artccBoundaries)
		{
			return null;
		}

		string summary = $"{artccBoundaries.LocationCount:N0} ARTCC(s), {artccBoundaries.RingCount:N0} boundary line(s), "
			+ $"{artccBoundaries.GeojsonFilesWritten.Count:N0} GeoJSON file(s)";

		return new SubServiceRunResult(Title, summary, artccBoundaries.Messages);
	}

	/// <inheritdoc />
	/// <remarks>The block goes on <see cref="AiracServiceSettings.ArtccBoundaries"/>.</remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputBy"] = _outputBy.ToString(),
			["LocationFilter"] = string.Join(',', SelectedLocationIds()),
			["SplitAtAntimeridian"] = YesNo(SplitAtAntimeridian),
		};

		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		string[] selectedLocations = [.. SelectedLocationIds()];

		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("GeoJSON files", DescribeGeojsonFiles()),
			new ServicePreviewRow("ARTCCs", selectedLocations.Length > 0 ? string.Join(", ", selectedLocations) : "Every ARTCC"),
			new ServicePreviewRow("Split at antimeridian", SplitAtAntimeridian ? "Yes" : "No"),
			new ServicePreviewRow("FE-Buddy properties", DescribeFebProperties()),
			new ServicePreviewRow("Region of interest", HasRoi ? $"{DescribeRoi()}; lines are clipped at its edge" : DescribeRoi()),
			new ServicePreviewRow("Upload to vNAS", DescribeVnasFiles()),
			new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()),
		];

		return [new ServicePreviewSection("ARTCC Boundaries", rows)];
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_outputBy = Enum.TryParse(Get("OutputBy"), true, out ArtccBoundaryOutputBy outputBy) ? outputBy : ArtccBoundaryOutputBy.HighLow;
		_splitAtAntimeridian = GetBool("SplitAtAntimeridian", true);
		LoadSharedSettings();

		// Re-apply the saved location filter to any already-built toggles, without a dirty check
		// per toggle; ClearDirty below re-takes the snapshot once.
		_savedLocationFilter = ParseList(Get("LocationFilter"));
		_suppressLocationChanges = true;
		try
		{
			foreach (ArtccToggle toggle in Locations)
			{
				toggle.IsSelected = _savedLocationFilter.Contains(toggle.Artcc);
			}
		}
		finally
		{
			_suppressLocationChanges = false;
		}

		foreach (string name in new[]
		{
			nameof(OutputHighLow), nameof(OutputHighLowUnlimited), nameof(OutputByArtccAltitude), nameof(SplitAtAntimeridian),
		})
		{
			OnPropertyChanged(name);
		}

		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("OutputBy", _outputBy.ToString());
		Set("LocationFilter", string.Join(',', SelectedLocationIds()));
		Set("SplitAtAntimeridian", YesNo(SplitAtAntimeridian));
		SaveSharedSettings();
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation) => ValidateSharedSettings(validation);

	/// <inheritdoc />
	/// <remarks>
	/// HighLow and HighLowUnlimited each write one file per fixed group; ArtccAltitude writes one
	/// file per ticked ARTCC and altitude (every ARTCC when none is ticked, or none at all before
	/// the cycle's data has loaded). There is no alias file.
	/// </remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		if (_outputBy == ArtccBoundaryOutputBy.ArtccAltitude)
		{
			HashSet<string> selected = new(SelectedLocationIds(), StringComparer.OrdinalIgnoreCase);

			foreach ((string locationId, string altitude) in _locationAltitudePairs)
			{
				if (selected.Count > 0 && !selected.Contains(locationId))
				{
					continue;
				}

				string key = ArtccBoundaryOutputFiles.KeyFor(locationId, altitude);
				string className = ArtccBoundaryOutputFiles.ClassFor(locationId, altitude);

				yield return new OutputFileOption(key, locationId, altitude, key, IsGeojson: true, [(className, EramFieldKind.Line)]);
			}

			yield break;
		}

		yield return HighLowFile(ArtccBoundaryOutputFiles.HighClass);
		yield return HighLowFile(ArtccBoundaryOutputFiles.LowClass);

		if (_outputBy == ArtccBoundaryOutputBy.HighLowUnlimited)
		{
			yield return HighLowFile(ArtccBoundaryOutputFiles.UnlimitedClass);
		}
	}

	// ================= helpers =================

	private static OutputFileOption HighLowFile(string group)
	{
		string key = ArtccBoundaryOutputFiles.KeyFor(group);
		return new OutputFileOption(key, group, "Lines", key, IsGeojson: true, [(group, EramFieldKind.Line)]);
	}

	private static int AltitudeOrder(string altitude) => altitude switch
	{
		"HIGH" => 0,
		"LOW" => 1,
		_ => 2,
	};

	/// <summary>The "GeoJSON files" preview row: what the current file layout produces.</summary>
	private string DescribeGeojsonFiles() => _outputBy switch
	{
		ArtccBoundaryOutputBy.HighLow => "High and Low (UNLIMITED lines in both)",
		ArtccBoundaryOutputBy.HighLowUnlimited => "High, Low and Unlimited",
		ArtccBoundaryOutputBy.ArtccAltitude => "One file per ARTCC and altitude",
		_ => string.Empty,
	};

	/// <summary>
	/// The ARTCCs the filter holds, sorted. Before the cycle's list is built there are no toggles
	/// to read, so the saved filter stands in - otherwise a save or a run made before the data
	/// arrives would quietly drop the user's filter.
	/// </summary>
	private IEnumerable<string> SelectedLocationIds() =>
		Locations.Count > 0
			? Locations.Where(l => l.IsSelected).Select(l => l.Artcc)
			: _savedLocationFilter.OrderBy(a => a, StringComparer.OrdinalIgnoreCase);

	private void OnLocationToggled()
	{
		if (_suppressLocationChanges)
		{
			return;
		}

		MarkDirty();
	}

	private void ClearLocations()
	{
		if (Locations.All(l => !l.IsSelected))
		{
			return;
		}

		// One dirty check for the whole clear rather than one per toggle.
		_suppressLocationChanges = true;
		try
		{
			foreach (ArtccToggle toggle in Locations)
			{
				toggle.IsSelected = false;
			}
		}
		finally
		{
			_suppressLocationChanges = false;
		}

		MarkDirty();
	}

	private void SetOutputBy(ArtccBoundaryOutputBy value)
	{
		if (_outputBy == value)
		{
			return;
		}

		_outputBy = value;
		OnPropertyChanged(nameof(OutputHighLow));
		OnPropertyChanged(nameof(OutputHighLowUnlimited));
		OnPropertyChanged(nameof(OutputByArtccAltitude));
		MarkDirty();
	}
}
