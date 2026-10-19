using System;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>
/// How the installation ended: from what Windows Installer said, and - when it ended without saying -
/// from what is installed now. Uses no Windows Installer or WPF types, so the unit tests compile it in.
/// </summary>
public static class SetupOutcomeRules
{
	/// <summary>No <c>INSTALLMESSAGE_INSTALLEND</c> arrived, or it had no result. Not a value Windows Installer sends.</summary>
	public const int ResultNotSent = -1;

	/// <summary>A result of 0, no action result: what Wine's msi sends for a failure, and taken as one.</summary>
	public const int ResultNone = 0;

	/// <summary>The installation succeeded.</summary>
	public const int ResultSuccess = 1;

	/// <summary>The user cancelled the installation.</summary>
	public const int ResultUserExit = 2;

	/// <summary>The installation failed.</summary>
	public const int ResultFailure = 3;

	/// <summary>Decides the finish page's outcome.</summary>
	/// <param name="cancelSent">Whether the window cancelled the installation (the user's Cancel, or an answer that stops it).</param>
	/// <param name="fatalExit">Whether Windows Installer reported a fatal error.</param>
	/// <param name="installResult">
	/// <c>INSTALLMESSAGE_INSTALLEND</c>'s result field (an action result: 1 succeeded, 2 cancelled,
	/// 3 failed), or <see cref="ResultNotSent"/> when it didn't arrive or had no result.
	/// </param>
	/// <param name="removing">Whether FE-Buddy was being uninstalled.</param>
	/// <param name="versionBefore">The version installed before, or <see langword="null"/> for none.</param>
	/// <param name="versionWanted">The version this setup installs.</param>
	/// <param name="versionAfter">Reads the version installed now; <see langword="null"/> for none. Only read when it decides.</param>
	/// <returns>The outcome.</returns>
	/// <remarks>
	/// Windows Installer ends without <c>INSTALLMESSAGE_INSTALLEND</c> when the installation never
	/// ran, most often because its administrator prompt was declined. That is
	/// <see cref="SetupOutcome.NotStarted"/>, unless the machine shows the change was made after all: a
	/// new version installed, or FE-Buddy gone. A repair, or reinstalling the same version, can't be
	/// told from nothing happening, so it reads as not started.
	/// </remarks>
	public static SetupOutcome Decide(
		bool cancelSent,
		bool fatalExit,
		int installResult,
		bool removing,
		string? versionBefore,
		string versionWanted,
		Func<string?> versionAfter)
	{
		if (cancelSent || installResult == ResultUserExit)
		{
			return SetupOutcome.Cancelled;
		}

		if (fatalExit || installResult is ResultFailure or ResultNone)
		{
			return SetupOutcome.Failed;
		}

		if (installResult != ResultNotSent)
		{
			return SetupOutcome.Succeeded;
		}

		bool done = removing
			? versionAfter() is null
			: !string.Equals(versionBefore, versionWanted, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(versionAfter(), versionWanted, StringComparison.OrdinalIgnoreCase);

		return done ? SetupOutcome.Succeeded : SetupOutcome.NotStarted;
	}
}
