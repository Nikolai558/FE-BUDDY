using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Fixes;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using DomainFixUses = FeBuddy.Core.Domain.Fixes.FixUses;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Fixes</b> sub-service tab inside the AIRAC Service screen: which outputs to write, how
/// the GeoJSON is laid out (one merged file set, one per fix use, one per chart, or one per
/// chart + fix use combination the user lists), which fix uses/charts/combinations to include, the
/// optional region of interest, which FE-Buddy properties and the CRC ERAM defaults.
/// </summary>
/// <remarks>
/// Save, Undo and navigation come from the tab host's action bar; the run is launched by
/// <b>Run AIRAC Service</b> on the Preview Settings tab, and its results are shown on the Review
/// tab, described by this tab through <see cref="ISubServiceRunTarget"/>. Unlike most other AIRAC
/// sub-services, there is no <c>GenerateGeojson</c> toggle and no alias file: the sub-service
/// always writes GeoJSON.
/// </remarks>
public sealed class FixesViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.Fixes";

	private FixOutputBy _outputBy = FixOutputBy.All;
	private HashSet<string> _savedExcludedFixUses = new(StringComparer.OrdinalIgnoreCase);
	private HashSet<string> _savedExcludedCharts = new(StringComparer.OrdinalIgnoreCase);
	private bool _suppressFixUseChanges;
	private bool _suppressChartChanges;
	private bool _isEditingCombination;
	private int _editingIndex = -1;
	private FixGroupOption? _selectedChartOption;
	private FixGroupOption? _selectedFixUseOption;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public FixesViewModel()
	{
		FebProperties = FebPropertyToggle.ListFor(FixFebPropertyOptions.All, MarkDirty);
		SymbolDefaults = BuildClassDefaults(EramFieldKind.Symbol);
		TextDefaults = BuildClassDefaults(EramFieldKind.Text);

		FixUses.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFixUses));
		Charts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCharts));
		Combinations.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCombinations));

		TickAllFixUsesCommand = new RelayCommand(TickAllFixUses, () => FixUses.Any(t => !t.IsSelected));
		TickAllChartsCommand = new RelayCommand(TickAllCharts, () => Charts.Any(t => !t.IsSelected));

		AddCombinationCommand = new RelayCommand(BeginAddCombination, CanBeginAddCombination);
		ConfirmCombinationCommand = new RelayCommand(ConfirmCombination, CanConfirmCombination);
		CancelCombinationCommand = new RelayCommand(CancelCombinationEdit);
		EditCombinationCommand = new RelayCommand<FixCombinationItem>(BeginEditCombination);
		DeleteCombinationCommand = new RelayCommand<FixCombinationItem>(DeleteCombination);

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Fixes";

	// ================= outputs =================

	/// <inheritdoc />
	protected override int EnabledOutputCount => 1;

	/// <inheritdoc />
	protected override string NoDefaultRoiHint =>
		"No default ROI is set, so the GeoJSON covers every fix. Set one in Settings, or override it here.";

	/// <inheritdoc />
	/// <remarks>Fixes has no Lines file: only Symbols and Text are ever written.</remarks>
	protected override (string? Lines, string? Symbols, string? Text) EmitKeys => (null, "EmitSymbols", "EmitText");

	/// <inheritdoc />
	/// <remarks>Fixes has no alias file.</remarks>
	protected override bool HasAliasFile => false;

	// ================= file layout =================

	/// <summary>Whether every included fix goes into one merged Symbols file and one merged Text file.</summary>
	public bool OutputAll
	{
		get => _outputBy == FixOutputBy.All;
		set { if (value) SetOutputBy(FixOutputBy.All); }
	}

	/// <summary>Whether the GeoJSON is split into a Symbols file and a Text file per fix use.</summary>
	public bool OutputByFixUse
	{
		get => _outputBy == FixOutputBy.FixUse;
		set { if (value) SetOutputBy(FixOutputBy.FixUse); }
	}

	/// <summary>Whether the GeoJSON is split into a Symbols file and a Text file per chart.</summary>
	public bool OutputByChart
	{
		get => _outputBy == FixOutputBy.Chart;
		set { if (value) SetOutputBy(FixOutputBy.Chart); }
	}

	/// <summary>Whether the GeoJSON is split into a Symbols file and a Text file per listed chart + fix use combination.</summary>
	public bool OutputByCombination
	{
		get => _outputBy == FixOutputBy.ChartAndFixUse;
		set { if (value) SetOutputBy(FixOutputBy.ChartAndFixUse); }
	}

	/// <summary>Whether the Fix Uses card shows: the file layout is <see cref="OutputByFixUse"/>.</summary>
	public bool ShowFixUses => _outputBy == FixOutputBy.FixUse;

	/// <summary>Whether the Charts card shows: the file layout is <see cref="OutputByChart"/>.</summary>
	public bool ShowCharts => _outputBy == FixOutputBy.Chart;

	/// <summary>Whether the Combinations card shows: the file layout is <see cref="OutputByCombination"/>.</summary>
	public bool ShowCombinations => _outputBy == FixOutputBy.ChartAndFixUse;

	// ================= fix uses =================

	/// <summary>
	/// One toggle per fix use present in the selected cycle's <c>FIX_BASE</c>. Empty until the
	/// cycle's data is loaded; before then the saved <c>ExcludedFixUses</c> list stands in.
	/// </summary>
	public ObservableCollection<FixGroupToggle> FixUses { get; } = [];

	/// <summary>Whether the fix use list has been built from the selected cycle yet.</summary>
	public bool HasFixUses => FixUses.Count > 0;

	/// <summary>Selects every fix use.</summary>
	public ICommand TickAllFixUsesCommand { get; }

	// ================= charts =================

	/// <summary>
	/// One toggle per chart present in the selected cycle's <c>FIX_BASE</c>, plus a final "No
	/// chart" entry when some fix has none. Empty until the cycle's data is loaded.
	/// </summary>
	public ObservableCollection<FixGroupToggle> Charts { get; } = [];

	/// <summary>Whether the chart list has been built from the selected cycle yet.</summary>
	public bool HasCharts => Charts.Count > 0;

	/// <summary>Selects every chart.</summary>
	public ICommand TickAllChartsCommand { get; }

	// ================= combinations =================

	/// <summary>The chart + fix use combinations to write in the <see cref="OutputByCombination"/> layout, in list order.</summary>
	public ObservableCollection<FixCombinationItem> Combinations { get; } = [];

	/// <summary>Whether any combination has been added.</summary>
	public bool HasCombinations => Combinations.Count > 0;

	/// <summary>The chart choices for the combination editor, in the same order as the Charts card.</summary>
	public IReadOnlyList<FixGroupOption> ChartOptions { get; private set; } = [];

	/// <summary>The fix use choices for the combination editor, in the same order as the Fix Uses card.</summary>
	public IReadOnlyList<FixGroupOption> FixUseOptions { get; private set; } = [];

	/// <summary>Whether the combination editor row is open, either to add or to edit an entry.</summary>
	public bool IsEditingCombination
	{
		get => _isEditingCombination;
		private set
		{
			if (SetProperty(ref _isEditingCombination, value))
			{
				OnPropertyChanged(nameof(ConfirmCombinationText));
			}
		}
	}

	/// <summary>The chart chosen in the open combination editor, or <see langword="null"/> until one is picked.</summary>
	public FixGroupOption? SelectedChartOption
	{
		get => _selectedChartOption;
		set
		{
			if (SetProperty(ref _selectedChartOption, value))
			{
				OnPropertyChanged(nameof(CombinationEditorHint));
			}
		}
	}

	/// <summary>The fix use chosen in the open combination editor, or <see langword="null"/> until one is picked.</summary>
	public FixGroupOption? SelectedFixUseOption
	{
		get => _selectedFixUseOption;
		set
		{
			if (SetProperty(ref _selectedFixUseOption, value))
			{
				OnPropertyChanged(nameof(CombinationEditorHint));
			}
		}
	}

	/// <summary>The confirm button's label: "Add" for a new combination, "Save" while editing an existing one.</summary>
	public string ConfirmCombinationText => _editingIndex >= 0 ? "Save" : "Add";

	/// <summary>"Already in the list." while the chosen pair duplicates another combination; empty otherwise.</summary>
	public string CombinationEditorHint => IsDuplicateSelection() ? "Already in the list." : string.Empty;

	/// <summary>Opens the editor to add a new combination.</summary>
	public ICommand AddCombinationCommand { get; }

	/// <summary>Adds the new combination, or saves the one being edited.</summary>
	public ICommand ConfirmCombinationCommand { get; }

	/// <summary>Closes the editor without adding or saving anything.</summary>
	public ICommand CancelCombinationCommand { get; }

	/// <summary>Opens the editor with an existing combination's values, to change it.</summary>
	public ICommand EditCombinationCommand { get; }

	/// <summary>Removes a combination from the list.</summary>
	public ICommand DeleteCombinationCommand { get; }

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>
	/// Builds the fix use and chart toggle lists (and their combination-editor options) from the
	/// cycle's <c>FIX_BASE</c>, then adds a CRC defaults row for each chart (and any fix use the
	/// cycle has that FE-Buddy does not already know about).
	/// </remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
		_savedExcludedFixUses = ParseList(Get("ExcludedFixUses"));
		_savedExcludedCharts = ParseList(Get("ExcludedCharts"));

		string[] fixUseNames = [.. (data.Fix?.FixBase ?? [])
			.Select(f => DomainFixUses.Name(f.FixUseCode))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(KnownFixUseOrder)
			.ThenBy(name => name, StringComparer.OrdinalIgnoreCase)];

		_suppressFixUseChanges = true;
		try
		{
			FixUses.Clear();
			foreach (string name in fixUseNames)
			{
				string token = DomainFixUses.Token(name);
				FixUses.Add(new FixGroupToggle(token, name, !_savedExcludedFixUses.Contains(token), OnFixUseToggled));
			}
		}
		finally
		{
			_suppressFixUseChanges = false;
		}

		Dictionary<string, string> chartNameByToken = new(StringComparer.OrdinalIgnoreCase);
		bool hasNoChartFix = false;

		foreach (var row in data.Fix?.FixBase ?? [])
		{
			IReadOnlyList<string> charts = FixCharts.Parse(row.Charts);

			if (charts.Count == 0)
			{
				hasNoChartFix = true;
				continue;
			}

			foreach (string chart in charts)
			{
				chartNameByToken.TryAdd(FixCharts.Token(chart), chart);
			}
		}

		string[] chartTokens = [.. chartNameByToken.Keys.OrderBy(token => chartNameByToken[token], StringComparer.OrdinalIgnoreCase)];

		_suppressChartChanges = true;
		try
		{
			Charts.Clear();
			foreach (string token in chartTokens)
			{
				Charts.Add(new FixGroupToggle(token, chartNameByToken[token], !_savedExcludedCharts.Contains(token), OnChartToggled));
			}

			if (hasNoChartFix)
			{
				Charts.Add(new FixGroupToggle(FixCharts.NoChart, "No chart", !_savedExcludedCharts.Contains(FixCharts.NoChart), OnChartToggled));
			}
		}
		finally
		{
			_suppressChartChanges = false;
		}

		ChartOptions = [.. Charts.Select(c => new FixGroupOption(c.Token, c.Label))];
		FixUseOptions = [.. FixUses.Select(t => new FixGroupOption(t.Token, t.Label))];
		OnPropertyChanged(nameof(ChartOptions));
		OnPropertyChanged(nameof(FixUseOptions));

		RelabelCombinations();

		List<EramClassDefault> newRows = [];

		foreach (string token in chartTokens)
		{
			newRows.AddRange(RowsFor(token));
		}

		if (hasNoChartFix)
		{
			newRows.AddRange(RowsFor(FixCharts.NoChart));
		}

		foreach (string name in fixUseNames.Where(name => !DomainFixUses.IsKnown(name)))
		{
			newRows.AddRange(RowsFor(DomainFixUses.Token(name)));
		}

		AddCrcRows(newRows);

		// The per-fix-use and per-chart files exist only now; list them even when the tab has
		// unsaved edits (the resync below then does nothing).
		RefreshVnasFiles();

		// The lists were empty when this tab snapshotted itself at construction; re-take the
		// snapshot now they reflect what is actually saved.
		ResyncSavedState();
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		if (result.Fixes is not { } fixes)
		{
			return null;
		}

		string summary = $"{fixes.FixCount:N0} fix(es)";

		if (HasRoi)
		{
			summary += $", {fixes.GeojsonFixCount:N0} in the region";
		}

		summary += $", {fixes.GeojsonFilesWritten.Count:N0} GeoJSON file(s)";

		return new SubServiceRunResult(Title, summary, fixes.Messages);
	}

	/// <inheritdoc />
	/// <remarks>The block goes on <see cref="AiracServiceSettings.Fixes"/>.</remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputBy"] = _outputBy.ToString(),
			["ExcludedFixUses"] = string.Join(',', ExcludedFixUseTokens()),
			["ExcludedCharts"] = string.Join(',', ExcludedChartTokens()),
			["Combinations"] = string.Join(',', Combinations.Select(c => $"{c.Chart}+{c.FixUse}")),
		};

		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		List<ServicePreviewRow> rows =
		[
			new ServicePreviewRow("GeoJSON files", DescribeGeojsonFiles()),
		];

		switch (_outputBy)
		{
			case FixOutputBy.FixUse:
				rows.Add(new ServicePreviewRow("Fix uses", DescribeFixUses()));
				break;

			case FixOutputBy.Chart:
				rows.Add(new ServicePreviewRow("Charts", DescribeCharts()));
				break;

			case FixOutputBy.ChartAndFixUse:
				rows.Add(new ServicePreviewRow("Combinations", DescribeCombinations()));
				break;
		}

		rows.Add(new ServicePreviewRow("FE-Buddy properties", DescribeFebProperties()));
		rows.Add(new ServicePreviewRow("Region of interest", DescribeRoi()));
		rows.Add(new ServicePreviewRow("Upload to vNAS", DescribeVnasFiles()));
		rows.Add(new ServicePreviewRow("CRC ERAM defaults", DescribeCrcDefaults()));

		return [new ServicePreviewSection("Fixes", rows)];
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_outputBy = Enum.TryParse(Get("OutputBy"), true, out FixOutputBy outputBy) ? outputBy : FixOutputBy.All;

		_savedExcludedFixUses = ParseList(Get("ExcludedFixUses"));
		_savedExcludedCharts = ParseList(Get("ExcludedCharts"));

		// Re-apply the saved exclusions to any already-built toggles, without a dirty check per
		// toggle; ClearDirty below re-takes the snapshot once.
		_suppressFixUseChanges = true;
		try
		{
			foreach (FixGroupToggle toggle in FixUses)
			{
				toggle.IsSelected = !_savedExcludedFixUses.Contains(toggle.Token);
			}
		}
		finally
		{
			_suppressFixUseChanges = false;
		}

		_suppressChartChanges = true;
		try
		{
			foreach (FixGroupToggle toggle in Charts)
			{
				toggle.IsSelected = !_savedExcludedCharts.Contains(toggle.Token);
			}
		}
		finally
		{
			_suppressChartChanges = false;
		}

		// Rebuilds the Combinations list and brings the combination CRC rows in line with it,
		// before LoadSharedSettings below loads the CRC values those rows hold.
		LoadCombinationsFromConfig();

		LoadSharedSettings();

		RaiseOwnSettingProperties();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set("OutputBy", _outputBy.ToString());
		Set("ExcludedFixUses", string.Join(',', ExcludedFixUseTokens()));
		Set("ExcludedCharts", string.Join(',', ExcludedChartTokens()));
		Set("Combinations", string.Join(',', Combinations.Select(c => $"{c.Chart}+{c.FixUse}")));
		SaveSharedSettings();
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		if (!EmitSymbols && !EmitText)
		{
			validation.Add("Neither Symbols nor Text is selected. Turn at least one back on, or deselect Fixes on the General tab.");
		}

		if (_outputBy == FixOutputBy.FixUse && FixUses.Count > 0 && FixUses.All(t => !t.IsSelected))
		{
			validation.Add("No fix uses are ticked. Tick at least one, or choose another file layout.");
		}

		if (_outputBy == FixOutputBy.Chart && Charts.Count > 0 && Charts.All(t => !t.IsSelected))
		{
			validation.Add("No charts are ticked. Tick at least one, or choose another file layout.");
		}

		if (_outputBy == FixOutputBy.ChartAndFixUse && Combinations.Count == 0)
		{
			validation.Add("Add at least one chart + fix use combination, or choose another file layout.");
		}

		ValidateSharedSettings(validation);
	}

	/// <inheritdoc />
	/// <remarks>
	/// All mode writes the merged Symbols/Text files; FixUse and Chart mode each write a
	/// Symbols/Text pair per ticked entry; ChartAndFixUse writes a pair per listed combination, in
	/// list order. Fixes has no alias file.
	/// </remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		switch (_outputBy)
		{
			case FixOutputBy.All:
				if (EmitSymbols)
				{
					yield return new OutputFileOption(FixOutputFiles.Symbols, "Every fix", "Symbols",
						"the Fixes Symbols file", IsGeojson: true, [(FixOutputFiles.AllClass, EramFieldKind.Symbol)]);
				}

				if (EmitText)
				{
					yield return new OutputFileOption(FixOutputFiles.Text, "Every fix", "Text",
						"the Fixes Text file", IsGeojson: true, [(FixOutputFiles.AllClass, EramFieldKind.Text)]);
				}

				break;

			case FixOutputBy.FixUse:
				foreach (string token in IncludedFixUseTokens())
				{
					if (EmitSymbols)
					{
						yield return new OutputFileOption(FixOutputFiles.GroupKey(token, CrcFeatureKind.Symbol), token, "Symbols",
							$"the {token} Symbols file", IsGeojson: true, [(token, EramFieldKind.Symbol)]);
					}

					if (EmitText)
					{
						yield return new OutputFileOption(FixOutputFiles.GroupKey(token, CrcFeatureKind.Text), token, "Text",
							$"the {token} Text file", IsGeojson: true, [(token, EramFieldKind.Text)]);
					}
				}

				break;

			case FixOutputBy.Chart:
				foreach (string token in IncludedChartTokens())
				{
					if (EmitSymbols)
					{
						yield return new OutputFileOption(FixOutputFiles.GroupKey(token, CrcFeatureKind.Symbol), token, "Symbols",
							$"the {token} Symbols file", IsGeojson: true, [(token, EramFieldKind.Symbol)]);
					}

					if (EmitText)
					{
						yield return new OutputFileOption(FixOutputFiles.GroupKey(token, CrcFeatureKind.Text), token, "Text",
							$"the {token} Text file", IsGeojson: true, [(token, EramFieldKind.Text)]);
					}
				}

				break;

			case FixOutputBy.ChartAndFixUse:
				foreach (FixCombinationItem combination in Combinations)
				{
					string group = combination.Group;

					if (EmitSymbols)
					{
						yield return new OutputFileOption(FixOutputFiles.GroupKey(group, CrcFeatureKind.Symbol), group, "Symbols",
							$"the {group} Symbols file", IsGeojson: true, [(group, EramFieldKind.Symbol)]);
					}

					if (EmitText)
					{
						yield return new OutputFileOption(FixOutputFiles.GroupKey(group, CrcFeatureKind.Text), group, "Text",
							$"the {group} Text file", IsGeojson: true, [(group, EramFieldKind.Text)]);
					}
				}

				break;
		}
	}

	// ================= helpers =================

	private ObservableCollection<EramClassDefault> BuildClassDefaults(EramFieldKind kind) =>
	[
		new(FixOutputFiles.AllClass, kind, MarkDirty),
		.. DomainFixUses.All.Select(name => new EramClassDefault(DomainFixUses.Token(name), kind, MarkDirty)),
	];

	private IEnumerable<EramClassDefault> RowsFor(string className)
	{
		yield return new EramClassDefault(className, EramFieldKind.Symbol, MarkDirty);
		yield return new EramClassDefault(className, EramFieldKind.Text, MarkDirty);
	}

	/// <summary>A fix use name's position in <see cref="DomainFixUses.All"/>, or last (alphabetically) when it is not one FE-Buddy recognizes.</summary>
	private static int KnownFixUseOrder(string name)
	{
		for (int i = 0; i < DomainFixUses.All.Count; i++)
		{
			if (DomainFixUses.All[i].Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}

		return int.MaxValue;
	}

	/// <summary>
	/// The fix use tokens the run includes: the ticked toggles, or - before the cycle's list is
	/// built - every known fix use minus the saved exclusions.
	/// </summary>
	private IEnumerable<string> IncludedFixUseTokens() =>
		FixUses.Count > 0
			? FixUses.Where(t => t.IsSelected).Select(t => t.Token)
			: DomainFixUses.All.Select(DomainFixUses.Token).Where(token => !_savedExcludedFixUses.Contains(token));

	/// <summary>The fix use tokens the run excludes, sorted; the mirror image of <see cref="IncludedFixUseTokens"/>.</summary>
	private IEnumerable<string> ExcludedFixUseTokens() =>
		FixUses.Count > 0
			? FixUses.Where(t => !t.IsSelected).Select(t => t.Token)
			: _savedExcludedFixUses.OrderBy(token => token, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// The chart tokens the run includes: the ticked toggles, or - before the cycle's list is
	/// built - none, since the Chart layout has nothing to write until the charts are known.
	/// </summary>
	private IEnumerable<string> IncludedChartTokens() =>
		Charts.Count > 0 ? Charts.Where(t => t.IsSelected).Select(t => t.Token) : [];

	/// <summary>
	/// The chart tokens the run excludes. Before the cycle's list is built there are no toggles to
	/// read, so the saved list stands in - otherwise a save or a run made before the data arrives
	/// would quietly drop the user's exclusions.
	/// </summary>
	private IEnumerable<string> ExcludedChartTokens() =>
		Charts.Count > 0
			? Charts.Where(t => !t.IsSelected).Select(t => t.Token)
			: _savedExcludedCharts.OrderBy(token => token, StringComparer.OrdinalIgnoreCase);

	private void OnFixUseToggled()
	{
		if (_suppressFixUseChanges)
		{
			return;
		}

		MarkDirty();
	}

	private void OnChartToggled()
	{
		if (_suppressChartChanges)
		{
			return;
		}

		MarkDirty();
	}

	private void TickAllFixUses()
	{
		if (FixUses.All(t => t.IsSelected))
		{
			return;
		}

		_suppressFixUseChanges = true;
		try
		{
			foreach (FixGroupToggle toggle in FixUses)
			{
				toggle.IsSelected = true;
			}
		}
		finally
		{
			_suppressFixUseChanges = false;
		}

		MarkDirty();
	}

	private void TickAllCharts()
	{
		if (Charts.All(t => t.IsSelected))
		{
			return;
		}

		_suppressChartChanges = true;
		try
		{
			foreach (FixGroupToggle toggle in Charts)
			{
				toggle.IsSelected = true;
			}
		}
		finally
		{
			_suppressChartChanges = false;
		}

		MarkDirty();
	}

	private void SetOutputBy(FixOutputBy value)
	{
		if (_outputBy == value)
		{
			return;
		}

		_outputBy = value;
		OnPropertyChanged(nameof(OutputAll));
		OnPropertyChanged(nameof(OutputByFixUse));
		OnPropertyChanged(nameof(OutputByChart));
		OnPropertyChanged(nameof(OutputByCombination));
		OnPropertyChanged(nameof(ShowFixUses));
		OnPropertyChanged(nameof(ShowCharts));
		OnPropertyChanged(nameof(ShowCombinations));
		MarkDirty();
	}

	// ---- combinations ----

	private bool CanBeginAddCombination() => IsCycleReady && ChartOptions.Count > 0 && FixUseOptions.Count > 0;

	private void BeginAddCombination()
	{
		_editingIndex = -1;
		SelectedChartOption = null;
		SelectedFixUseOption = null;
		IsEditingCombination = true;
	}

	private void BeginEditCombination(FixCombinationItem? item)
	{
		if (item is null)
		{
			return;
		}

		int index = Combinations.IndexOf(item);

		if (index < 0)
		{
			return;
		}

		_editingIndex = index;
		SelectedChartOption = ChartOptions.FirstOrDefault(o => o.Token.Equals(item.Chart, StringComparison.OrdinalIgnoreCase))
			?? new FixGroupOption(item.Chart, ChartLabel(item.Chart));
		SelectedFixUseOption = FixUseOptions.FirstOrDefault(o => o.Token.Equals(item.FixUse, StringComparison.OrdinalIgnoreCase))
			?? new FixGroupOption(item.FixUse, item.FixUse);
		IsEditingCombination = true;
	}

	private void CancelCombinationEdit() => EndCombinationEdit();

	private void EndCombinationEdit()
	{
		_editingIndex = -1;
		SelectedChartOption = null;
		SelectedFixUseOption = null;
		IsEditingCombination = false;
	}

	private bool CanConfirmCombination() =>
		SelectedChartOption is not null && SelectedFixUseOption is not null && !IsDuplicateSelection();

	private bool IsDuplicateSelection()
	{
		if (SelectedChartOption is not { } chart || SelectedFixUseOption is not { } fixUse)
		{
			return false;
		}

		for (int i = 0; i < Combinations.Count; i++)
		{
			if (i == _editingIndex)
			{
				continue;
			}

			FixCombinationItem existing = Combinations[i];

			if (existing.Chart.Equals(chart.Token, StringComparison.OrdinalIgnoreCase)
				&& existing.FixUse.Equals(fixUse.Token, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private void ConfirmCombination()
	{
		if (SelectedChartOption is not { } chart || SelectedFixUseOption is not { } fixUse)
		{
			return;
		}

		string? oldGroup = _editingIndex >= 0 && _editingIndex < Combinations.Count
			? Combinations[_editingIndex].Group
			: null;

		string group = FixOutputFiles.CombinationGroup(chart.Token, fixUse.Token);
		string label = $"{chart.Label} + {fixUse.Label}";
		string fileNames = CombinationFileNames(group);

		if (_editingIndex >= 0 && _editingIndex < Combinations.Count)
		{
			FixCombinationItem item = Combinations[_editingIndex];
			item.Chart = chart.Token;
			item.FixUse = fixUse.Token;
			item.Label = label;
			item.FileNames = fileNames;
		}
		else
		{
			Combinations.Add(new FixCombinationItem(chart.Token, fixUse.Token, label, fileNames));
		}

		AddCrcRows(RowsFor(group));

		if (oldGroup is not null && !oldGroup.Equals(group, StringComparison.OrdinalIgnoreCase))
		{
			RemoveCombinationCrcRowsIfUnused(oldGroup);
		}

		EndCombinationEdit();
		MarkDirty();
	}

	private void DeleteCombination(FixCombinationItem? item)
	{
		if (item is null)
		{
			return;
		}

		int index = Combinations.IndexOf(item);

		if (index < 0)
		{
			return;
		}

		string group = item.Group;
		Combinations.RemoveAt(index);

		if (IsEditingCombination)
		{
			if (_editingIndex == index)
			{
				EndCombinationEdit();
			}
			else if (_editingIndex > index)
			{
				_editingIndex--;
			}
		}

		RemoveCombinationCrcRowsIfUnused(group);
		OnPropertyChanged(nameof(CombinationEditorHint));
		MarkDirty();
	}

	/// <summary>
	/// Removes a combination group's CRC rows once nothing lists it any more, unless the group is
	/// also a fix-use or chart class - those rows belong to the Fix Uses/Charts pickers and are
	/// never removed.
	/// </summary>
	private void RemoveCombinationCrcRowsIfUnused(string group)
	{
		if (Combinations.Any(c => c.Group.Equals(group, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}

		if (IsFixedClass(group))
		{
			return;
		}

		RemoveCrcRow(SymbolDefaults, group);
		RemoveCrcRow(TextDefaults, group);
	}

	private bool IsFixedClass(string group) =>
		group.Equals(FixOutputFiles.AllClass, StringComparison.OrdinalIgnoreCase)
		|| group.Equals(FixCharts.NoChart, StringComparison.OrdinalIgnoreCase)
		|| DomainFixUses.IsKnown(group)
		|| FixUses.Any(t => t.Token.Equals(group, StringComparison.OrdinalIgnoreCase))
		|| Charts.Any(c => c.Token.Equals(group, StringComparison.OrdinalIgnoreCase));

	private static void RemoveCrcRow(ObservableCollection<EramClassDefault> rows, string className)
	{
		EramClassDefault? row = rows.FirstOrDefault(r => r.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase));

		if (row is not null)
		{
			rows.Remove(row);
		}
	}

	private static string CombinationFileNames(string group) => $"Fix_{group}_Symbols / _Text";

	/// <summary>A chart token's NASR name once the cycle's data is known, otherwise the token itself.</summary>
	private string ChartLabel(string chartToken) =>
		ChartOptions.FirstOrDefault(o => o.Token.Equals(chartToken, StringComparison.OrdinalIgnoreCase))?.Label ?? chartToken;

	/// <summary>Re-labels every saved combination once the cycle's chart names and fix use names are known.</summary>
	private void RelabelCombinations()
	{
		foreach (FixCombinationItem item in Combinations)
		{
			string chartLabel = ChartLabel(item.Chart);
			string fixUseLabel = FixUseOptions.FirstOrDefault(o => o.Token.Equals(item.FixUse, StringComparison.OrdinalIgnoreCase))?.Label ?? item.FixUse;
			item.Label = $"{chartLabel} + {fixUseLabel}";
		}
	}

	/// <summary>Rebuilds <see cref="Combinations"/> from the saved config, and its CRC rows to match.</summary>
	private void LoadCombinationsFromConfig()
	{
		List<(string Chart, string FixUse)> saved = ParseCombinationsFromConfig();
		string[] oldGroups = [.. Combinations.Select(c => c.Group).Distinct(StringComparer.OrdinalIgnoreCase)];
		HashSet<string> newGroups = new(StringComparer.OrdinalIgnoreCase);

		// An open editor points at a list position that is about to be rebuilt.
		EndCombinationEdit();
		Combinations.Clear();

		foreach ((string chartToken, string fixUseToken) in saved)
		{
			string group = FixOutputFiles.CombinationGroup(chartToken, fixUseToken);
			newGroups.Add(group);
			string label = $"{ChartLabel(chartToken)} + {fixUseToken}";
			Combinations.Add(new FixCombinationItem(chartToken, fixUseToken, label, CombinationFileNames(group)));
		}

		AddCrcRows(saved.SelectMany(pair => RowsFor(FixOutputFiles.CombinationGroup(pair.Chart, pair.FixUse))));

		foreach (string group in oldGroups)
		{
			if (!newGroups.Contains(group))
			{
				RemoveCombinationCrcRowsIfUnused(group);
			}
		}

		RelabelCombinations();
	}

	private List<(string Chart, string FixUse)> ParseCombinationsFromConfig()
	{
		List<(string, string)> result = [];
		HashSet<(string, string)> seen = [];

		string[] entries = (Get("Combinations") ?? string.Empty)
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (string entry in entries)
		{
			string[] parts = entry.Split('+');

			if (parts.Length != 2)
			{
				continue;
			}

			// Tokenized the way the parser does, so a hand-edited "ENROUTE LOW+wypnt" still names
			// the same files and CRC class.
			string chart = FixCharts.Token(parts[0]);
			string fixUse = DomainFixUses.Token(parts[1]);

			if (chart.Length == 0 || fixUse.Length == 0)
			{
				continue;
			}

			if (seen.Add((chart, fixUse)))
			{
				result.Add((chart, fixUse));
			}
		}

		return result;
	}

	private string DescribeGeojsonFiles()
	{
		List<string> kinds = [];
		if (EmitSymbols) kinds.Add("Symbols");
		if (EmitText) kinds.Add("Text");

		string files = kinds.Count > 0 ? string.Join(", ", kinds) : "none";

		string layout = _outputBy switch
		{
			FixOutputBy.All => "one file each",
			FixOutputBy.FixUse => "one pair per fix use",
			FixOutputBy.Chart => "one pair per chart",
			FixOutputBy.ChartAndFixUse => "one pair per combination",
			_ => string.Empty,
		};

		return $"{files} - {layout}";
	}

	private string DescribeFixUses()
	{
		string[] included = [.. IncludedFixUseTokens()];

		if (included.Length == 0)
		{
			return "None";
		}

		return ExcludedFixUseTokens().Any() ? string.Join(", ", included) : "Every fix use";
	}

	private string DescribeCharts()
	{
		if (Charts.Count == 0)
		{
			return "Waiting for the cycle's chart list";
		}

		string[] included = [.. IncludedChartTokens()];

		if (included.Length == 0)
		{
			return "None";
		}

		return ExcludedChartTokens().Any() ? string.Join(", ", included) : "Every chart";
	}

	private string DescribeCombinations() =>
		Combinations.Count > 0 ? string.Join(", ", Combinations.Select(c => c.Label)) : "None";

	private void RaiseOwnSettingProperties()
	{
		foreach (string name in new[]
		{
			nameof(OutputAll), nameof(OutputByFixUse), nameof(OutputByChart), nameof(OutputByCombination),
			nameof(ShowFixUses), nameof(ShowCharts), nameof(ShowCombinations),
		})
		{
			OnPropertyChanged(name);
		}
	}
}
