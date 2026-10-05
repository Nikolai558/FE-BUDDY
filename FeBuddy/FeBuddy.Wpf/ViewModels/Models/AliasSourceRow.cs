using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.GitHub;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One custom alias file on the Concatenate Aliases tab: a file on this PC, or a web address with the
/// saved credential to download it with.
/// </summary>
/// <remarks>
/// <para>
/// The row holds only a credential's id - the secret never leaves <c>CredentialStore</c>. Its
/// commands call back into the tab, which owns the list, the dialogs and the save contract.
/// </para>
/// <para>
/// A GitHub address is shown as the file's Raw link (<see cref="TidyLocation"/>), whatever form it
/// was pasted in. When <b>Check</b> finds GitHub refused or hid the file, the row asks whether the
/// repository is private and points to the GitHub token guide - or, with a credential chosen, to the
/// guide's troubleshooting (<see cref="Troubleshooting"/>).
/// </para>
/// </remarks>
public sealed class AliasSourceRow : ObservableObject
{
	private readonly ConcatenateAliasesViewModel _owner;

	private int _number;
	private string _location;
	private Guid _credentialId;
	private string? _error;
	private string? _notice;
	private string? _checkMessage;
	private bool _checkSucceeded;
	private bool _isChecking;
	private CredentialChoice? _suggestion;
	private string? _suggestionSource;
	private AliasTroubleshooting _troubleshooting;

	/// <summary>Creates a row.</summary>
	/// <param name="owner">The tab the row belongs to.</param>
	/// <param name="kind">A file on this PC, or a web address.</param>
	/// <param name="location">The file's path, or the web address.</param>
	/// <param name="credentialId">The credential to download it with; <see cref="Guid.Empty"/> for none.</param>
	internal AliasSourceRow(ConcatenateAliasesViewModel owner, AliasSourceKind kind, string location, Guid credentialId)
	{
		_owner = owner;
		Kind = kind;
		_location = location;
		_credentialId = credentialId;

		BrowseCommand = new RelayCommand(() => _owner.Browse(this));
		CheckCommand = new RelayCommand(async () => await _owner.CheckAsync(this), () => !IsChecking);
		NewCredentialCommand = new RelayCommand(() => _owner.NewCredential(this));
		UseSuggestionCommand = new RelayCommand(() => CredentialId = Suggestion?.Id ?? CredentialId);
		MoveUpCommand = new RelayCommand(() => _owner.Move(this, -1), () => Number > 1);
		MoveDownCommand = new RelayCommand(() => _owner.Move(this, +1), () => Number < _owner.Sources.Count);
		RemoveCommand = new RelayCommand(() => _owner.Remove(this));
		AnswerPrivateCommand = new RelayCommand(() => Troubleshooting = AliasTroubleshooting.Private);
		AnswerPublicCommand = new RelayCommand(() => Troubleshooting = AliasTroubleshooting.Public);
		OpenTokenGuideCommand = new RelayCommand(() => BrowserLauncher.Open(Links.GitHubTokenGuide));
		OpenTokenTroubleshootingCommand = new RelayCommand(() => BrowserLauncher.Open(Links.GitHubTokenGuideTroubleshooting));
	}

	/// <summary>Whether it is a file on this PC or a web address.</summary>
	public AliasSourceKind Kind { get; }

	/// <summary>Whether it is a file on this PC.</summary>
	public bool IsFile => Kind == AliasSourceKind.File;

	/// <summary>Whether it is a web address.</summary>
	public bool IsUrl => Kind == AliasSourceKind.Url;

	/// <summary>Its place in the merge, from 1.</summary>
	public int Number
	{
		get => _number;
		set
		{
			if (SetProperty(ref _number, value))
			{
				OnPropertyChanged(nameof(Header));
			}
		}
	}

	/// <summary>The row's heading, e.g. <c>1 · File on this PC</c>.</summary>
	public string Header => $"{Number} · {(IsFile ? "File on this PC" : "Web address")}";

	/// <summary>What the location box asks for when it is empty.</summary>
	public string Placeholder => IsFile
		? @"e.g. C:\Users\me\Documents\ZOB-Alias.txt"
		: "e.g. https://github.com/vZOB/facility/raw/refs/heads/main/ZOB-Alias.txt";

