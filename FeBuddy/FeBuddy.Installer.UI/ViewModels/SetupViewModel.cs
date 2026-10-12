using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;

using FeBuddy.Installer.UI.Setup;
using FeBuddy.Versioning;

using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.UI.ViewModels;

/// <summary>
/// The setup window: which page is showing, what each page says for this run's
/// <see cref="SetupMode"/>, and the install's progress and outcome.
/// </summary>
/// <remarks>
/// It knows nothing of the installer session. <see cref="SetupUI"/> listens for
/// <see cref="StartRequested"/> to start Windows Installer, and while it runs calls the
/// install-time members (<see cref="BeginInstall"/>, <see cref="ReportProgress"/>,
/// <see cref="AskUser"/>, <see cref="Finish"/>...) on the window's thread. The window supplies the
/// two things only a view can do: <see cref="ShowPrompt"/> and <see cref="PickFolder"/>.
/// </remarks>
public sealed class SetupViewModel : ObservableObject
{
	private const string Product = SetupContext.ProductName;

	private readonly List<string> _errors = [];
	private IReadOnlyList<LicenseParagraph>? _license;

	private SetupPage _page;
	private SetupOperation _operation;
	private bool _repairChosen = true;
	private string _installFolder;
	private string? _folderError;
	private bool _startMenuShortcut;
	private bool _desktopShortcut;
	private double _progress;
	private string _progressDetail = "Getting ready…";
	private bool _isRollingBack;
	private bool _cancelAllowed;
	private int _cancelRequested;
	private bool _isCancelling;
	private SetupOutcome _outcome;
	private bool _launchApp = true;

	/// <summary>Creates the view-model for one run.</summary>
	/// <param name="context">What the run is.</param>
	public SetupViewModel(SetupContext context)
	{
		Context = context;
		_installFolder = context.InstallFolder;
		_startMenuShortcut = context.StartMenuShortcut;
		_desktopShortcut = context.DesktopShortcut;
		_cancelAllowed = context.CanCancel;
		_operation = context.Mode == SetupMode.Uninstall ? SetupOperation.Remove : SetupOperation.Install;

		Steps = new ObservableCollection<StepItem>(BuildSteps());

		PrimaryCommand = new RelayCommand(OnPrimary, () => Page != SetupPage.Progress);
		BackCommand = new RelayCommand(OnBack);
		CancelCommand = new RelayCommand(OnCancel, () => Page != SetupPage.Progress || (CancelAllowed && !IsCancelling));
		BrowseCommand = new RelayCommand(OnBrowse);
		OpenLogCommand = new RelayCommand(OnOpenLog, () => Context.LogFile is not null);

		Page = context.ProgressOnly ? SetupPage.Progress : SetupPage.Welcome;
		UpdateSteps();
	}

	// ------------------------------------------------------------------ hooks

	/// <summary>The user started the install; the host hands the choices to Windows Installer.</summary>
	public event Action<SetupChoices>? StartRequested;

	/// <summary>The window should close (the user left before installing, or finished).</summary>
	public event Action? CloseRequested;

	/// <summary>Shows a prompt over the window and returns the choice. Set by the window.</summary>
	public Func<PromptRequest, MessageResult>? ShowPrompt { get; set; }

	/// <summary>Shows the folder picker, starting in the given folder. Set by the window.</summary>
	public Func<string?, string?>? PickFolder { get; set; }

	// ------------------------------------------------------------------ run

	/// <summary>What the run is.</summary>
	public SetupContext Context { get; }

	/// <summary>This MSI's version, e.g. <c>3.0.0-beta.9</c>.</summary>
	public string Version => Context.Version;

	/// <summary>The caption chip: <c>v3.0.0-beta.9</c>.</summary>
	public string VersionChip => "v" + Context.Version;

	/// <summary>Whether this MSI is a pre-release.</summary>
	public bool IsPrerelease => IsPre(Context.Version);

	/// <summary>Whether the installed copy is a pre-release.</summary>
	public bool InstalledIsPrerelease => IsPre(Context.InstalledVersion);

	/// <summary>The installed version, for the from → to card.</summary>
	public string InstalledVersion => Context.InstalledVersion ?? "the installed version";

