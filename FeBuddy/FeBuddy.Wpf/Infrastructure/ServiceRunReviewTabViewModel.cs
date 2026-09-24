using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>Where one sub-service has got to during a run.</summary>
public enum RunStepStatus
{
	/// <summary>Selected for this run, not started yet.</summary>
	Waiting = 0,

	/// <summary>Running now.</summary>
	Working = 1,

	/// <summary>Finished successfully.</summary>
	Finished = 2,

	/// <summary>Did not finish.</summary>
	Failed = 3,
}

/// <summary>
/// One sub-service's line in the run feed: what it is, where it has got to, and the last thing
/// it reported.
/// </summary>
/// <param name="name">The sub-service's name, as the tab rail shows it.</param>
public sealed class RunStep(string name) : ObservableObject
{
	private RunStepStatus _status = RunStepStatus.Waiting;
	private string? _detail;

	/// <summary>The sub-service's name.</summary>
	public string Name { get; } = name;

	/// <summary>Where it has got to.</summary>
	public RunStepStatus Status
	{
		get => _status;
		set
		{
			if (SetProperty(ref _status, value))
			{
				OnPropertyChanged(nameof(StatusText));
			}
		}
	}

	/// <summary>The status as a word, for the row.</summary>
	public string StatusText => Status switch
	{
		RunStepStatus.Waiting => "waiting",
		RunStepStatus.Working => "working…",
		RunStepStatus.Finished => "finished",
		_ => "failed",
	};

	/// <summary>The last progress message this sub-service reported, if any.</summary>
	public string? Detail
	{
		get => _detail;
		set => SetProperty(ref _detail, value);
	}
}

/// <summary>
/// The <b>Review</b> tab: everything about a run, in one place.
/// </summary>
/// <remarks>
/// It appears only once a run has started, at the very end of the rail. The sub-service tabs
/// hold settings only; what happened when the service ran lives here - the live step feed,
/// anything that went wrong at <see cref="LogLevel.Error"/> level, the advisories
/// (<see cref="ServiceMessage.IsAdvisory"/>) that explain missing output such as "nothing
/// matched your filters", each sub-service's results with its warnings and its routine notices
/// collapsed behind a toggle, and the files written with the way out to the output folder.
/// </remarks>
public sealed class ServiceRunReviewTabViewModel : ServiceTabViewModel
{
	private bool _isRunning;
	private bool _hasRun;
	private string? _summary;
	private string? _outputDirectory;
	private double _elapsedSeconds;
	private bool _isFileListCollapsed = true;

	private readonly Stopwatch _stopwatch = new();

	/// <summary>Creates the tab.</summary>
	public ServiceRunReviewTabViewModel()
	{
		OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, () => OutputDirectory is not null);
		ToggleFileListCommand = new RelayCommand(() => IsFileListCollapsed = !IsFileListCollapsed);

