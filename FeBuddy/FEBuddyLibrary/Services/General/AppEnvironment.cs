using FEBuddyLibrary.Models.Services.General;

namespace FEBuddyLibrary.Services.General;

/// <summary>
/// Process-wide facts established by the launch sequence and read by the GUI and by other
/// services: whether the machine is online, the trustworthy launch-time UTC clock, and the
/// version-check result.
/// </summary>
/// <remarks>
/// Per <c>Developer_Notes.md</c>, <see cref="HasInternetConnection"/> is assumed
/// <see langword="true"/> until the connectivity check proves otherwise, so a feature that
/// reads it before launch finishes does not wrongly disable itself.
/// </remarks>
public static class AppEnvironment
{
	/// <summary>
	/// Whether the machine currently has internet access. Assumed <see langword="true"/> until
	/// <c>LaunchSequence</c> runs the connectivity check and possibly sets it to
	/// <see langword="false"/>.
	/// </summary>
	public static bool HasInternetConnection { get; internal set; } = true;

	/// <summary>
	/// The UTC time captured at launch from the most trustworthy available source. All AIRAC
	/// cycle maths should be based on this rather than <see cref="DateTime.UtcNow"/> so a run
	/// is consistent even if the machine clock drifts mid-session.
	/// </summary>
	public static DateTime? LaunchUtcNow { get; internal set; }

	/// <summary>How <see cref="LaunchUtcNow"/> was obtained.</summary>
	public static UtcTimeSource? LaunchUtcSource { get; internal set; }

	/// <summary>The result of the launch-time version check, or <see langword="null"/> until it runs.</summary>
	public static VersionCheckResult? Version { get; internal set; }

	/// <summary><see langword="true"/> once the launch sequence has finished (successfully or with degraded steps).</summary>
	public static bool LaunchCompleted { get; internal set; }

	/// <summary>
	/// Raised whenever any property here changes. The GUI subscribes and marshals to its
	/// dispatcher.
	/// </summary>
	public static event EventHandler? Changed;

	/// <summary>Raises <see cref="Changed"/>. Called by <c>LaunchSequence</c> after each update.</summary>
	internal static void RaiseChanged()
	{
		try
		{
			Changed?.Invoke(null, EventArgs.Empty);
		}
		catch
		{
			// A misbehaving subscriber must not break the launch sequence.
		}
	}

	/// <summary>Resets every value to its pre-launch default. Unit tests only.</summary>
	internal static void ResetForTesting()
	{
		HasInternetConnection = true;
		LaunchUtcNow = null;
		LaunchUtcSource = null;
		Version = null;
		LaunchCompleted = false;
		Changed = null;
	}
}
