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
		"An application designed to assist VATUSA Facility Engineers with routine, tedious, and " +
		"sometimes complex tasks, including the production and maintenance of AIRAC cycle release " +
		"resources, alias files, and GeoJSON files (including file health checks), ERAM and STARS " +
		"adaptation conversions; and other facility engineering workflows.";

	private readonly Dispatcher _dispatcher;
	private readonly ObservableCollection<LogEntry> _allLog = [];

	private bool _newsButtonHighlighted;
	private string _nextCycleLine = "Next AIRAC cycle: …";
	private bool _isLogCollapsed = true;
	private LogLevel? _levelFilter;

	/// <summary>Creates the view-model, seeds the log from <see cref="AppLog"/>, and starts following it.</summary>
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

		// Seed from whatever the log already holds, then follow it live.
		foreach (LogEntry entry in AppLog.Entries.Reverse())
		{
			_allLog.Add(entry);
		}

		AppLog.EntryAdded += OnLogEntryAdded;
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
	public int InfoCount => _allLog.Count(e => e.Level == LogLevel.Info);

	/// <summary>How many Success entries there are.</summary>
	public int SuccessCount => _allLog.Count(e => e.Level == LogLevel.Success);

	/// <summary>How many Warning entries there are.</summary>
	public int WarningCount => _allLog.Count(e => e.Level == LogLevel.Warning);

	/// <summary>How many Error entries there are.</summary>
	public int ErrorCount => _allLog.Count(e => e.Level == LogLevel.Error);

	private void OnLogEntryAdded(object? sender, LogEntry entry) =>
		_dispatcher.BeginInvoke(() =>
		{
			_allLog.Insert(0, entry); // newest first
			RaiseLogCounts();
		});

	private void OnEnvironmentChanged(object? sender, EventArgs e) =>
		_dispatcher.BeginInvoke(() =>
		{
			RefreshNews();
			RefreshNextCycleLine();
		});

	private void OnCycleStateChanged(object? sender, AiracCycleDataCacheEntry e) =>
		_dispatcher.BeginInvoke(RefreshNextCycleLine);

	private void RefreshNews()
	{
		NewsCheckResult? news = AppEnvironment.News;

		Posts.Clear();
		if (news is not null)
		{
			foreach (NewsPost post in news.Posts)
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
			UserConfigFile.Save("General");
			NewsButtonHighlighted = false;
		}
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

	private void RaiseLogCounts()
	{
		OnPropertyChanged(nameof(AllCount));
		OnPropertyChanged(nameof(InfoCount));
		OnPropertyChanged(nameof(SuccessCount));
		OnPropertyChanged(nameof(WarningCount));
		OnPropertyChanged(nameof(ErrorCount));
	}
}
