using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using FeBuddy.Installer.UI.Setup;
using FeBuddy.Installer.UI.ViewModels;
using FeBuddy.Installer.UI.Views;

using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.UI;

/// <summary>
/// FE-Buddy's setup window as the MSI's embedded UI: Windows Installer calls
/// <see cref="Initialize"/> before it starts, <see cref="ProcessMessage"/> for everything it has to
/// say while it runs, and <see cref="Shutdown"/> at the end. MakeSfxCA finds this class by its
/// interface (there must be exactly one) and DTF's SfxCA stub loads it inside msiexec.exe.
/// </summary>
/// <remarks>
/// <para>
/// The window runs on its own STA thread. <see cref="Initialize"/> blocks until the user starts
/// the install (or leaves), then hands the choices to the session - the session handle is valid
/// only during that call - and tells Windows Installer to show none of its own dialogs. Messages
/// arrive on Windows Installer's thread: progress is folded in here and posted to the window,
/// questions are asked on the window's thread and waited for.
/// </para>
/// <para>
/// A basic-UI run (<c>/qb</c>, <c>/passive</c>; Package.wxs sets SupportBasicUI) gets the same
/// window showing progress only, with no questions but Windows Installer's own. A silent run
/// (<c>/qn</c>) never loads this. If this fails to load, DTF's host carries on with Windows
/// Installer's basic progress UI; on a PC whose policy disables embedded UIs, Windows Installer
/// shows the package's stock dialogs (FeBuddy.UI.wxs) instead.
/// </para>
/// <para>
/// If the window itself fails to open (its theme or the window can't be built), <see cref="Initialize"/>
/// logs why and returns <see langword="false"/>, so Windows Installer carries on with its own UI at the
/// level asked for: the stock dialogs, or its basic progress. Should the window fail later, while the
/// install runs, the install goes on without it rather than ending msiexec.
/// </para>
/// </remarks>
public sealed class SetupUI : IEmbeddedUI
{
	private readonly ManualResetEvent _started = new(false);
	private readonly ManualResetEvent _left = new(false);
	private readonly ManualResetEvent _windowReady = new(false);
	private readonly ProgressTracker _progress = new();

	private Thread? _uiThread;
	private Dispatcher? _dispatcher;
	private SetupViewModel? _viewModel;
	private SetupWindow? _window;
	private SetupChoices? _choices;

	private bool _cancelSent;
	private bool _fatalExit;

	// Set on the window's thread when the window fails; read once its events are set.
	private volatile bool _windowFailed;
	private Exception? _windowFailure;
	private int _installResult = SetupOutcomeRules.ResultNotSent;
	private string? _lastAction;
	private int _updatePosted;

	/// <inheritdoc />
	public bool Initialize(Session session, string resourcePath, ref InstallUIOptions internalUILevel)
	{
		InstallUIOptions level = (InstallUIOptions)((int)internalUILevel & 0xF);
		bool full = level == InstallUIOptions.Full;
		bool basic = level is InstallUIOptions.Basic or InstallUIOptions.Reduced;
		if (!full && !basic)
		{
			return false;
		}

		bool canCancel = ((int)internalUILevel & (int)InstallUIOptions.HideCancel) == 0;
		SetupContext context = SetupContext.Read(session, progressOnly: !full, canCancel);
		session.Log($"FE-BUDDY setup window: level={level}, mode={context.Mode}, version='{context.Version}', " +
			$"installed='{context.InstalledVersion}', folder='{context.InstallFolder}'");

		PrepareForWpf();
		// A background thread: whatever happens to the window, it never keeps msiexec from exiting.
		_uiThread = new Thread(() => RunWindow(context)) { Name = "FE-BUDDY setup window", IsBackground = true };
		_uiThread.SetApartmentState(ApartmentState.STA);
		_uiThread.Start();

		if (context.ProgressOnly)
		{
			// Nothing to ask: install with whatever the command line set, keeping the recorded
			// shortcut choices on an update the way the full UI does.
			_windowReady.WaitOne();
			if (_windowFailed)
			{
				return HandBack(session);
			}

			_choices = new SetupChoices(
				context.Mode == SetupMode.Uninstall ? SetupOperation.Remove : SetupOperation.Install,
				context.InstallFolder, context.StartMenuShortcut, context.DesktopShortcut);
			if (context.Mode is SetupMode.Update or SetupMode.Rollback)
			{
				_choices.ApplyTo(session, context.Mode);
			}
		}
		else if (WaitHandle.WaitAny([_started, _left]) == 1)
		{
			if (_windowFailed)
			{
				return HandBack(session);
			}

			session.Log("FE-BUDDY setup window: closed before installing.");
			throw new InstallCanceledException();
		}
		else
		{
			_choices!.ApplyTo(session, context.Mode);
		}

		session.Log($"FE-BUDDY setup window: starting {_choices.Operation} (start menu={_choices.StartMenuShortcut}, desktop={_choices.DesktopShortcut}).");

		// Windows Installer shows none of its dialogs now; it still resolves a missing source itself.
		internalUILevel = InstallUIOptions.NoChange | InstallUIOptions.SourceResolutionOnly;
		return true;
	}

