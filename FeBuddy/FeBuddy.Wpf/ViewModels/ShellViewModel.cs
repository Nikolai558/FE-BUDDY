using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Domain.Airac.Models;

using FeBuddy.Versioning;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs <c>ShellWindow</c>: navigation, the status-bar Zulu clock, the top-centre AIRAC
/// status indicator, the version chip and update badge, and the one-time notice about FE-Buddy 2.x's
/// GitHub token variable. Everything but navigation is driven by <see cref="AppEnvironment"/> and
/// <see cref="AiracCycleDataCache"/>.
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
	// Segoe Fluent Icons code-points for the nav rows. The glyphs XAML uses live in Theme/Icons.xaml.
	private const string GlyphDashboard = ""; // Home
	private const string GlyphAiracService = ""; // Calendar (MDL2)
	private const string GlyphFileConversions = ""; // Switch
	private const string GlyphMap = ""; // MapPin
	private const string GlyphSettings = ""; // Setting
	private const string GlyphInfo = ""; // Info

	/// <summary>How many seconds the update badge shows each of its two lines before the other.</summary>
	private const int BadgeTurnSeconds = 3;

	private readonly Dispatcher _dispatcher;

	private object? _current;
	private string _zuluClock = string.Empty;
	private bool _navCollapsed;
	private bool _updateDeclinedThisSession;

	private string _airacStatus = "Preparing AIRAC data…";
	private string _versionText = "…";
	private bool _isUpdateAvailable;
	private bool _isOnline = true;
	private string _versionBrushKey = "Brush.Accent.Text";
	private string _updateTooltipTitle = "Checking for updates…";
	private string _updateTooltipBody = string.Empty;
	private string _updateBadgeVersionText = string.Empty;
	private bool _badgeShowsVersion;
	private int _badgeSeconds;

	/// <summary>Builds the navigation, starts the Zulu clock, and follows the launch state.</summary>
	public ShellViewModel()
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		PrimaryNav =
		[
			Nav("Dashboard",      GlyphDashboard,     () => new DashboardViewModel()),
			Nav("AIRAC Service",  GlyphAiracService,  () => new AiracServiceViewModel()),
			Nav("File Conversions", GlyphFileConversions, () => new FileConversionsViewModel()),
			Nav("Map",            GlyphMap,           () => new MapViewModel()),
		];

		SystemNav =
		[
			Nav("Settings", GlyphSettings, () => new SettingsViewModel(OpenUpdateWindow, DescribeUnfinishedWork)),
			Nav("Info",     GlyphInfo,     () => new InfoViewModel()),
		];

		SystemHealth = [];

		RecheckCommand = new RelayCommand(RecheckNow);
		ToggleNavCommand = new RelayCommand(() => NavCollapsed = !NavCollapsed);
		OpenUpdateWindowCommand = new RelayCommand(OpenUpdateWindow, () => IsUpdateAvailable);

		PrimaryNav[0].IsActive = true;

		// Zulu clock, ticked once a second on the UI thread; the update badge takes turns on the same beat.
		UpdateZulu();
		var clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
		clock.Tick += (_, _) =>
		{
			UpdateZulu();
			AdvanceUpdateBadge();
		};
		clock.Start();

		// Live launch/pipeline state.
		AppEnvironment.Changed += OnEnvironmentChanged;
		AiracCycleDataCache.Instance.StateChanged += OnCycleStateChanged;

		RefreshVersionState();
		RefreshAiracStatus();
		RefreshSystemHealth();

		// The launch sequence may have found the old token variable before this subscribed.
		_dispatcher.InvokeAsync(ShowLegacyGitHubTokenNotice, DispatcherPriority.ApplicationIdle);
	}

	/// <summary>The main nav group: Dashboard, AIRAC Service, File Conversions, Map.</summary>
	public ObservableCollection<NavItem> PrimaryNav { get; }

	/// <summary>The SYSTEM nav group: Settings, Info.</summary>
	public ObservableCollection<NavItem> SystemNav { get; }

	/// <summary>The rows of the systems-health popover.</summary>
	public ObservableCollection<HealthRow> SystemHealth { get; }

	/// <summary>Re-runs the time, internet and version checks.</summary>
	public ICommand RecheckCommand { get; }

	/// <summary>Collapses or expands the nav rail.</summary>
	public ICommand ToggleNavCommand { get; }

	/// <summary>Opens the modal update window. Enabled only when an update is available.</summary>
	public ICommand OpenUpdateWindowCommand { get; }

	/// <summary><see langword="true"/> for an icons-only nav rail.</summary>
	public bool NavCollapsed
	{
		get => _navCollapsed;
		set => SetProperty(ref _navCollapsed, value);
	}

	/// <summary>View-model of the section currently on screen.</summary>
	public object? Current
	{
		get => _current;
		private set => SetProperty(ref _current, value);
	}

	/// <summary>
	/// The top-centre AIRAC status readout: narrates the launch pipeline
	/// (<c>Downloading cycle 2610…</c> → <c>Parsing cycle 2610…</c> →
	/// <c>AIRAC 2610 · eff 01 OCT 2026</c>), then settles on the current cycle. Never a
	/// constant - it reads <see cref="AiracCycleDataCache"/> state.
	/// </summary>
	public string AiracStatus
	{
		get => _airacStatus;
		private set => SetProperty(ref _airacStatus, value);
	}

	/// <summary>The running version, e.g. <c>v3.0.0</c> or <c>dev</c>.</summary>
	public string VersionText
	{
		get => _versionText;
		private set => SetProperty(ref _versionText, value);
	}

	/// <summary><see langword="true"/> when a newer version exists on the user's channel - makes the version chip a button.</summary>
	public bool IsUpdateAvailable
	{
		get => _isUpdateAvailable;
		private set
		{
			if (SetProperty(ref _isUpdateAvailable, value))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Whether the machine currently has internet - gates the update tooltip.</summary>
	public bool IsOnline
	{
		get => _isOnline;
		private set => SetProperty(ref _isOnline, value);
	}

	/// <summary>
	/// Resource key for the version text brush: <c>Brush.Accent.Text</c> normally, or
	/// <c>Brush.Warn</c> for the rest of the session once the user declines an available
	/// update.
	/// </summary>
	public string VersionBrushKey
	{
		get => _versionBrushKey;
		private set
		{
			if (SetProperty(ref _versionBrushKey, value))
			{
				OnPropertyChanged(nameof(VersionIsWarning));
			}
		}
	}

	/// <summary><see langword="true"/> once the user has declined an available update this session - the version text and the update badge turn amber.</summary>
	public bool VersionIsWarning => _versionBrushKey == "Brush.Warn";

	/// <summary>Title line of the FE-BUDDY caption tooltip - update state only.</summary>
	public string UpdateTooltipTitle
	{
		get => _updateTooltipTitle;
		private set => SetProperty(ref _updateTooltipTitle, value);
	}

	/// <summary>Body line of the FE-BUDDY caption tooltip.</summary>
	public string UpdateTooltipBody
	{
		get => _updateTooltipBody;
		private set => SetProperty(ref _updateTooltipBody, value);
	}

	/// <summary>
	/// The update badge's second line, e.g. <c>v3.0.0-beta.1 available</c>. The badge beside the
	/// version shows only while <see cref="IsUpdateAvailable"/>, taking turns between "Update
	/// available!" and this.
	/// </summary>
	public string UpdateBadgeVersionText
	{
		get => _updateBadgeVersionText;
		private set => SetProperty(ref _updateBadgeVersionText, value);
	}

	/// <summary>
	/// Whether the update badge is on its <see cref="UpdateBadgeVersionText"/> turn. It turns every
	/// few seconds while the badge is red; once the user chooses "Later" (<see cref="VersionIsWarning"/>)
	/// the badge goes amber and stays on "Update available!".
	/// </summary>
	public bool BadgeShowsVersion
	{
		get => _badgeShowsVersion;
		private set => SetProperty(ref _badgeShowsVersion, value);
	}

	/// <summary>e.g. <c>1543Z  ·  Tue 30 Aug</c>.</summary>
	public string ZuluClock
	{
		get => _zuluClock;
		private set => SetProperty(ref _zuluClock, value);
	}

	/// <summary>Worst state across <see cref="SystemHealth"/> - drives the nav dot colour.</summary>
	public StatusKind HealthWorst =>
		SystemHealth.Any(h => h.Kind == StatusKind.Down) ? StatusKind.Down :
		SystemHealth.Any(h => h.Kind == StatusKind.Warn) ? StatusKind.Warn :
		StatusKind.Ok;

	/// <summary>The health popover's heading, e.g. <c>1 needs attention</c>.</summary>
	public string HealthSummary
	{
		get
		{
			var issues = SystemHealth.Count(h => h.Kind is StatusKind.Warn or StatusKind.Down);
			return issues switch
			{
				0 => "All systems nominal",
				1 => "1 needs attention",
				_ => $"{issues} need attention",
			};
		}
	}

	private void OnEnvironmentChanged(object? sender, EventArgs e)
	{
		_dispatcher.BeginInvoke(() =>
		{
			RefreshVersionState();
			RefreshSystemHealth();
		});

		// At idle, so the main window is on screen before the notice opens over it.
		_dispatcher.InvokeAsync(ShowLegacyGitHubTokenNotice, DispatcherPriority.ApplicationIdle);
	}

	/// <summary>
	/// Shows the one-time notice that FE-Buddy 2.x's <c>FEBUDDY_GITHUB_TOKEN</c> environment variable
	/// is still set (<see cref="LegacyGitHubTokenNotice"/>), the first time launch has found it. The
	/// user deletes it in Windows' own Environment Variables window: FE-Buddy never reads its value
	/// and never deletes it.
	/// </summary>
	private static void ShowLegacyGitHubTokenNotice()
	{
		IReadOnlyList<EnvironmentVariableTarget> targets = LegacyGitHubTokenNotice.Take();

		if (targets.Count == 0)
		{
			return;
		}

		Window? owner = Application.Current?.MainWindow is { IsVisible: true } main ? main : null;

		if (ConfirmWindow.Show(owner, "Your FE-Buddy 2.x GitHub token is still on this PC", DescribeLegacyGitHubToken(targets),
			confirmText: "Open Environment Variables", cancelText: "Close"))
		{
			OpenEnvironmentVariables();
		}
	}

	/// <summary>The notice's text: where the variable is set, why it matters and how to delete it.</summary>
	private static string DescribeLegacyGitHubToken(IReadOnlyList<EnvironmentVariableTarget> targets)
	{
		bool forUser = targets.Contains(EnvironmentVariableTarget.User);
		bool forPc = targets.Contains(EnvironmentVariableTarget.Machine);

		string where = forUser && forPc ? "for your Windows account and for everyone on this PC"
			: forPc ? "for everyone on this PC"
			: "for your Windows account";
		string section = forUser && forPc ? "both User variables and System variables"
			: forPc ? "System variables"
			: "User variables";
		string administrator = forPc ? " Deleting a system variable needs an administrator." : string.Empty;

		return
			$"FE-Buddy 2.x kept a GitHub token in the FEBUDDY_GITHUB_TOKEN environment variable, and it is still set {where}. " +
			"FE-Buddy 3 never uses it, and Windows keeps it as plain text that any program you run can read.\n\n" +
			$"To delete it, open Environment Variables, select FEBUDDY_GITHUB_TOKEN under {section} and press Delete.{administrator} " +
			"If you no longer need the token, delete it on GitHub too. To have FE-Buddy use a GitHub token, add it in " +
			"Settings ▸ Credentials, where Windows keeps it encrypted.\n\n" +
			"FE-Buddy has not read the token, and will not show this again.";
	}

	/// <summary>Opens Windows' own Environment Variables window, where the user deletes the variable themselves.</summary>
	private static void OpenEnvironmentVariables()
	{
		try
		{
			Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "rundll32.exe"), "sysdm.cpl,EditEnvironmentVariables")
			{
				UseShellExecute = false,
			});
		}
		catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
		{
			Toast.Warn("Could not open Environment Variables", "Search the Start menu for \"environment variables\" instead.");
		}
	}

	private void OnCycleStateChanged(object? sender, AiracCycleDataCacheEntry e) =>
		_dispatcher.BeginInvoke(() =>
		{
			RefreshAiracStatus();
			RefreshSystemHealth();
		});

	private void RefreshVersionState()
	{
		IsOnline = AppEnvironment.HasInternetConnection;

		VersionCheckResult? version = AppEnvironment.Version;

		if (version is null)
		{
			VersionText = AppEnvironment.LaunchCompleted ? "dev" : "…";
			IsUpdateAvailable = false;
			UpdateTooltipTitle = IsOnline ? "Checking for updates…" : "Update state unknown offline";
			UpdateTooltipBody = string.Empty;
			VersionBrushKey = "Brush.Accent.Text";
			return;
		}

		VersionText = version.CurrentVersion is "0.0.0" or "" ? "dev" : $"v{version.CurrentVersion.TrimStart('v', 'V')}";

		// As in 2.x: a copy the MSI did not install (a dev build, or one run from elsewhere) says so.
		if (!AppEnvironment.IsMsiInstalled && VersionText != "dev")
		{
			VersionText += " - DEV";
		}

		IsUpdateAvailable = version.UpdateAvailable && version.LatestVersion is not null;

		if (!version.CheckSucceeded)
		{
			UpdateTooltipTitle = "Update state unknown offline";
			UpdateTooltipBody = version.Message ?? string.Empty;
			VersionBrushKey = "Brush.Accent.Text";
		}
		else if (IsUpdateAvailable)
		{
			UpdateTooltipTitle = $"v{version.LatestVersion} available!";
			UpdateTooltipBody = $"You are on v{version.CurrentVersion.TrimStart('v', 'V')}. Click to review and update.";
			UpdateBadgeVersionText = $"v{version.LatestVersion} available";
			VersionBrushKey = _updateDeclinedThisSession ? "Brush.Warn" : "Brush.Accent.Text";
		}
		else if (version.RunningPreReleaseChannel is { } running)
		{
			UpdateTooltipTitle = $"Running a {running.DisplayName()} release";
			UpdateTooltipBody = $"Newer than the latest {version.Channel.DisplayName()} release, v{version.LatestVersion}.";
			VersionBrushKey = "Brush.Accent.Text";
		}
		else if (version.IsAheadOfLatestRelease)
		{
			UpdateTooltipTitle = "Running a development build";
			UpdateTooltipBody = version.LatestVersion is not null
				? $"Ahead of the latest published v{version.LatestVersion}."
				: string.Empty;
			VersionBrushKey = "Brush.Accent.Text";
		}
		else
		{
			UpdateTooltipTitle = "You are running the latest version.";
			UpdateTooltipBody = string.Empty;
			VersionBrushKey = "Brush.Accent.Text";
		}
	}

	private void RefreshAiracStatus()
	{
		IReadOnlyList<AiracCycleDataCacheEntry> entries = AiracCycleDataCache.Instance.Entries;

		if (entries.Count == 0)
		{
			AiracStatus = AppEnvironment.LaunchCompleted ? "AIRAC data not loaded" : "Preparing AIRAC data…";
			return;
		}

		AiracCycleDataCacheEntry? downloading = entries.FirstOrDefault(x => x.State == CycleDataState.Downloading);
		if (downloading is not null)
		{
			AiracStatus = $"Downloading cycle {downloading.Cycle.AiracCycleId}…";
			return;
		}

		AiracCycleDataCacheEntry? parsing = entries.FirstOrDefault(x => x.State == CycleDataState.Parsing);
		if (parsing is not null)
		{
			AiracStatus = $"Parsing cycle {parsing.Cycle.AiracCycleId}…";
			return;
		}

		AiracCycleReadiness readiness = AiracCycleDataCache.Instance.ComputeReadiness();
		AiracCycleDataCacheEntry? current = entries.FirstOrDefault(x => x.Position == AiracCyclePosition.Current);

		AiracStatus = readiness switch
		{
			AiracCycleReadiness.Unavailable => "AIRAC data unavailable — current cycle failed",
			AiracCycleReadiness.Waiting => "Waiting for AIRAC data…",
			_ when current is not null =>
				$"AIRAC {current.Cycle.AiracCycleId}  ·  eff {current.Cycle.EffectiveDateUtc:dd MMM yyyy}"
				+ (readiness == AiracCycleReadiness.Degraded ? "  ·  degraded" : string.Empty),
			_ => "Waiting for AIRAC data…",
		};
	}

	private void RefreshSystemHealth()
	{
		SystemHealth.Clear();

		SystemHealth.Add(AppEnvironment.HasInternetConnection
			? new HealthRow("Internet", "connected", StatusKind.Ok)
			: new HealthRow("Internet", "offline — online features are disabled", StatusKind.Down));

		AiracCycleReadiness readiness = AiracCycleDataCache.Instance.Entries.Count == 0
			? AiracCycleReadiness.Waiting
			: AiracCycleDataCache.Instance.ComputeReadiness();

		SystemHealth.Add(readiness switch
		{
			AiracCycleReadiness.Ready => new HealthRow("AIRAC data", "previous / current / next ready", StatusKind.Ok),
			AiracCycleReadiness.Degraded => new HealthRow("AIRAC data", "ready — a best-effort cycle failed", StatusKind.Warn),
			AiracCycleReadiness.Unavailable => new HealthRow("AIRAC data", "current cycle failed to download or parse", StatusKind.Down),
			_ => new HealthRow("AIRAC data", "downloading and parsing…", StatusKind.Warn),
		});

		VersionCheckResult? version = AppEnvironment.Version;
		SystemHealth.Add(version switch
		{
			null => new HealthRow("Updates", "checking…", StatusKind.Warn),
			{ CheckSucceeded: false } => new HealthRow("Updates", "state unknown (offline)", StatusKind.Warn),
			{ UpdateAvailable: true } v => new HealthRow("Updates", $"v{v.LatestVersion} available", StatusKind.Warn),
			{ RunningPreReleaseChannel: { } running } v => new HealthRow("Updates", $"{running.DisplayName()} release — ahead of {v.Channel.DisplayName()}", StatusKind.Ok),
			{ IsAheadOfLatestRelease: true } => new HealthRow("Updates", "dev build — ahead of release", StatusKind.Ok),
			_ => new HealthRow("Updates", "latest version", StatusKind.Ok),
		});

		OnPropertyChanged(nameof(HealthWorst));
		OnPropertyChanged(nameof(HealthSummary));
	}

	private void RecheckNow()
	{
		Toast.Info("Re-checking", "Contacting the time and version services…");
		_ = AppEnvironment.RecheckAsync();
	}

	private void OpenUpdateWindow()
	{
		// An update, or - from Settings, after choosing a more stable channel - a release to go back to.
		VersionCheckResult? version = AppEnvironment.Version;
		if (version is null || !(version.UpdateAvailable || version.CanGoBack))
		{
			return;
		}

		UpdateWindowViewModel vm = new(version, AppEnvironment.IsMsiInstalled, DescribeUnfinishedWork);
		UpdateWindow window = new()
		{
			DataContext = vm,
			Owner = Application.Current?.MainWindow,
		};

		window.ShowDialog();

		if (vm.InstallerStarted)
		{
			// The MSI cannot replace FE-Buddy's files while it runs; it relaunches FE-Buddy at the end.
			Application.Current?.Shutdown();
			return;
		}

		// Declining to go back leaves no update waiting, so nothing to colour.
		if (vm.UserDeclined && !vm.IsGoingBack)
		{
			_updateDeclinedThisSession = true;
			RefreshVersionState();
		}
	}

	// What closing FE-Buddy for an update would lose: a run in progress and unsaved edits on any
	// page opened this session. Pages never opened have nothing to lose.
	private IReadOnlyList<string> DescribeUnfinishedWork()
	{
		var work = new List<string>();

		foreach (NavItem item in PrimaryNav.Concat(SystemNav))
		{
			switch (item.CreatedViewModel)
			{
				case TabbedServiceViewModel service:
					if (service.IsRunning)
					{
						work.Add($"A {service.ScreenTitle} run is in progress.");
					}

					string[] dirty = [.. service.Tabs.Where(t => t.IsDirty).Select(t => t.Title)];
					if (dirty.Length > 0)
					{
						work.Add($"{service.ScreenTitle}: {string.Join(", ", dirty)} {(dirty.Length == 1 ? "has" : "have")} unsaved changes.");
					}

					break;

				case SettingsViewModel settings when settings.IsDirty:
					work.Add("Settings has unsaved changes.");
					break;
			}
		}

		return work;
	}

	private void UpdateZulu()
		=> ZuluClock = DateTime.UtcNow.ToString("HHmm'Z'  ·  ddd dd MMM", CultureInfo.InvariantCulture);

	/// <summary>One second on: the update badge changes line every <see cref="BadgeTurnSeconds"/> while it is red.</summary>
	private void AdvanceUpdateBadge()
	{
		if (!IsUpdateAvailable || VersionIsWarning)
		{
			_badgeSeconds = 0;
			BadgeShowsVersion = false;
			return;
		}

		if (++_badgeSeconds % BadgeTurnSeconds == 0)
		{
			BadgeShowsVersion = !BadgeShowsVersion;
		}
	}

	private NavItem Nav(string title, string glyph, Func<object> factory)
		=> new(title, glyph, factory, OnActivated);

	private void OnActivated(NavItem item)
	{
		foreach (var other in PrimaryNav.Concat(SystemNav))
		{
			if (!ReferenceEquals(other, item))
			{
				other.IsActive = false;
			}
		}

		Current = item.ViewModel;
	}
}
