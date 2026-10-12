using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="WelcomeViewModel"/>: Go to Info and Close both close the welcome, only the first
/// asks for Info, and the Discord link leaves it open.
/// </summary>
public sealed class WelcomeViewModelTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void either_button_closes_the_welcome_and_only_go_to_info_asks_for_info(bool goToInfo)
	{
		WelcomeViewModel welcome = new(() => { });
		int closes = 0;
		welcome.CloseRequested += (_, _) => closes++;

		(goToInfo ? welcome.GoToInfoCommand : welcome.CloseCommand).Execute(null);

		Assert.Equal(1, closes);
		Assert.Equal(goToInfo, welcome.GoToInfo);
	}

	[Fact]
	public void the_discord_link_opens_and_the_welcome_stays_open()
	{
		int opened = 0;
		WelcomeViewModel welcome = new(() => opened++);
		bool closed = false;
		welcome.CloseRequested += (_, _) => closed = true;

		welcome.OpenDiscordCommand.Execute(null);

		Assert.Equal(1, opened);
		Assert.False(closed);
		Assert.False(welcome.GoToInfo);
	}
}
