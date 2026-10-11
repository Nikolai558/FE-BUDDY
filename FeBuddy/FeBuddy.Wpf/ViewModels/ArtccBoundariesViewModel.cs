using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>ARTCC Boundaries</b> sub-service tab inside the AIRAC Service screen: how the boundary
/// GeoJSON is laid out, the area (the ARTCCs ticked, an ROI the boundaries are cut off at, or every
/// ARTCC), whether lines are split at the antimeridian, which FE-Buddy properties and the CRC ERAM
/// defaults.
/// </summary>
/// <remarks>
/// Unlike every other AIRAC sub-service, ARTCC Boundaries writes GeoJSON Lines only: there is no
/// Outputs card, no file-kind choice and no alias file. Save, Undo and navigation come from the
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
	protected override IReadOnlyList<SubServiceArea> Areas => SubServiceSettingsReader.ArtccOrRoiAreas;

	/// <inheritdoc />
	protected override IEnumerable<string> AreaArtccIds() => SelectedLocationIds();

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
	/// One toggle per ARTCC in the selected cycle's <c>ARB_SEG</c>: the ARTCCs area's boundaries.
	/// Empty until the cycle's data is loaded.
	/// </summary>
	public ObservableCollection<ArtccToggle> Locations { get; } = [];

	/// <summary>Whether the ARTCC list has been built from the selected cycle yet.</summary>
	public bool HasLocations => Locations.Count > 0;

	/// <summary>Deselects every ARTCC.</summary>
	public ICommand ClearLocationsCommand { get; }

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>
	/// Builds the ARTCC toggle list and the (LocationId, Altitude) pairs from the cycle's
	/// <c>ARB_SEG</c>, then adds a CRC defaults row for each pair (used by ArtccAltitude mode).
	/// </remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
		_savedLocationFilter = ParseArtccListOrFacility(Get("LocationFilter"));

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
		RefreshOutputFiles();

		// The lists were empty when this tab snapshotted itself at construction; re-take the
		// snapshot now they reflect what is actually saved (or the Settings facility).
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
			["SplitAtAntimeridian"] = YesNo(SplitAtAntimeridian),
		};

		// The ARTCCs only while the area is the ARTCCs, so the parser sees exactly what the run will do.
		if (Area == SubServiceArea.Artccs)
		{
			s["LocationFilter"] = string.Join(',', SelectedLocationIds());
		}

		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("GeoJSON files", DescribeGeojsonFiles()),
			new ServicePreviewRow("Split at antimeridian", SplitAtAntimeridian ? "Yes" : "No"),
			new ServicePreviewRow("FE-Buddy properties", DescribeFebProperties()),
			new ServicePreviewRow("Area", DescribeArea()),
			new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()),
		];

		return [new ServicePreviewSection("ARTCC Boundaries", rows) { WhatYoullGet = WhatYoullGet }];
	}

	/// <inheritdoc />
	protected override IEnumerable<SummaryBlock> BuildWhatYoullGet()
	{
		string[] artccs = [.. SelectedLocationIds()];

		string boundaries = Area != SubServiceArea.Artccs ? "Every ARTCC boundary"
			: artccs.Length > 0 ? $"The boundaries of {SummaryLines.Join(artccs, "and")}"
			: "No boundaries yet: tick an ARTCC on the Area card";

		yield return new SummaryBlock(SubServiceOutputKinds.Geojson, new SummaryLines()
			.Add(SummaryJoin.First, boundaries)
			.Add(SummaryJoin.AndOnly, RoiLine(roi => $"the parts inside {roi}"))
			.ToList())
		{
			Files = [.. SummaryFiles()],
			Notes = _outputBy == ArtccBoundaryOutputBy.HighLow ? ["Boundaries labeled \"UNLIMITED\" go in both files."] : [],
		};
	}

	/// <summary>
	/// The files What You'll Get lists: the fixed High, Low (and Unlimited) files, or with one file per
	/// ARTCC and altitude, each file while there are only a few, otherwise how they're named.
	/// </summary>
	private IEnumerable<SummaryFile> SummaryFiles()
	{
		if (_outputBy != ArtccBoundaryOutputBy.ArtccAltitude)
		{
			yield return HighLowSummaryFile(ArtccBoundaryOutputFiles.HighClass);
			yield return HighLowSummaryFile(ArtccBoundaryOutputFiles.LowClass);

			if (_outputBy == ArtccBoundaryOutputBy.HighLowUnlimited)
			{
				yield return HighLowSummaryFile(ArtccBoundaryOutputFiles.UnlimitedClass);
			}

			yield break;
		}

		OutputFileOption[] files = [.. OutputFiles()];

		if (Area == SubServiceArea.Artccs && files.Length is > 0 and <= MaxSummaryFiles)
		{
			foreach (OutputFileOption file in files)
			{
				yield return new SummaryFile($"{file.Key}.geojson", $"{file.Group}'s boundary labeled \"{file.Label}\"");
			}

			yield break;
		}

		yield return new SummaryFile(
			$"{ArtccBoundaryOutputFiles.KeyFor("<ARTCC>", "<altitude>")}.geojson",
			$"One file for each ARTCC and altitude, e.g. `{ArtccBoundaryOutputFiles.KeyFor("ZOB", "HIGH")}.geojson`");
	}

	private static SummaryFile HighLowSummaryFile(string group) =>
		new($"{ArtccBoundaryOutputFiles.KeyFor(group)}.geojson", $"Boundaries labeled \"{group.ToUpperInvariant()}\"");

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_outputBy = Enum.TryParse(Get("OutputBy"), true, out ArtccBoundaryOutputBy outputBy) ? outputBy : ArtccBoundaryOutputBy.HighLow;
		_splitAtAntimeridian = GetBool("SplitAtAntimeridian", true);
		LoadSharedSettings();

		// Re-apply the saved location filter to any already-built toggles, without a dirty check
		// per toggle; ClearDirty below re-takes the snapshot once.
		_savedLocationFilter = ParseArtccListOrFacility(Get("LocationFilter"));
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
			// The ticked ARTCCs narrow the files only while the area is the ARTCCs.
			HashSet<string> selected = Area == SubServiceArea.Artccs
				? new(SelectedLocationIds(), StringComparer.OrdinalIgnoreCase)
				: new(StringComparer.OrdinalIgnoreCase);

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
		ArtccBoundaryOutputBy.HighLow => "High and Low (UNLIMITED boundaries in both)",
		ArtccBoundaryOutputBy.HighLowUnlimited => "High, Low, and Unlimited",
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
