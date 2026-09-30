using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

using Microsoft.Win32;

using FeBuddy.Versioning;
using FeBuddy.Versioning.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// SYSTEM ▸ Settings. Section order: Facility Profile, Default Region of Interest, GeoJSON Files,
/// Credentials, FE-Buddy's GitHub Requests, Updates, Reset FE-Buddy (with Uninstall FE-Buddy…
/// across from its button). Every value persists to
/// <c>UserConfig.json</c>, except credentials, which live in Windows Credential Manager and are
/// saved at once (<see cref="CredentialsViewModel"/>). FE-Buddy's GitHub Requests saves only the
/// chosen token's id.
/// </summary>
/// <remarks>
/// <para>
/// Every value, the default ROI included, waits for <b>Save</b>. While any differs from what is
/// saved the page shows "Unsaved changes", its nav row an amber dot, and Save is live.
/// </para>
/// <para>
/// The default ROI is also written by the Map page (through <see cref="DefaultRoiStore"/>), so
/// the page keeps the saved ROI apart from the one on screen: an edit here stays pending until
/// Save, and a change made on the Map shows here unless an edit here is still pending.
/// </para>
/// <para>
/// Export and Import move every setting in the app (not only this page's) to and from a file,
/// through <see cref="UserConfigTransfer"/>, so one user's setup can be handed to another.
/// </para>
/// <para>
/// Reset FE-Buddy asks what to keep (<see cref="ResetViewModel"/>), saves a copy of the settings
/// first if they go and the user wants one, records the reset (<see cref="AppDataReset"/>) and
/// restarts FE-Buddy, which carries it out as it starts.
/// </para>
/// <para>
/// Uninstall FE-Buddy…, shown only in the MSI-installed copy, says what goes (<see cref="UninstallViewModel"/>),
/// saves a copy of the settings first if the user wants one, starts Windows' uninstall
/// (<see cref="AppUninstall"/>) and closes FE-Buddy.
/// </para>
/// </remarks>
public sealed class SettingsViewModel : ObservableObject, IHasUnsavedChanges, IConfigPage
{
	private const string ChannelKey = UserConfigKeys.UpdateChannel;
	private const string OutputDirKey = UserConfigKeys.DefaultOutputDirectory;
	private const string AddFolderKey = UserConfigKeys.AddFeBuddyOutputFolder;
	/// <summary>
	/// Where the selected facility persists. Internal (rather than private) so the Procedures tab
	/// can read it directly at settings-build time, as <c>PrimaryFacility</c>, without copying the
	/// value into its own config node.
	/// </summary>
	internal const string ArtccKey = "Services.AiracService.UserArtccId";
	private const string PrecisionKey = UserConfigKeys.CoordinatePrecision;

	private readonly Dispatcher _dispatcher;
	private readonly Action? _openUpdateWindow;
	private readonly Func<IReadOnlyList<string>>? _describeUnfinishedWork;
	private bool _isCheckingForUpdates;

	private ReleaseChannel _channel;
	private ReleaseChannel _savedChannel;
	private string? _selectedFacility;
	private string _outputDir = string.Empty;
	private bool _addFeBuddyFolder = true;
	private int _coordinatePrecision = 6;
	private bool _prettyPrintGeojson;
	private RegionOfInterest? _defaultRoi;
	private RegionOfInterest? _savedRoi;
	private bool _useGitHubToken;
	private Guid _gitHubCredentialId;
	private bool _isDirty;
	private SavedStateSnapshot _savedState = SavedStateSnapshot.Of(new Dictionary<string, string>());

	/// <summary>Creates the Settings page.</summary>
	/// <param name="openUpdateWindow">
	/// Opens the update window for the current <see cref="AppEnvironment.Version"/>. The shell
	/// owns it (it tracks a "Later" for the version chip); "Check for updates now" calls it when
	/// the check finds an update.
	/// </param>
	/// <param name="describeUnfinishedWork">
	/// What closing FE-Buddy now would lose (a run in progress, unsaved edits on any page), for the
	/// Reset FE-Buddy and Uninstall FE-Buddy windows. The shell knows every page, so it supplies it.
	/// </param>
	public SettingsViewModel(Action? openUpdateWindow = null, Func<IReadOnlyList<string>>? describeUnfinishedWork = null)
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
		_openUpdateWindow = openUpdateWindow;
		_describeUnfinishedWork = describeUnfinishedWork;

		RefreshGitHubTokens();
		CredentialStore.Default.Changed += (_, _) => _dispatcher.BeginInvoke(RefreshGitHubTokens);
		LoadFromConfig();
		DefaultRoiStore.Changed += OnDefaultRoiChanged;
		ConfigPages.Register(this);

