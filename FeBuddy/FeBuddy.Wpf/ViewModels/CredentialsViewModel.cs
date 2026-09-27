using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.GitHub.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// SYSTEM ▸ Settings ▸ Credentials: the user's saved credentials (<see cref="CredentialStore"/>).
/// Unlike the rest of Settings, every change here is saved at once - credentials live in Windows
/// Credential Manager, not <c>UserConfig.json</c>, so they have nothing to do with the page's Save.
/// </summary>
public sealed class CredentialsViewModel : ObservableObject
{
	private const string LogSource = "Credentials";

	private readonly CredentialStore _store;
	private readonly Dispatcher _dispatcher;
	private LegacyGitHubToken? _legacyToken;
	private string? _loadError;
	private bool _isBusy;

	/// <summary>Builds the card over <paramref name="store"/> and lists what it holds.</summary>
	/// <param name="store">The credentials.</param>
	public CredentialsViewModel(CredentialStore store)
	{
		_store = store;
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		AddCommand = new RelayCommand(Add, () => !IsBusy);
		EditCommand = new RelayCommand<CredentialRow>(Edit, _ => !IsBusy);
		RemoveCommand = new RelayCommand<CredentialRow>(Remove, _ => !IsBusy);
		CheckCommand = new RelayCommand<CredentialRow>(row => _ = CheckAsync(row), row => row is { CanCheck: true, IsChecking: false });
		RemoveAllCommand = new RelayCommand(RemoveAll, () => !IsBusy && HasCredentials);
		MoveLegacyTokenCommand = new RelayCommand(() => _ = MoveLegacyTokenAsync(), () => !IsBusy);

		// Kyle's features, or another window, may add or remove credentials too.
		_store.Changed += (_, _) => _dispatcher.BeginInvoke(Refresh);

		Refresh();
	}

	/// <summary>Explains the card under its heading.</summary>
	public const string Description =
		"Sign-ins FE-Buddy uses to download from protected websites, such as a private GitHub repository. They are " +
		"kept in Windows Credential Manager, encrypted with your Windows sign-in - never in UserConfig.json or a " +
		"settings export - and a saved password or token is never shown again. Changes here are saved straight away.";

	/// <summary>The saved credentials, by name.</summary>
	public ObservableCollection<CredentialRow> Rows { get; } = [];

	/// <summary>Whether any credential is saved.</summary>
	public bool HasCredentials => Rows.Count > 0;

	/// <summary>Why the credentials could not be listed, when Credential Manager failed; otherwise <see langword="null"/>.</summary>
	public string? LoadError
	{
		get => _loadError;
		private set
		{
			if (SetProperty(ref _loadError, value))
			{
				OnPropertyChanged(nameof(HasLoadError));
			}
		}
	}

	/// <summary>Whether <see cref="LoadError"/> is shown.</summary>
	public bool HasLoadError => LoadError is not null;

	/// <summary>Whether the old <c>FEBUDDY_GITHUB_TOKEN</c> environment variable holds a token that can be moved here.</summary>
	public bool HasLegacyToken => _legacyToken is not null;

	/// <summary>The notice about the old environment variable.</summary>
	public string LegacyTokenMessage =>
		$"A GitHub token is set in the {GitHubAuth.LegacyEnvironmentVariableName} environment variable. FE-Buddy no longer " +
		"reads it: Windows keeps environment variables as plain text that every program can read. Move it here to keep " +
		"using it for FE-Buddy's GitHub requests.";

