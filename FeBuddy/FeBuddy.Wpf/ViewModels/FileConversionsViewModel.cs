using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>File Conversions</b> screen. It opens on a picker - the <b>Source</b>, then its
/// <b>File</b> when the source has a choice of them, then the <b>Output</b> - and <b>Continue</b>
/// opens that conversion's page in its place: its settings, its Convert button, and its last run's
/// results under that. The back arrow returns to the picker.
/// </summary>
/// <remarks>
/// <para>
/// What can be picked is a tree of <see cref="ConversionChoice"/>s, built in the constructor; each
/// output opens a conversion page (a <see cref="ConversionTabViewModel"/>). The pages are built once
/// and kept, so a page left with unsaved edits still has them when it is opened again.
/// </para>
/// <para>
/// Adding a conversion means adding its page and its place in the tree here, and its DataTemplate
/// to <c>FileConversionsView</c>; a new file of a source, or a new output, is one more choice in the
/// tree. Running a conversion and showing its results are shared.
/// </para>
/// </remarks>
public sealed class FileConversionsViewModel : ObservableObject, IOpensAtStart
{
	private const string GeojsonOutput = "GeoJSON for CRC";

	private readonly Dispatcher _dispatcher;
	private readonly ConversionTabViewModel[] _conversions;
	private readonly Dictionary<ConversionTabViewModel, ServiceRunReviewTabViewModel> _results = [];
	private ConversionTabViewModel? _page;
	private ConversionTabViewModel? _running;

	/// <summary>Builds every conversion's page and the picker's choices.</summary>
	public FileConversionsViewModel()
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		DatToGeojsonViewModel dat = new();
		SctToGeojsonViewModel sct = new();
		EramToGeojsonViewModel eram = new();

		_conversions = [dat, sct, eram];

		foreach (ConversionTabViewModel conversion in _conversions)
		{
			conversion.RunRequested += async (_, _) => await RunAsync(conversion);
		}

		Sources =
		[
			new("FAA Radar Video Map .dat files", "FAA RADAR Video Maps (RVMs), one map per .dat file.")
			{
				Outputs = [new(GeojsonOutput, "One .geojson file per map, named after it.") { Conversion = dat }],
			},
			new("Legacy Sector File (.sct2)", "VRC sector files, .sct2 or .sct.")
			{
				Outputs =
				[
					new(GeojsonOutput, "A folder per sector file: its boundaries, airways, GEO, labels, regions, and each SID and STAR diagram.")
					{
						Conversion = sct,
					},
				],
			},
			new("FAA ERAM Adaptation Files", "Files from an unzipped ERAM adaptation export.")
			{
				Files =
				[
					new("Geomaps.xml", "The export's video maps. With ConsoleCommandControl.xml beside it, its brightness and filter menus too.")
					{
						Outputs =
						[
							new(GeojsonOutput, "Each map, in the layout you pick, and ConsoleCommandControl.txt listing the menus.")
							{
								Conversion = eram,
							},
						],
					},
				],
			},
		];

		WirePicks(Sources);

