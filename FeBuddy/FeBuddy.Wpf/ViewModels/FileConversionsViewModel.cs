using System.Windows;
using System.Windows.Threading;

using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>File Conversions</b> screen: one tab per conversion, always on the rail, each with its
/// own run button, and a Review tab for the last run's outcome.
/// </summary>
/// <remarks>
/// <para>
/// The tab machinery lives in <see cref="TabbedServiceViewModel"/>, shared with AIRAC Services.
/// Unlike AIRAC there is no General tab (nothing is picked - every conversion is always there)
/// and no Preview Settings tab (each conversion runs on its own, with its own input files).
/// </para>
/// <para>
/// Adding a conversion means adding its tab to the list in the constructor and its
/// DataTemplate to <c>TabbedServiceView</c>; running it and showing the outcome are shared.
/// </para>
/// </remarks>
public sealed class FileConversionsViewModel : TabbedServiceViewModel
{
	private readonly Dispatcher _dispatcher;
	private readonly ServiceRunReviewTabViewModel _runReview = new();
	private readonly ConversionTabViewModel[] _conversions;
	private bool _runReviewShown;

	/// <summary>Builds the screen with every conversion's tab.</summary>
	public FileConversionsViewModel()
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		_conversions =
		[
			new DatToGeojsonViewModel(),
			new SctToGeojsonViewModel(),
		];

		foreach (ConversionTabViewModel conversion in _conversions)
		{
			conversion.RunRequested += async (_, _) => await RunAsync(conversion);
		}

		RebuildTabs(_conversions);
	}

	/// <inheritdoc />
	public override string ScreenTitle => "File Conversions";

	/// <summary>
	/// The run-review tab, once a run has started. Held back until then so the rail does not
	/// carry an empty tab about a run that has not happened.
	/// </summary>
	protected override ServiceTabViewModel? PostRunTab => _runReviewShown ? _runReview : null;

	/// <summary>
	/// Saves the tab if the user agrees, refuses to start while it is invalid, then runs the
	/// conversion off the UI thread and shows how it went on the Review tab.
	/// </summary>
	/// <param name="conversion">The tab whose run button was pressed.</param>
	private async Task RunAsync(ConversionTabViewModel conversion)
	{
		if (IsRunning || !TrySave(conversion))
		{
			return;
		}

		conversion.Revalidate();

		if (conversion.Status == ServiceTabStatus.Invalid)
		{
			Toast.Warn("Cannot run", $"{conversion.Title} has settings that need fixing.");
			return;
		}

		IReadOnlyDictionary<string, string> settings = conversion.BuildSettingsBlock(
			OutputPreferences.Directory, OutputPreferences.AddFeBuddyOutputFolder);

		SetRunning(true);
		_runReview.BeginRun([]);
		_runReviewShown = true;
		ShowPostRunTab();

		try
		{
			ServiceResult result = await Task.Run(() => conversion.Execute(settings, ReportStep));
			ConversionRunOutcome outcome = conversion.DescribeRun(result);

			_runReview.CompleteRun(result, outcome.Summary, [outcome.Details], outcome.FilesWritten, outcome.OutputDirectory);

			// A run where some files failed still finished, but should not read as all-clear.
			if (result.Messages.Any(m => m.Level == LogLevel.Error))
			{
				Toast.Warn($"{conversion.Title} finished with errors", outcome.Summary);
			}
			else
			{
				Toast.Success($"{conversion.Title} complete", outcome.Summary);
			}
		}
		catch (Exception ex)
		{
			_runReview.FailRun(ex.Message);
			Toast.Error($"{conversion.Title} failed", ex.Message);
		}
		finally
		{
			SetRunning(false);
		}
	}

	/// <summary>Offers to save a tab with unsaved edits before it runs, as the settings-save contract requires.</summary>
	/// <param name="conversion">The tab about to run.</param>
	/// <returns><see langword="true"/> when nothing is left unsaved.</returns>
	private static bool TrySave(ConversionTabViewModel conversion)
	{
		if (!conversion.IsDirty)
		{
			return true;
		}

		bool proceed = ConfirmWindow.Show(
			Application.Current?.MainWindow,
			"Unsaved settings",
			$"{conversion.Title} has unsaved changes. The newly input data will be saved before execution.",
			confirmText: "Save & Continue");

		// A failed save has already explained why, and the bad field is marked.
		return proceed && conversion.Save();
	}

	/// <summary>Feeds a progress report to the Review tab's run feed, on the UI thread.</summary>
	private void ReportStep(string step, string message, bool isComplete) =>
		_dispatcher.BeginInvoke(() => _runReview.ReportStep(step, message, isComplete));

	private void SetRunning(bool running)
	{
		IsRunning = running;

		foreach (ConversionTabViewModel conversion in _conversions)
		{
			conversion.CanRun = !running;
		}
	}
}
