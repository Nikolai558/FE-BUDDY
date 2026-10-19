using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.News;
using FeBuddy.Core.Application.News.Models;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The Dashboard: the description box, the News feed (primary content), and a live
/// activity-log viewer over <see cref="AppLog"/>.
/// </summary>
public sealed class DashboardViewModel : ObservableObject
{
	/// <summary>The FE-Buddy description shown in the description box, word for word.</summary>
	public const string DescriptionText =
		"An application designed to assist VATUSA Facility Engineers with routine, " +
		"tedious, and sometimes complex tasks, including the production and maintenance " +
		"of AIRAC cycle release resources, alias files, and GeoJSON files, ERAM and STARS " +
		"adaptation conversions for use in CRC; and other facility engineering workflows.";

	/// <summary>
	/// The most entries the activity log shows, newest first: a run can log thousands, and the log file
	/// (and <see cref="AppLog.Entries"/>) keep every one.
	/// </summary>
	internal const int MaxShownEntries = 2000;

	private readonly Dispatcher _dispatcher;
	private readonly ObservableCollection<LogEntry> _allLog = [];

	// Entries logged since the last batch was shown; any thread adds, the UI thread takes them.
	private readonly ConcurrentQueue<LogEntry> _incoming = new();
	private int _batchQueued;

	// How many entries of each level the log shows, kept as entries come and go rather than counted.
	private readonly Dictionary<LogLevel, int> _counts = [];

	private bool _newsButtonHighlighted;
	private string _nextCycleLine = "Next AIRAC cycle: …";
	private bool _cycleDayDismissed;
	private bool _isLogCollapsed = true;
	private LogLevel? _levelFilter;

	/// <summary>Creates the view-model, seeds the log from <see cref="AppLog.DisplayEntries"/> and follows it (<see cref="AppLog.FollowDisplay"/>).</summary>
	public DashboardViewModel()
	{
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		Posts = [];
		LogView = CollectionViewSource.GetDefaultView(_allLog);
		LogView.Filter = o => _levelFilter is null || (o is LogEntry e && e.Level == _levelFilter);

		OpenNewsCommand = new RelayCommand(OpenNews);
		OpenDiscordCommand = new RelayCommand(() => BrowserLauncher.Open(Links.Discord));
		SetLogFilterCommand = new RelayCommand<string>(SetLogFilter);
		ToggleLogCommand = new RelayCommand(() => IsLogCollapsed = !IsLogCollapsed);
		ClearLogCommand = new RelayCommand(ClearLog);
		DismissCycleDayCommand = new RelayCommand(DismissCycleDay);

		// Seed from whatever the log already holds since the last Clear and follow it live, in one step: the
		// launch logs from another thread while this is built, and each entry must show exactly once.
		foreach (LogEntry entry in AppLog.FollowDisplay(OnLogEntryAdded).Reverse().Take(MaxShownEntries))
		{
			_allLog.Add(entry);
			Count(entry, 1);
		}

		AppEnvironment.Changed += OnEnvironmentChanged;
		AiracCycleDataCache.Instance.StateChanged += OnCycleStateChanged;

		RefreshNews();
		RefreshNextCycleLine();
		RaiseLogCounts();
	}

	/// <summary>Parsed News posts, newest first.</summary>
	public ObservableCollection<NewsPost> Posts { get; }

	/// <summary>Filtered, newest-first view over the activity log.</summary>
	public ICollectionView LogView { get; }

	/// <summary>Opens the News page and marks every post read.</summary>
	public ICommand OpenNewsCommand { get; }

	/// <summary>Opens the Discord invite.</summary>
	public ICommand OpenDiscordCommand { get; }

	/// <summary>Parameter is one of <c>All</c>, <c>Info</c>, <c>Success</c>, <c>Warning</c>, <c>Error</c>.</summary>
	public ICommand SetLogFilterCommand { get; }

	/// <summary>Expands or collapses the activity log.</summary>
	public ICommand ToggleLogCommand { get; }

	/// <summary>Empties the activity log on screen. The log file keeps every entry.</summary>
	public ICommand ClearLogCommand { get; }

	/// <summary>Hides the cycle-day banner for the rest of the session.</summary>
	public ICommand DismissCycleDayCommand { get; }

	/// <summary>The Discord invite the description box links to.</summary>
	public string DiscordUrl => Links.Discord;

	/// <summary>The description-box body: <see cref="DescriptionText"/>.</summary>
	public string Description => DescriptionText;

	/// <summary>Next AIRAC cycle line: id, effective date, and a day counter.</summary>
	public string NextCycleLine
	{
		get => _nextCycleLine;
		private set => SetProperty(ref _nextCycleLine, value);
	}