	/// <summary>The file's full path, or the web address.</summary>
	public string Location
	{
		get => _location;
		set
		{
			if (SetProperty(ref _location, value ?? string.Empty))
			{
				ClearCheck();
				_owner.RowChanged();
			}
		}
	}

	/// <summary>The saved credential to download it with; <see cref="Guid.Empty"/> for none.</summary>
	public Guid CredentialId
	{
		get => _credentialId;
		set
		{
			if (SetProperty(ref _credentialId, value))
			{
				ClearCheck();
				_owner.RowChanged();
			}
		}
	}

	/// <summary>Why the row cannot be saved, or <see langword="null"/>.</summary>
	public string? Error
	{
		get => _error;
		internal set
		{
			if (SetProperty(ref _error, value))
			{
				OnPropertyChanged(nameof(HasError));
				OnPropertyChanged(nameof(HasNotice));
			}
		}
	}

	/// <summary>Whether <see cref="Error"/> is set.</summary>
	public bool HasError => Error is not null;

	/// <summary>
	/// Something that will make the file fail to read but does not stop a save - the file is not on
	/// this PC, or the credential is not - or <see langword="null"/>.
	/// </summary>
	public string? Notice
	{
		get => _notice;
		internal set
		{
			if (SetProperty(ref _notice, value))
			{
				OnPropertyChanged(nameof(HasNotice));
			}
		}
	}

	/// <summary>Whether <see cref="Notice"/> is set, and there is no <see cref="Error"/> to show instead.</summary>
	public bool HasNotice => Notice is not null && !HasError;

	/// <summary>What the last <b>Check</b> found, or <see langword="null"/> before one (or after an edit).</summary>
	public string? CheckMessage
	{
		get => _checkMessage;
		private set
		{
			if (SetProperty(ref _checkMessage, value))
			{
				OnPropertyChanged(nameof(HasCheckMessage));
				OnPropertyChanged(nameof(CheckFailed));
			}
		}
	}

	/// <summary>Whether there is a <see cref="CheckMessage"/> to show.</summary>
	public bool HasCheckMessage => CheckMessage is not null;

	/// <summary>Whether the last <b>Check</b> read the file.</summary>
	public bool CheckSucceeded
	{
		get => _checkSucceeded;
		private set
		{
			if (SetProperty(ref _checkSucceeded, value))
			{
				OnPropertyChanged(nameof(CheckFailed));
			}
		}
	}

	/// <summary>Whether the last <b>Check</b> could not read the file.</summary>
	public bool CheckFailed => HasCheckMessage && !CheckSucceeded;