		ContinueCommand = new RelayCommand(() => Page = SelectedOutput?.Conversion, () => SelectedOutput is not null);
		BackCommand = new RelayCommand(Back);
	}

	/// <summary>The screen's heading.</summary>
	public string ScreenTitle => "File Conversions";

	// ================= picker =================

	/// <summary>What can be converted, in the picker's first list.</summary>
	public IReadOnlyList<ConversionChoice> Sources { get; }

	/// <summary>The source picked, or <see langword="null"/> before one is.</summary>
	public ConversionChoice? SelectedSource => Sources.FirstOrDefault(c => c.IsSelected);

	/// <summary>The picked source's files, or empty when it asks no more.</summary>
	public IReadOnlyList<ConversionChoice> Files => SelectedSource?.Files ?? [];

	/// <summary>Whether the picker shows the File list.</summary>
	public bool HasFiles => Files.Count > 0;

	/// <summary>The file picked, or <see langword="null"/> when the source has none.</summary>
	public ConversionChoice? SelectedFile => Files.FirstOrDefault(c => c.IsSelected);

	/// <summary>What the picked file, or the picked source when it has no files, can be converted to.</summary>
	public IReadOnlyList<ConversionChoice> Outputs => (HasFiles ? SelectedFile : SelectedSource)?.Outputs ?? [];

	/// <summary>Whether the picker shows the Output list, and Continue.</summary>
	public bool HasOutputs => Outputs.Count > 0;

	/// <summary>The output picked.</summary>
	public ConversionChoice? SelectedOutput => Outputs.FirstOrDefault(c => c.IsSelected);

	/// <summary>The conversion Continue opens, e.g. <c>FAA ERAM Adaptation Files ▸ Geomaps.xml → GeoJSON for CRC</c>.</summary>
	public string? PickSummary => SelectedOutput is { } output
		? $"{string.Join(" ▸ ", new[] { SelectedSource, SelectedFile }.OfType<ConversionChoice>().Select(c => c.Name))} → {output.Name}"
		: null;

	/// <summary>Opens the picked output's conversion page.</summary>
	public ICommand ContinueCommand { get; }

	// ================= conversion page =================

	/// <summary>The conversion page open in place of the picker, or <see langword="null"/> on the picker.</summary>
	public ConversionTabViewModel? Page
	{
		get => _page;
		private set
		{
			if (SetProperty(ref _page, value))
			{
				OnPropertyChanged(nameof(Results));
			}
		}
	}

	/// <summary>
	/// The open page's last run, shown under its Convert card: progress, problems and the files
	/// written. <see langword="null"/> until the page's conversion has run this session.
	/// </summary>
	public ServiceRunReviewTabViewModel? Results => Page is null ? null : _results.GetValueOrDefault(Page);

	/// <summary>Offers to save the page's unsaved edits, then returns to the picker.</summary>
	public ICommand BackCommand { get; }

	/// <summary><see langword="true"/> while a conversion is running.</summary>
	public bool IsRunning => _running is not null;

	/// <summary>Every conversion page, built or not yet opened.</summary>
	public IReadOnlyList<ConversionTabViewModel> Conversions => _conversions;

	/// <inheritdoc />
	/// <remarks>
	/// Back to the picker, with its picks and every page's edits kept - or, while a conversion runs,
	/// to that conversion's page, where its progress is.
	/// </remarks>
	public void ReturnToStart() => Page = _running;

	// ================= private =================

	/// <summary>
	/// Makes each list pick one: ticking a choice unticks the rest of its list, and a list that
	/// appears with nothing picked yet gets its top choice.
	/// </summary>
	private void WirePicks(IReadOnlyList<ConversionChoice> list)
	{
		foreach (ConversionChoice choice in list)
		{
			choice.Picked += (_, _) =>
			{
				foreach (ConversionChoice other in list.Where(other => !ReferenceEquals(other, choice)))
				{
					other.IsSelected = false;
				}

				PickTop(Files);
				PickTop(Outputs);
				RaisePicks();
			};

			WirePicks(choice.Files);
			WirePicks(choice.Outputs);
		}
	}

	private static void PickTop(IReadOnlyList<ConversionChoice> list)
	{
		if (list.Count > 0 && !list.Any(c => c.IsSelected))
		{
			list[0].IsSelected = true;
		}
	}

	private void RaisePicks()
	{
		foreach (string name in new[]
		{
			nameof(SelectedSource), nameof(Files), nameof(HasFiles), nameof(SelectedFile),
			nameof(Outputs), nameof(HasOutputs), nameof(SelectedOutput), nameof(PickSummary),
		})
		{
			OnPropertyChanged(name);
		}

		CommandManager.InvalidateRequerySuggested();
	}

	private void Back()
	{
		if (TabbedServiceViewModel.ConfirmLeave(Page))
		{
			Page = null;
		}
	}

	/// <summary>
	/// Saves the page if the user agrees, refuses to start while it is invalid, asks anything the
	/// page needs to (<see cref="ConversionTabViewModel.ConfirmRun"/>), then runs the conversion off
	/// the UI thread and shows how it went under the page's Convert card.
	/// </summary>
	/// <param name="conversion">The page whose Convert button was pressed.</param>
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

		if (!conversion.ConfirmRun(settings))
		{
			return;
		}

		ServiceRunReviewTabViewModel results = ResultsFor(conversion);

		SetRunning(conversion);
		results.BeginRun([]);

		try
		{
			ServiceResult result = await Task.Run(() => conversion.Execute(
				settings,
				(step, message, isComplete) => _dispatcher.BeginInvoke(() => results.ReportStep(step, message, isComplete))));

			ConversionRunOutcome outcome = conversion.DescribeRun(result);

			results.CompleteRun(result, outcome.Summary, [outcome.Details], outcome.FilesWritten, outcome.OutputDirectory);

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
			results.FailRun(ex.Message);
			Toast.Error($"{conversion.Title} failed", ex.Message);
		}
		finally
		{
			SetRunning(null);
		}
	}

	/// <summary>Offers to save a page with unsaved edits before it runs, as the settings-save contract requires.</summary>
	/// <param name="conversion">The page about to run.</param>
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

	/// <summary>The conversion's results, made the first time it runs.</summary>
	private ServiceRunReviewTabViewModel ResultsFor(ConversionTabViewModel conversion)
	{
		if (!_results.TryGetValue(conversion, out ServiceRunReviewTabViewModel? results))
		{
			results = new ServiceRunReviewTabViewModel();
			_results[conversion] = results;
			OnPropertyChanged(nameof(Results));
		}

		return results;
	}

	/// <summary>Turns every Convert button off while <paramref name="running"/> runs, and back on after.</summary>
	private void SetRunning(ConversionTabViewModel? running)
	{
		_running = running;
		OnPropertyChanged(nameof(IsRunning));
		CommandManager.InvalidateRequerySuggested();

		foreach (ConversionTabViewModel conversion in _conversions)
		{
			conversion.CanRun = running is null;
		}
	}
}
