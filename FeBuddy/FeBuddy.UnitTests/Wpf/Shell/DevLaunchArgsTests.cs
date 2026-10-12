using FeBuddy.Wpf.Shell;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers <see cref="DevLaunchArgs.IsRequested"/>: <c>--dev</c> and <c>--devmode</c> ask for a dev
/// launch in any case; nothing else does. (That only a Debug build acts on it is <c>App.OnStartup</c>'s
/// <c>#if DEBUG</c>, which isn't tested.)
/// </summary>
public sealed class DevLaunchArgsTests
{
	[Theory]
	[InlineData("--dev")]
	[InlineData("--DEV")]
	[InlineData("--Dev")]
	[InlineData("--devmode")]
	[InlineData("--DevMode")]
	[InlineData("--DEVMODE")]
	[InlineData(" --dev ")]
	public void dev_or_devmode_in_any_case_asks_for_a_dev_launch(string arg)
	{
		Assert.True(DevLaunchArgs.IsRequested([arg]));
		Assert.True(DevLaunchArgs.IsRequested(["--other", arg]));
	}

	[Theory]
	[InlineData("-dev")]
	[InlineData("/dev")]
	[InlineData("dev")]
	[InlineData("--developer")]
	[InlineData("--dev-mode")]
	[InlineData("")]
	public void anything_else_does_not(string arg)
	{
		Assert.False(DevLaunchArgs.IsRequested([arg]));
	}

	[Fact]
	public void no_arguments_is_a_normal_launch()
	{
		Assert.False(DevLaunchArgs.IsRequested([]));
		Assert.Throws<ArgumentNullException>(() => DevLaunchArgs.IsRequested(null!));
	}

	[Fact]
	public void a_restart_into_a_dev_launch_passes_an_argument_that_asks_for_one()
	{
		Assert.True(DevLaunchArgs.IsRequested([DevLaunchArgs.Argument]));
	}
}
