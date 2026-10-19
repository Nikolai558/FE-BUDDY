using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.News.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Dashboard's activity log and News feed (<see cref="DashboardViewModel"/>): entries are shown
/// in batches, newest first, at most <see cref="DashboardViewModel.MaxShownEntries"/>, with their counts
/// kept as they come and go; and the News posts are put in again only when they changed.
/// </summary>
[Collection("AppLog")]
public sealed class DashboardViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Dashboard_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the log at a throwaway folder, from a cleared display.</summary>
	public DashboardViewModelTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		AppLog.ClearDisplay();
		AppEnvironment.ResetForTesting();
	}

	/// <summary>Restores the real log and deletes the folder.</summary>
	public void Dispose()
	{
		AppEnvironment.ResetForTesting();
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	/// <summary>A run's worth of entries shows the newest, newest first, with counts that match what is shown.</summary>
	[Fact]
	public void the_log_shows_the_newest_entries_with_their_counts()
	{
		DashboardViewModel dashboard = new();
		List<string?> changed = [];
		dashboard.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		for (int i = 0; i < DashboardViewModel.MaxShownEntries + 100; i++)
		{
			if (i % 10 == 0)
			{
				AppLog.Warning("Test", $"warning {i}");
			}
			else
			{
				AppLog.Info("Test", $"info {i}");
			}
		}

		dashboard.ShowIncoming();

		LogEntry[] shown = [.. dashboard.LogView.Cast<LogEntry>()];
		Assert.Equal(DashboardViewModel.MaxShownEntries, dashboard.AllCount);
		Assert.Equal($"info {DashboardViewModel.MaxShownEntries + 99}", shown[0].Message);
		Assert.Equal("warning 100", shown[^1].Message);
		Assert.Equal(shown.Count(e => e.Level == LogLevel.Warning), dashboard.WarningCount);
		Assert.Equal(shown.Count(e => e.Level == LogLevel.Info), dashboard.InfoCount);
		Assert.Equal(0, dashboard.ErrorCount);
		Assert.Single(changed, nameof(DashboardViewModel.AllCount));

		dashboard.ClearLogCommand.Execute(null);

		Assert.Equal(0, dashboard.AllCount);
		Assert.Equal(0, dashboard.WarningCount);
		Assert.Equal(0, dashboard.InfoCount);
	}

	/// <summary>An environment change that leaves the News as it was leaves the posts on screen as they are.</summary>
	[Fact]
	public void the_news_posts_are_put_in_again_only_when_they_changed()
	{
		NewsPost first = new(new NewsPostId(new DateOnly(2026, 10, 1), 1), "1 Oct 2026", "First", "Body.");
		AppEnvironment.News = new NewsCheckResult([first], first.Id, 0, ParseSucceeded: true, FromNetwork: true);
		DashboardViewModel dashboard = new();
		int resets = 0;
		dashboard.Posts.CollectionChanged += (_, _) => resets++;

		AppEnvironment.News = new NewsCheckResult([first], first.Id, 1, ParseSucceeded: true, FromNetwork: true);
		dashboard.RefreshNewsForTesting();

		Assert.Equal(0, resets);
		Assert.True(dashboard.NewsButtonHighlighted);

		NewsPost second = new(new NewsPostId(new DateOnly(2026, 10, 2), 1), "2 Oct 2026", "Second", "Body.");
		AppEnvironment.News = new NewsCheckResult([second, first], second.Id, 1, ParseSucceeded: true, FromNetwork: true);
		dashboard.RefreshNewsForTesting();

		Assert.Equal([second, first], dashboard.Posts);
	}
}