		// Keeps the Has* flags honest however the collections are filled.
		Errors.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasErrors));
		Advisories.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAdvisories));
		FilesWritten.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFiles));
		Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasResults));
	}

	/// <inheritdoc />
	public override string Title => "Review";

	/// <inheritdoc />
	public override bool IsRunnable => false;

	/// <summary>One row per sub-service in the run, in the order they run.</summary>
	public ObservableCollection<RunStep> Steps { get; } = [];

	/// <summary>Every error the run reported, across sub-services.</summary>
	public ObservableCollection<string> Errors { get; } = [];

	/// <summary>Whether anything failed.</summary>
	public bool HasErrors => Errors.Count > 0;

	/// <summary>Messages the run flagged for this tab, e.g. that nothing matched the filters.</summary>
	public ObservableCollection<string> Advisories { get; } = [];

	/// <summary>Whether the run left any advisory.</summary>
	public bool HasAdvisories => Advisories.Count > 0;

	/// <summary>
	/// One block per sub-service that finished: what it produced, its warnings, and its routine
	/// notices. Empty while running and after a run that failed.
	/// </summary>
	public ObservableCollection<SubServiceRunResult> Results { get; } = [];

	/// <summary>Whether any sub-service reported results.</summary>
	public bool HasResults => Results.Count > 0;

	/// <summary>Every file the run wrote, across sub-services.</summary>
	public ObservableCollection<string> FilesWritten { get; } = [];

	/// <summary>Whether the run wrote anything.</summary>
	public bool HasFiles => FilesWritten.Count > 0;

	/// <summary>Whether a run is in progress.</summary>
	public bool IsRunning
	{
		get => _isRunning;
		private set
		{
			if (SetProperty(ref _isRunning, value))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Whether a run has finished in this session.</summary>
	public bool HasRun
	{
		get => _hasRun;
		private set => SetProperty(ref _hasRun, value);
	}

	/// <summary>A one-line summary of what the run produced.</summary>
	public string? Summary
	{
		get => _summary;
		private set => SetProperty(ref _summary, value);
	}

	/// <summary>How long the run took.</summary>
	public double ElapsedSeconds
	{
		get => _elapsedSeconds;
		private set
		{
			if (SetProperty(ref _elapsedSeconds, value))
			{
				OnPropertyChanged(nameof(ElapsedText));
			}
		}
	}

	/// <summary>The elapsed time, formatted.</summary>
	public string ElapsedText => $"{ElapsedSeconds:0.0}s";

	/// <summary>The folder the run wrote into, or <see langword="null"/> before a run writes anything.</summary>
	public string? OutputDirectory
	{
		get => _outputDirectory;
		private set
		{
			if (SetProperty(ref _outputDirectory, value))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Opens the folder the run wrote into.</summary>
	public ICommand OpenOutputFolderCommand { get; }

	/// <summary>Expands or minimizes the list of written files.</summary>
	public ICommand ToggleFileListCommand { get; }

	/// <summary>
	/// Whether the list of written files is minimized to its count. Starts minimized on every
	/// run and is deliberately not persisted, like the Dashboard activity log: a Departures run
	/// alone writes thousands of files, so the list is there to dig into, not to read by default.
	/// </summary>
	public bool IsFileListCollapsed
	{
		get => _isFileListCollapsed;
		set => SetProperty(ref _isFileListCollapsed, value);
	}

	/// <summary>Starts a run: seeds one step per sub-service and clears the last run's outcome.</summary>
	/// <param name="subServiceNames">The sub-services taking part, in run order.</param>
	public void BeginRun(IEnumerable<string> subServiceNames)
	{
		Steps.Clear();
		Errors.Clear();
		Advisories.Clear();
		Results.Clear();
		FilesWritten.Clear();

		foreach (string name in subServiceNames)
		{
			Steps.Add(new RunStep(name));
		}

		Summary = null;
		OutputDirectory = null;
		ElapsedSeconds = 0;
		IsFileListCollapsed = true;
		IsRunning = true;
		HasRun = false;

		_stopwatch.Restart();
	}

	/// <summary>
	/// Moves a sub-service's step along from a progress report.
	/// </summary>
	/// <param name="subService">The sub-service the report came from.</param>
	/// <param name="message">What it reported.</param>
	/// <param name="isComplete">Whether this report means that sub-service has finished.</param>
	public void ReportStep(string subService, string message, bool isComplete)
	{
		RunStep? step = Steps.FirstOrDefault(s => string.Equals(s.Name, subService, StringComparison.OrdinalIgnoreCase));

		if (step is null)
		{
			step = new RunStep(subService);
			Steps.Add(step);
		}

		step.Detail = message;
		step.Status = isComplete ? RunStepStatus.Finished : RunStepStatus.Working;
	}

	/// <summary>
	/// Records a finished run: its errors and advisories, each sub-service's results, its files,
	/// and how long it took.
	/// </summary>
	/// <param name="result">The aggregated result.</param>
	/// <param name="summary">The one-line summary of what was produced.</param>
	/// <param name="subServiceResults">Each sub-service's block, in run order.</param>
	/// <param name="filesWritten">Every file written, across sub-services.</param>
	/// <param name="outputDirectory">The folder to offer to open.</param>
	public void CompleteRun(
		AiracServiceResult result,
		string summary,
		IEnumerable<SubServiceRunResult> subServiceResults,
		IEnumerable<string> filesWritten,
		string? outputDirectory)
	{
		_stopwatch.Stop();

		IsRunning = false;
		HasRun = true;
		Summary = summary;
		ElapsedSeconds = result.Elapsed.TotalSeconds;
		OutputDirectory = outputDirectory;

		foreach (ServiceMessage message in result.Messages.Where(m => m.Level == LogLevel.Error))
		{
			Errors.Add($"[{message.Source}] {message.Text}");
		}

		foreach (ServiceMessage message in result.Messages.Where(m => m.IsAdvisory && m.Level != LogLevel.Error))
		{
			Advisories.Add(message.Text);
		}

		foreach (SubServiceRunResult subServiceResult in subServiceResults)
		{
			Results.Add(subServiceResult);
		}

		foreach (string file in filesWritten)
		{
			FilesWritten.Add(file);
		}

		// Anything still mid-flight when the run ended never reported its completion.
		foreach (RunStep step in Steps.Where(s => s.Status == RunStepStatus.Working))
		{
			step.Status = RunStepStatus.Finished;
		}
	}

	/// <summary>Records a run that threw before it could finish.</summary>
	/// <param name="error">The failure message.</param>
	/// <param name="filesWritten">Anything it managed to write first.</param>
	/// <param name="outputDirectory">The folder to offer to open, when there is something in it.</param>
	/// <remarks>
	/// A run that fails part way through has often already written real files, so the elapsed
	/// time and the way to those files matter here as much as they do on a clean run.
	/// </remarks>
	public void FailRun(string error, IEnumerable<string>? filesWritten = null, string? outputDirectory = null)
	{
		_stopwatch.Stop();

		IsRunning = false;
		HasRun = true;
		Summary = "The run did not finish.";
		ElapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
		Errors.Add(error);

		foreach (string file in filesWritten ?? [])
		{
			FilesWritten.Add(file);
		}

		if (outputDirectory is not null)
		{
			OutputDirectory = outputDirectory;
		}

		foreach (RunStep step in Steps.Where(s => s.Status != RunStepStatus.Finished))
		{
			step.Status = RunStepStatus.Failed;
		}
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServiceReviewSection> BuildReviewSummary() => [];

	private void OpenOutputFolder()
	{
		if (OutputDirectory is not { } directory || !Directory.Exists(directory))
		{
			Toast.Warn("Nothing to open", "The run has not written anything yet.");
			return;
		}

		Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
	}
}