	/// <summary>Whether an action that talks to Credential Manager is running.</summary>
	public bool IsBusy
	{
		get => _isBusy;
		private set
		{
			if (SetProperty(ref _isBusy, value))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Opens the editor for a new credential.</summary>
	public ICommand AddCommand { get; }

	/// <summary>Opens the editor for a saved credential. The parameter is its <see cref="CredentialRow"/>.</summary>
	public ICommand EditCommand { get; }

	/// <summary>Removes a saved credential, once confirmed. The parameter is its <see cref="CredentialRow"/>.</summary>
	public ICommand RemoveCommand { get; }

	/// <summary>Asks GitHub whether a saved GitHub token works. The parameter is its <see cref="CredentialRow"/>.</summary>
	public ICommand CheckCommand { get; }

	/// <summary>Removes every FE-Buddy credential from this PC, once confirmed.</summary>
	public ICommand RemoveAllCommand { get; }

	/// <summary>Moves the token in <c>FEBUDDY_GITHUB_TOKEN</c> into a credential and removes the variable.</summary>
	public ICommand MoveLegacyTokenCommand { get; }

	private static Window? Owner => Application.Current?.MainWindow;

	private void Refresh()
	{
		IReadOnlyList<CredentialInfo> credentials;
		try
		{
			credentials = _store.List();
			LoadError = null;
		}
		catch (Win32Exception ex)
		{
			AppLog.Warning(LogSource, $"Could not read Windows Credential Manager: {ex.Message}");
			credentials = [];
			LoadError = $"Windows Credential Manager could not be read: {ex.Message}";
		}

		Rows.Clear();
		foreach (CredentialInfo credential in credentials)
		{
			Rows.Add(new CredentialRow(credential));
		}

		// A machine-wide variable left behind after a move needs an administrator; once a token is
		// in use here, stop asking about it.
		LegacyGitHubToken? legacy = GitHubAuth.FindLegacyToken();
		bool movedAlready = legacy is { IsMachineWide: true } && credentials.Any(c => c.UseForFeBuddyGitHub);
		_legacyToken = movedAlready ? null : legacy;

		OnPropertyChanged(nameof(HasCredentials));
		OnPropertyChanged(nameof(HasLegacyToken));
		CommandManager.InvalidateRequerySuggested();
	}

	private void Add()
	{
		if (CredentialEditorWindow.Edit(Owner, _store, null) is { } saved)
		{
			Toast.Success("Credential saved", $"{saved.Name} is saved in Windows Credential Manager.");
		}
	}

	private void Edit(CredentialRow? row)
	{
		if (row is not null && CredentialEditorWindow.Edit(Owner, _store, row.Info) is { } saved)
		{
			Toast.Success("Credential saved", $"{saved.Name} is updated.");
		}
	}

	private void Remove(CredentialRow? row)
	{
		if (row is null
			|| !ConfirmWindow.Show(
				Owner,
				"Remove credential",
				$"Remove {row.Name} from this PC? Anything set to use it will need another credential.",
				confirmText: "Remove"))
		{
			return;
		}

		Run(() =>
		{
			_store.Delete(row.Info.Id);
			Toast.Info("Credential removed", $"{row.Name} is no longer on this PC.");
		});
	}

	private void RemoveAll()
	{
		if (!ConfirmWindow.Show(
			Owner,
			"Remove all credentials",
			$"Remove all {Rows.Count} FE-Buddy credentials from this PC? Anything set to use them will need new ones.",
			confirmText: "Remove all"))
		{
			return;
		}

		Run(() =>
		{
			int removed = _store.DeleteAll();
			Toast.Info("Credentials removed", $"{removed} credential(s) removed from this PC.");
		});
	}

	private async Task CheckAsync(CredentialRow? row)
	{
		if (row is null)
		{
			return;
		}

		row.IsChecking = true;
		CommandManager.InvalidateRequerySuggested();

		try
		{
			CredentialCheck check = await GitHubAuth.CheckTokenAsync(row.Info.Id);

			if (check.Succeeded)
			{
				Toast.Success($"{row.Name} works", check.Message);
			}
			else
			{
				Toast.Warn($"{row.Name} did not work", check.Message);
			}
		}
		catch (Win32Exception ex)
		{
			Toast.Error("Could not check the token", ex.Message);
		}
		finally
		{
			row.IsChecking = false;
			CommandManager.InvalidateRequerySuggested();
		}
	}

	private async Task MoveLegacyTokenAsync()
	{
		if (!ConfirmWindow.Show(
			Owner,
			"Move the GitHub token",
			$"Move the token in {GitHubAuth.LegacyEnvironmentVariableName} into a GitHub credential, used for FE-Buddy's " +
			"update checks, News and update downloads? The variable is then removed from your Windows user settings, " +
			"so FE-Buddy 2.x will no longer see it either.",
			confirmText: "Move it"))
		{
			return;
		}

		IsBusy = true;
		try
		{
			// Removing a user variable notifies every open window, which can take a few seconds.
			LegacyTokenMove? move = await Task.Run(GitHubAuth.MoveLegacyToken);

			if (move is null)
			{
				Toast.Info("Nothing to move", $"{GitHubAuth.LegacyEnvironmentVariableName} is no longer set.");
			}
			else if (move.MachineVariableRemains)
			{
				Toast.Warn(
					"Token moved",
					$"It is now the credential {move.Credential.Name}. A copy of {GitHubAuth.LegacyEnvironmentVariableName} is set for " +
					"every user of this PC; an administrator can remove it in System Properties ▸ Environment Variables.");
			}
			else
			{
				Toast.Success("Token moved", $"It is now the credential {move.Credential.Name}, and the environment variable is gone.");
			}
		}
		catch (Exception ex) when (ex is Win32Exception or ArgumentException or System.Security.SecurityException)
		{
			AppLog.Warning(LogSource, $"Could not move the GitHub token: {ex.Message}");
			Toast.Error("Could not move the token", ex.Message);
		}
		finally
		{
			IsBusy = false;
			Refresh();
		}
	}

	private void Run(Action action)
	{
		try
		{
			action();
		}
		catch (Win32Exception ex)
		{
			AppLog.Warning(LogSource, $"Windows Credential Manager failed: {ex.Message}");
			Toast.Error("Windows Credential Manager failed", ex.Message);
		}
	}

	/// <summary>One saved credential in the card.</summary>
	/// <param name="info">The credential.</param>
	public sealed class CredentialRow(CredentialInfo info) : ObservableObject
	{
		private bool _isChecking;

		/// <summary>The credential.</summary>
		public CredentialInfo Info { get; } = info;

		/// <summary>Its name.</summary>
		public string Name => Info.Name;

		/// <summary>Its kind, user name and websites on one line.</summary>
		public string Details =>
			string.Join("  ·  ", new[]
			{
				Info.Kind.DisplayName(),
				Info.UserName is { Length: > 0 } user ? $"user {user}" : null,
				$"only for {string.Join(", ", Info.Hosts)}",
			}.Where(part => part is not null));

		/// <summary>Whether FE-Buddy's own GitHub requests use it.</summary>
		public bool IsUsedByFeBuddy => Info.UseForFeBuddyGitHub;

		/// <summary>Whether it can be checked with GitHub.</summary>
		public bool CanCheck => Info.Kind == CredentialKind.GitHubToken;

		/// <summary>Whether a check is running.</summary>
		public bool IsChecking
		{
			get => _isChecking;
			set
			{
				if (SetProperty(ref _isChecking, value))
				{
					OnPropertyChanged(nameof(CheckLabel));
				}
			}
		}

		/// <summary>The check button's label.</summary>
		public string CheckLabel => IsChecking ? "Checking…" : "Check";
	}
}
