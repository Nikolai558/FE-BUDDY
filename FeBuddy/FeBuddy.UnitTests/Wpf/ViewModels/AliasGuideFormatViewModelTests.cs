using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="AliasGuideFormatViewModel"/>: Web to start with, the three radio buttons, the
/// files each choice writes, and which button closed the window.
/// </summary>
public sealed class AliasGuideFormatViewModelTests
{
	[Fact]
	public void web_is_chosen_to_start_with()
	{
		AliasGuideFormatViewModel choice = new();

		Assert.Equal(AliasGuideFormatChoice.Web, choice.Choice);
		Assert.True(choice.IsWeb);
		Assert.False(choice.IsMarkdown);
		Assert.False(choice.IsBoth);
		Assert.Equal([AliasGuideFormat.Html], choice.Formats);
		Assert.False(choice.Confirmed);
	}

	[Fact]
	public void markdown_writes_only_markdown()
	{
		AliasGuideFormatViewModel choice = new() { IsMarkdown = true };

		Assert.Equal(AliasGuideFormatChoice.Markdown, choice.Choice);
		Assert.False(choice.IsWeb);
		Assert.Equal([AliasGuideFormat.Markdown], choice.Formats);
	}

	[Fact]
	public void both_writes_the_web_page_then_the_markdown()
	{
		AliasGuideFormatViewModel choice = new() { IsBoth = true };

		Assert.Equal(AliasGuideFormatChoice.Both, choice.Choice);
		Assert.Equal([AliasGuideFormat.Html, AliasGuideFormat.Markdown], choice.Formats);
	}

	[Fact]
	public void choosing_web_again_goes_back_to_one_web_page()
	{
		AliasGuideFormatViewModel choice = new() { IsBoth = true };
		Assert.True(choice.IsBoth);

		choice.IsWeb = true;

		Assert.True(choice.IsWeb);
		Assert.False(choice.IsBoth);
		Assert.Equal([AliasGuideFormat.Html], choice.Formats);
	}

	/// <summary>A radio button unchecked by its neighbour sets false; that must not change the choice.</summary>
	[Fact]
	public void an_unchecked_radio_button_leaves_the_choice_alone()
	{
		AliasGuideFormatViewModel choice = new() { IsMarkdown = true };
		Assert.True(choice.IsMarkdown);

		choice.IsWeb = false;
		choice.IsMarkdown = false;
		choice.IsBoth = false;

		Assert.Equal(AliasGuideFormatChoice.Markdown, choice.Choice);
	}

	[Fact]
	public void a_new_choice_refreshes_every_radio_button()
	{
		AliasGuideFormatViewModel choice = new();
		List<string?> changed = [];
		choice.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		choice.IsBoth = true;

		Assert.Equal(
			[nameof(AliasGuideFormatViewModel.Choice), nameof(AliasGuideFormatViewModel.IsWeb), nameof(AliasGuideFormatViewModel.IsMarkdown), nameof(AliasGuideFormatViewModel.IsBoth)],
			changed);

		changed.Clear();
		choice.Choice = AliasGuideFormatChoice.Both;

		Assert.Empty(changed);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void each_button_closes_the_window_and_says_which_it_was(bool continues)
	{
		AliasGuideFormatViewModel choice = new();
		int closes = 0;
		choice.CloseRequested += (_, _) => closes++;

		(continues ? choice.ContinueCommand : choice.CancelCommand).Execute(null);

		Assert.Equal(1, closes);
		Assert.Equal(continues, choice.Confirmed);
	}
}
