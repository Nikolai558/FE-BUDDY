using System.ComponentModel;

using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="InfoViewModel"/>: What's New and the Alias Command Guide are the two featured
/// cards, each opens its page in place of the cards, and Back returns to where the page was opened
/// from - the cards, or What's New for the guide opened from there. The other cards launch a
/// browser, so they are not opened here. The guide reads the facility from a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class InfoViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Info_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public InfoViewModelTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		UserConfigFile.ConfigureForTesting(null);
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

	private static List<string?> WatchChanges(InfoViewModel info)
	{
		List<string?> changed = [];
		((INotifyPropertyChanged)info).PropertyChanged += (_, e) => changed.Add(e.PropertyName);
		return changed;
	}

	[Fact]
	public void the_first_two_cards_are_the_featured_whats_new_and_alias_command_guide()
	{
		InfoViewModel info = new();

		Assert.Equal(["What's New in v3.0?", "Alias Command Guide"], info.Resources.Take(2).Select(card => card.Title));
		Assert.All(info.Resources.Take(2), card => Assert.True(card.IsFeatured));
	}

	[Fact]
	public void no_other_card_is_featured()
	{
		InfoViewModel info = new();

		InfoViewModel.Resource[] others = [.. info.Resources.Skip(2)];

		Assert.NotEmpty(others);
		Assert.All(others, card => Assert.False(card.IsFeatured));
	}

	[Fact]
	public void every_card_has_a_title_a_blurb_and_something_to_open()
	{
		InfoViewModel info = new();

		Assert.All(info.Resources, card =>
		{
			Assert.False(string.IsNullOrWhiteSpace(card.Title));
			Assert.False(string.IsNullOrWhiteSpace(card.Blurb));
			Assert.NotNull(card.Open);
		});
	}

	[Fact]
	public void no_page_is_open_to_start_with()
	{
		Assert.Null(new InfoViewModel().Page);
	}

	[Fact]
	public void opening_the_first_card_shows_the_whats_new_page()
	{
		InfoViewModel info = new();
		List<string?> changed = WatchChanges(info);

		info.Resources[0].Open.Execute(null);

		Assert.IsType<WhatsNewViewModel>(info.Page);
		Assert.Contains(nameof(InfoViewModel.Page), changed);
	}

	[Fact]
	public void opening_the_second_card_shows_the_alias_command_guide_page()
	{
		InfoViewModel info = new();

		info.Resources[1].Open.Execute(null);

		AliasGuideViewModel guide = Assert.IsType<AliasGuideViewModel>(info.Page);
		Assert.NotEmpty(guide.Document.Sections);
	}

	[Fact]
	public void back_on_the_whats_new_page_returns_to_the_cards()
	{
		InfoViewModel info = new();
		info.Resources[0].Open.Execute(null);
		WhatsNewViewModel page = Assert.IsType<WhatsNewViewModel>(info.Page);
		List<string?> changed = WatchChanges(info);

		page.BackCommand.Execute(null);

		Assert.Null(info.Page);
		Assert.Contains(nameof(InfoViewModel.Page), changed);
	}

	[Fact]
	public void back_on_the_guide_opened_from_its_card_returns_to_the_cards()
	{
		InfoViewModel info = new();
		info.Resources[1].Open.Execute(null);

		Assert.IsType<AliasGuideViewModel>(info.Page).BackCommand.Execute(null);

		Assert.Null(info.Page);
	}

	[Fact]
	public void the_guide_opened_from_whats_new_goes_back_to_whats_new()
	{
		InfoViewModel info = new();
		info.Resources[0].Open.Execute(null);

		Assert.IsType<WhatsNewViewModel>(info.Page).OpenGuideCommand.Execute(null);
		Assert.IsType<AliasGuideViewModel>(info.Page).BackCommand.Execute(null);

		Assert.IsType<WhatsNewViewModel>(info.Page).BackCommand.Execute(null);
		Assert.Null(info.Page);
	}

	[Fact]
	public void a_page_can_be_opened_again_after_going_back()
	{
		InfoViewModel info = new();

		info.Resources[0].Open.Execute(null);
		Assert.IsType<WhatsNewViewModel>(info.Page).BackCommand.Execute(null);
		info.Resources[0].Open.Execute(null);

		Assert.IsType<WhatsNewViewModel>(info.Page);
	}
}