		SaveCommand = new RelayCommand(Save, () => IsDirty);
		CheckNowCommand = new RelayCommand(CheckForUpdates, () => AppEnvironment.HasInternetConnection && !IsCheckingForUpdates);
		RollbackCommand = new RelayCommand(() => BrowserLauncher.Open(Links.ChangeLog));
		BrowseOutputCommand = new RelayCommand(BrowseOutput);
		SetPrecisionCommand = new RelayCommand<string>(p => { if (int.TryParse(p, out int n)) CoordinatePrecision = n; });
		EditRoiCommand = new RelayCommand(EditRoi);
		ClearRoiCommand = new RelayCommand(ClearRoi, () => DefaultRoi is not null);
		ExportCommand = new RelayCommand(Export);
		ImportCommand = new RelayCommand(Import);
		NewGitHubTokenCommand = new RelayCommand(NewGitHubToken);
		ResetCommand = new RelayCommand(Reset);
		UninstallCommand = new RelayCommand(Uninstall, () => CanUninstall);

		AiracCycleDataCache.Instance.StateChanged += (_, _) => _dispatcher.BeginInvoke(RefreshFacilities);
		RefreshFacilities();
	}

	/// <summary><see langword="true"/> when a saved setting has been edited since the last Save.</summary>
	public bool IsDirty
	{
		get => _isDirty;
		private set
		{
			if (SetProperty(ref _isDirty, value))
			{
				OnPropertyChanged(nameof(HasUnsavedChanges));

				// Save is live only while there is something to save.
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <inheritdoc />
	public bool HasUnsavedChanges => IsDirty;

	/// <inheritdoc />
	public string ConfigPageName => "Settings";

	/// <summary>
	/// Re-evaluates the page after a setting changed. Call from every setter whose value <see cref="Save"/>
	/// persists. Dirty means "differs from what was last saved", so putting a value back the
	/// way it was clears the warning again.
	/// </summary>
	private void MarkDirty() => IsDirty = !_savedState.Matches(CurrentValues()) || IsRoiPending;

	/// <summary>Whether the ROI on screen differs from the saved one: set or cleared here, not saved yet.</summary>
	private bool IsRoiPending => DefaultRoi != _savedRoi;

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
		[UserConfigKeys.FeBuddyGitHubCredentialId] = GitHubCredentialValue,
	};

	// ================= 1. FACILITY PROFILE =================

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

	/// <summary>
	/// Where every service writes its output; the Desktop until one is saved. An AIRAC Service run
	/// writes into an <c>AIRAC_&lt;cycle&gt;</c> folder inside it.
	/// </summary>
	public string OutputDirectory
	{
		get => _outputDir;
		set
		{
			if (SetProperty(ref _outputDir, value))
			{
				MarkDirty();
				OnPropertyChanged(nameof(OutputFolderExample));
			}
		}
	}

	/// <summary>When on (default), output is written under a <c>FE-Buddy_Output</c> folder; off means straight to the chosen directory.</summary>
	public bool AddFeBuddyOutputFolder
	{
		get => _addFeBuddyFolder;
		set
		{
			if (SetProperty(ref _addFeBuddyFolder, value))
			{
				MarkDirty();
				OnPropertyChanged(nameof(OutputFolderExample));
			}
		}
	}

	/// <summary>
	/// Where a run of the current cycle would write with the values on screen, e.g.
	/// <c>…\FE-Buddy_Output\AIRAC_2610</c> - between backticks, for the view's <c>bhv:InlineCode</c>.
	/// </summary>
	public string OutputFolderExample
	{
		get
		{
			string cycleId = AppEnvironment.GetAiracCycle(AiracCyclePosition.Current).AiracCycleId;
			return $"A run of AIRAC cycle {cycleId} writes to `{AiracOutputPaths.CycleDirectory(OutputDirectory, AddFeBuddyOutputFolder, cycleId)}`";
		}
	}

	/// <summary>Picks <see cref="OutputDirectory"/> with a folder dialog.</summary>
	public ICommand BrowseOutputCommand { get; }

	// ================= 2. DEFAULT REGION OF INTEREST =================

	/// <summary>Explains what an ROI is, under the Default Region of Interest heading.</summary>
	public const string RoiExplainer =
		"Region of Interest (ROI):\n" +
		"A lat/lon axis-aligned rectangular region defined by southwest (bottom-left) and northeast (top-right) corners -\n" +
		"a box defining the data you are interested in.\n\n" +
		"Depending on the data type and operation, geometries may be clipped/cropped " +
		"to the ROI or included in full when associated with an entity inside it.\n\n" +
		"Consider making your ROI a little larger than your ARTCC boundary so nearby data still appears.\n\n" +
		"Some operations let you override this default ROI for specific files later using a custom ROI for that feature.";

	/// <summary>
	/// The default ROI on screen, or <see langword="null"/> when none is set. It becomes the saved
	/// default ROI when the page is saved.
	/// </summary>
	public RegionOfInterest? DefaultRoi
	{
		get => _defaultRoi;
		private set
		{
			if (SetProperty(ref _defaultRoi, value))
			{
				OnPropertyChanged(nameof(DefaultRoiSummary));
				OnPropertyChanged(nameof(HasDefaultRoi));
				MarkDirty();
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Whether a default ROI is set on screen.</summary>
	public bool HasDefaultRoi => DefaultRoi is not null;

	/// <summary>The default ROI's corners on one line, or that none is set.</summary>
	public string DefaultRoiSummary => DefaultRoi is { } r
		? $"SW {r.SwLat:0.####}, {r.SwLon:0.####}    ·    NE {r.NeLat:0.####}, {r.NeLon:0.####}"
		: "No default ROI is set.";

	/// <summary>Opens the ROI picker; what the user confirms is saved with the rest of the page by <see cref="SaveCommand"/>.</summary>
	public ICommand EditRoiCommand { get; }

	/// <summary>Turns the default ROI off; saved with the rest of the page by <see cref="SaveCommand"/>.</summary>
	public ICommand ClearRoiCommand { get; }

	// ================= 3. GEOJSON FILES =================

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

	// ================= 4. CREDENTIALS =================

	/// <summary>The Credentials card. Its changes are saved at once and take no part in <see cref="SaveCommand"/>.</summary>
	public CredentialsViewModel Credentials { get; } = new(CredentialStore.Default);

	// ================= 5. FE-BUDDY'S GITHUB REQUESTS =================

	/// <summary>Explains the card under its heading.</summary>
	public const string GitHubRequestsDescription =
		"Advanced - most people never need this. FE-Buddy checks for updates, reads News and downloads updates from " +
		"its public GitHub repository, which works without a GitHub account. A GitHub token lifts GitHub's limit of " +
		"60 requests an hour.";

	/// <summary>Whether FE-Buddy's own GitHub requests are sent with a GitHub token.</summary>
	public bool UseGitHubToken
	{
		get => _useGitHubToken;
		set
		{
			if (SetProperty(ref _useGitHubToken, value))
			{
				OnPropertyChanged(nameof(DontUseGitHubToken));
				OnPropertyChanged(nameof(GitHubTokenNotice));
				OnPropertyChanged(nameof(HasGitHubTokenNotice));
				MarkDirty();
			}
		}
	}

	/// <summary>The "Don't use a GitHub token" radio button: the opposite of <see cref="UseGitHubToken"/>.</summary>
	public bool DontUseGitHubToken
	{
		get => !UseGitHubToken;
		set => UseGitHubToken = !value;
	}

	/// <summary>The GitHub token to send them with; <see cref="Guid.Empty"/> until one is chosen.</summary>
	public Guid GitHubCredentialId
	{
		get => _gitHubCredentialId;
		set
		{
			if (SetProperty(ref _gitHubCredentialId, value))
			{
				OnPropertyChanged(nameof(GitHubTokenNotice));
				OnPropertyChanged(nameof(HasGitHubTokenNotice));
				MarkDirty();
			}
		}
	}

	/// <summary>The saved GitHub tokens that may go to GitHub's API, for the drop-down.</summary>
	public ObservableCollection<CredentialChoice> GitHubTokens { get; } = [];

	/// <summary>What is missing while "Use a GitHub token" is on, or <see langword="null"/>.</summary>
	public string? GitHubTokenNotice =>
		!UseGitHubToken ? null
		: GitHubTokens.Count == 0 ? "You have no GitHub token saved yet. Add one with New GitHub token…"
		: GitHubTokens.All(t => t.Id != GitHubCredentialId) ? "Choose a GitHub token. Until you do, FE-Buddy's requests go without one."
		: null;

	/// <summary>Whether <see cref="GitHubTokenNotice"/> is shown.</summary>
	public bool HasGitHubTokenNotice => GitHubTokenNotice is not null;

	/// <summary>Adds a GitHub token in the credential editor and chooses it.</summary>
	public ICommand NewGitHubTokenCommand { get; }

	/// <summary>What <see cref="Save"/> writes: the chosen token's id, or blank for none.</summary>
	private string GitHubCredentialValue =>
		UseGitHubToken && GitHubCredentialId != Guid.Empty ? GitHubCredentialId.ToString("N") : string.Empty;

	private void RefreshGitHubTokens()
	{
		IReadOnlyList<CredentialInfo> saved;

		try
		{
			saved = CredentialStore.Default.List();
		}
		catch (Win32Exception)
		{
			saved = [];   // The Credentials card reports it.
		}

		CredentialChoice.Sync(GitHubTokens,
		[
			.. saved
				.Where(info => info.Kind == CredentialKind.GitHubToken && CredentialHosts.Allows(info.Hosts, CredentialHosts.GitHubApiHost))
				.Select(CredentialChoice.For),
		]);

		OnPropertyChanged(nameof(GitHubTokenNotice));
		OnPropertyChanged(nameof(HasGitHubTokenNotice));
	}

	private void NewGitHubToken()
	{
		if (CredentialEditorWindow.Edit(Application.Current?.MainWindow, CredentialStore.Default, null) is not { } saved)
		{
			return;
		}

		RefreshGitHubTokens();

		if (GitHubTokens.Any(t => t.Id == saved.Id))
		{
			UseGitHubToken = true;
			GitHubCredentialId = saved.Id;
		}
		else
		{
			Toast.Warn("Not a GitHub token for GitHub's API",
				$"{saved.Name} is saved, but only a GitHub personal access token whose websites cover api.github.com (github.com does) can be used here.");
		}
	}

	// ================= 6. UPDATES =================

	/// <summary>Explains the channels under the Updates heading: each one includes every channel above it.</summary>
	public const string UpdatesDescription =
		"Choose the earliest stage of release you want to be offered. You are also offered every release that is further " +
		"along, so each channel includes the ones listed above it: Release Candidate offers release candidates and stable " +
		"releases, and Alpha offers every release.";

	/// <summary>What choosing Stable offers.</summary>
	public const string StableOffers = "Stable releases only";

	/// <summary>What a stable release is, for the Stable tooltip.</summary>
	public const string StableDescription =
		"Fully tested releases with no known serious problems. The right choice for almost everyone.";

	/// <summary>What choosing Release Candidate offers.</summary>
	public const string ReleaseCandidateOffers = "Release candidates and stable releases";

	/// <summary>What a release candidate is, for the Release Candidate tooltip.</summary>
	public const string ReleaseCandidateDescription =
		"Believed to be finished and working correctly, and in a final round of testing. If no problems turn up, it " +
		"becomes the next stable release.";

	/// <summary>What choosing Beta offers.</summary>
	public const string BetaOffers = "Betas, release candidates and stable releases";

	/// <summary>What a beta is, for the Beta tooltip.</summary>
	public const string BetaDescription =
		"Every planned feature is in and working, but testing is still under way, so expect some bugs that have not been " +
		"fixed yet.";

	/// <summary>What choosing Alpha offers.</summary>
	public const string AlphaOffers = "Every release: alphas, betas, release candidates and stable releases";

	/// <summary>What an alpha is, for the Alpha tooltip.</summary>
	public const string AlphaDescription =
		"Early builds with features still being worked on. Things may be unfinished, change from one build to the next, " +
		"or not work at all.";

	/// <summary>
	/// The update channel. Until the user chooses one, the channel of the build they are running
	/// (<see cref="UpdateChannelSetting"/>); saving a different one checks it straight away.
	/// </summary>
	public ReleaseChannel Channel
	{
		get => _channel;
		set
		{
			if (SetProperty(ref _channel, value))
			{
				MarkDirty();
				OnPropertyChanged(nameof(IsPreReleaseChannel));
				OnPropertyChanged(nameof(ChannelWarning));
			}
		}
	}

	/// <summary>Whether a channel other than Stable is chosen, which shows <see cref="ChannelWarning"/>.</summary>
	public bool IsPreReleaseChannel => Channel != ReleaseChannel.Stable;

	/// <summary>The recommendation to stay on Stable, naming the pre-release channel chosen.</summary>
	public string ChannelWarning =>
		$"We recommend staying on Stable. The {Channel.DisplayName()} channel may change or break things you are used to.";

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

	// ================= save =================

	/// <summary>Writes every setting on the page to <c>UserConfig.json</c>. Live only while <see cref="IsDirty"/>.</summary>
	public ICommand SaveCommand { get; }

	// ================= export / import =================

	/// <summary>The file-dialog filter for settings files.</summary>
	private const string SettingsFileFilter = "FE-Buddy settings (*.json)|*.json|All files (*.*)|*.*";

	/// <summary>How many folders the import confirmation lists under each heading before "…and N more".</summary>
	private const int ImportListLength = 8;

	/// <summary>Writes every saved setting that can leave this PC to a file the user picks.</summary>
	public ICommand ExportCommand { get; }

	/// <summary>Reads a settings file the user picks, shows what it would change, and imports it once confirmed.</summary>
	public ICommand ImportCommand { get; }

	// ================= reset =================

	/// <summary>Explains Reset FE-Buddy, on its card.</summary>
	public const string ResetDescription =
		"Start over as if FE-Buddy had just been installed. It deletes the AIRAC, Telephony and Wx Station " +
		"data it has downloaded and its logs, and - if you choose - your settings and saved credentials, " +
		"then restarts and downloads the AIRAC data again. Files in your output folder are not touched.";

	/// <summary>Opens the Reset FE-Buddy window, and resets and restarts FE-Buddy once confirmed.</summary>
	public ICommand ResetCommand { get; }

	// ================= uninstall =================

	/// <summary>
	/// Whether this copy can uninstall itself: it is the one the installer installed, and the
	/// installer recorded its ProductCode. Otherwise Uninstall FE-Buddy… is not shown.
	/// </summary>
	public bool CanUninstall { get; } =
		AppEnvironment.IsMsiInstalled && AppUninstall.UninstallerArguments(InstalledProduct.ProductCode) is not null;

	/// <summary>Opens the Uninstall FE-Buddy window, and starts Windows' uninstall and closes FE-Buddy once confirmed.</summary>
	public ICommand UninstallCommand { get; }

	/// <inheritdoc />
	public void ReloadFromConfig() => LoadFromConfig();

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

		// The check reads the saved channel; say so if the page shows a different, unsaved one.
		bool isUnsaved = Channel != version.Channel;
		string unsaved = isUnsaved ? $" Save to check the {Channel.DisplayName()} channel instead." : string.Empty;

		// An update, or - running a pre-release on the more stable channel saved here - that channel's
		// latest release to go back to (not while another channel waits to be saved).
		if (version.UpdateAvailable || (version.CanGoBack && !isUnsaved))
		{
			_openUpdateWindow?.Invoke();
			return;
		}

		string current = version.CurrentVersion.TrimStart('v', 'V');

		if (version.RunningPreReleaseChannel is { } running)
		{
			Toast.Success("No update available",
				$"v{current} is a {running.DisplayName()} release, newer than the latest {version.Channel.DisplayName()} release (v{version.LatestVersion}).{unsaved}");
		}
		else if (version.IsAheadOfLatestRelease)
		{
			Toast.Success("No update available",
				$"This development build (v{current}) is ahead of the latest {version.Channel.DisplayName()} release (v{version.LatestVersion}).{unsaved}");
		}
		else if (version.LatestVersion is null)
		{
			Toast.Success("No update available", $"There are no releases on the {version.Channel.DisplayName()} channel yet.{unsaved}");
		}
		else
		{
			Toast.Success("You're up to date", $"v{current} is the latest {version.Channel.DisplayName()} release.{unsaved}");
		}
	}

	private void Save()
	{
		// Written only when changed: until the user picks one, the channel follows the running build.
		bool channelChanged = Channel != _savedChannel;
		if (channelChanged)
		{
			UserConfigFile.TrySetValue(ChannelKey, Channel.ToString());
		}


		UserConfigFile.TrySetValue(OutputDirKey, OutputDirectory);
		UserConfigFile.TrySetValue(AddFolderKey, AddFeBuddyOutputFolder ? "Y" : "N");
		UserConfigFile.TrySetValue(PrecisionKey, CoordinatePrecision.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.TrySetValue(UserConfigKeys.PrettyPrintGeojson, PrettyPrintGeojson ? "Y" : "N");
		UserConfigFile.TrySetValue(UserConfigKeys.FeBuddyGitHubCredentialId, GitHubCredentialValue);
		if (!string.IsNullOrWhiteSpace(SelectedFacility))
		{
			UserConfigFile.TrySetValue(ArtccKey, SelectedFacility!);
		}

		UserConfigFile.Write();

		// Only an ROI set or cleared here: an untouched one is left as the Map page last saved it.
		if (IsRoiPending)
		{
			_savedRoi = DefaultRoi;

			if (DefaultRoi is { } roi)
			{
				DefaultRoiStore.Set(roi);
			}
			else
			{
				DefaultRoiStore.Clear();
			}
		}

		// Applied as soon as it is saved, so the next file written follows it without a restart.
		OutputFormatting.PrettyPrintGeojson = PrettyPrintGeojson;

		_savedChannel = Channel;
		_savedState = SavedStateSnapshot.Of(CurrentValues());
		IsDirty = false;
		Toast.Success("Settings saved", "Written to UserConfig.json.");

		// A new channel is checked straight away: its newer releases, or - running a pre-release after
		// choosing a more stable channel - its latest release to go back to.
		if (channelChanged && AppEnvironment.HasInternetConnection)
		{
			CheckForUpdates();
		}
	}

	/// <summary>
	/// Puts every value on the page to what <c>UserConfig</c> holds, dropping unsaved edits. The
	/// values read become the saved state: the page is clean until one differs from it.
	/// </summary>
	private void LoadFromConfig()
	{
		Channel = UpdateChannelSetting.Read(AppVersion.Current);
		_savedChannel = Channel;
		SelectedFacility = Blank(UserConfigFile.GetValue(ArtccKey));
		OutputDirectory = OutputPreferences.Directory;
		AddFeBuddyOutputFolder = OutputPreferences.AddFeBuddyOutputFolder;
		CoordinatePrecision = int.TryParse(UserConfigFile.GetValue(PrecisionKey), out int p) && p is >= 0 and <= 15 ? p : 6;
		PrettyPrintGeojson = string.Equals(
			UserConfigFile.GetValue(UserConfigKeys.PrettyPrintGeojson)?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);
		GitHubCredentialId = Guid.TryParse(UserConfigFile.GetValue(UserConfigKeys.FeBuddyGitHubCredentialId), out Guid gitHubId) ? gitHubId : Guid.Empty;
		UseGitHubToken = GitHubCredentialId != Guid.Empty;

		_savedRoi = DefaultRoiStore.Load();
		DefaultRoi = _savedRoi;

		_savedState = SavedStateSnapshot.Of(CurrentValues());
		MarkDirty();
	}

	private void Export()
	{
		Window? owner = Application.Current?.MainWindow;

		// The export is of what is saved; edits still on screen would silently be missing from it.
		IReadOnlyList<string> unsaved = ConfigPages.WithUnsavedChanges();
		if (unsaved.Count > 0
			&& !ConfirmWindow.Show(
				owner,
				"Unsaved changes",
				$"{JoinNames(unsaved)} {(unsaved.Count == 1 ? "has" : "have")} unsaved changes, which will not be in the export. "
				+ "Save them first to include them.",
				confirmText: "Export saved settings"))
		{
			return;
		}

		string facility = string.IsNullOrWhiteSpace(SelectedFacility) ? string.Empty : $" {SelectedFacility}";

		SaveFileDialog dialog = new()
		{
			Title = "Export FE-Buddy settings",
			Filter = SettingsFileFilter,
			DefaultExt = ".json",
			AddExtension = true,
			FileName = $"FE-Buddy Settings{facility} {DateTime.Now:yyyy-MM-dd}.json",
			InitialDirectory = OutputPreferences.BrowseDirectory(),
		};

		if (dialog.ShowDialog(owner) != true)
		{
			return;
		}

		try
		{
			UserConfigExportResult result = UserConfigTransfer.Export(dialog.FileName);
			string leftOut = result.LeftOutCount > 0 ? " Settings that only apply to this PC were left out." : string.Empty;
			Toast.Success("Settings exported", $"{result.SettingCount} settings written to {Path.GetFileName(result.Path)}.{leftOut}");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or UserConfigTransferException)
		{
			AppLog.Warning("Settings", $"Could not export settings to '{dialog.FileName}': {ex.Message}");
			Toast.Error("Export failed", ex.Message);
		}
	}

	private void Import()
	{
		Window? owner = Application.Current?.MainWindow;

		OpenFileDialog dialog = new()
		{
			Title = "Import FE-Buddy settings",
			Filter = SettingsFileFilter,
			Multiselect = false,
			InitialDirectory = OutputPreferences.BrowseDirectory(),
		};

		if (dialog.ShowDialog(owner) != true)
		{
			return;
		}

		UserConfigImportPlan plan;
		try
		{
			plan = UserConfigTransfer.Plan(UserConfigTransfer.Read(dialog.FileName));
		}
		catch (UserConfigTransferException ex)
		{
			AppLog.Warning("Settings", $"Could not import '{dialog.FileName}': {ex.Message}");
			Toast.Error("Cannot import settings", ex.Message);
			return;
		}

		if (!plan.HasChanges)
		{
			Toast.Info("Nothing to import", plan.SkippedFolders.Count > 0
				? $"{plan.Package.FileName} has the same settings as this PC, apart from folders or files that do not work on this PC."
				: $"{plan.Package.FileName} has the same settings as this PC.");
			return;
		}

		if (!ConfirmWindow.Show(owner, "Import settings", DescribeImport(plan, ConfigPages.WithUnsavedChanges()), confirmText: "Import"))
		{
			return;
		}

		try
		{
			UserConfigTransfer.Apply(plan);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning("Settings", $"Could not import '{dialog.FileName}': {ex.Message}");
			Toast.Error("Import failed", $"Nothing was changed. {ex.Message}");
			return;
		}

		// Every page built so far read its values once; have each read the imported ones.
		OutputFormatting.LoadFromUserConfig();
		IReadOnlyList<string> notReloaded = ConfigPages.ReloadAll();
		MapLayersState.ReloadFromConfigIfCreated();
		DefaultRoiStore.NotifyReloaded();

		string skipped = plan.SkippedFolders.Count > 0
			? " Folders and files that do not work on this PC were not taken, as the import summary listed."
			: string.Empty;
		Toast.Success("Settings imported", $"{plan.ChangedCount} settings updated from {plan.Package.FileName}.{skipped}");

		if (notReloaded.Count > 0)
		{
			// Saving on one of these would write its old values back over the import.
			Toast.Warn("Restart FE-Buddy",
				$"{JoinNames(notReloaded)} could not show the imported settings. Restart FE-Buddy before saving there, " +
				"or the settings from before the import would be saved back.");
		}
	}

	/// <summary>The import confirmation: what changes, what this PC keeps, and what is lost.</summary>
	/// <param name="plan">The import.</param>
	/// <param name="unsavedPages">The pages whose unsaved edits the import would drop.</param>
	/// <returns>The dialog text.</returns>
	private static string DescribeImport(UserConfigImportPlan plan, IReadOnlyList<string> unsavedPages)
	{
		UserConfigPackage package = plan.Package;
		StringBuilder text = new();

		text.Append(CultureInfo.InvariantCulture, $"Import the settings in {package.FileName}");
		if (package.IsPlainConfigFile)
		{
			text.Append(" (a UserConfig.json from another PC)");
		}
		else if (package.AppVersion is { } version)
		{
			string when = package.ExportedUtc is { } exported
				? $" on {exported.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture)}"
				: string.Empty;
			text.Append(CultureInfo.InvariantCulture, $" (exported from FE-Buddy v{version.TrimStart('v', 'V')}{when})");
		}

		text.Append(CultureInfo.InvariantCulture, $"?\n\n{plan.ChangedCount} {(plan.ChangedCount == 1 ? "setting changes" : "settings change")}.");

		// The same settings can produce different output on a different version of FE-Buddy.
		string running = AppVersion.Current.TrimStart('v', 'V');
		if (package.AppVersion?.TrimStart('v', 'V') is { } theirs && !string.Equals(theirs, running, StringComparison.OrdinalIgnoreCase))
		{
			text.Append(
				$"\n\nThis PC runs FE-Buddy v{running}, the file came from v{theirs}. The settings import all the same, "
				+ "but for identical output both PCs should run the same version.");
		}

		// Paths between backticks show in the code look (ConfirmWindow's bhv:InlineCode).
		AppendList(text, "Folders and files:", plan.AppliedFolders, folder =>
			folder.Path.Length == 0 ? folder.Note!
			: folder.Note is null ? $"`{folder.Path}`"
			: $"`{folder.Path}` ({folder.Note})");

		AppendList(text, "Not taken, as they do not work on this PC:", plan.SkippedFolders, folder => $"the file's `{folder.Path}` {folder.Note}");

		if (plan.KeptForThisPc.Count > 0)
		{
			string kept = JoinNames([.. plan.KeptForThisPc.Select(label => char.ToLowerInvariant(label[0]) + label[1..])]);
			text.Append(CultureInfo.InvariantCulture, $"\n\nKept as they are on this PC: {kept}.");
		}

		if (unsavedPages.Count > 0)
		{
			text.Append(CultureInfo.InvariantCulture, $"\n\nUnsaved changes on {JoinNames(unsavedPages)} will be lost.");
		}

		text.Append(CultureInfo.InvariantCulture, $"\n\nYour current settings are kept in {Path.GetFileName(UserConfigFile.BeforeImportFilePath)} in case you want them back.");

		return text.ToString();
	}

	/// <summary>
	/// Adds a heading and one line per folder, the first <see cref="ImportListLength"/> of them, so a
	/// file with many folders cannot grow the confirmation off the screen.
	/// </summary>
	private static void AppendList(StringBuilder text, string heading, IReadOnlyList<ImportedFolder> folders, Func<ImportedFolder, string> detail)
	{
		if (folders.Count == 0)
		{
			return;
		}

		text.Append(CultureInfo.InvariantCulture, $"\n\n{heading}");

		foreach (ImportedFolder folder in folders.Take(ImportListLength))
		{
			text.Append(CultureInfo.InvariantCulture, $"\n  • {folder.Label}: {detail(folder)}");
		}

		if (folders.Count > ImportListLength)
		{
			text.Append(CultureInfo.InvariantCulture, $"\n  • …and {folders.Count - ImportListLength} more");
		}
	}

	/// <summary>e.g. <c>Settings, Airways and Fixes</c>.</summary>
	/// <param name="names">The names.</param>
	/// <returns>The names as one phrase.</returns>
	private static string JoinNames(IReadOnlyList<string> names) => names.Count switch
	{
		0 => string.Empty,
		1 => names[0],
		_ => $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}",
	};

	/// <summary>
	/// Asks what the reset keeps, saves a copy of the settings first when they go and the user wants
	/// one, then records the reset and restarts FE-Buddy, which carries it out as it starts.
	/// </summary>
	private void Reset()
	{
		Window? owner = Application.Current?.MainWindow;

		ResetViewModel choices = new(
			hasSettings: File.Exists(UserConfigFile.ConfigFilePath),
			credentialCount: CountCredentials(),
			unfinishedWork: _describeUnfinishedWork?.Invoke() ?? []);

		new ResetWindow(choices) { Owner = owner }.ShowDialog();

		if (!choices.Confirmed)
		{
			return;
		}

		if (choices.SavesCopy && !SaveSettingsCopy(owner, "reset", "reset"))
		{
			return;
		}

		try
		{
			AppDataReset.Request(choices.BuildRequest(Environment.ProcessId));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning("Settings", $"Could not record the reset: {ex.Message}");
			Toast.Error("Could not reset FE-Buddy", ex.Message);
			return;
		}

		AppRestart.Restart(owner, "the reset");
	}

	/// <summary>
	/// Says what the uninstall removes, saves a copy of the settings first when the user wants one,
	/// then starts Windows' uninstall of this install and closes FE-Buddy so it can remove the files.
	/// </summary>
	private void Uninstall()
	{
		string? arguments = AppUninstall.UninstallerArguments(InstalledProduct.ProductCode);
		if (!CanUninstall || arguments is null)
		{
			return;
		}

		Window? owner = Application.Current?.MainWindow;

		UninstallViewModel choices = new(
			hasSettings: File.Exists(UserConfigFile.ConfigFilePath),
			credentialCount: CountCredentials(),
			unfinishedWork: _describeUnfinishedWork?.Invoke() ?? []);

		new UninstallWindow(choices) { Owner = owner }.ShowDialog();

		if (!choices.Confirmed)
		{
			return;
		}

		if (choices.SavesCopy && !SaveSettingsCopy(owner, "uninstall", "uninstalled"))
		{
			return;
		}

		try
		{
			// Not elevated, as Windows starts it: Windows Installer asks for permission itself (see AppUninstall).
			Process.Start(new ProcessStartInfo("msiexec.exe", arguments) { UseShellExecute = true });
		}
		catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
		{
			AppLog.Warning("Settings", $"Could not start the uninstall: {ex.Message}");
			Toast.Error("Could not start the uninstall",
				$"{ex.Message} Uninstall FE-Buddy from Windows Settings ▸ Apps ▸ Installed apps instead.");
			return;
		}

		AppLog.Info("Settings", "Started Windows' uninstall of FE-Buddy; closing FE-Buddy.");
		Application.Current?.Shutdown();
	}

	/// <summary>How many credentials are saved; 0 when Windows Credential Manager cannot be read.</summary>
	private static int CountCredentials()
	{
		try
		{
			return CredentialStore.Default.List().Count;
		}
		catch (Win32Exception ex)
		{
			AppLog.Warning("Settings", $"Could not read Windows Credential Manager: {ex.Message}");
			return 0;
		}
	}

	/// <summary>
	/// Copies <c>UserConfig.json</c> to where the user picks, before a reset or an uninstall deletes
	/// it. The copy is the file as saved - a full backup, which Import reads back.
	/// </summary>
	/// <param name="owner">The window the file dialog belongs to.</param>
	/// <param name="action">What is about to happen, for messages: <c>reset</c> or <c>uninstall</c>.</param>
	/// <param name="done">The same as a past participle: <c>reset</c> or <c>uninstalled</c>.</param>
	/// <returns><see langword="true"/> once the copy is saved; <see langword="false"/> to stop.</returns>
	private static bool SaveSettingsCopy(Window? owner, string action, string done)
	{
		SaveFileDialog dialog = new()
		{
			Title = "Save a copy of your settings",
			Filter = SettingsFileFilter,
			DefaultExt = ".json",
			AddExtension = true,
			FileName = $"FE-Buddy Settings backup {DateTime.Now:yyyy-MM-dd}.json",
			InitialDirectory = OutputPreferences.DefaultDirectory,
		};

		if (dialog.ShowDialog(owner) != true)
		{
			Toast.Info($"Nothing was {done}", "No copy of your settings was saved, so FE-Buddy was left as it is.");
			return false;
		}

		// Both empty FE-Buddy's own folder, so a copy saved in it would go too.
		string folder = Path.GetFullPath(AppDataReset.RootDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		if (Path.GetFullPath(dialog.FileName).StartsWith(folder, StringComparison.OrdinalIgnoreCase))
		{
			Toast.Warn("Choose another folder", $"The {action} empties {AppDataReset.RootDirectory}, copy and all. Nothing was {done}.");
			return false;
		}

		try
		{
			File.Copy(UserConfigFile.ConfigFilePath, dialog.FileName, overwrite: true);
			AppLog.Info("Settings", $"Saved a copy of the settings to '{dialog.FileName}' before the {action}.");
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning("Settings", $"Could not save a copy of the settings to '{dialog.FileName}': {ex.Message}");
			Toast.Error("Could not save the copy", $"{ex.Message} Nothing was {done}.");
			return false;
		}
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
			AiracCycleInfo current = AppEnvironment.GetAiracCycle(AiracCyclePosition.Current);
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
			InitialDirectory = OutputPreferences.BrowseDirectory(OutputDirectory),
		};

		if (dialog.ShowDialog() == true)
		{
			OutputDirectory = dialog.FolderName;
		}
	}

	private void EditRoi()
	{
		RegionOfInterest? picked = RoiPickerWindow.Pick(
			Application.Current?.MainWindow, DefaultRoi, "Default Region of Interest");
		if (picked is not null)
		{
			// Pending until Save, like every other value on the page.
			DefaultRoi = picked;
		}
	}

	private void ClearRoi() => DefaultRoi = null;

	// View-models live for the whole session (NavItem caches them), so Settings has to hear when
	// the Map page's inline editor changes or clears the default ROI. The saved ROI always
	// follows it; the one on screen does too, unless an edit made here is still waiting for Save.
	private void OnDefaultRoiChanged(object? sender, EventArgs e) =>
		_dispatcher.BeginInvoke(() =>
		{
			bool pendingHere = IsRoiPending;
			_savedRoi = DefaultRoiStore.Load();

			if (!pendingHere)
			{
				DefaultRoi = _savedRoi;
			}

			MarkDirty();
		});

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