	/// <summary>
	/// The window couldn't open: says why in the install log, and declines, so Windows Installer
	/// carries on with its own UI.
	/// </summary>
	private bool HandBack(Session session)
	{
		session.Log($"FE-BUDDY setup window: could not open, so Windows Installer's own UI is used instead. {_windowFailure}");
		return false;
	}

	/// <inheritdoc />
	public MessageResult ProcessMessage(
		InstallMessage messageType,
		Record messageRecord,
		MessageButtons buttons,
		MessageIcon icon,
		MessageDefaultButton defaultButton)
	{
		// A window that failed can't be asked anything: its thread has ended.
		if (_windowFailed || _viewModel is null || _dispatcher is null)
		{
			return MessageResult.None;
		}

		switch (messageType)
		{
			case InstallMessage.InstallStart:
				_dispatcher.BeginInvoke(new Action(_viewModel.BeginInstall));
				return MessageResult.OK;

			case InstallMessage.ActionStart:
				_progress.OnActionStart();
				_lastAction = messageRecord.FieldCount >= 2 ? messageRecord.GetString(2) : null;
				return Progressed();

			case InstallMessage.ActionData:
				_progress.OnActionData();
				return Progressed();

			case InstallMessage.Progress:
				_progress.OnProgress(
					Int(messageRecord, 1), Int(messageRecord, 2), Int(messageRecord, 3), Int(messageRecord, 4));
				return Progressed();

			case InstallMessage.CommonData:
				// Field 1 = 2: show (field 2 = 1) or hide Cancel. Windows Installer hides it once
				// the changes can no longer be stopped.
				if (Int(messageRecord, 1) == 2)
				{
					bool allowed = Int(messageRecord, 2) == 1;
					_dispatcher.BeginInvoke(new Action(() => _viewModel.SetCancelAllowed(allowed)));
				}

				return MessageResult.OK;

			case InstallMessage.Error:
			case InstallMessage.Warning:
			case InstallMessage.User:
			case InstallMessage.OutOfDiskSpace:
				return Ask(InstallerPrompts.ForMessage(messageType, messageRecord.ToString(), buttons, icon), messageType);

			case InstallMessage.FilesInUse:
				return Ask(InstallerPrompts.ForFilesInUse(InstallerPrompts.Applications(messageRecord), restartManager: false), messageType);

			case InstallMessage.RMFilesInUse:
				return Ask(InstallerPrompts.ForFilesInUse(InstallerPrompts.Applications(messageRecord), restartManager: true), messageType);

			case InstallMessage.FatalExit:
				_fatalExit = true;
				string fatal = messageRecord.ToString();
				_dispatcher.Invoke(() => _viewModel.AddError(fatal));
				return MessageResult.OK;

			case InstallMessage.InstallEnd:
				// Fields: product name, product code, and how the installation ended - an action
				// result, as an MSI log's "Return value": 1 succeeded, 2 cancelled, 3 failed. Not an
				// error code such as 1603.
				_installResult = messageRecord.FieldCount >= 3 && !messageRecord.IsNull(3)
					? messageRecord.GetInteger(3)
					: SetupOutcomeRules.ResultNotSent;
				return MessageResult.OK;

			case InstallMessage.Initialize:
			case InstallMessage.Terminate:
				return MessageResult.OK;

			default:
				// Info (the log's lines), ShowDialog, ResolveSource...: Windows Installer's business.
				return MessageResult.None;
		}
	}

