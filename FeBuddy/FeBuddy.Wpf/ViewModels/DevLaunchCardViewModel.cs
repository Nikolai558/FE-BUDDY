using System.IO;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.Launch.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Settings' Developer card, shown only in a dev launch: a box for each optional launch step, ticked
/// when a dev launch runs it. Each change is saved at once to <see cref="DevLaunchFile"/> -
/// outside every settings profile, so the page's Save, profiles, Import and Export never touch it -
/// and applies from the next dev launch. <see cref="RestartCommand"/> starts that dev launch now.
/// </summary>
public sealed class DevLaunchCardViewModel : ObservableObject
{
	private readonly Action _restart;
	private string? _saveError;

	/// <summary>Builds the card from the steps saved for a dev launch.</summary>
	/// <param name="saved">The steps a dev launch runs, as saved.</param>
	/// <param name="restart">Restarts FE-Buddy in a dev launch, once the user has agreed to close it.</param>
	public DevLaunchCardViewModel(OptionalLaunchStep saved, Action restart)
	{
		_restart = restart ?? throw new ArgumentNullException(nameof(restart));

		Steps = [.. OptionalLaunchSteps.InCardOrder.Select(step => new DevLaunchStepToggle(step, (saved & step) == step, () => Save()))];
		RestartCommand = new RelayCommand(Restart);
	}

	/// <summary>
	/// The card for how FE-Buddy was started: one for a dev launch, built from the saved steps, and
	/// none for a normal launch - which never shows it.
	/// </summary>
	/// <param name="options">How FE-Buddy was started.</param>
	/// <param name="restart">Restarts FE-Buddy in a dev launch.</param>
	/// <returns>The card, or <see langword="null"/> in a normal launch.</returns>
	public static DevLaunchCardViewModel? For(LaunchOptions options, Action restart)
	{
		ArgumentNullException.ThrowIfNull(options);
		return options.IsDevLaunch ? new DevLaunchCardViewModel(DevLaunchFile.Read(), restart) : null;
	}

	/// <summary>One box per optional launch step, in the card's order.</summary>
	public IReadOnlyList<DevLaunchStepToggle> Steps { get; }

	/// <summary>The steps ticked now.</summary>
	public OptionalLaunchStep Chosen =>
		Steps.Where(toggle => toggle.IsChecked).Aggregate(OptionalLaunchStep.None, (steps, toggle) => steps | toggle.Step);

	/// <summary>Saves the boxes, then restarts FE-Buddy in a dev launch with them.</summary>
	public ICommand RestartCommand { get; }

	/// <summary>Why the last save failed, or <see langword="null"/> when it didn't.</summary>
	public string? SaveError
	{
		get => _saveError;
		private set
		{
			if (SetProperty(ref _saveError, value))
			{
				OnPropertyChanged(nameof(HasSaveError));
			}
		}
	}

	/// <summary>Whether <see cref="SaveError"/> has something to say.</summary>
	public bool HasSaveError => SaveError is not null;

	private bool Save()
	{
		try
		{
			DevLaunchFile.Save(Chosen);
			SaveError = null;
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning("DevLaunch", $"Could not save {DevLaunchFile.FileName}: {ex.Message}");
			SaveError = $"Couldn't save these choices: {ex.Message}";
			return false;
		}
	}

	private void Restart()
	{
		if (Save())
		{
			_restart();
		}
	}
}