	/// <summary>
	/// <see langword="true"/> on the day a new AIRAC cycle takes effect, until the user dismisses
	/// the banner. Not persisted: it comes back on the next launch that day, and not again until
	/// the next cycle.
	/// </summary>
	public bool ShowCycleDay => !_cycleDayDismissed && AiracCycleResolver.IsEffectiveDate(AppEnvironment.CheckedUtcDate);

	/// <summary>The cycle-day banner's text, naming the cycle that took effect today.</summary>
	public string CycleDayLine =>
		$"Cycle {AppEnvironment.GetAiracCycle(AiracCyclePosition.Current).AiracCycleId} is in effect. May your diffs be small.";

	/// <summary><see langword="true"/> when the newest News post is newer than <c>General.NewsLastOpen</c> - the News button draws attention.</summary>
	public bool NewsButtonHighlighted
	{
		get => _newsButtonHighlighted;
		private set => SetProperty(ref _newsButtonHighlighted, value);
	}

	/// <summary><see langword="true"/> when News failed to parse - the Dashboard shows a short notice instead of the feed.</summary>
	public bool NewsUnavailable => AppEnvironment.News is { ParseSucceeded: false };

	/// <summary>
	/// <see langword="true"/> until the launch-time News check has published a result - the feed shows an advisory
	/// saying it is fetching, rather than an empty panel that reads as "no news".
	/// </summary>
	public bool NewsFetching => AppEnvironment.News is null;

	/// <summary>Whether the activity log is collapsed to just its filter chips and their counts.</summary>
	/// <remarks>
	/// Starts collapsed on every launch and is deliberately not persisted: the log is a
	/// troubleshooting view, so expanding it is a decision about the session in front of the
	/// user rather than a setting they would want carried forward. Picking any filter chip
	/// expands it.
	/// </remarks>
	public bool IsLogCollapsed
	{
		get => _isLogCollapsed;
		set => SetProperty(ref _isLogCollapsed, value);
	}

	/// <summary>The active level filter, or <see langword="null"/> for "All".</summary>
	public LogLevel? LevelFilter
	{
		get => _levelFilter;
		private set
		{
			if (SetProperty(ref _levelFilter, value))
			{
				LogView.Refresh();
				OnPropertyChanged(nameof(FilterIsAll));
				OnPropertyChanged(nameof(FilterIsInfo));
				OnPropertyChanged(nameof(FilterIsSuccess));
				OnPropertyChanged(nameof(FilterIsWarning));
				OnPropertyChanged(nameof(FilterIsError));
			}
		}
	}

	/// <summary>Whether the All chip is checked.</summary>
	public bool FilterIsAll => _levelFilter is null;

	/// <summary>Whether the Info chip is checked.</summary>
	public bool FilterIsInfo => _levelFilter == LogLevel.Info;

	/// <summary>Whether the Success chip is checked.</summary>
	public bool FilterIsSuccess => _levelFilter == LogLevel.Success;

	/// <summary>Whether the Warning chip is checked.</summary>
	public bool FilterIsWarning => _levelFilter == LogLevel.Warning;

	/// <summary>Whether the Error chip is checked.</summary>
	public bool FilterIsError => _levelFilter == LogLevel.Error;

	/// <summary>How many log entries there are.</summary>
	public int AllCount => _allLog.Count;

	/// <summary>How many Info entries there are.</summary>
	public int InfoCount => _counts.GetValueOrDefault(LogLevel.Info);

	/// <summary>How many Success entries there are.</summary>
	public int SuccessCount => _counts.GetValueOrDefault(LogLevel.Success);

	/// <summary>How many Warning entries there are.</summary>
	public int WarningCount => _counts.GetValueOrDefault(LogLevel.Warning);

	/// <summary>How many Error entries there are.</summary>
	public int ErrorCount => _counts.GetValueOrDefault(LogLevel.Error);

	/// <summary>
	/// Queues the entry, and the first since the last batch asks the UI thread to show them all, at
	/// Background priority: a run logs hundreds a second, which one dispatcher item each would flood.
	/// </summary>
	private void OnLogEntryAdded(object? sender, LogEntry entry)
	{
		_incoming.Enqueue(entry);

		if (Interlocked.Exchange(ref _batchQueued, 1) == 0)
		{
			_dispatcher.BeginInvoke(ShowIncoming, DispatcherPriority.Background);
		}
	}