	/// <summary>Whether a <b>Check</b> is running.</summary>
	public bool IsChecking
	{
		get => _isChecking;
		internal set
		{
			if (SetProperty(ref _isChecking, value))
			{
				OnPropertyChanged(nameof(CheckLabel));
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>The <b>Check</b> button's text.</summary>
	public string CheckLabel => IsChecking ? "Checking…" : "Check";

	/// <summary>
	/// What the row offers after GitHub refused or hid the file on <b>Check</b>; cleared by any edit
	/// and by the next check.
	/// </summary>
	public AliasTroubleshooting Troubleshooting
	{
		get => _troubleshooting;
		internal set
		{
			if (SetProperty(ref _troubleshooting, value))
			{
				OnPropertyChanged(nameof(AsksIfPrivate));
				OnPropertyChanged(nameof(ShowsPrivateHelp));
				OnPropertyChanged(nameof(ShowsPublicHelp));
				OnPropertyChanged(nameof(ShowsCredentialHelp));
			}
		}
	}

	/// <summary>Whether the row is asking if the repository is private.</summary>
	public bool AsksIfPrivate => Troubleshooting == AliasTroubleshooting.AskIfPrivate;

	/// <summary>Whether the row shows how to read a private repository: with a GitHub token.</summary>
	public bool ShowsPrivateHelp => Troubleshooting == AliasTroubleshooting.Private;

	/// <summary>Whether the row shows what to check in the address of a public repository's file.</summary>
	public bool ShowsPublicHelp => Troubleshooting == AliasTroubleshooting.Public;

	/// <summary>Whether the row shows what to check about the credential GitHub refused.</summary>
	public bool ShowsCredentialHelp => Troubleshooting == AliasTroubleshooting.CredentialRefused;

	/// <summary>
	/// A credential an earlier web address already uses that may be sent to this one's website too,
	/// offered while this one has none - "the same token as file 1".
	/// </summary>
	public CredentialChoice? Suggestion => _suggestion;

	/// <summary>Whether there is a <see cref="Suggestion"/> to offer.</summary>
	public bool HasSuggestion => _suggestion is not null;

	/// <summary>The suggestion button's text, e.g. <c>Use ZOB GitHub, like file 1</c>.</summary>
	public string SuggestionLabel => _suggestion is null ? string.Empty : $"Use {_suggestion.Name}, like {_suggestionSource}";

	/// <summary>Picks a file for a file row.</summary>
	public ICommand BrowseCommand { get; }

	/// <summary>Reads the file now, to show whether the run will be able to.</summary>
	public ICommand CheckCommand { get; }

	/// <summary>Adds a credential in the credential editor, and chooses it for this row.</summary>
	public ICommand NewCredentialCommand { get; }

	/// <summary>Chooses <see cref="Suggestion"/>.</summary>
	public ICommand UseSuggestionCommand { get; }

	/// <summary>Merges this file one place earlier.</summary>
	public ICommand MoveUpCommand { get; }

	/// <summary>Merges this file one place later.</summary>
	public ICommand MoveDownCommand { get; }

	/// <summary>Takes this file off the list.</summary>
	public ICommand RemoveCommand { get; }

	/// <summary>Answers "yes, the repository is private".</summary>
	public ICommand AnswerPrivateCommand { get; }

	/// <summary>Answers "no, the repository is public".</summary>
	public ICommand AnswerPublicCommand { get; }

	/// <summary>Opens the GitHub token guide.</summary>
	public ICommand OpenTokenGuideCommand { get; }

	/// <summary>Opens the GitHub token guide's "If something goes wrong" section.</summary>
	public ICommand OpenTokenTroubleshootingCommand { get; }

	/// <summary>The row as the library sees it.</summary>
	/// <returns>The custom alias file.</returns>
	public AliasSource ToSource() =>
		new(Number, Kind, Location.Trim(), IsUrl && CredentialId != Guid.Empty ? CredentialId : null);

	/// <summary>
	/// Shows a GitHub address as the file's Raw link, the form GitHub's Raw button gives
	/// (<c>…/raw/refs/heads/main/…</c>) - for a file's page (<c>…/blob/main/…</c>), say. Leaves any
	/// other address alone, and one with a secret in it, so the row can say why that can't be saved.
	/// </summary>
	internal void TidyLocation()
	{
		string location = Location.Trim();

		if (IsUrl
			&& Uri.TryCreate(location, UriKind.Absolute, out Uri? url)
			&& UrlSecrets.Describe(url) is null
			&& GitHubFileUrl.ToRawLink(url) is { } raw
			&& raw.AbsoluteUri != location)
		{
			Location = raw.AbsoluteUri;
		}
	}

	/// <summary>Shows what a <b>Check</b> found.</summary>
	/// <param name="succeeded">Whether the file was read.</param>
	/// <param name="message">What to show.</param>
	/// <param name="troubleshooting">What to offer next, after GitHub refused or hid the file.</param>
	internal void SetCheck(bool succeeded, string message, AliasTroubleshooting troubleshooting = AliasTroubleshooting.None)
	{
		CheckSucceeded = succeeded;
		CheckMessage = message;
		Troubleshooting = troubleshooting;
	}

	/// <summary>Offers a credential another row uses, or stops offering one.</summary>
	/// <param name="suggestion">The credential, or <see langword="null"/>.</param>
	/// <param name="source">The row it comes from, e.g. <c>file 1</c>.</param>
	internal void SetSuggestion(CredentialChoice? suggestion, string? source)
	{
		if (Equals(_suggestion, suggestion) && _suggestionSource == source)
		{
			return;
		}

		_suggestion = suggestion;
		_suggestionSource = source;
		OnPropertyChanged(nameof(Suggestion));
		OnPropertyChanged(nameof(HasSuggestion));
		OnPropertyChanged(nameof(SuggestionLabel));
	}

	private void ClearCheck()
	{
		CheckMessage = null;
		CheckSucceeded = false;
		Troubleshooting = AliasTroubleshooting.None;
	}
}

