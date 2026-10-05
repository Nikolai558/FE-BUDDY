using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Domain.Telephony;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.SharedData.Models;
using FeBuddy.Core.Infrastructure.Telephony;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Telephony</b> sub-service tab inside the AIRAC Service screen: the <c>Telephony.txt</c>
/// alias file, the user's virtual airlines merged into it, and whether it goes to vNAS.
/// </summary>
/// <remarks>
/// <para>
/// The alias file is Telephony's only output, and it covers every operator in the FAA telephony
/// pages; the only thing to add is the Virtual Airlines card's list, each one written as a card of
/// its own marked <c>--VA--</c>, after the FAA's operators. The tab still derives from
/// <see cref="GeojsonSubServiceViewModel"/> for the alias file and the Upload to vNAS card, with
/// <see cref="EmitKeys"/> <c>(null, null, null)</c> and one output that cannot be turned off.
/// </para>
/// <para>
/// Like Wx Stations, its data does not come from the selected cycle at all: every run downloads the
/// latest FAA telephony pages into one kept copy, whichever cycle is run - see
/// <see cref="RefreshTelephonyData"/>. Save, Undo and navigation come from the tab host's action
/// bar; the run is launched by <b>Run AIRAC Service</b> on the Preview Settings tab, and its
/// results are shown on the Review tab, described by this tab through
/// <see cref="ISubServiceRunTarget"/>.
/// </para>
/// <para>
/// The card can also include the VATSIM-Radar Virtual Airline List (<see cref="IncludeVatsimRadarList"/>),
/// which every run then downloads too. The tab reads FE-Buddy's kept copy of it - downloading one
/// when the box is ticked and there is none - so the editor can turn away a virtual airline that
/// is already on it, exactly; one that differs at all is the user's to add, with a card of its own.
/// </para>
/// </remarks>
public sealed class TelephonyViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.Telephony";

	/// <summary>The numbered list's node under <see cref="Node"/>: <c>VirtualAirlines.1.Designator</c>, as the settings block names it.</summary>
	private const string VirtualAirlinesKey = "VirtualAirlines";

	private string _telephonyDataStatus = string.Empty;
	private bool _isEditingVirtualAirline;
	private int _editingIndex = -1;
	private string _editDesignator = string.Empty;
	private string _editTelephony = string.Empty;
	private string _editOrganization = string.Empty;
	private bool _includeVatsimRadarList;
	private IReadOnlyList<VirtualAirline> _vatsimRadarList = [];
	private DateTime? _vatsimRadarDownloadedUtc;
	private string? _vatsimRadarFailure;
	private bool _isDownloadingVatsimRadarList;

	/// <summary>Builds the tab, restores its saved settings and reads FE-Buddy's kept copy of the VATSIM-Radar list.</summary>
	public TelephonyViewModel()
	{
		VirtualAirlines.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasVirtualAirlines));

		AddVirtualAirlineCommand = new RelayCommand(BeginAddVirtualAirline);
		ConfirmVirtualAirlineCommand = new RelayCommand(ConfirmVirtualAirline, CanConfirmVirtualAirline);
		CancelVirtualAirlineCommand = new RelayCommand(EndVirtualAirlineEdit);
		EditVirtualAirlineCommand = new RelayCommand<VirtualAirlineItem>(BeginEditVirtualAirline);
		DeleteVirtualAirlineCommand = new RelayCommand<VirtualAirlineItem>(DeleteVirtualAirline);
		DownloadVatsimRadarListCommand = new RelayCommand(() => _ = DownloadVatsimRadarListAsync(), () => !IsDownloadingVatsimRadarList);

		LoadFromConfig();
		RefreshTelephonyData();
		UseVatsimRadarCopy(VatsimRadarVirtualAirlines.ReadKeptCopy(TelephonyFiles.VatsimRadarAirlinesFilePath));
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Telephony";

	// ================= outputs =================

	/// <inheritdoc />
	protected override string NoRoiEffect => "nothing changes: Telephony is not limited to a region";

	/// <inheritdoc />
	/// <remarks>Telephony writes no GeoJSON at all.</remarks>
	protected override (string? Lines, string? Symbols, string? Text) EmitKeys => (null, null, null);

	// ================= telephony data =================

	/// <summary>What the Telephony Data card and the Preview Settings tab say about FE-Buddy's kept copies of the FAA pages.</summary>
	public string TelephonyDataStatus => _telephonyDataStatus;

	/// <summary>
	/// Re-reads how old FE-Buddy's kept copy of the FAA telephony register is
	/// (<see cref="TelephonyFiles.RegisterFilePath"/>). Called when the tab is built and by the AIRAC
	/// Service screen after every run, which is when the copies are replaced.
	/// </summary>
	/// <remarks>
	/// Never blocks the run: a missing copy is what the run downloads, and a failed download is
	/// reported by the run itself.
	/// </remarks>
	public void RefreshTelephonyData()
	{
		_telephonyDataStatus = File.Exists(TelephonyFiles.RegisterFilePath)
			? $"FE-Buddy's copy is from {File.GetLastWriteTime(TelephonyFiles.RegisterFilePath):d MMM yyyy}. Every run downloads the latest pages first, and uses this copy only if it can't."
			: "FE-Buddy has no copy yet. Every run downloads the latest pages first, so the first run needs an internet connection.";

		OnPropertyChanged(nameof(TelephonyDataStatus));
	}

	// ================= virtual airlines =================

	/// <summary>
	/// Whether the VATSIM-Radar Virtual Airline List's virtual airlines are written too, after the
	/// user's own. Ticking it with no kept copy of the list downloads one, so a new virtual airline
	/// can be checked against it.
	/// </summary>
	public bool IncludeVatsimRadarList
	{
		get => _includeVatsimRadarList;
		set
		{
			if (!SetProperty(ref _includeVatsimRadarList, value))
			{
				return;
			}

			MarkDirty();
			RefreshVatsimRadarMatches();

			if (value && _vatsimRadarDownloadedUtc is null && !IsDownloadingVatsimRadarList)
			{
				_ = DownloadVatsimRadarListAsync();
			}
		}
	}

	/// <summary>Whether the VATSIM-Radar list is being downloaded right now.</summary>
	public bool IsDownloadingVatsimRadarList
	{
		get => _isDownloadingVatsimRadarList;
		private set
		{
			if (SetProperty(ref _isDownloadingVatsimRadarList, value))
			{
				OnPropertyChanged(nameof(VatsimRadarListStatus));
			}
		}
	}

	/// <summary>
	/// What the card says about FE-Buddy's copy of the VATSIM-Radar list: how old it is and how many
	/// virtual airlines a run would write from it, that there is none yet, or that it is downloading.
	/// </summary>
	public string VatsimRadarListStatus
	{
		get
		{
			if (IsDownloadingVatsimRadarList)
			{
				return "Downloading the latest list…";
			}

			string copy = _vatsimRadarDownloadedUtc is { } downloadedUtc
				? $"FE-Buddy's copy is from {downloadedUtc.ToLocalTime():d MMM yyyy}: {_vatsimRadarList.Count:N0} virtual airlines."
				: "FE-Buddy has no copy yet, so a virtual airline you add can't be checked against it.";
			string failed = _vatsimRadarFailure is { } reason ? $" The latest couldn't be downloaded ({reason})." : string.Empty;

			return $"{copy}{failed} Every run downloads the latest list first.";
		}
	}

	/// <summary>Downloads the latest VATSIM-Radar list now, to check new virtual airlines against.</summary>
	public ICommand DownloadVatsimRadarListCommand { get; }

	/// <summary>
	/// Downloads the VATSIM-Radar list, unit tests only: stands in for
	/// <see cref="TelephonyDownloader.RefreshVatsimRadarAirlinesAsync"/> so they never touch the network.
	/// </summary>
	internal Func<CancellationToken, Task<SharedDataRefreshResult>> RefreshVatsimRadarList { get; set; } =
		TelephonyDownloader.RefreshVatsimRadarAirlinesAsync;

	/// <summary>The user's virtual airlines, in list order - the order their cards are written in.</summary>
	public ObservableCollection<VirtualAirlineItem> VirtualAirlines { get; } = [];

	/// <summary>Whether any virtual airline has been added.</summary>
	public bool HasVirtualAirlines => VirtualAirlines.Count > 0;

	/// <summary>Whether the editor row is open, either to add a virtual airline or to edit one.</summary>
	public bool IsEditingVirtualAirline
	{
		get => _isEditingVirtualAirline;
		private set
		{
			if (SetProperty(ref _isEditingVirtualAirline, value))
			{
				OnPropertyChanged(nameof(ConfirmVirtualAirlineText));
			}
		}
	}

	/// <summary>The three-letter designator typed in the open editor.</summary>
	public string EditDesignator
	{
		get => _editDesignator;
		set
		{
			if (SetProperty(ref _editDesignator, value))
			{
				RaiseEditorFeedback();
			}
		}
	}

	/// <summary>The telephony typed in the open editor.</summary>
	public string EditTelephony
	{
		get => _editTelephony;
		set
		{
			if (SetProperty(ref _editTelephony, value))
			{
				RaiseEditorFeedback();
			}
		}
	}

	/// <summary>The virtual organization typed in the open editor.</summary>
	public string EditOrganization
	{
		get => _editOrganization;
		set
		{
			if (SetProperty(ref _editOrganization, value))
			{
				RaiseEditorFeedback();
			}
		}
	}

	/// <summary>The confirm button's label: "Add" for a new virtual airline, "Save" while editing one.</summary>
	public string ConfirmVirtualAirlineText => _editingIndex >= 0 ? "Save" : "Add";

	/// <summary>
	/// What is wrong with what the editor holds, once there is something to say: a 3LD that can't
	/// become three letters (a digit or symbol in it, or more than three), a telephony with no letter
	/// or digit, a virtual airline already in the list, or - with the VATSIM-Radar list included -
	/// one already on that list, exactly. Empty while the editor is fine, or still being filled in -
	/// <c>DV</c> is on its way to a 3LD, so it says nothing yet.
	/// </summary>
	public string VirtualAirlineEditorHint
	{
		get
		{
			string designator = EditDesignator.Trim();

			if (designator.Length > 3 || !designator.All(char.IsAsciiLetter))
			{
				return "The 3LD is three letters, A to Z.";
			}

			if (EditTelephony.Trim().Length > 0 && TelephonyNaming.CommandName(EditTelephony) is null)
			{
				return "The telephony needs a letter or a digit.";
			}

			if (IsDuplicateVirtualAirline())
			{
				return "Already in the list.";
			}

			return FindOnVatsimRadarList(EditDesignator, EditTelephony, EditOrganization) is { } listed
				? $"Already on the {VatsimRadarVirtualAirlines.ListName}, which you've chosen to include, as {Describe(listed)}. " +
					"It's written from there, so it can't be added again."
				: string.Empty;
		}
	}

	/// <summary>
	/// A heads-up, not a problem: with the VATSIM-Radar list included, it has a virtual airline with
	/// the same 3LD and telephony as the editor's but another virtual organization - so both are
	/// written, each with a card of its own. Empty otherwise, and while <see cref="VirtualAirlineEditorHint"/> has something to say.
	/// </summary>
	public string VirtualAirlineEditorNote
	{
		get
		{
			string designator = EditDesignator.Trim();
			string telephony = EditTelephony.Trim();

			if (!IncludeVatsimRadarList || VirtualAirlineEditorHint.Length > 0 || designator.Length != 3 || telephony.Length == 0)
			{
				return string.Empty;
			}

			string[] organizations = [.. _vatsimRadarList
				.Where(listed => listed.Designator.Equals(designator, StringComparison.OrdinalIgnoreCase)
					&& listed.Telephony.Equals(telephony, StringComparison.OrdinalIgnoreCase))
				.Select(listed => listed.Organization)];

			return organizations.Length == 0
				? string.Empty
				: $"The {VatsimRadarVirtualAirlines.ListName} also has {designator.ToUpperInvariant()} · {telephony.ToUpperInvariant()}, " +
					$"for {string.Join(" and ", organizations)}. Yours is written too, with a card of its own.";
		}
	}

	/// <summary>Opens the editor to add a virtual airline.</summary>
	public ICommand AddVirtualAirlineCommand { get; }

	/// <summary>Adds the new virtual airline, or saves the one being edited.</summary>
	public ICommand ConfirmVirtualAirlineCommand { get; }

	/// <summary>Closes the editor without adding or saving anything.</summary>
	public ICommand CancelVirtualAirlineCommand { get; }

	/// <summary>Opens the editor with a virtual airline's values, to change them.</summary>
	public ICommand EditVirtualAirlineCommand { get; }

	/// <summary>Removes a virtual airline from the list.</summary>
	public ICommand DeleteVirtualAirlineCommand { get; }

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>Does nothing: Telephony has no lists built from the selected cycle's NASR data.</remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		if (result.Telephony is not { } telephony)
		{
			return null;
		}

		string fromVatsimRadar = telephony.VatsimRadarVirtualAirlineCount > 0
			? $" ({telephony.VatsimRadarVirtualAirlineCount:N0} from the VATSIM-Radar list)"
			: string.Empty;

		string operators = telephony.VirtualAirlineCount > 0
			? $"{telephony.IcaoAssignmentCount:N0} ICAO operator(s), {telephony.SpecialCallSignCount:N0} U.S. special call sign(s) " +
				$"and {telephony.VirtualAirlineCount:N0} virtual airline(s){fromVatsimRadar}"
			: $"{telephony.IcaoAssignmentCount:N0} ICAO operator(s) and {telephony.SpecialCallSignCount:N0} U.S. special call sign(s)";

		string summary = telephony.AliasFilePath is not null
			? $"{TelephonyOutputFiles.Alias}: {telephony.AliasCommandCount:N0} command(s) for {operators}"
			: "Nothing written";

		return new SubServiceRunResult(Title, summary, telephony.Messages);
	}

	/// <inheritdoc />
	/// <remarks>The block goes on <see cref="AiracServiceSettings.Telephony"/>.</remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase);
		AddSharedSettings(s);

		foreach ((string key, string value) in VirtualAirlineKeys())
		{
			s[key] = value;
		}

		s[TelephonySettingsParser.IncludeVatsimRadarKey] = YesNo(IncludeVatsimRadarList);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("Alias file", $"{TelephonyOutputFiles.Alias}, every operator in the FAA telephony pages"),
			new ServicePreviewRow("Virtual airlines", HasVirtualAirlines
				? string.Join(", ", VirtualAirlines.Select(va => $"{va.Designator} ({va.Telephony}, {va.Organization})"))
				: "None"),
			new ServicePreviewRow("VATSIM-Radar list", IncludeVatsimRadarList ? $"Included. {VatsimRadarListStatus}" : "Not included"),
			new ServicePreviewRow("Telephony data", TelephonyDataStatus),
			new ServicePreviewRow("Upload to vNAS", DescribeVnasFiles()),
		];

		return [new ServicePreviewSection("Telephony", rows)];
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		LoadSharedSettings();

		// The field, not the property: restoring a saved choice is no reason to download the list.
		_includeVatsimRadarList = GetBool(TelephonySettingsParser.IncludeVatsimRadarKey, defaultValue: false);
		OnPropertyChanged(nameof(IncludeVatsimRadarList));

		LoadVirtualAirlines();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		SaveSharedSettings();
		Set(TelephonySettingsParser.IncludeVatsimRadarKey, YesNo(IncludeVatsimRadarList));

		// The list is written whole: a removed virtual airline must not leave its numbered keys behind.
		RemoveSubtree(VirtualAirlinesKey);

		foreach ((string key, string value) in VirtualAirlineKeys())
		{
			Set(key, value);
		}
	}

	/// <inheritdoc />
	/// <remarks>
	/// The editor only adds a virtual airline that can be written, so a problem here comes from a
	/// hand-edited or imported config - reported by number, as the run would.
	/// </remarks>
	protected override void Validate(ServiceValidation validation)
	{
		ValidateSharedSettings(validation);

		for (int i = 0; i < VirtualAirlines.Count; i++)
		{
			VirtualAirlineItem item = VirtualAirlines[i];

			if (TelephonySettingsParser.VirtualAirlineProblem(item.Designator, item.Telephony, item.Organization) is { } problem)
			{
				validation.Add($"Virtual airline {i + 1} ({item.Designator}): {problem}. Edit or delete it.");
			}
		}
	}

	/// <inheritdoc />
	/// <remarks>Only the alias file; with no GeoJSON there is nothing to carry CRC-ERAM defaults.</remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		yield return OutputFileOption.AliasFile(TelephonyOutputFiles.Alias);
	}

	// ================= virtual airlines: editing =================

	private void BeginAddVirtualAirline()
	{
		_editingIndex = -1;
		SetEditor(string.Empty, string.Empty, string.Empty);
		IsEditingVirtualAirline = true;
	}

	private void BeginEditVirtualAirline(VirtualAirlineItem? item)
	{
		int index = item is null ? -1 : VirtualAirlines.IndexOf(item);

		if (index < 0)
		{
			return;
		}

		_editingIndex = index;
		SetEditor(item!.Designator, item.Telephony, item.Organization);
		IsEditingVirtualAirline = true;
	}

	private void EndVirtualAirlineEdit()
	{
		_editingIndex = -1;
		SetEditor(string.Empty, string.Empty, string.Empty);
		IsEditingVirtualAirline = false;
	}

	/// <summary>Fills the editor, and refreshes what depends on which virtual airline it is editing.</summary>
	private void SetEditor(string designator, string telephony, string organization)
	{
		EditDesignator = designator;
		EditTelephony = telephony;
		EditOrganization = organization;
		OnPropertyChanged(nameof(ConfirmVirtualAirlineText));
		RaiseEditorFeedback();
	}

	/// <summary>What the editor holds can be written, and is neither in the list already nor - when it is included - on the VATSIM-Radar list.</summary>
	private bool CanConfirmVirtualAirline() =>
		TelephonySettingsParser.VirtualAirlineProblem(EditDesignator, EditTelephony, EditOrganization) is null
		&& !IsDuplicateVirtualAirline()
		&& FindOnVatsimRadarList(EditDesignator, EditTelephony, EditOrganization) is null;

	/// <summary>Raises what the editor says about what it holds: the problem, and the heads-up.</summary>
	private void RaiseEditorFeedback()
	{
		OnPropertyChanged(nameof(VirtualAirlineEditorHint));
		OnPropertyChanged(nameof(VirtualAirlineEditorNote));
	}

	/// <summary>Whether the editor holds a virtual airline the list already has (other than the one being edited), ignoring case.</summary>
	private bool IsDuplicateVirtualAirline()
	{
		for (int i = 0; i < VirtualAirlines.Count; i++)
		{
			VirtualAirlineItem item = VirtualAirlines[i];

			if (i != _editingIndex
				&& item.Designator.Equals(EditDesignator.Trim(), StringComparison.OrdinalIgnoreCase)
				&& item.Telephony.Equals(EditTelephony.Trim(), StringComparison.OrdinalIgnoreCase)
				&& item.Organization.Equals(EditOrganization.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Adds or saves what the editor holds, trimmed: the 3LD and telephony upper case, as the card
	/// prints them, and the virtual organization as typed.
	/// </summary>
	private void ConfirmVirtualAirline()
	{
		if (!CanConfirmVirtualAirline())
		{
			return;
		}

		string designator = EditDesignator.Trim().ToUpperInvariant();
		string telephony = EditTelephony.Trim().ToUpperInvariant();
		string organization = EditOrganization.Trim();

		if (_editingIndex >= 0 && _editingIndex < VirtualAirlines.Count)
		{
			VirtualAirlineItem item = VirtualAirlines[_editingIndex];
			item.Designator = designator;
			item.Telephony = telephony;
			item.Organization = organization;
		}
		else
		{
			VirtualAirlines.Add(new VirtualAirlineItem(designator, telephony, organization));
		}

		EndVirtualAirlineEdit();
		RefreshVatsimRadarMatches();
		MarkDirty();
	}

	private void DeleteVirtualAirline(VirtualAirlineItem? item)
	{
		int index = item is null ? -1 : VirtualAirlines.IndexOf(item);

		if (index < 0)
		{
			return;
		}

		VirtualAirlines.RemoveAt(index);

		if (_editingIndex == index)
		{
			EndVirtualAirlineEdit();
		}
		else if (_editingIndex > index)
		{
			_editingIndex--;
		}

		RaiseEditorFeedback();
		MarkDirty();
	}

	// ================= the VATSIM-Radar list =================

	/// <summary>
	/// The virtual airline on the VATSIM-Radar list that is the same as the one given - while the
	/// list is included; <see langword="null"/> otherwise.
	/// </summary>
	private VirtualAirline? FindOnVatsimRadarList(string designator, string telephony, string organization) =>
		IncludeVatsimRadarList ? VatsimRadarVirtualAirlines.FindSame(_vatsimRadarList, designator, telephony, organization) : null;

	/// <summary>A virtual airline as the list shows one: <c>DAL · DELTA · Fly Delta Virtual</c>.</summary>
	private static string Describe(VirtualAirline virtualAirline) =>
		$"{virtualAirline.Designator.ToUpperInvariant()} · {virtualAirline.Telephony.ToUpperInvariant()} · {virtualAirline.Organization}";

	/// <summary>Marks each of the user's virtual airlines that the included VATSIM-Radar list has too, and refreshes what the editor says.</summary>
	private void RefreshVatsimRadarMatches()
	{
		foreach (VirtualAirlineItem item in VirtualAirlines)
		{
			item.IsOnVatsimRadarList = FindOnVatsimRadarList(item.Designator, item.Telephony, item.Organization) is not null;
		}

		RaiseEditorFeedback();
	}

	/// <summary>
	/// Uses a copy of the VATSIM-Radar list - FE-Buddy's kept copy, read when the tab is built or after
	/// a download - or none. Tests hand one in directly.
	/// </summary>
	/// <param name="copy">The copy, or <see langword="null"/> when there is none.</param>
	internal void UseVatsimRadarCopy(VatsimRadarCopy? copy)
	{
		_vatsimRadarList = copy?.VirtualAirlines ?? [];
		_vatsimRadarDownloadedUtc = copy?.DownloadedUtc;

		OnPropertyChanged(nameof(VatsimRadarListStatus));
		RefreshVatsimRadarMatches();
	}

	/// <summary>
	/// Downloads the latest VATSIM-Radar list and uses it - or, when that fails, keeps using the copy
	/// FE-Buddy has and says why. Never throws: it runs from a button or a ticked box.
	/// </summary>
	internal async Task DownloadVatsimRadarListAsync()
	{
		if (IsDownloadingVatsimRadarList)
		{
			return;
		}

		IsDownloadingVatsimRadarList = true;

		try
		{
			SharedDataRefreshResult result = await RefreshVatsimRadarList(CancellationToken.None);
			_vatsimRadarFailure = result.FailureReason;
			UseVatsimRadarCopy(result.FilePath is { } path ? VatsimRadarVirtualAirlines.ReadKeptCopy(path) : null);
		}
		catch (Exception ex)
		{
			_vatsimRadarFailure = ex.Message;
		}
		finally
		{
			IsDownloadingVatsimRadarList = false;
			CommandManager.InvalidateRequerySuggested();
		}
	}

	// ================= virtual airlines: saving =================

	/// <summary>The list as numbered keys, from 1 in list order: <c>VirtualAirlines.1.Designator</c> and so on.</summary>
	private IEnumerable<(string Key, string Value)> VirtualAirlineKeys()
	{
		for (int i = 0; i < VirtualAirlines.Count; i++)
		{
			VirtualAirlineItem item = VirtualAirlines[i];
			string prefix = TelephonySettingsParser.VirtualAirlinesPrefix + (i + 1).ToString(CultureInfo.InvariantCulture) + ".";

			yield return (prefix + TelephonySettingsParser.DesignatorKey, item.Designator);
			yield return (prefix + TelephonySettingsParser.TelephonyKey, item.Telephony);
			yield return (prefix + TelephonySettingsParser.OrganizationKey, item.Organization);
		}
	}

	/// <summary>
	/// Rebuilds the list from the saved numbered keys, in number order. Every saved virtual airline
	/// is shown, even one that can't be written, so <see cref="Validate"/> can say which to fix.
	/// </summary>
	private void LoadVirtualAirlines()
	{
		// An open editor points at a list position that is about to be rebuilt.
		EndVirtualAirlineEdit();
		VirtualAirlines.Clear();

		string prefix = $"{NodePath}.{TelephonySettingsParser.VirtualAirlinesPrefix}";
		SortedDictionary<int, Dictionary<string, string>> byNumber = [];

		foreach ((string key, string value) in UserConfigFile.SnapshotValues())
		{
			if (!key.StartsWith(prefix, StringComparison.Ordinal))
			{
				continue;
			}

			string[] parts = key[prefix.Length..].Split('.');

			if (parts.Length == 2 && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int number))
			{
				if (!byNumber.TryGetValue(number, out Dictionary<string, string>? fields))
				{
					fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					byNumber[number] = fields;
				}

				fields[parts[1]] = value.Trim();
			}
		}

		foreach (Dictionary<string, string> fields in byNumber.Values)
		{
			string designator = fields.GetValueOrDefault(TelephonySettingsParser.DesignatorKey, string.Empty);
			string telephony = fields.GetValueOrDefault(TelephonySettingsParser.TelephonyKey, string.Empty);
			string organization = fields.GetValueOrDefault(TelephonySettingsParser.OrganizationKey, string.Empty);

			// A number with nothing in it has nothing to show or write.
			if (designator.Length > 0 || telephony.Length > 0 || organization.Length > 0)
			{
				VirtualAirlines.Add(new VirtualAirlineItem(designator.ToUpperInvariant(), telephony.ToUpperInvariant(), organization));
			}
		}

		RefreshVatsimRadarMatches();
	}
}
