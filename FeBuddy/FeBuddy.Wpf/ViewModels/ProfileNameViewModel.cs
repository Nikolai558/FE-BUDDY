using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs <c>ProfileNameWindow</c>, which asks for a settings profile's name: for a new profile
/// (Settings ▸ Settings Profile ▸ New…), with whether it starts from a copy of the profile in use,
/// or for a new name for the one in use (Rename…).
/// </summary>
/// <remarks>
/// The name becomes part of a file's name, so <see cref="UserConfigFile.ProfileNameProblem"/>'s rules
/// apply, and no two profiles may have names that differ only in case. The button stays off until
/// the name can be used.
/// </remarks>
public sealed class ProfileNameViewModel : ObservableObject
{
	private readonly IReadOnlyList<string> _profiles;
	private readonly string? _renaming;
	private string _name;
	private bool _copyCurrent = true;

	/// <summary>Creates the view-model.</summary>
	/// <param name="heading">The window's heading, e.g. <c>New Settings Profile</c>.</param>
	/// <param name="description">What happens, under the heading.</param>
	/// <param name="confirmText">The button's label.</param>
	/// <param name="profiles">Every profile now, whose names can't be used again.</param>
	/// <param name="renaming">The profile being renamed, which may keep its name in another case; <see langword="null"/> for a new one.</param>
	/// <param name="copyFrom">The profile a new one can start from a copy of; <see langword="null"/> to not offer a copy.</param>
	/// <param name="unsavedPages">The pages whose unsaved edits are lost as FE-Buddy switches to the profile; empty when none, or it doesn't switch.</param>
	public ProfileNameViewModel(
		string heading,
		string description,
		string confirmText,
		IReadOnlyList<string> profiles,
		string? renaming,
		string? copyFrom,
		IReadOnlyList<string> unsavedPages)
	{
		ArgumentNullException.ThrowIfNull(profiles);
		ArgumentNullException.ThrowIfNull(unsavedPages);

		Heading = heading;
		Description = description;
		ConfirmText = confirmText;
		_profiles = profiles;
		_renaming = renaming;
		_name = renaming ?? string.Empty;
		CopyFrom = copyFrom;
		UnsavedNote = unsavedPages.Count == 0
			? null
			: $"Unsaved changes on {SettingsViewModel.JoinNames(unsavedPages)} will be lost as FE-Buddy switches to it.";

		ConfirmCommand = new RelayCommand(() => Close(confirmed: true), () => CanConfirm);
		CancelCommand = new RelayCommand(() => Close(confirmed: false));
	}

	/// <summary>Raised when the window should close.</summary>
	public event EventHandler? CloseRequested;

	/// <summary>The window's heading.</summary>
	public string Heading { get; }

	/// <summary>What happens, under the heading.</summary>
	public string Description { get; }

	/// <summary>The button's label.</summary>
	public string ConfirmText { get; }

	/// <summary>The name typed. It is trimmed before it is used.</summary>
	public string Name
	{
		get => _name;
		set
		{
			if (SetProperty(ref _name, value ?? string.Empty))
			{
				OnPropertyChanged(nameof(Problem));
				OnPropertyChanged(nameof(HasProblem));
				OnPropertyChanged(nameof(CanConfirm));
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>The name as it is used: without the spaces around it.</summary>
	public string TrimmedName => Name.Trim();

	/// <summary>Why the name can't be used, or <see langword="null"/>. Nothing is said while the box is empty.</summary>
	public string? Problem => TrimmedName.Length == 0 ? null : ProblemWith(Name, _profiles, _renaming);

	/// <summary>Whether <see cref="Problem"/> is shown.</summary>
	public bool HasProblem => Problem is not null;

	/// <summary>Whether the button is on: a name that can be used, and - renaming - a different one.</summary>
	public bool CanConfirm =>
		TrimmedName.Length > 0
		&& Problem is null
		&& !string.Equals(TrimmedName, _renaming, StringComparison.Ordinal);

	/// <summary>The profile a new one can start from a copy of; <see langword="null"/> when no copy is offered.</summary>
	public string? CopyFrom { get; }

	/// <summary>Whether the copy choice is shown.</summary>
	public bool OffersCopy => CopyFrom is not null;

	/// <summary>The copy choice's label, e.g. <c>Start with a copy of Default's settings</c>.</summary>
	public string CopyLabel => $"Start with a copy of {CopyFrom}'s settings";

	/// <summary>Start the new profile from a copy of <see cref="CopyFrom"/>'s settings; off, from every default. On to start with.</summary>
	public bool CopyCurrent
	{
		get => _copyCurrent;
		set => SetProperty(ref _copyCurrent, value);
	}

	/// <summary>What switching to the profile loses, or <see langword="null"/>.</summary>
	public string? UnsavedNote { get; }

	/// <summary>Whether <see cref="UnsavedNote"/> is shown.</summary>
	public bool HasUnsavedNote => UnsavedNote is not null;

	/// <summary>Whether the user chose the button, not Cancel.</summary>
	public bool Confirmed { get; private set; }

	/// <summary>Closes the window and goes ahead. Off while the name can't be used.</summary>
	public ICommand ConfirmCommand { get; }

	/// <summary>Closes the window and does nothing.</summary>
	public ICommand CancelCommand { get; }

	/// <summary>
	/// Why a profile can't be called <paramref name="name"/>: a name no file can have
	/// (<see cref="UserConfigFile.ProfileNameProblem"/>), or another profile's in any case.
	/// </summary>
	/// <param name="name">The name typed.</param>
	/// <param name="profiles">Every profile now.</param>
	/// <param name="renaming">The profile being renamed, which may keep its own name; <see langword="null"/> for a new one.</param>
	/// <returns>A sentence saying what is wrong, or <see langword="null"/>.</returns>
	internal static string? ProblemWith(string? name, IReadOnlyList<string> profiles, string? renaming)
	{
		if (UserConfigFile.ProfileNameProblem(name) is { } problem)
		{
			return problem;
		}

		string trimmed = name!.Trim();
		string? taken = profiles.FirstOrDefault(p => p.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
			&& !p.Equals(renaming, StringComparison.OrdinalIgnoreCase));

		return taken is null ? null : $"There is already a profile called {taken}.";
	}

	private void Close(bool confirmed)
	{
		Confirmed = confirmed;
		CloseRequested?.Invoke(this, EventArgs.Empty);
	}
}
