using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Application.Airac.VnasAlias.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One custom alias file on the vNAS Alias Upload tab: a file on this PC, or a web address with the
/// saved credential to download it with.
/// </summary>
/// <remarks>
/// The row holds only a credential's id - the secret never leaves <c>CredentialStore</c>. Its
/// commands call back into the tab, which owns the list, the dialogs and the save contract.
/// </remarks>
public sealed class AliasSourceRow : ObservableObject
{
	private readonly VnasAliasViewModel _owner;

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

	/// <summary>Creates a row.</summary>
	/// <param name="owner">The tab the row belongs to.</param>
	/// <param name="kind">A file on this PC, or a web address.</param>
	/// <param name="location">The file's path, or the web address.</param>
	/// <param name="credentialId">The credential to download it with; <see cref="Guid.Empty"/> for none.</param>
	internal AliasSourceRow(VnasAliasViewModel owner, AliasSourceKind kind, string location, Guid credentialId)
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
		: "e.g. https://github.com/vZOB/facility/blob/main/ZOB-Alias.txt";

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

	/// <summary>The row as the library sees it.</summary>
	/// <returns>The custom alias file.</returns>
	public AliasSource ToSource() =>
		new(Number, Kind, Location.Trim(), IsUrl && CredentialId != Guid.Empty ? CredentialId : null);

	/// <summary>Shows what a <b>Check</b> found.</summary>
	/// <param name="succeeded">Whether the file was read.</param>
	/// <param name="message">What to show.</param>
	internal void SetCheck(bool succeeded, string message)
	{
		CheckSucceeded = succeeded;
		CheckMessage = message;
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
	}
}

/// <summary>One entry in a custom alias file's credential drop-down.</summary>
/// <param name="Id">The credential's id; <see cref="Guid.Empty"/> for "no credential".</param>
/// <param name="Name">The credential's name.</param>
/// <param name="Label">What the drop-down shows, e.g. <c>ZOB GitHub (GitHub personal access token)</c>.</param>
public sealed record CredentialChoice(Guid Id, string Name, string Label)
{
	/// <summary>Download without a credential.</summary>
	public static CredentialChoice None { get; } = new(Guid.Empty, "None", "None - the file is public");
}
