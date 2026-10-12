namespace FeBuddy.Wpf.Shell;

/// <summary>
/// Reads the command line for a dev launch: <c>--dev</c> or <c>--devmode</c>, in any case. Only a
/// Debug build acts on it (see <c>App.OnStartup</c>).
/// </summary>
public static class DevLaunchArgs
{
	/// <summary>The argument that asks for a dev launch, as a restart into one passes it.</summary>
	public const string Argument = "--dev";

	/// <summary>Whether the command line asks for a dev launch.</summary>
	/// <param name="args">The command-line arguments.</param>
	/// <returns><see langword="true"/> when one of them is <c>--dev</c> or <c>--devmode</c>, ignoring case.</returns>
	public static bool IsRequested(IEnumerable<string> args)
	{
		ArgumentNullException.ThrowIfNull(args);

		return args.Any(arg =>
			string.Equals(arg?.Trim(), Argument, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(arg?.Trim(), "--devmode", StringComparison.OrdinalIgnoreCase));
	}
}
