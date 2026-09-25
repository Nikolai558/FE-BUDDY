using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

using Microsoft.Win32;

using FeBuddy.Versioning.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// SYSTEM ▸ Settings. Section order: Updates, Facility Profile, Default Region of Interest,
/// GeoJSON Files. Every value persists to <c>UserConfig.json</c>.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
	private const string ChannelKey = UserConfigKeys.UpdateChannel;
	private const string OutputDirKey = UserConfigKeys.DefaultOutputDirectory;
	private const string AddFolderKey = UserConfigKeys.AddFeBuddyOutputFolder;
	private const string ArtccKey = "Services.AiracService.UserArtccId";
	private const string PrecisionKey = UserConfigKeys.CoordinatePrecision;

	private readonly Dispatcher _dispatcher;
	private readonly Action? _openUpdateWindow;
	private bool _isCheckingForUpdates;

	private ReleaseChannel _channel;
	private string? _selectedFacility;
	private string _outputDir = string.Empty;
	private bool _addFeBuddyFolder = true;
	private int _coordinatePrecision = 6;
	private bool _prettyPrintGeojson;
	private RegionOfInterest? _defaultRoi;
	private bool _isDirty;
	private SavedStateSnapshot _savedState = SavedStateSnapshot.Of(new Dictionary<string, string>());

	/// <summary>Creates the Settings page.</summary>
	/// <param name="openUpdateWindow">
	/// Opens the update window for the current <see cref="AppEnvironment.Version"/>. The shell
	/// owns it (it tracks a "Later" for the version chip); "Check for updates now" calls it when
	/// the check finds an update.
	/// </param>
	public SettingsViewModel(Action? openUpdateWindow = null)
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
		_openUpdateWindow = openUpdateWindow;

		_channel = VersionCheckResult.ParseChannel(UserConfigFile.GetValue(ChannelKey));
		_selectedFacility = Blank(UserConfigFile.GetValue(ArtccKey));
		_outputDir = Blank(UserConfigFile.GetValue(OutputDirKey)) ?? OutputPreferences.DefaultDirectory;
		_addFeBuddyFolder = !string.Equals(UserConfigFile.GetValue(AddFolderKey), "N", StringComparison.OrdinalIgnoreCase);
		_coordinatePrecision = int.TryParse(UserConfigFile.GetValue(PrecisionKey), out int p) && p is >= 0 and <= 15 ? p : 6;
		_prettyPrintGeojson = string.Equals(
			UserConfigFile.GetValue(UserConfigKeys.PrettyPrintGeojson)?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);

		_defaultRoi = DefaultRoiStore.Load();
		DefaultRoiStore.Changed += OnDefaultRoiChanged;

		SaveCommand = new RelayCommand(Save);
		CheckNowCommand = new RelayCommand(CheckForUpdates, () => AppEnvironment.HasInternetConnection && !IsCheckingForUpdates);
		RollbackCommand = new RelayCommand(() => BrowserLauncher.Open(Links.ChangeLog));
		BrowseOutputCommand = new RelayCommand(BrowseOutput);
		SetPrecisionCommand = new RelayCommand<string>(p => { if (int.TryParse(p, out int n)) CoordinatePrecision = n; });
		EditRoiCommand = new RelayCommand(EditRoi);
		ClearRoiCommand = new RelayCommand(ClearRoi, () => DefaultRoi is not null);

		AiracCycleDataCache.Instance.StateChanged += (_, _) => _dispatcher.BeginInvoke(RefreshFacilities);
		RefreshFacilities();

		// Everything above is the loaded state; the page is clean until it differs from this.
		_savedState = SavedStateSnapshot.Of(CurrentValues());
	}

	/// <summary><see langword="true"/> when a saved setting has been edited since the last Save.</summary>
	public bool IsDirty
	{
		get => _isDirty;
		private set => SetProperty(ref _isDirty, value);
	}

	/// <summary>
	/// Re-evaluates the page after a setting changed. Call from every setter whose value <see cref="Save"/>
	/// persists. Dirty means "differs from what was last saved", so putting a value back the
	/// way it was clears the warning again.
	/// </summary>
	private void MarkDirty() => IsDirty = !_savedState.Matches(CurrentValues());

	/// <summary>
	/// Every value <see cref="Save"/> persists, as the strings it would write. Keep in step with it:
	/// a value missing here would never raise "unsaved changes".
	/// </summary>
	/// <returns>The values by UserConfig key.</returns>
	private Dictionary<string, string> CurrentValues() => new(StringComparer.Ordinal)
	{
		[ChannelKey] = Channel.ToString(),
		[OutputDirKey] = OutputDirectory,
		[AddFolderKey] = AddFeBuddyOutputFolder ? "Y" : "N",
		[PrecisionKey] = CoordinatePrecision.ToString(CultureInfo.InvariantCulture),
		[UserConfigKeys.PrettyPrintGeojson] = PrettyPrintGeojson ? "Y" : "N",
		[ArtccKey] = SelectedFacility ?? string.Empty,
	};

	// ================= 1. UPDATES =================

	/// <summary>The update channels, in the order the menu shows them.</summary>
	public IReadOnlyList<ReleaseChannel> Channels { get; } =
		[ReleaseChannel.Stable, ReleaseChannel.Beta, ReleaseChannel.Alpha];

	/// <summary>The update channel. <see cref="ReleaseChannel.Stable"/> unless the developers tell you otherwise.</summary>
	public ReleaseChannel Channel
	{
		get => _channel;
		set { if (SetProperty(ref _channel, value)) MarkDirty(); }
	}

	/// <summary>Whether the machine has internet; the update check needs it.</summary>
	public bool IsOnline => AppEnvironment.HasInternetConnection;

	/// <summary>Re-runs the version check, then opens the update window or toasts that there is nothing new.</summary>
	public ICommand CheckNowCommand { get; }

	/// <summary><see langword="true"/> while "Check for updates now" is waiting on GitHub (the button shows "Checking…").</summary>
	public bool IsCheckingForUpdates
	{
		get => _isCheckingForUpdates;
		private set
		{
			if (SetProperty(ref _isCheckingForUpdates, value))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Opens the releases page, where an older version can be downloaded.</summary>
	public ICommand RollbackCommand { get; }

	// ================= 2. FACILITY PROFILE =================

	/// <summary>Facilities from the current cycle's parsed airports, as <c>ArtccName (RespArtccId)</c>.</summary>
	public ObservableCollection<FacilityOption> Facilities { get; } = [];

	/// <summary>The selected facility's <c>RespArtccId</c>. Persists to <c>Services.AiracService.UserArtccId</c>.</summary>
	public string? SelectedFacility
	{
		get => _selectedFacility;
		set { if (SetProperty(ref _selectedFacility, value)) MarkDirty(); }
	}

	/// <summary>Whether the AIRAC data is ready, so <see cref="Facilities"/> can be filled.</summary>
	public bool FacilitiesReady { get; private set; }

	/// <summary>Shown in place of the facility list until <see cref="FacilitiesReady"/>.</summary>
	public string FacilityWaitingMessage =>
		"Waiting for AIRAC data to finish downloading and parsing. The facility list will be available in a moment.";

	/// <summary>Where every service writes its output.</summary>
	public string OutputDirectory
	{
		get => _outputDir;
		set { if (SetProperty(ref _outputDir, value)) MarkDirty(); }
	}

	/// <summary>When on (default), output is written under a <c>FE-Buddy_Output</c> folder; off means straight to the chosen directory.</summary>
	public bool AddFeBuddyOutputFolder
	{
		get => _addFeBuddyFolder;
		set { if (SetProperty(ref _addFeBuddyFolder, value)) MarkDirty(); }
	}

	/// <summary>Picks <see cref="OutputDirectory"/> with a folder dialog.</summary>
	public ICommand BrowseOutputCommand { get; }

	// ================= 3. DEFAULT REGION OF INTEREST =================

	/// <summary>Explains what an ROI is, under the Default Region of Interest heading.</summary>
	public const string RoiExplainer =
		"Region of Interest (ROI): a lat/lon axis-aligned rectangular region defined by southwest " +
		"(bottom-left) and northeast (top-right) corners - a box defining the data you are interested in. " +
		"Depending on the data type and operation, geometries may be clipped to the ROI or included in " +
		"full when associated with an entity inside it. Make the box a little larger than your ARTCC " +
		"boundary so nearby data still appears. Some operations let you override this ROI for specific " +
		"files later.";

	/// <summary>The saved default ROI, or <see langword="null"/> when none is set.</summary>
	public RegionOfInterest? DefaultRoi
	{
		get => _defaultRoi;
		private set
		{
			if (SetProperty(ref _defaultRoi, value))
			{
				OnPropertyChanged(nameof(DefaultRoiSummary));
				OnPropertyChanged(nameof(HasDefaultRoi));
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Whether a default ROI is saved.</summary>
	public bool HasDefaultRoi => DefaultRoi is not null;

	/// <summary>The default ROI's corners on one line, or that none is set.</summary>
	public string DefaultRoiSummary => DefaultRoi is { } r
		? $"SW {r.SwLat:0.####}, {r.SwLon:0.####}    ·    NE {r.NeLat:0.####}, {r.NeLon:0.####}"
		: "No default ROI is set.";

	/// <summary>Opens the ROI picker and saves what the user confirms. Saved straight away, not by <see cref="SaveCommand"/>.</summary>
	public ICommand EditRoiCommand { get; }

	/// <summary>Turns the default ROI off. Saved straight away.</summary>
	public ICommand ClearRoiCommand { get; }

	// ================= 4. GEOJSON FILES =================

	/// <summary>Explains the FE-Buddy properties.</summary>
	public const string FebPropertiesDescription =
		"Include FE-Buddy Properties, when available. Custom GeoJSON property fields that increase file " +
		"size but can be helpful for debugging or viewing data in a GeoJSON viewer in order to identify " +
		"an object. Every FE-Buddy property is prefixed with feb.";

	/// <summary>Explains the coordinate precision choice.</summary>
	public const string CoordinatePrecisionDescription =
		"Will round all coordinates in GeoJSON files to a maximum number of decimal points in order to " +
		"save space but retain your desired level of accuracy.";

	/// <summary>How many decimal places GeoJSON coordinates are rounded to.</summary>
	public int CoordinatePrecision
	{
		get => _coordinatePrecision;
		private set
		{
			if (SetProperty(ref _coordinatePrecision, value))
			{
				OnPropertyChanged(nameof(IsPrecision5));
				OnPropertyChanged(nameof(IsPrecision6));
				OnPropertyChanged(nameof(IsPrecision7));
				MarkDirty();
			}
		}
	}

	/// <summary>Whether 5 decimal places is chosen.</summary>
	public bool IsPrecision5 => CoordinatePrecision == 5;

	/// <summary>Whether 6 decimal places is chosen.</summary>
	public bool IsPrecision6 => CoordinatePrecision == 6;

	/// <summary>Whether 7 decimal places is chosen.</summary>
	public bool IsPrecision7 => CoordinatePrecision == 7;

	/// <summary>Sets <see cref="CoordinatePrecision"/>. The command parameter is <c>"5"</c>, <c>"6"</c> or <c>"7"</c>.</summary>
	public ICommand SetPrecisionCommand { get; }

	/// <summary>Explains the file-layout choice under its heading.</summary>
	public const string FileLayoutDescription =
		"How every GeoJSON file FE-Buddy writes is laid out. Single line keeps files small; pretty print " +
		"puts each property on its own line so a file is easy to read in a text editor. Applies to all " +
		"GeoJSON output, whichever service writes it.";

	/// <summary>
	/// <see langword="true"/> to write GeoJSON pretty printed; <see langword="false"/> (the default)
	/// for single line. Takes effect for the rest of the session as soon as Settings is saved.
	/// </summary>
	public bool PrettyPrintGeojson
	{
		get => _prettyPrintGeojson;
		set { if (SetProperty(ref _prettyPrintGeojson, value)) MarkDirty(); }
	}

	/// <summary>
	/// Shown under the choice when developer mode is on, because developer mode pretty prints
	/// regardless and the single-line choice would otherwise look broken.
	/// </summary>
	public bool IsDevModeForcingPrettyPrint => DevMode.IsEnabled;

	// ================= save =================

	/// <summary>Writes every setting on the page to <c>UserConfig.json</c>.</summary>
	public ICommand SaveCommand { get; }

	private async void CheckForUpdates()
	{
		IsCheckingForUpdates = true;
		try
		{
			await AppEnvironment.RecheckAsync();
		}
		finally
		{
			IsCheckingForUpdates = false;
		}

		VersionCheckResult? version = AppEnvironment.Version;
		if (version is null || !version.CheckSucceeded)
		{
			Toast.Warn("Could not check for updates", version?.Message ?? "The version service did not answer.");
			return;
		}

		if (version.UpdateAvailable)
		{
			_openUpdateWindow?.Invoke();
			return;
		}

		// The check reads the saved channel; say so if the page shows a different, unsaved one.
		string unsaved = Channel != version.Channel ? $" Save to check the {Channel} channel instead." : string.Empty;
		string current = version.CurrentVersion.TrimStart('v', 'V');

		if (version.IsAheadOfLatestRelease)
		{
			Toast.Success("No update available",
				$"This development build (v{current}) is ahead of the latest {version.Channel} release (v{version.LatestVersion}).{unsaved}");
		}
		else if (version.LatestVersion is null)
		{
			Toast.Success("No update available", $"There are no releases on the {version.Channel} channel yet.{unsaved}");
		}
		else
		{
			Toast.Success("You're up to date", $"v{current} is the latest {version.Channel} release.{unsaved}");
		}
	}

	private void Save()
	{
		UserConfigFile.TrySetValue(ChannelKey, Channel.ToString());
		UserConfigFile.TrySetValue(OutputDirKey, OutputDirectory);
		UserConfigFile.TrySetValue(AddFolderKey, AddFeBuddyOutputFolder ? "Y" : "N");
		UserConfigFile.TrySetValue(PrecisionKey, CoordinatePrecision.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.TrySetValue(UserConfigKeys.PrettyPrintGeojson, PrettyPrintGeojson ? "Y" : "N");
		if (!string.IsNullOrWhiteSpace(SelectedFacility))
		{
			UserConfigFile.TrySetValue(ArtccKey, SelectedFacility!);
		}

		UserConfigFile.Write();

		// Applied as soon as it is saved, so the next file written follows it without a restart.
		OutputFormatting.PrettyPrintGeojson = PrettyPrintGeojson;

		_savedState = SavedStateSnapshot.Of(CurrentValues());
		IsDirty = false;
		Toast.Success("Settings saved", "Written to UserConfig.json.");
	}

	private void RefreshFacilities()
	{
		AiracCycleReadiness readiness = AiracCycleDataCache.Instance.Entries.Count == 0
			? AiracCycleReadiness.Waiting
			: AiracCycleDataCache.Instance.ComputeReadiness();

		FacilitiesReady = readiness is AiracCycleReadiness.Ready or AiracCycleReadiness.Degraded;
		OnPropertyChanged(nameof(FacilitiesReady));
		OnPropertyChanged(nameof(IsOnline));
		CommandManager.InvalidateRequerySuggested();

		if (!FacilitiesReady || Facilities.Count > 0)
		{
			return;
		}

		_ = LoadFacilitiesAsync();
	}

	private async Task LoadFacilitiesAsync()
	{
		try
		{
			AiracCycleInfo current = AiracCycleResolver.GetCycle(AiracCyclePosition.Current);
			var data = await AiracCycleDataCache.Instance.GetAsync(current.AiracCycleId).ConfigureAwait(false);

			var options = (data.Apt?.AptBase ?? [])
				.Where(a => !string.IsNullOrWhiteSpace(a.RespArtccId))
				.Select(a => new FacilityOption(a.RespArtccId.Trim(), string.IsNullOrWhiteSpace(a.ArtccName) ? a.RespArtccId.Trim() : a.ArtccName.Trim()))
				.DistinctBy(o => o.ArtccId, StringComparer.OrdinalIgnoreCase)
				.OrderBy(o => o.Display, StringComparer.OrdinalIgnoreCase)
				.ToList();

			await _dispatcher.BeginInvoke(() =>
			{
				Facilities.Clear();
				foreach (FacilityOption o in options)
				{
					Facilities.Add(o);
				}
			});
		}
		catch (Exception ex)
		{
			AppLog.Warning("Settings", $"Could not load the facility list: {ex.Message}");
		}
	}

	private void BrowseOutput()
	{
		OpenFolderDialog dialog = new()
		{
			Title = "Select the default output directory",
			InitialDirectory = Directory.Exists(OutputDirectory) ? OutputDirectory : null,
		};

		if (dialog.ShowDialog() == true)
		{
			OutputDirectory = dialog.FolderName;
		}
	}

	private void EditRoi()
	{
		RegionOfInterest? picked = RoiPickerWindow.Pick(
			Application.Current?.MainWindow, DefaultRoi, Map.BaseMap.UsStates);
		if (picked is not null)
		{
			DefaultRoiStore.Set(picked);
			DefaultRoi = picked;
			Toast.Success("Default ROI saved", "Written to UserConfig.json.");
		}
	}

	private void ClearRoi()
	{
		DefaultRoiStore.Clear();
		DefaultRoi = null;
	}

	// View-models live for the whole session (NavItem caches them), so Settings has to hear when
	// the Map page's inline editor changes or clears the default ROI.
	private void OnDefaultRoiChanged(object? sender, EventArgs e) =>
		_dispatcher.BeginInvoke(() => DefaultRoi = DefaultRoiStore.Load());

	private static string? Blank(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;

	/// <summary>One facility choice: its <c>RespArtccId</c> and a display label.</summary>
	/// <param name="ArtccId">The facility's ARTCC id.</param>
	/// <param name="Name">The facility's name.</param>
	public sealed record FacilityOption(string ArtccId, string Name)
	{
		/// <summary>e.g. <c>Cleveland ARTCC (ZOB)</c>.</summary>
		public string Display => $"{Name} ({ArtccId})";
	}
}