	/// <inheritdoc />
	public void Shutdown()
	{
		if (_windowFailed || _viewModel is null || _dispatcher is null)
		{
			return;
		}

		SetupContext context = _viewModel.Context;
		SetupOutcome outcome = SetupOutcomeRules.Decide(
			_cancelSent,
			_fatalExit,
			_installResult,
			removing: _choices?.Operation == SetupOperation.Remove,
			context.InstalledVersion,
			context.Version,
			() => InstalledFeBuddy.Read().Version);

		_dispatcher.Invoke(() =>
		{
			_viewModel.Finish(outcome);

			// A basic-UI run ends with Windows Installer: nothing waits for the user.
			if (_viewModel.Context.ProgressOnly)
			{
				_window?.CloseNow();
			}
		});

		// The finish page stays up until the user closes it.
		_uiThread?.Join();
	}

	// ------------------------------------------------------------------ window thread

	private void RunWindow(SetupContext context)
	{
		try
		{
			Application app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
			app.Resources = (ResourceDictionary)Application.LoadComponent(
				new Uri("/FeBuddy.Installer.UI;component/Theme/Setup.xaml", UriKind.Relative));

			_dispatcher = Dispatcher.CurrentDispatcher;
			_viewModel = new SetupViewModel(context);
			_viewModel.StartRequested += choices =>
			{
				_choices = choices;
				_started.Set();
			};

			_window = new SetupWindow(_viewModel);
			_window.Loaded += (_, _) => _windowReady.Set();
			_window.Closed += (_, _) => _left.Set();
			app.Run(_window);
		}
		catch (Exception ex)
		{
			// Uncaught, this would end msiexec from a background thread. Initialize reads it, if it's
			// still waiting, and hands the install back to Windows Installer's own UI.
			_windowFailure = ex;
			_windowFailed = true;
		}
		finally
		{
			_windowReady.Set();
			_left.Set();
		}
	}

	// ------------------------------------------------------------------ messages

	private MessageResult Progressed()
	{
		if (_viewModel!.TakeCancelRequest())
		{
			_cancelSent = true;
			return MessageResult.Cancel;
		}

		// Many progress messages arrive per second; the window takes the latest, once per frame.
		if (Interlocked.Exchange(ref _updatePosted, 1) == 0)
		{
			_dispatcher!.BeginInvoke(DispatcherPriority.Render, new Action(() =>
			{
				Interlocked.Exchange(ref _updatePosted, 0);
				_viewModel.ReportProgress(_progress.Fraction, _progress.IsRollingBack);
				_viewModel.ReportAction(_lastAction);
			}));
		}

		return MessageResult.OK;
	}

	private MessageResult Ask(PromptRequest request, InstallMessage messageType)
	{
		MessageResult answer = (MessageResult)_dispatcher!.Invoke(new Func<object>(() =>
		{
			if (messageType is InstallMessage.Error or InstallMessage.OutOfDiskSpace)
			{
				_viewModel!.AddError(request.Message);
			}

			return _viewModel!.AskUser(request);
		}));

		if (answer is MessageResult.Cancel or MessageResult.Abort)
		{
			_cancelSent = true;
		}

		return answer;
	}

	private static int Int(Record record, int field) =>
		record.FieldCount >= field && !record.IsNull(field) ? record.GetInteger(field) : 0;

	// ------------------------------------------------------------------ WPF in msiexec

	/// <summary>
	/// Sharp text at any display scaling: msiexec.exe declares no DPI awareness, so a WPF window in
	/// it is drawn at 96 DPI and stretched. Asks for per-monitor awareness first - for the process
	/// if nothing has set it yet, else for this thread - and lets WPF follow the window between
	/// monitors (it does not for an AppDomain with no target framework, which is what SfxCA makes).
	/// </summary>
	private static void PrepareForWpf()
	{
		AppContext.SetSwitch("Switch.System.Windows.DoNotScaleForDpiChanges", false);

		try
		{
			if (!SetProcessDpiAwarenessContext(DpiAwarenessPerMonitorV2))
			{
				SetThreadDpiAwarenessContext(DpiAwarenessPerMonitorV2);
			}
		}
		catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
		{
			// Before Windows 10 1703: the window is drawn at system DPI instead.
		}
	}

	private static readonly IntPtr DpiAwarenessPerMonitorV2 = new(-4);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

	[DllImport("user32.dll")]
	private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr value);
}
