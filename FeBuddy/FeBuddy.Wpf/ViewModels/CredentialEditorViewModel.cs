using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs <c>CredentialEditorWindow</c>: adding a credential, or editing one. A saved secret is never
/// shown again - the secret box starts empty, and leaving it empty keeps the saved one.
/// </summary>
public sealed class CredentialEditorViewModel : ObservableObject
{
	private readonly CredentialStore _store;
	private readonly CredentialInfo? _existing;

	private string _name;
	private CredentialKindOption _selectedKind;
	private string _userName;
	private string _hostsText;
	private string? _error;

	/// <summary>Opens the editor.</summary>
	/// <param name="store">Where the credential is saved.</param>
	/// <param name="existing">The credential to edit, or <see langword="null"/> to add one.</param>
	public CredentialEditorViewModel(CredentialStore store, CredentialInfo? existing)
	{
		_store = store;
		_existing = existing;

		Kinds = [.. Enum.GetValues<CredentialKind>().Select(k => new CredentialKindOption(k))];

		_name = existing?.Name ?? string.Empty;
		_selectedKind = Kinds.First(k => k.Kind == (existing?.Kind ?? CredentialKind.GitHubToken));
		_userName = existing?.UserName ?? string.Empty;
		_hostsText = string.Join(", ", existing?.Hosts ?? DefaultHosts(_selectedKind.Kind));

		SaveCommand = new RelayCommand(Save);
		CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
		CreateGitHubTokenCommand = new RelayCommand(() => BrowserLauncher.Open(Links.GitHubNewToken));
	}

	/// <summary>Raised when the editor should close: saved, or cancelled.</summary>
	public event EventHandler? CloseRequested;

	/// <summary>The saved credential, once saved; <see langword="null"/> when cancelled.</summary>
	public CredentialInfo? Result { get; private set; }

	/// <summary>The window's title.</summary>
	public string Title => _existing is null ? "Add Credential" : "Edit Credential";

	/// <summary>The kinds to choose from.</summary>
	public IReadOnlyList<CredentialKindOption> Kinds { get; }

	/// <summary>The name the user gives the credential, e.g. <c>ZOB GitHub</c>.</summary>
	public string Name
	{
		get => _name;
		set => SetProperty(ref _name, value);
	}

	/// <summary>What the credential is.</summary>
	public CredentialKindOption SelectedKind
	{
		get => _selectedKind;
		set
		{
			CredentialKind before = _selectedKind.Kind;
			if (!SetProperty(ref _selectedKind, value))
			{
				return;
			}

			// Swap the suggested websites along with the kind, but never throw away ones the user typed.
			if (string.IsNullOrWhiteSpace(HostsText) || HostsText == string.Join(", ", DefaultHosts(before)))
			{
				HostsText = string.Join(", ", DefaultHosts(value.Kind));
			}

			foreach (string name in new[]
			{
				nameof(IsUsernamePassword), nameof(IsGitHubToken), nameof(SecretLabel), nameof(SecretHint),
			})
			{
				OnPropertyChanged(name);
			}
		}
	}

	/// <summary>Whether the kind needs a user name.</summary>
	public bool IsUsernamePassword => SelectedKind.Kind == CredentialKind.UsernamePassword;

	/// <summary>Whether the kind is a GitHub token, which shows the GitHub guidance and the FE-Buddy option.</summary>
	public bool IsGitHubToken => SelectedKind.Kind == CredentialKind.GitHubToken;

	/// <summary>The user name, for a user name and password.</summary>
	public string UserName
	{
		get => _userName;
		set => SetProperty(ref _userName, value);
	}

	/// <summary>
	/// The password or token being typed. Set by the window from its password box, never bound
	/// back to it, and cleared when the editor closes.
	/// </summary>
	public string Secret { get; set; } = string.Empty;

	/// <summary>The secret box's label, e.g. <c>Password</c>.</summary>
	public string SecretLabel => SelectedKind.Kind switch
	{
		CredentialKind.UsernamePassword => "Password",
		CredentialKind.GitHubToken => "Personal access token",
		_ => "Token or API key",
	};

	/// <summary>The line under the secret box.</summary>
	public string SecretHint => _existing is null
		? $"Once saved, the {SelectedKind.Kind.SecretName()} is never shown again."
		: $"Leave empty to keep the saved {SelectedKind.Kind.SecretName()}. It is never shown again.";

	/// <summary>The websites the credential may be sent to, as the user types them.</summary>
	public string HostsText
	{
		get => _hostsText;
		set => SetProperty(ref _hostsText, value);
	}

	/// <summary>Why the credential cannot be saved yet; <see langword="null"/> while there is nothing wrong.</summary>
	public string? Error
	{
		get => _error;
		private set
		{
			if (SetProperty(ref _error, value))
			{
				OnPropertyChanged(nameof(HasError));
			}
		}
	}

	/// <summary>Whether <see cref="Error"/> is shown.</summary>
	public bool HasError => Error is not null;

	/// <summary>Explains the websites box.</summary>
	public const string HostsDescription =
		"FE-Buddy only ever sends this credential to these websites, over HTTPS - never anywhere else, even if a " +
		"setting points there. Each one covers its subdomains too: github.com covers api.github.com.";

	/// <summary>How to make a GitHub token that can do as little harm as possible.</summary>
	public const string GitHubTokenAdvice =
		"Use a fine-grained token with read-only Contents access to just the repositories FE-Buddy needs, and give it " +
		"an expiry date. FE-Buddy never needs to write to GitHub.";

	/// <summary>Saves and closes, or shows why it cannot.</summary>
	public ICommand SaveCommand { get; }

	/// <summary>Closes without saving.</summary>
	public ICommand CancelCommand { get; }

	/// <summary>Opens GitHub's page for creating a fine-grained token.</summary>
	public ICommand CreateGitHubTokenCommand { get; }

	private void Save()
	{
		if (!CredentialHosts.TryParse(HostsText, out IReadOnlyList<string> hosts, out string? hostError))
		{
			Error = hostError;
			return;
		}

		CredentialDraft draft = new(
			_existing?.Id, Name, SelectedKind.Kind, UserName, Secret, hosts);

		try
		{
			if (_store.Validate(draft) is { } error)
			{
				Error = error;
				return;
			}

			Result = _store.Save(draft);
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
		{
			Error = $"Windows Credential Manager could not save it: {ex.Message}";
			return;
		}

		CloseRequested?.Invoke(this, EventArgs.Empty);
	}

	private static IReadOnlyList<string> DefaultHosts(CredentialKind kind) =>
		kind == CredentialKind.GitHubToken ? CredentialHosts.GitHubDefaults : [];

	/// <summary>One kind in the drop-down.</summary>
	/// <param name="Kind">The kind.</param>
	public sealed record CredentialKindOption(CredentialKind Kind)
	{
		/// <summary>The kind's name.</summary>
		public string Name => Kind.DisplayName();

		/// <summary>The kind's name, which is what the themed drop-down shows for the chosen item.</summary>
		/// <returns><see cref="Name"/>.</returns>
		public override string ToString() => Name;
	}
}