	/// <summary>The rail's steps.</summary>
	public ObservableCollection<StepItem> Steps { get; }

	/// <summary>The page showing.</summary>
	public SetupPage Page
	{
		get => _page;
		private set
		{
			if (Set(ref _page, value))
			{
				OnPropertyChanged(nameof(IsWelcomePage));
				OnPropertyChanged(nameof(IsLicensePage));
				OnPropertyChanged(nameof(IsOptionsPage));
				OnPropertyChanged(nameof(IsProgressPage));
				OnPropertyChanged(nameof(IsFinishPage));
				OnPropertyChanged(nameof(License));
				RaiseFooter();
				UpdateSteps();
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Page visibility, for the view.</summary>
	public bool IsWelcomePage => Page == SetupPage.Welcome;

	/// <inheritdoc cref="IsWelcomePage" />
	public bool IsLicensePage => Page == SetupPage.License;

	/// <inheritdoc cref="IsWelcomePage" />
	public bool IsOptionsPage => Page == SetupPage.Options;

	/// <inheritdoc cref="IsWelcomePage" />
	public bool IsProgressPage => Page == SetupPage.Progress;

	/// <inheritdoc cref="IsWelcomePage" />
	public bool IsFinishPage => Page == SetupPage.Finish;

	// ------------------------------------------------------------------ welcome

	/// <summary>Which welcome variant shows.</summary>
	public bool IsInstallWelcome => Context.Mode == SetupMode.Install;

	/// <inheritdoc cref="IsInstallWelcome" />
	public bool IsChangeWelcome => Context.Mode is SetupMode.Update or SetupMode.Rollback;

	/// <inheritdoc cref="IsInstallWelcome" />
	public bool IsBlockedWelcome => Context.Mode == SetupMode.Blocked;

	/// <inheritdoc cref="IsInstallWelcome" />
	public bool IsMaintenanceWelcome => Context.Mode == SetupMode.Maintenance;

	/// <inheritdoc cref="IsInstallWelcome" />
	public bool IsUninstallWelcome => Context.Mode == SetupMode.Uninstall;

	/// <summary>The welcome page's heading.</summary>
	public string WelcomeHeading => Context.Mode switch
	{
		SetupMode.Install => $"Welcome to {Product}",
		SetupMode.Update when IsSameVersion => $"Reinstall {Product} {Version}",
		SetupMode.Update => $"Update {Product}",
		SetupMode.Rollback => $"Go back to {Product} {Version}",
		SetupMode.Blocked => "This version can't be installed",
		SetupMode.Maintenance => $"{Product} {Version} is installed",
		_ => $"Uninstall {Product}",
	};

	/// <summary>The welcome page's text under the heading.</summary>
	public string WelcomeBody => Context.Mode switch
	{
		SetupMode.Install =>
			$"{Product} assists VATSIM ARTCC Facility Engineers with their routine, tedious and sometimes complex tasks. " +
			"Every AIRAC cycle it turns the FAA's new data into the GeoJSON video maps and alias files your facility needs.",
		SetupMode.Update when IsSameVersion =>
			$"{Product} {Version} is already installed from a different build. Setup replaces it with this one. " +
			"Your settings, profiles and downloaded data are kept.",
		SetupMode.Update =>
			$"A new version of {Product} is ready. Setup replaces the installed copy in place - " +
			"your settings, profiles and downloaded data are kept.",
		SetupMode.Rollback =>
			$"You're on the pre-release {InstalledVersion}. Setup replaces it with {Version}, an earlier release - " +
			"your settings, profiles and downloaded data are kept.",
		SetupMode.Blocked =>
			$"{Product} {InstalledVersion} is installed. It's a newer stable release than {Version}, and a stable " +
			"release never goes back to an older one. Nothing has been changed.",
		SetupMode.Maintenance => "This version is already on this PC. What would you like to do?",
		_ => $"Setup removes {Product} {InstalledVersion} from this PC.",
	};

	/// <summary>A note under the welcome page's card.</summary>
	public string? WelcomeNote => Context.Mode switch
	{
		SetupMode.Install => $"Setup installs {Product} {Version}. It takes about a minute.",
		SetupMode.Update or SetupMode.Rollback =>
			$"{Product} has to be closed while it updates - Setup offers to close it if it's open.",
		SetupMode.Blocked =>
			$"{Product} keeps itself up to date: carry on with the version you have. To install {Version} anyway, uninstall {Product} first.",
		_ => null,
	};

	/// <summary>An update to the very version installed (a different build of it).</summary>
	public bool IsSameVersion =>
		ProductVersion.TryParse(Context.InstalledVersion, out ProductVersion? installed)
		&& ProductVersion.TryParse(Context.Version, out ProductVersion? candidate)
		&& candidate!.ComparePrecedenceTo(installed!) == 0;

	/// <summary>Maintenance: Repair is chosen (else Uninstall).</summary>
	public bool RepairChosen
	{
		get => _repairChosen;
		set
		{
			if (Set(ref _repairChosen, value))
			{
				OnPropertyChanged(nameof(RemoveChosen));
				RaiseFooter();
				UpdateSteps();
			}
		}
	}

	/// <summary>Maintenance: Uninstall is chosen.</summary>
	public bool RemoveChosen
	{
		get => !_repairChosen;
		set => RepairChosen = !value;
	}

	// ------------------------------------------------------------------ license

	/// <summary>The license, as paragraphs; loaded the first time the page shows.</summary>
	public IReadOnlyList<LicenseParagraph> License =>
		Page == SetupPage.License ? _license ??= LicenseText.Load() : [];

	// ------------------------------------------------------------------ options

	/// <summary>The install folder.</summary>
	public string InstallFolder
	{
		get => _installFolder;
		set
		{
			if (Set(ref _installFolder, value))
			{
				FolderError = null;
			}
		}
	}

	/// <summary>Why the folder can't be used, or <see langword="null"/>.</summary>
	public string? FolderError
	{
		get => _folderError;
		private set
		{
			if (Set(ref _folderError, value))
			{
				OnPropertyChanged(nameof(HasFolderError));
			}
		}
	}

	/// <summary>Whether <see cref="FolderError"/> shows.</summary>
	public bool HasFolderError => FolderError is not null;

	/// <summary>Create the Start Menu shortcut.</summary>
	public bool StartMenuShortcut
	{
		get => _startMenuShortcut;
		set => Set(ref _startMenuShortcut, value);
	}

	/// <summary>Create the Desktop shortcut.</summary>
	public bool DesktopShortcut
	{
		get => _desktopShortcut;
		set => Set(ref _desktopShortcut, value);
	}

	// ------------------------------------------------------------------ progress

	/// <summary>The progress page's heading.</summary>
	public string ProgressHeading => IsRollingBack
		? "Undoing changes…"
		: _operation switch
		{
			SetupOperation.Remove => $"Uninstalling {Product}…",
			SetupOperation.Repair => $"Repairing {Product}…",
			_ => Context.Mode switch
			{
				SetupMode.Update when IsSameVersion => $"Reinstalling {Product}…",
				SetupMode.Update => $"Updating {Product}…",
				_ => $"Installing {Product}…",
			},
		};

	/// <summary>The progress page's text under the heading.</summary>
	public string ProgressBody => IsRollingBack
		? "Setup stopped, and is putting everything back the way it was."
		: _operation == SetupOperation.Remove
			? "This takes a few seconds."
			: "This usually takes under a minute.";

	/// <summary>Progress, 0-100.</summary>
	public double ProgressPercent
	{
		get => _progress;
		private set
		{
			if (Set(ref _progress, value))
			{
				OnPropertyChanged(nameof(ProgressPercentText));
			}
		}
	}

	/// <summary>Progress as text: <c>42%</c>.</summary>
	public string ProgressPercentText => $"{Math.Round(ProgressPercent):0}%";

	/// <summary>What Windows Installer is doing ("Copying new files").</summary>
	public string ProgressDetail
	{
		get => _progressDetail;
		private set => Set(ref _progressDetail, value);
	}

	/// <summary>Windows Installer is undoing its changes.</summary>
	public bool IsRollingBack
	{
		get => _isRollingBack;
		private set
		{
			if (Set(ref _isRollingBack, value))
			{
				OnPropertyChanged(nameof(ProgressHeading));
				OnPropertyChanged(nameof(ProgressBody));
			}
		}
	}

	/// <summary>Windows Installer allows Cancel now (it hides it near the end).</summary>
	public bool CancelAllowed
	{
		get => _cancelAllowed;
		private set
		{
			if (Set(ref _cancelAllowed, value))
			{
				RaiseFooter();
			}
		}
	}

	/// <summary>The user asked to cancel, and it has not been passed on yet.</summary>
	public bool IsCancelling
	{
		get => _isCancelling;
		private set
		{
			if (Set(ref _isCancelling, value))
			{
				RaiseFooter();
			}
		}
	}

	// ------------------------------------------------------------------ finish

	/// <summary>How it ended.</summary>
	public SetupOutcome Outcome
	{
		get => _outcome;
		private set => Set(ref _outcome, value);
	}

	/// <summary>The finish page variant.</summary>
	public bool Succeeded => Outcome == SetupOutcome.Succeeded;

	/// <inheritdoc cref="Succeeded" />
	public bool WasCancelled => Outcome == SetupOutcome.Cancelled;

	/// <inheritdoc cref="Succeeded" />
	public bool Failed => Outcome == SetupOutcome.Failed;

	/// <summary>The finish page's heading.</summary>
	public string FinishHeading => Outcome switch
	{
		SetupOutcome.Cancelled => "Setup was cancelled",
		SetupOutcome.Failed => "Setup couldn't finish",
		_ => _operation switch
		{
			SetupOperation.Remove => $"{Product} is uninstalled",
			SetupOperation.Repair => $"{Product} is repaired",
			_ => Context.Mode switch
			{
				SetupMode.Update when !IsSameVersion => $"{Product} is up to date",
				SetupMode.Rollback => $"{Product} {Version} is installed",
				_ => $"{Product} is installed",
			},
		},
	};

	/// <summary>The finish page's text.</summary>
	public string FinishBody => Outcome switch
	{
		SetupOutcome.Cancelled => _operation == SetupOperation.Remove || Context.Mode != SetupMode.Install
			? $"Nothing was changed - {Product} is still installed, just as it was."
			: "Nothing was changed - everything Setup had done has been undone.",
		SetupOutcome.Failed =>
			$"Windows Installer stopped and undid its changes, so {Product} is as it was before. What it reported is below.",
		_ => _operation switch
		{
			SetupOperation.Remove => $"{Product}, its settings and its saved credentials have been removed from this PC. Thanks for using {Product}.",
			SetupOperation.Repair => $"Every {Product} file is back as it should be. Your settings were not touched.",
			_ => Context.Mode switch
			{
				SetupMode.Update when !IsSameVersion =>
					$"Updated from {InstalledVersion} to {Version}. Your settings and profiles are just as you left them.",
				SetupMode.Rollback =>
					$"Replaced the pre-release {InstalledVersion} with {Version}. Your settings and profiles are kept.",
				_ => $"{Product} {Version} is ready to use. {WhereToFindIt()}",
			},
		},
	};

	/// <summary>What Windows Installer reported, for a failed run.</summary>
	public string ErrorDetails => _errors.Count == 0
		? "Windows Installer didn't say why."
		: string.Join(Environment.NewLine + Environment.NewLine, _errors);

	/// <summary>Whether the finish page offers "Launch FE-BUDDY".</summary>
	public bool ShowLaunch => Succeeded && _operation != SetupOperation.Remove;

	/// <summary>Launch FE-Buddy when the window closes.</summary>
	public bool LaunchApp
	{
		get => _launchApp;
		set => Set(ref _launchApp, value);
	}

	/// <summary>Whether the run is being logged (a log path to show on failure).</summary>
	public bool HasLogFile => Context.LogFile is not null;

	/// <summary>The log's path.</summary>
	public string? LogFile => Context.LogFile;

	/// <summary>Whether the install has ended.</summary>
	public bool IsFinished => Page == SetupPage.Finish;

	// ------------------------------------------------------------------ footer

	/// <summary>The main button's text.</summary>
	public string PrimaryText => Page switch
	{
		SetupPage.Welcome => Context.Mode switch
		{
			SetupMode.Install => "Next",
			SetupMode.Update when IsSameVersion => "Reinstall",
			SetupMode.Update => "Update",
			SetupMode.Rollback => $"Install {Version}",
			SetupMode.Blocked => "Close",
			SetupMode.Maintenance => RepairChosen ? "Repair" : "Uninstall",
			_ => "Uninstall",
		},
		SetupPage.License => "Next",
		SetupPage.Options => "Install",
		SetupPage.Finish => Succeeded ? "Finish" : "Close",
		_ => string.Empty,
	};

	/// <summary>The main button removes FE-Buddy (it turns red).</summary>
	public bool PrimaryIsDanger => Page == SetupPage.Welcome
		&& (Context.Mode == SetupMode.Uninstall || (Context.Mode == SetupMode.Maintenance && RemoveChosen));

	/// <summary>The main button shows (not while installing).</summary>
	public bool ShowPrimary => Page != SetupPage.Progress;

	/// <summary>Back shows (license and options pages).</summary>
	public bool ShowBack => Page is SetupPage.License or SetupPage.Options;

	/// <summary>Cancel shows: before installing (not on a page whose main button is Close), and while installing if allowed.</summary>
	public bool ShowCancel => Page switch
	{
		SetupPage.Progress => CancelAllowed && !Context.ProgressOnly,
		SetupPage.Finish => false,
		_ => Context.Mode != SetupMode.Blocked,
	};

	/// <summary>The Cancel button's text.</summary>
	public string CancelText => IsCancelling ? "Cancelling…" : "Cancel";

	/// <summary>The main button.</summary>
	public ICommand PrimaryCommand { get; }

	/// <summary>Back.</summary>
	public ICommand BackCommand { get; }

	/// <summary>Cancel: leave before installing, or stop the install.</summary>
	public ICommand CancelCommand { get; }

	/// <summary>Opens the folder picker.</summary>
	public ICommand BrowseCommand { get; }

	/// <summary>Opens the install log.</summary>
	public ICommand OpenLogCommand { get; }

	// ------------------------------------------------------------------ install-time (host)

	/// <summary>Windows Installer has started: show the progress page.</summary>
	public void BeginInstall() => Page = SetupPage.Progress;

	/// <summary>Moves the bar.</summary>
	/// <param name="fraction">0 to 1.</param>
	/// <param name="rollingBack">Windows Installer is undoing its changes.</param>
	public void ReportProgress(double fraction, bool rollingBack)
	{
		ProgressPercent = Math.Max(0, Math.Min(100, fraction * 100));
		IsRollingBack = rollingBack;
	}

	/// <summary>Shows what Windows Installer is doing.</summary>
	/// <param name="description">The action's description ("Copying new files").</param>
	public void ReportAction(string? description)
	{
		if (!string.IsNullOrWhiteSpace(description))
		{
			ProgressDetail = description!.Trim().TrimEnd('.');
		}
	}

	/// <summary>Windows Installer shows or hides Cancel (it hides it once the changes can't be stopped).</summary>
	/// <param name="allowed">Whether Cancel may show.</param>
	public void SetCancelAllowed(bool allowed) => CancelAllowed = allowed && Context.CanCancel;

	/// <summary>
	/// Whether to answer Windows Installer's next message with Cancel. True once per request: after
	/// that, Windows Installer is rolling back and every message is answered normally.
	/// </summary>
	/// <returns><see langword="true"/> when the user asked to cancel.</returns>
	/// <remarks>Called on Windows Installer's thread, hence the interlocked flag.</remarks>
	public bool TakeCancelRequest() => Interlocked.Exchange(ref _cancelRequested, 0) == 1;

	/// <summary>Keeps an error Windows Installer reported, for the finish page.</summary>
	/// <param name="text">The message.</param>
	public void AddError(string? text)
	{
		if (!string.IsNullOrWhiteSpace(text) && !_errors.Contains(text!.Trim()))
		{
			_errors.Add(text.Trim());
			OnPropertyChanged(nameof(ErrorDetails));
		}
	}

	/// <summary>Asks the user (an error, a warning, files in use) and returns the answer.</summary>
	/// <param name="request">The prompt.</param>
	/// <returns>The answer, or the prompt's close result when the window can't ask.</returns>
	public MessageResult AskUser(PromptRequest request) =>
		ShowPrompt?.Invoke(request) ?? request.CloseResult;

	/// <summary>The install ended: show the finish page.</summary>
	/// <param name="outcome">How it ended.</param>
	public void Finish(SetupOutcome outcome)
	{
		Outcome = outcome;
		if (outcome == SetupOutcome.Succeeded)
		{
			ProgressPercent = 100;
		}

		IsCancelling = false;
		foreach (string name in new[]
		{
			nameof(Succeeded), nameof(WasCancelled), nameof(Failed), nameof(FinishHeading),
			nameof(FinishBody), nameof(ShowLaunch), nameof(ErrorDetails),
		})
		{
			OnPropertyChanged(name);
		}

		Page = SetupPage.Finish;
	}

	/// <summary>
	/// The window is closing (the X): leave before installing, ask to cancel while installing.
	/// </summary>
	/// <returns><see langword="true"/> when the window may close now.</returns>
	public bool OnWindowClosing()
	{
		switch (Page)
		{
			case SetupPage.Progress:
				if (CancelAllowed && !Context.ProgressOnly)
				{
					RequestCancel();
				}

				return false;

			case SetupPage.Finish:
				LaunchIfChosen();
				return true;

			default:
				return true;
		}
	}

	// ------------------------------------------------------------------ actions

	private void OnPrimary()
	{
		switch (Page)
		{
			case SetupPage.Welcome when Context.Mode == SetupMode.Install:
				Page = SetupPage.License;
				break;

			case SetupPage.Welcome when Context.Mode == SetupMode.Blocked:
				CloseRequested?.Invoke();
				break;

			case SetupPage.Welcome:
				Start(Context.Mode switch
				{
					SetupMode.Uninstall => SetupOperation.Remove,
					SetupMode.Maintenance => RepairChosen ? SetupOperation.Repair : SetupOperation.Remove,
					_ => SetupOperation.Install,
				});
				break;

			case SetupPage.License:
				Page = SetupPage.Options;
				break;

			case SetupPage.Options:
				if (ValidateFolder())
				{
					Start(SetupOperation.Install);
				}

				break;

			case SetupPage.Finish:
				LaunchIfChosen();
				CloseRequested?.Invoke();
				break;
		}
	}

	private void OnBack()
	{
		if (Page == SetupPage.Options)
		{
			Page = SetupPage.License;
		}
		else if (Page == SetupPage.License)
		{
			Page = SetupPage.Welcome;
		}
	}

	private void OnCancel()
	{
		if (Page == SetupPage.Progress)
		{
			RequestCancel();
		}
		else
		{
			CloseRequested?.Invoke();
		}
	}

	private void RequestCancel()
	{
		if (IsCancelling)
		{
			return;
		}

		MessageResult answer = AskUser(new PromptRequest(
			PromptKind.Warning,
			"Cancel setup?",
			$"Setup stops and undoes what it has done so far, leaving {Product} as it was.",
			[],
			[
				new PromptButton("Keep going", MessageResult.No),
				new PromptButton("Cancel setup", MessageResult.Yes, IsPrimary: true, IsDanger: true),
			],
			MessageResult.No));

		// Windows Installer may have moved on while the prompt was open.
		if (answer == MessageResult.Yes && Page == SetupPage.Progress && CancelAllowed)
		{
			Interlocked.Exchange(ref _cancelRequested, 1);
			IsCancelling = true;
		}
	}

	private void OnBrowse()
	{
		string? picked = PickFolder?.Invoke(InstallFolder);
		if (!string.IsNullOrWhiteSpace(picked))
		{
			InstallFolder = picked!.EndsWith(@"\", StringComparison.Ordinal) ? picked : picked + @"\";
		}
	}

	private void OnOpenLog()
	{
		try
		{
			Process.Start(new ProcessStartInfo(Context.LogFile!) { UseShellExecute = true });
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException or InvalidOperationException)
		{
			// Nothing to open: the path is on the page for the user to find.
		}
	}

	private void Start(SetupOperation operation)
	{
		_operation = operation;
		OnPropertyChanged(nameof(ProgressHeading));
		OnPropertyChanged(nameof(ProgressBody));

		Page = SetupPage.Progress;
		StartRequested?.Invoke(new SetupChoices(operation, InstallFolder, StartMenuShortcut, DesktopShortcut));
	}

	private bool ValidateFolder()
	{
		string folder = InstallFolder.Trim();
		if (folder.Length == 0)
		{
			FolderError = "Choose a folder to install to.";
			return false;
		}

		try
		{
			if (!Path.IsPathRooted(folder) || folder.StartsWith(@"\\", StringComparison.Ordinal)
				|| folder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
			{
				FolderError = @"Use a full folder path on this PC, like C:\Program Files\FE-BUDDY\.";
				return false;
			}

			string full = Path.GetFullPath(folder);
			InstallFolder = full.EndsWith(@"\", StringComparison.Ordinal) ? full : full + @"\";
			return true;
		}
		catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
		{
			FolderError = "That isn't a folder path Windows can use.";
			return false;
		}
	}

	private void LaunchIfChosen()
	{
		if (!ShowLaunch || !LaunchApp || Context.ProgressOnly)
		{
			return;
		}

		// The installed folder as the package recorded it - the chosen one on a fresh install, the
		// kept one on an update.
		string? exe = InstalledFeBuddy.Read().ExePath;
		if (exe is null || !File.Exists(exe))
		{
			return;
		}

		try
		{
			Process.Start(new ProcessStartInfo(exe)
			{
				UseShellExecute = true,
				WorkingDirectory = Path.GetDirectoryName(exe),
			});
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
		{
			// FE-Buddy is installed; the user can start it from the Start menu.
		}
	}

	// ------------------------------------------------------------------ helpers

	private IEnumerable<StepItem> BuildSteps()
	{
		string action = Context.Mode switch
		{
			SetupMode.Update when IsSameVersion => "Reinstall",
			SetupMode.Update => "Update",
			SetupMode.Maintenance => "Repair",
			SetupMode.Uninstall => "Uninstall",
			_ => "Install",
		};

		yield return new StepItem(SetupPage.Welcome, 1, Context.Mode == SetupMode.Uninstall ? "Confirm" : "Welcome");
		if (Context.Mode == SetupMode.Install)
		{
			yield return new StepItem(SetupPage.License, 2, "License");
			yield return new StepItem(SetupPage.Options, 3, "Options");
		}

		int next = Context.Mode == SetupMode.Install ? 4 : 2;
		yield return new StepItem(SetupPage.Progress, next, action);
		yield return new StepItem(SetupPage.Finish, next + 1, "Finish");
	}

	private void UpdateSteps()
	{
		StepItem? progressStep = Steps.FirstOrDefault(s => s.Page == SetupPage.Progress);
		if (progressStep is not null && Context.Mode == SetupMode.Maintenance)
		{
			// The maintenance step follows the choice: Repair or Uninstall.
			int index = Steps.IndexOf(progressStep);
			string title = RepairChosen ? "Repair" : "Uninstall";
			if (progressStep.Title != title)
			{
				Steps[index] = new StepItem(SetupPage.Progress, progressStep.Number, title);
			}
		}

		foreach (StepItem step in Steps)
		{
			step.State = step.Page == Page ? "Current" : step.Page < Page ? "Done" : "Upcoming";
		}
	}

	private void RaiseFooter()
	{
		foreach (string name in new[]
		{
			nameof(PrimaryText), nameof(PrimaryIsDanger), nameof(ShowPrimary), nameof(ShowBack),
			nameof(ShowCancel), nameof(CancelText),
		})
		{
			OnPropertyChanged(name);
		}
	}

	private string WhereToFindIt() => (StartMenuShortcut, DesktopShortcut) switch
	{
		(true, true) => "You'll find it in the Start menu and on your desktop.",
		(true, false) => "You'll find it in the Start menu.",
		(false, true) => "You'll find it on your desktop.",
		_ => $"It's in {InstallFolder}",
	};

	private static bool IsPre(string? version) =>
		ProductVersion.TryParse(version, out ProductVersion? parsed) && parsed!.IsPrerelease;
}
