using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="NavItem"/>: its page is built once, and the shell hears about the row both
/// when it becomes active and when it is clicked again while active (which takes a page back to
/// its start).
/// </summary>
public sealed class NavItemTests
{
	private const string Glyph = "i";

	[Fact]
	public void becoming_active_tells_the_shell_once()
	{
		int activated = 0;
		NavItem item = new("Info", Glyph, () => new object(), _ => activated++) { IsActive = true };

		item.IsActive = true;

		Assert.Equal(1, activated);
	}

	[Fact]
	public void a_click_on_the_active_row_tells_the_shell_again()
	{
		int activated = 0;
		NavItem item = new("Info", Glyph, () => new object(), _ => activated++) { IsActive = true };

		item.ClickCommand.Execute(null);

		Assert.Equal(2, activated);
	}

	[Fact]
	public void a_click_on_an_inactive_row_is_left_to_its_radio_button()
	{
		int activated = 0;
		NavItem item = new("Info", Glyph, () => new object(), _ => activated++);

		item.ClickCommand.Execute(null);

		Assert.Equal(0, activated);
	}

	[Fact]
	public void the_page_is_built_once_and_only_when_asked_for()
	{
		int built = 0;
		NavItem item = new("Info", Glyph, () => { built++; return new object(); }, _ => { });

		Assert.Null(item.CreatedViewModel);
		object first = item.ViewModel;

		Assert.Same(first, item.ViewModel);
		Assert.Same(first, item.CreatedViewModel);
		Assert.Equal(1, built);
	}
}
