namespace FeBuddy.Core.Application.Launch.Models;

/// <summary>
/// How FE-Buddy was started: a normal launch, which runs every launch step, or a dev launch (a
/// Debug build started with <c>--dev</c>), which runs only the <see cref="OptionalLaunchStep"/>s
/// chosen on Settings' Developer card. The app works out which from its command line; Core only
/// ever sees this.
/// </summary>
/// <param name="IsDevLaunch">Whether this is a dev launch.</param>
/// <param name="Steps">The optional steps a dev launch runs. A normal launch ignores it.</param>
public sealed record LaunchOptions(bool IsDevLaunch, OptionalLaunchStep Steps)
{
	/// <summary>A normal launch: every step runs.</summary>
	public static LaunchOptions Normal { get; } = new(false, OptionalLaunchStep.All);

	/// <summary>A dev launch that runs only <paramref name="steps"/>.</summary>
	/// <param name="steps">The optional steps to run.</param>
	/// <returns>The options.</returns>
	public static LaunchOptions DevLaunch(OptionalLaunchStep steps) => new(true, steps);

	/// <summary>
	/// Whether the app has already read the settings (<see cref="LaunchSequence.ReadSettings"/>) before
	/// building its window, so the launch sequence doesn't read them again.
	/// </summary>
	public bool SettingsRead { get; init; }

	/// <summary>Whether a step runs: always in a normal launch, and in a dev launch when it was chosen.</summary>
	/// <param name="step">The step.</param>
	/// <returns><see langword="true"/> when it runs.</returns>
	public bool Runs(OptionalLaunchStep step) => !IsDevLaunch || (Steps & step) == step;
}
