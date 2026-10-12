using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.Launch.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers Settings' Developer card (<see cref="DevLaunchCardViewModel"/>): it exists only in a dev
/// launch, starts from the saved choices, saves each change at once, and restarts into a dev launch
/// only once saved.
/// </summary>
[Collection("AppLog")]
public sealed class DevLaunchCardViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_DevCard_" + Guid.NewGuid().ToString("N"));
	private int _restarts;

	public DevLaunchCardViewModelTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		DevLaunchSettings.ConfigureForTesting(_root);
	}

	public void Dispose()
	{
		DevLaunchSettings.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch
		{
			// Best-effort.
		}
	}

	private void Restart() => _restarts++;

	[Fact]
	public void a_normal_launch_has_no_developer_card()
	{
		Assert.Null(DevLaunchCardViewModel.For(LaunchOptions.Normal, Restart));
	}

	[Fact]
	public void a_dev_launch_shows_every_step_ticked_as_saved()
	{
		DevLaunchSettings.Save(OptionalLaunchStep.ParseAiracData | OptionalLaunchStep.DownloadNews);

		DevLaunchCardViewModel card = DevLaunchCardViewModel.For(LaunchOptions.DevLaunch(OptionalLaunchStep.None), Restart)!;

		Assert.Equal(OptionalLaunchSteps.InCardOrder, card.Steps.Select(toggle => toggle.Step));
		Assert.Equal(["Parse AIRAC data", "Download News"], card.Steps.Where(toggle => toggle.IsChecked).Select(toggle => toggle.Label));
		Assert.Equal(OptionalLaunchStep.ParseAiracData | OptionalLaunchStep.DownloadNews, card.Chosen);
		Assert.All(card.Steps, toggle => Assert.False(string.IsNullOrWhiteSpace(toggle.Description)));
	}

	[Fact]
	public void with_nothing_saved_every_box_is_off()
	{
		DevLaunchCardViewModel card = DevLaunchCardViewModel.For(LaunchOptions.DevLaunch(OptionalLaunchStep.All), Restart)!;

		Assert.All(card.Steps, toggle => Assert.False(toggle.IsChecked));
	}

	[Fact]
	public void each_tick_is_saved_straight_away_for_the_next_dev_launch()
	{
		DevLaunchCardViewModel card = new(OptionalLaunchStep.None, Restart);

		card.Steps.Single(toggle => toggle.Step == OptionalLaunchStep.DownloadAiracData).IsChecked = true;
		Assert.Equal(OptionalLaunchStep.DownloadAiracData, DevLaunchSettings.Read());

		card.Steps.Single(toggle => toggle.Step == OptionalLaunchStep.WelcomeWindow).IsChecked = true;
		card.Steps.Single(toggle => toggle.Step == OptionalLaunchStep.DownloadAiracData).IsChecked = false;
		Assert.Equal(OptionalLaunchStep.WelcomeWindow, DevLaunchSettings.Read());

		Assert.False(card.HasSaveError);
		Assert.Equal(0, _restarts);
	}

	[Fact]
	public void restart_saves_the_boxes_then_restarts()
	{
		DevLaunchCardViewModel card = new(OptionalLaunchStep.CheckForNewerVersion, Restart);

		card.RestartCommand.Execute(null);

		Assert.Equal(1, _restarts);
		Assert.Equal(OptionalLaunchStep.CheckForNewerVersion, DevLaunchSettings.Read());
	}

	[Fact]
	public void choices_that_cannot_be_saved_say_why_and_do_not_restart()
	{
		// A folder where the file should be: it can't be written.
		Directory.CreateDirectory(DevLaunchSettings.FilePath);
		DevLaunchCardViewModel card = new(OptionalLaunchStep.None, Restart);

		card.Steps[0].IsChecked = true;

		Assert.True(card.HasSaveError);
		Assert.StartsWith("Couldn't save these choices:", card.SaveError, StringComparison.Ordinal);

		card.RestartCommand.Execute(null);
		Assert.Equal(0, _restarts);
	}

	[Fact]
	public void a_restart_is_required()
	{
		Assert.Throws<ArgumentNullException>(() => new DevLaunchCardViewModel(OptionalLaunchStep.None, null!));
		Assert.Throws<ArgumentNullException>(() => DevLaunchCardViewModel.For(null!, Restart));
	}
}
