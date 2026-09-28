using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Base for one conversion tab on the File Conversions screen. Unlike an AIRAC sub-service,
/// a conversion is always on the rail and runs on its own, from the run button at the foot of
/// its own tab; the screen does the running and shows the outcome on its Review tab.
/// </summary>
/// <remarks>
/// A run happens in three steps, split by thread: <see cref="BuildSettingsBlock"/> reads the
/// tab on the UI thread, <see cref="Execute"/> does the work on a background thread from that
/// block alone, and <see cref="DescribeRun"/> turns the result into the Review tab's content back
/// on the UI thread.
/// </remarks>
public abstract class ConversionTabViewModel : SubServiceSettingsViewModel, IRunAction
{
	private bool _canRun = true;

	/// <summary>Wires the run button.</summary>
	protected ConversionTabViewModel()
	{
		RunCommand = new RelayCommand(() => RunRequested?.Invoke(this, EventArgs.Empty), () => CanRun && RunBlocker is null);
	}

	/// <summary>Raised when the user presses the run button; the screen runs the conversion.</summary>
	public event EventHandler? RunRequested;

	/// <inheritdoc />
	public ICommand RunCommand { get; }

	/// <inheritdoc />
	public abstract string RunLabel { get; }

	/// <summary>
	/// Whether the run button is live. The screen turns it off on every tab while any conversion
	/// is running, so two runs never overlap.
	/// </summary>
	public bool CanRun
	{
		get => _canRun;
		set
		{
			if (SetProperty(ref _canRun, value))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>
	/// Why the conversion cannot run with what is picked right now - e.g. no input files - or
	/// <see langword="null"/> when it can. Shown beside the run button, which stays off until it
	/// clears.
	/// </summary>
	/// <remarks>
	/// Kept apart from validation on purpose: it checks inputs that are not all saved (picked
	/// files are not), so it must never stop the user saving the settings that are.
	/// </remarks>
	public abstract string? RunBlocker { get; }

	/// <summary>Builds the raw settings block the library's parser reads, from the tab as it stands.</summary>
	/// <param name="outputDirectory">The run's output directory.</param>
	/// <param name="addFeBuddyOutputFolder">Whether to wrap output in a <c>FE-Buddy_Output</c> folder.</param>
	/// <returns>The settings block.</returns>
	public abstract IReadOnlyDictionary<string, string> BuildSettingsBlock(string outputDirectory, bool addFeBuddyOutputFolder);

	/// <summary>
	/// Runs the conversion. Called on a background thread, so it must read nothing from the tab -
	/// only <paramref name="settings"/>.
	/// </summary>
	/// <param name="settings">The block <see cref="BuildSettingsBlock"/> returned.</param>
	/// <param name="reportStep">
	/// Reports progress for the Review tab's run feed: the step's name (e.g. a file name), what it
	/// is doing, and whether it has finished. Safe to call from any thread.
	/// </param>
	/// <returns>The library's result.</returns>
	public abstract ServiceResult Execute(IReadOnlyDictionary<string, string> settings, Action<string, string, bool> reportStep);

	/// <summary>Describes a finished run for the Review tab.</summary>
	/// <param name="result">What <see cref="Execute"/> returned.</param>
	/// <returns>The summary, results block, files and output folder.</returns>
	public abstract ConversionRunOutcome DescribeRun(ServiceResult result);

	/// <summary>Tells the view and the run button that <see cref="RunBlocker"/> may have changed.</summary>
	protected void RaiseRunBlockerChanged()
	{
		OnPropertyChanged(nameof(RunBlocker));
		CommandManager.InvalidateRequerySuggested();
	}

	/// <inheritdoc />
	/// <remarks>A conversion has no Preview Settings tab, so it contributes nothing.</remarks>
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary() => [];
}