	/// <summary>Shows the entries logged since the last batch, newest first, and drops the oldest past <see cref="MaxShownEntries"/>.</summary>
	internal void ShowIncoming()
	{
		Interlocked.Exchange(ref _batchQueued, 0);

		bool changed = false;

		while (_incoming.TryDequeue(out LogEntry? entry))
		{
			_allLog.Insert(0, entry);
			Count(entry, 1);
			changed = true;
		}

		while (_allLog.Count > MaxShownEntries)
		{
			Count(_allLog[^1], -1);
			_allLog.RemoveAt(_allLog.Count - 1);
		}

		if (changed)
		{
			RaiseLogCounts();
		}
	}

	private void Count(LogEntry entry, int by) => _counts[entry.Level] = _counts.GetValueOrDefault(entry.Level) + by;

	private void OnEnvironmentChanged(object? sender, EventArgs e) =>
		_dispatcher.BeginInvoke(() =>
		{
			RefreshNews();
			RefreshNextCycleLine();

			// The launch-time clock check can move CheckedUtcDate.
			OnPropertyChanged(nameof(ShowCycleDay));
			OnPropertyChanged(nameof(CycleDayLine));
		});

	private void OnCycleStateChanged(object? sender, AiracCycleDataCacheEntry e) =>
		_dispatcher.BeginInvoke(RefreshNextCycleLine);

	/// <summary>Shows the latest News check now, as an environment change does. Unit tests only.</summary>
	internal void RefreshNewsForTesting() => RefreshNews();

	/// <summary>
	/// Shows the latest News check: the posts are put in again only when they changed, since every
	/// environment change (six during launch) comes here, and each post put in builds its view again.
	/// </summary>
	private void RefreshNews()
	{
		NewsCheckResult? news = AppEnvironment.News;
		IReadOnlyList<NewsPost> posts = news?.Posts ?? [];

		if (!posts.SequenceEqual(Posts))
		{
			Posts.Clear();
			foreach (NewsPost post in posts)
			{
				Posts.Add(post);
			}
		}

		NewsButtonHighlighted = news is { ParseSucceeded: true, NewPostCount: > 0 };
		OnPropertyChanged(nameof(NewsUnavailable));
		OnPropertyChanged(nameof(NewsFetching));
	}

	private void RefreshNextCycleLine()
	{
		AiracCycleDataCacheEntry? next = AiracCycleDataCache.Instance.Entries
			.FirstOrDefault(x => x.Position == AiracCyclePosition.Next);

		AiracCycleInfo cycle = next?.Cycle ?? AppEnvironment.GetAiracCycle(AiracCyclePosition.Next);

		int days = cycle.EffectiveDateUtc.DayNumber - AppEnvironment.CheckedUtcDate.DayNumber;
		string dayText = days switch
		{
			< 0 => "now in effect",
			0 => "effective today",
			1 => "in 1 day",
			_ => $"in {days} days",
		};

		NextCycleLine = $"Next AIRAC cycle {cycle.AiracCycleId}  ·  effective {cycle.EffectiveDateUtc:dd MMM yyyy}  ·  {dayText}";
	}

	private void OpenNews()
	{
		BrowserLauncher.Open(NewsService.PageUrl);

		// Opening News marks everything read - but only when the check actually parsed; a parse
		// failure leaves NewsLastOpen untouched so the posts still show as new once it works.
		NewsCheckResult? news = AppEnvironment.News;
		if (news is { ParseSucceeded: true, LatestPostId: { } latest })
		{
			UserConfigFile.TrySetValue(UserConfigKeys.NewsLastOpen, latest.ToString());
			SettingsWrites.TrySave("General", "News you have read");
			NewsButtonHighlighted = false;
		}
	}

	private void DismissCycleDay()
	{
		_cycleDayDismissed = true;
		OnPropertyChanged(nameof(ShowCycleDay));
	}

	private void SetLogFilter(string? which)
	{
		LevelFilter = which switch
		{
			"Info" => LogLevel.Info,
			"Success" => LogLevel.Success,
			"Warning" => LogLevel.Warning,
			"Error" => LogLevel.Error,
			_ => null,
		};

		// Picking a chip is asking to see those entries, so open the log; the user can still
		// minimize it again.
		IsLogCollapsed = false;
	}

	private void ClearLog()
	{
		// AppLog remembers the clear, so the next Dashboard (one is made per navigation) seeds
		// from after it. Every entry is cleared, whatever the filter shows; the filter stays.
		AppLog.ClearDisplay();
		_incoming.Clear();
		_allLog.Clear();
		_counts.Clear();
		RaiseLogCounts();
	}

	private void RaiseLogCounts()
	{
		OnPropertyChanged(nameof(AllCount));
		OnPropertyChanged(nameof(InfoCount));
		OnPropertyChanged(nameof(SuccessCount));
		OnPropertyChanged(nameof(WarningCount));
		OnPropertyChanged(nameof(ErrorCount));
	}
}
