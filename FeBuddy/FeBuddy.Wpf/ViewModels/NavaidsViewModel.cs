using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Domain.Crc;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Navaids;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>NAVAIDs</b> sub-service tab inside the AIRAC Service screen: which outputs to write, how
/// the GeoJSON is laid out (one merged file set or one per NAVAID type), which NAVAID types to
/// include, the symbol style choice, the optional region of interest, which FE-Buddy properties
/// and the CRC ERAM defaults.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Preview Settings tab, and its results are shown on the Review
/// tab, described by this tab through <see cref="ISubServiceRunTarget"/>.
/// </remarks>
public sealed class NavaidsViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.Navaids";
	private const string ShutdownStatus = "SHUTDOWN";

	private bool _generateGeojson = true;
	private NavaidOutputBy _outputBy = NavaidOutputBy.All;
	private NavaidSymbolStyleBy _symbolStyleBy = NavaidSymbolStyleBy.Type;
	private string _fanMarkerStyle = string.Empty;
	private HashSet<string> _savedExcludedTypes = new(StringComparer.OrdinalIgnoreCase);
	private bool _suppressTypeChanges;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public NavaidsViewModel()
	{
		FebProperties = FebPropertyToggle.ListFor(NavaidFebPropertyOptions.All, MarkDirty);
		SymbolDefaults = BuildClassDefaults(EramFieldKind.Symbol);
		TextDefaults = BuildClassDefaults(EramFieldKind.Text);

		Types.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTypes));
		TickAllTypesCommand = new RelayCommand(TickAllTypes, () => Types.Any(t => !t.IsSelected));

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "NAVAIDs";

	// ================= outputs =================

	/// <summary>Whether this run writes GeoJSON for the NAVAIDs.</summary>
	public bool GenerateGeojson
	{
		get => _generateGeojson;
		set
		{
			if (!value && !CanTurnOffOutput())
			{
				// The value never changed, but the control already did - put it back.
				RestoreRejectedToggle(nameof(GenerateGeojson));
				return;
			}

			if (SetProperty(ref _generateGeojson, value))
			{
				MarkDirty();
			}
		}
	}

	/// <inheritdoc />
	protected override int EnabledOutputCount =>
		(GenerateGeojson ? 1 : 0) + (GenerateAliasFile ? 1 : 0);

	/// <inheritdoc />
	protected override string NoDefaultRoiHint =>
		"No default ROI is set, so the GeoJSON covers every NAVAID. Set one in Settings, or override it here.";

	/// <inheritdoc />
	/// <remarks>NAVAIDs has no Lines file: only Symbols and Text are ever written.</remarks>
	protected override (string? Lines, string Symbols, string Text) EmitKeys => (null, "EmitSymbols", "EmitText");

	// ================= file layout =================

	/// <summary>Whether every included NAVAID goes into one merged Symbols file and one merged Text file.</summary>
	public bool OutputAllInOne
	{
		get => _outputBy == NavaidOutputBy.All;
		set { if (value) SetOutputBy(NavaidOutputBy.All); }
	}

	/// <summary>Whether the GeoJSON is split into a Symbols file and a Text file per NAVAID type.</summary>
	public bool OutputByType
	{
		get => _outputBy == NavaidOutputBy.Type;
		set { if (value) SetOutputBy(NavaidOutputBy.Type); }
	}

	// ================= NAVAID types =================

	/// <summary>
	/// One toggle per NAVAID type present in the selected cycle's <c>NAV_BASE</c>. Empty until the
	/// cycle's data is loaded; before then the saved <c>ExcludedTypes</c> list stands in.
	/// </summary>
	public ObservableCollection<NavaidTypeToggle> Types { get; } = [];

	/// <summary>Whether the type list has been built from the selected cycle yet.</summary>
	public bool HasTypes => Types.Count > 0;

	/// <summary>Selects every NAVAID type.</summary>
	public ICommand TickAllTypesCommand { get; }

	// ================= symbol style =================

	/// <summary>Whether every NAVAID's Symbol Feature is styled from its own type. All mode only.</summary>
	public bool StyleByNavaidType
	{
		get => _symbolStyleBy == NavaidSymbolStyleBy.Type;
		set { if (value) SetSymbolStyleBy(NavaidSymbolStyleBy.Type); }
	}

	/// <summary>Whether every NAVAID in the merged Symbols file is styled the same way. All mode only.</summary>
	public bool StyleForWholeFile
	{
		get => _symbolStyleBy == NavaidSymbolStyleBy.File;
		set { if (value) SetSymbolStyleBy(NavaidSymbolStyleBy.File); }
	}

	/// <summary>
	/// The CRC symbol style fan markers get when <see cref="StyleByNavaidType"/> is on, or empty
	/// until chosen. Kept in the option's own spelling, so a value saved in another case still
	/// selects in the drop-down.
	/// </summary>
	public string FanMarkerStyle
	{
		get => _fanMarkerStyle;
		set { if (SetProperty(ref _fanMarkerStyle, CanonicalStyle(value))) MarkDirty(); }
	}

	/// <summary>The choices <see cref="FanMarkerStyle"/> offers.</summary>
	public IReadOnlyList<string> FanMarkerStyleOptions { get; } = CrcPropertyValidator.ValidSymbolStyles;

	/// <summary>
	/// Whether the NAVAID Symbol Style card shows: GeoJSON and Symbols are on, the files are
	/// merged (<see cref="OutputAllInOne"/>), and the merged Symbols file is actually getting
	/// CRC-ERAM defaults - the only place a per-Feature <c>style</c> matters.
	/// </summary>
	public bool ShowSymbolStyleChoice =>
		_outputBy == NavaidOutputBy.All
		&& GenerateGeojson
		&& EmitSymbols
		&& SymbolDefaultsInUse.Any(row => row.ClassName.Equals(NavaidOutputFiles.AllClass, StringComparison.OrdinalIgnoreCase));

	/// <summary>Whether the fan marker style picker shows: styling is by type, and fan markers are included.</summary>
	public bool ShowFanMarkerStyle =>
		ShowSymbolStyleChoice && _symbolStyleBy == NavaidSymbolStyleBy.Type && IsFanMarkerIncluded();

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>Builds the NAVAID type toggle list from the cycle's <c>NAV_BASE</c>.</remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
		_savedExcludedTypes = ParseList(Get("ExcludedTypes"));

		string[] types = [.. (data.Nav?.NavBase ?? [])
			.Where(n => !(n.NavStatus?.Trim() ?? string.Empty).Equals(ShutdownStatus, StringComparison.OrdinalIgnoreCase))
			.Select(n => (n.NavType ?? string.Empty).Trim().ToUpperInvariant())
			.Where(t => t.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(KnownTypeOrder)
			.ThenBy(t => t, StringComparer.OrdinalIgnoreCase)];

		_suppressTypeChanges = true;
		try
		{
			Types.Clear();
			foreach (string type in types)
			{
				Types.Add(new NavaidTypeToggle(type, !_savedExcludedTypes.Contains(type), OnTypeToggled));
			}
		}
		finally
		{
			_suppressTypeChanges = false;
		}

		// The list was empty when this tab snapshotted itself at construction; re-take the
		// snapshot now the toggles reflect what is actually saved.
		ResyncSavedState();
		RaiseVisibilityFlags();
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		if (result.Navaids is not { } navaids)
		{
			return null;
		}

		string summary = $"{navaids.NavaidCount:N0} NAVAID(s)";

		if (HasRoi)
		{
			summary += $", {navaids.GeojsonNavaidCount:N0} in the region";
		}

		summary += $", {navaids.GeojsonFilesWritten.Count:N0} GeoJSON file(s)";

		if (navaids.AliasFilePath is not null)
		{
			summary += $", NAVAIDs.txt: {navaids.AliasCommandCount:N0} alias command(s)";
		}

		return new SubServiceRunResult(Title, summary, navaids.Messages);
	}

	/// <inheritdoc />
	/// <remarks>The block goes on <see cref="AiracServiceSettings.Navaids"/>.</remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["GenerateGeojson"] = YesNo(GenerateGeojson),
			["OutputBy"] = _outputBy.ToString(),
			["ExcludedTypes"] = string.Join(',', ExcludedTypeNames()),
		};

		// SymbolStyleBy only means anything for the merged All-mode Symbols file; FanMarkerStyle
		// only when a per-Feature style is actually required (see ShowFanMarkerStyle).
		if (_outputBy == NavaidOutputBy.All)
		{
			s["SymbolStyleBy"] = _symbolStyleBy.ToString();
		}

		if (ShowFanMarkerStyle)
		{
			s["FanMarkerStyle"] = FanMarkerStyle;
		}

		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		List<string> outputs = [];
		if (GenerateGeojson) outputs.Add("GeoJSON");
		if (GenerateAliasFile) outputs.Add("Alias file (NAVAIDs.txt)");

		List<ServicePreviewRow> rows =
		[
			new ServicePreviewRow("Outputs", string.Join(", ", outputs)),
			new ServicePreviewRow("GeoJSON files", DescribeGeojsonFiles()),
			new ServicePreviewRow("Types", DescribeTypes()),
			new ServicePreviewRow("FE-Buddy properties", DescribeFebProperties()),
			new ServicePreviewRow("Region of interest", HasRoi ? $"{DescribeRoi()}; GeoJSON only - the alias file covers every NAVAID" : DescribeRoi()),
		];

		if (ShowSymbolStyleChoice)
		{
			rows.Add(new ServicePreviewRow("Symbol style", DescribeSymbolStyle()));
		}

		rows.Add(new ServicePreviewRow("Upload to vNAS", DescribeVnasFiles()));
		rows.Add(new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()));

		return [new ServicePreviewSection("NAVAIDs", rows)];
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_generateGeojson = GetBool("GenerateGeojson", true);
		_outputBy = Enum.TryParse(Get("OutputBy"), true, out NavaidOutputBy outputBy) ? outputBy : NavaidOutputBy.All;
		_symbolStyleBy = Enum.TryParse(Get("SymbolStyleBy"), true, out NavaidSymbolStyleBy styleBy) ? styleBy : NavaidSymbolStyleBy.Type;
		_fanMarkerStyle = CanonicalStyle(Get("FanMarkerStyle"));

		LoadSharedSettings();

		// Both outputs off would leave the tab in a state its own guard forbids; a hand-edited
		// config is the only way to get here, so fall back to the default rather than honour it.
		if (!_generateGeojson && !GenerateAliasFile)
		{
			_generateGeojson = true;
			GenerateAliasFile = true;
		}

		// Re-apply the saved exclusion list to any already-built toggles, without a dirty check
		// per toggle; ClearDirty below re-takes the snapshot once.
		_savedExcludedTypes = ParseList(Get("ExcludedTypes"));
		_suppressTypeChanges = true;
		try
		{
			foreach (NavaidTypeToggle toggle in Types)
			{
				toggle.IsSelected = !_savedExcludedTypes.Contains(toggle.Type);
			}
		}
		finally
		{
			_suppressTypeChanges = false;
		}

		UpdateSymbolStyleFromFeatures();
		RaiseOwnSettingProperties();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("GenerateGeojson", YesNo(GenerateGeojson));
		Set("OutputBy", _outputBy.ToString());
		Set("ExcludedTypes", string.Join(',', ExcludedTypeNames()));
		Set("SymbolStyleBy", _symbolStyleBy.ToString());
		Set("FanMarkerStyle", FanMarkerStyle);
		SaveSharedSettings();
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		if (GenerateGeojson && !EmitSymbols && !EmitText)
		{
			validation.Add(
				"GeoJSON is on but neither Symbols nor Text is selected. Turn at least one back on, "
				+ "or switch GeoJSON off.");
		}

		if (Types.Count > 0 && Types.All(t => !t.IsSelected))
		{
			validation.Add("No NAVAID types are ticked. Tick at least one, or deselect NAVAIDs on the General tab.");
		}

		if (ShowFanMarkerStyle && string.IsNullOrWhiteSpace(FanMarkerStyle))
		{
			validation.AddField(nameof(FanMarkerStyle), "Pick the symbol style fan markers get.");
		}

		ValidateSharedSettings(validation);
	}

	/// <inheritdoc />
	/// <remarks>
	/// All mode writes the merged Symbols/Text files; Type mode writes a Symbols/Text pair per
	/// ticked type. Either way, the alias file (if any) comes last.
	/// </remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		if (GenerateGeojson)
		{
			if (_outputBy == NavaidOutputBy.All)
			{
				if (EmitSymbols)
				{
					yield return new OutputFileOption(NavaidOutputFiles.Symbols, "Every NAVAID", "Symbols",
						"the NAVAIDs Symbols file", IsGeojson: true, [(NavaidOutputFiles.AllClass, EramFieldKind.Symbol)]);
				}

				if (EmitText)
				{
					yield return new OutputFileOption(NavaidOutputFiles.Text, "Every NAVAID", "Text",
						"the NAVAIDs Text file", IsGeojson: true, [(NavaidOutputFiles.AllClass, EramFieldKind.Text)]);
				}
			}
			else
			{
				foreach (string type in IncludedTypes())
				{
					string group = NavaidOutputFiles.TypeGroup(type);

					if (EmitSymbols)
					{
						yield return new OutputFileOption(
							NavaidOutputFiles.TypeKey(type, CrcFeatureKind.Symbol), group, "Symbols",
							$"the {group} Symbols file", IsGeojson: true, [(NavaidTypes.Token(type), EramFieldKind.Symbol)]);
					}

					if (EmitText)
					{
						yield return new OutputFileOption(
							NavaidOutputFiles.TypeKey(type, CrcFeatureKind.Text), group, "Text",
							$"the {group} Text file", IsGeojson: true, [(NavaidTypes.Token(type), EramFieldKind.Text)]);
					}
				}
			}
		}

		if (GenerateAliasFile)
		{
			yield return OutputFileOption.AliasFile(NavaidOutputFiles.Alias);
		}
	}

	/// <inheritdoc />
	/// <remarks>Refreshes the shared cards first (base), then this tab's own visibility flags.</remarks>
	protected override void MarkDirty()
	{
		base.MarkDirty();
		RaiseVisibilityFlags();
	}

	/// <inheritdoc />
	/// <remarks>Refreshes the shared cards first (base), then this tab's own visibility flags.</remarks>
	protected override void ClearDirty()
	{
		base.ClearDirty();
		RaiseVisibilityFlags();
	}

	// ================= helpers =================

	private ObservableCollection<EramClassDefault> BuildClassDefaults(EramFieldKind kind) =>
	[
		new(NavaidOutputFiles.AllClass, kind, MarkDirty),
		.. NavaidTypes.All.Select(type => new EramClassDefault(NavaidTypes.Token(type), kind, MarkDirty)),
	];

	/// <summary>A type's position in <see cref="NavaidTypes.All"/>, or last (alphabetically) when it is not one FE-Buddy recognizes.</summary>
	private static int KnownTypeOrder(string type)
	{
		for (int i = 0; i < NavaidTypes.All.Count; i++)
		{
			if (NavaidTypes.All[i].Equals(type, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}

		return int.MaxValue;
	}

	/// <summary>
	/// The types the run includes: the ticked toggles, or - before the cycle's list is built -
	/// every known type minus the saved exclusions.
	/// </summary>
	private IEnumerable<string> IncludedTypes() =>
		Types.Count > 0
			? Types.Where(t => t.IsSelected).Select(t => t.Type)
			: NavaidTypes.All.Where(t => !_savedExcludedTypes.Contains(t));

	/// <summary>The types the run excludes, sorted; the mirror image of <see cref="IncludedTypes"/>.</summary>
	private IEnumerable<string> ExcludedTypeNames() =>
		Types.Count > 0
			? Types.Where(t => !t.IsSelected).Select(t => t.Type)
			: _savedExcludedTypes.OrderBy(t => t, StringComparer.OrdinalIgnoreCase);

	/// <summary>The matching symbol style in its canonical spelling; anything else is kept as it is, for validation to report.</summary>
	private static string CanonicalStyle(string? value) =>
		CrcPropertyValidator.ValidSymbolStyles.FirstOrDefault(style => style.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase))
			?? value
			?? string.Empty;

	private bool IsFanMarkerIncluded() =>
		Types.Count > 0
			? Types.Any(t => t.Type.Equals(NavaidTypes.FanMarker, StringComparison.OrdinalIgnoreCase) && t.IsSelected)
			: !_savedExcludedTypes.Contains(NavaidTypes.FanMarker);

	private void SetOutputBy(NavaidOutputBy value)
	{
		if (_outputBy == value)
		{
			return;
		}

		_outputBy = value;
		OnPropertyChanged(nameof(OutputAllInOne));
		OnPropertyChanged(nameof(OutputByType));
		MarkDirty();
	}

	private void SetSymbolStyleBy(NavaidSymbolStyleBy value)
	{
		if (_symbolStyleBy == value)
		{
			return;
		}

		_symbolStyleBy = value;
		UpdateSymbolStyleFromFeatures();
		OnPropertyChanged(nameof(StyleByNavaidType));
		OnPropertyChanged(nameof(StyleForWholeFile));
		MarkDirty();
	}

	/// <summary>
	/// Keeps the merged Symbols file's CRC row in step with <see cref="StyleByNavaidType"/>: while
	/// every NAVAID carries its own style, the file's own defaults take none.
	/// </summary>
	private void UpdateSymbolStyleFromFeatures() =>
		SymbolDefaults
			.First(row => row.ClassName.Equals(NavaidOutputFiles.AllClass, StringComparison.OrdinalIgnoreCase))
			.StyleFromFeatures = _symbolStyleBy == NavaidSymbolStyleBy.Type;

	private void OnTypeToggled()
	{
		if (_suppressTypeChanges)
		{
			return;
		}

		MarkDirty();
	}

	private void TickAllTypes()
	{
		if (Types.All(t => t.IsSelected))
		{
			return;
		}

		// One dirty check for the whole batch rather than one per toggle.
		_suppressTypeChanges = true;
		try
		{
			foreach (NavaidTypeToggle toggle in Types)
			{
				toggle.IsSelected = true;
			}
		}
		finally
		{
			_suppressTypeChanges = false;
		}

		MarkDirty();
	}

	private void RaiseVisibilityFlags()
	{
		OnPropertyChanged(nameof(ShowSymbolStyleChoice));
		OnPropertyChanged(nameof(ShowFanMarkerStyle));
	}

	private void RaiseOwnSettingProperties()
	{
		foreach (string name in new[]
		{
			nameof(GenerateGeojson), nameof(OutputAllInOne), nameof(OutputByType),
			nameof(StyleByNavaidType), nameof(StyleForWholeFile), nameof(FanMarkerStyle),
		})
		{
			OnPropertyChanged(name);
		}
	}

	/// <summary>The "GeoJSON files" preview row: what is written and how it is grouped.</summary>
	private string DescribeGeojsonFiles()
	{
		if (!GenerateGeojson)
		{
			return "No";
		}

		List<string> kinds = [];
		if (EmitSymbols) kinds.Add("Symbols");
		if (EmitText) kinds.Add("Text");

		string files = kinds.Count > 0 ? string.Join(", ", kinds) : "none";

		return _outputBy == NavaidOutputBy.All
			? $"{files} - one file each"
			: $"{files} - one pair per NAVAID type";
	}

	/// <summary>The "Types" preview row.</summary>
	private string DescribeTypes()
	{
		string[] included = [.. IncludedTypes()];

		if (included.Length == 0)
		{
			return "None";
		}

		return ExcludedTypeNames().Any() ? string.Join(", ", included) : "Every type";
	}

	/// <summary>The "Symbol style" preview row, shown only while <see cref="ShowSymbolStyleChoice"/> is true.</summary>
	private string DescribeSymbolStyle() =>
		StyleByNavaidType
			? $"By NAVAID type (fan markers: {(string.IsNullOrWhiteSpace(FanMarkerStyle) ? "not set" : FanMarkerStyle)})"
			: "One style for the whole file";
}
