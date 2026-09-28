using System;

namespace FeBuddy.Versioning;

/// <summary>
/// FE-Buddy's one version-transition rule, shared by the installer's version-policy custom
/// action (FeBuddy.Installer.CustomActions) and the app.
/// </summary>
/// <remarks>
/// Moving to an equal or higher-precedence version is always allowed. Moving to a lower one (a
/// downgrade) is allowed only when the <b>installed</b> version is itself a pre-release - so
/// 2.9.0 -&gt; 3.0.0-alpha.1 -&gt; 2.9.0 is allowed (opting out of a pre-release back to stable),
/// but 3.0.0 -&gt; 2.9.0 is not (stable never silently regresses to an older stable).
/// </remarks>
public static class UpdatePolicy
{
	/// <summary>Is moving from <paramref name="installed"/> to <paramref name="candidate"/> allowed?</summary>
	/// <param name="installed">The installed version, or <see langword="null"/> for a fresh install.</param>
	/// <param name="candidate">The version about to be installed.</param>
	/// <returns><see langword="true"/> if the transition is allowed.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="candidate"/> is <see langword="null"/>.</exception>
	public static bool IsTransitionAllowed(ProductVersion? installed, ProductVersion candidate)
	{
		if (candidate is null)
		{
			throw new ArgumentNullException(nameof(candidate));
		}

		// Nothing installed (fresh install) - always allowed.
		if (installed is null)
		{
			return true;
		}

		// Equal or forward precedence (including re-running the same installer) - always allowed.
		if (candidate.ComparePrecedenceTo(installed) >= 0)
		{
			return true;
		}

		// A genuine downgrade: only off a pre-release (opting back out to stable).
		return installed.IsPrerelease;
	}

	/// <summary>
	/// String overload for callers holding raw version text (the MSI's session properties).
	/// Unparseable input fails open - returns <see langword="true"/> - so a parsing problem can
	/// never brick an install; callers log the raw strings so the problem is still visible.
	/// </summary>
	/// <param name="installedVersionText">The installed version, or blank for a fresh install.</param>
	/// <param name="candidateVersionText">The version about to be installed.</param>
	/// <returns><see langword="true"/> if the transition is allowed (or could not be judged).</returns>
	public static bool IsTransitionAllowed(string? installedVersionText, string? candidateVersionText)
	{
		ProductVersion? installed = null;
		if (!string.IsNullOrWhiteSpace(installedVersionText)
			&& !ProductVersion.TryParse(installedVersionText, out installed))
		{
			return true;
		}

		if (!ProductVersion.TryParse(candidateVersionText, out ProductVersion? candidate))
		{
			return true;
		}

		return IsTransitionAllowed(installed, candidate!);
	}
}
