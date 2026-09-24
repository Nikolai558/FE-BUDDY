using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Infrastructure;

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
/// The Dashboard: the verbatim description box, the News feed (primary content), and a live
/// activity-log viewer over <see cref="AppLog"/> (remediation plan Phase 6). No sample data.
/// </summary>
public sealed class DashboardViewModel : ObservableObject
{
    /// <summary>The verbatim FE-Buddy description (remediation plan 6.2).</summary>
    public const string DescriptionText =
        "An application designed to assist VATUSA Facility Engineers with routine, tedious, and " +
        "sometimes complex tasks, including the production and maintenance of AIRAC cycle release " +
        "resources, alias files, and GeoJSON files (including file health checks), ERAM and STARS " +
        "adaptation conversions; and other facility engineering workflows.";

    private readonly Dispatcher _dispatcher;
    private readonly ObservableCollection<LogEntry> _allLog = new();

    private bool _newsButtonHighlighted;
    private string _nextCycleLine = "Next AIRAC cycle: …";
    private bool _isLogCollapsed = true;
    private LogLevel? _levelFilter;

    public DashboardViewModel()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        Posts = new ObservableCollection<NewsPost>();
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

    /// <summary>Parsed News posts, newest first (remediation plan 6.1).</summary>
    public ObservableCollection<NewsPost> Posts { get; }

    /// <summary>Filtered, newest-first view over the activity log (remediation plan 6.4).</summary>
    public ICollectionView LogView { get; }

    public ICommand OpenNewsCommand { get; }

    public ICommand OpenDiscordCommand { get; }

    /// <summary>Parameter is one of <c>All</c>, <c>Info</c>, <c>Success</c>, <c>Warning</c>, <c>Error</c>.</summary>
    public ICommand SetLogFilterCommand { get; }

    public ICommand ToggleLogCommand { get; }

    /// <summary>The Discord invite the description box links to.</summary>
    public string DiscordUrl => Links.Discord;

    /// <summary>The description-box body (verbatim).</summary>
    public string Description => DescriptionText;

    /// <summary>Next AIRAC cycle line: id, effective date, and a day counter (remediation plan 6.2).</summary>
    public string NextCycleLine
    {
        get => _nextCycleLine;
        private set => SetProperty(ref _nextCycleLine, value);
    }

    /// <summary>True when the newest News post is newer than <c>General.NewsLastOpen</c> - the News button draws attention.</summary>
    public bool NewsButtonHighlighted
    {
        get => _newsButtonHighlighted;
        private set => SetProperty(ref _newsButtonHighlighted, value);
    }

    /// <summary>True when News failed to parse - the Dashboard shows a short notice instead of the feed.</summary>
    public bool NewsUnavailable => AppEnvironment.News is { ParseSucceeded: false };

    /// <summary>
    /// True until the launch-time News check has published a result - the feed shows an advisory
    /// saying it is fetching, rather than an empty panel that reads as "no news".
    /// </summary>
    public bool NewsFetching => AppEnvironment.News is null;

    /// <summary>Collapsed activity log shows only the filter chips with their counts.</summary>
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

    public bool FilterIsAll => _levelFilter is null;
    public bool FilterIsInfo => _levelFilter == LogLevel.Info;
    public bool FilterIsSuccess => _levelFilter == LogLevel.Success;
    public bool FilterIsWarning => _levelFilter == LogLevel.Warning;
    public bool FilterIsError => _levelFilter == LogLevel.Error;

    public int AllCount => _allLog.Count;
    public int InfoCount => _allLog.Count(e => e.Level == LogLevel.Info);
    public int SuccessCount => _allLog.Count(e => e.Level == LogLevel.Success);
    public int WarningCount => _allLog.Count(e => e.Level == LogLevel.Warning);
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

        AiracCycleInfo? cycle = next?.Cycle;

        if (cycle is null)
        {
            try
            {
                cycle = AiracCycleResolver.GetCycle(AiracCyclePosition.Next);
            }
            catch
            {
                NextCycleLine = "Next AIRAC cycle: unavailable";
                return;
            }
        }

        int days = cycle.EffectiveDateUtc.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
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

        // Opening News marks everything read - but only when the check actually parsed
        // (a parse failure must leave NewsLastOpen untouched, remediation plan 6.1).
        NewsCheckResult? news = AppEnvironment.News;
        if (news is { ParseSucceeded: true, LatestPostId: { } latest })
        {
            UserConfigFile.TrySetValue("General.NewsLastOpen", latest.ToString());
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
