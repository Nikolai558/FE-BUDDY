using System.ComponentModel;

using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="InfoViewModel"/>: What's New is the one featured card, opening it shows the
/// What's New page in place of the cards, and its Back returns to them. The other cards launch a
/// browser, so they are not opened here.
/// </summary>
public sealed class InfoViewModelTests
{
	[Fact]
	public void the_first_card_is_the_featured_whats_new()
	{
		InfoViewModel info = new();

		InfoViewModel.Resource first = info.Resources[0];

		Assert.Equal("What's New in v3.0?", first.Title);
		Assert.True(first.IsFeatured);
	}

	[Fact]
	public void no_other_card_is_featured()
	{
		InfoViewModel info = new();

		InfoViewModel.Resource[] others = [.. info.Resources.Skip(1)];

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
	public void the_whats_new_page_starts_closed()
	{
		Assert.Null(new InfoViewModel().WhatsNew);
	}

	[Fact]
	public void opening_the_first_card_shows_the_whats_new_page()
	{
		InfoViewModel info = new();
		List<string?> changed = [];
		((INotifyPropertyChanged)info).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		info.Resources[0].Open.Execute(null);

		Assert.NotNull(info.WhatsNew);
		Assert.Contains(nameof(InfoViewModel.WhatsNew), changed);
	}

	[Fact]
	public void back_on_the_whats_new_page_returns_to_the_cards()
	{
		InfoViewModel info = new();
		info.Resources[0].Open.Execute(null);
		WhatsNewViewModel? page = info.WhatsNew;
		Assert.NotNull(page);
		List<string?> changed = [];
		((INotifyPropertyChanged)info).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		page.BackCommand.Execute(null);

		Assert.Null(info.WhatsNew);
		Assert.Contains(nameof(InfoViewModel.WhatsNew), changed);
	}

	[Fact]
	public void the_whats_new_page_can_be_opened_again_after_going_back()
	{
		InfoViewModel info = new();

		info.Resources[0].Open.Execute(null);
		info.WhatsNew!.BackCommand.Execute(null);
		info.Resources[0].Open.Execute(null);

		Assert.NotNull(info.WhatsNew);
	}
}
