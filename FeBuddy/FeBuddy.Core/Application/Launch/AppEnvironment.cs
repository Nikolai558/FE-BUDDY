using FeBuddy.Core.Application.News.Models;
using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Platform;
using FeBuddy.Core.Infrastructure.Platform.Models;

namespace FeBuddy.Core.Application.Launch;

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

	/// <summary>The result of the launch-time News check, or <see langword="null"/> until it runs.</summary>
	public static NewsCheckResult? News { get; internal set; }

	/// <summary>
	/// <see langword="true"/> when this copy is the one the MSI installed (see
	/// <see cref="InstalledProduct.IsMsiInstalled"/>); <see langword="false"/> for a dev build or a
	/// copy run from anywhere else. Read once per process.
	/// </summary>
	public static bool IsMsiInstalled => IsMsiInstalledLazy.Value;

	private static readonly Lazy<bool> IsMsiInstalledLazy = new(() => InstalledProduct.IsMsiInstalled(AppContext.BaseDirectory));

	/// <summary><see langword="true"/> once the launch sequence has finished (successfully or with degraded steps).</summary>
	public static bool LaunchCompleted { get; internal set; }

	/// <summary>
	/// Raised whenever any property here changes. The GUI subscribes and marshals to its
	/// dispatcher.
	/// </summary>
	public static event EventHandler? Changed;

	/// <summary>
	/// The <see cref="HttpClient"/> the launch and re-check network calls use, or
	/// <see langword="null"/> (always, outside tests) for each check to create its own. Unit
	/// tests only.
	/// </summary>
	internal static HttpClient? HttpClientForTesting { get; set; }

	/// <summary>
	/// Re-runs the online-state checks (UTC time / internet, then the version check) and
	/// publishes the results, raising <see cref="Changed"/>. For the shell's "re-check" action
	/// and Settings' "check for updates now". Never throws.
	/// </summary>
	/// <param name="cancellationToken">Cancels the network calls.</param>
	public static async Task RecheckAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			UtcTimeCheckResult time = await UtcTimeCheck.RunAsync(HttpClientForTesting, cancellationToken).ConfigureAwait(false);
			HasInternetConnection = time.HasInternetConnection;
			LaunchUtcNow = time.UtcNow;
			LaunchUtcSource = time.Source;
			RaiseChanged();

			FeBuddy.Versioning.ReleaseChannel channel = VersionCheckResult.ParseChannel(
				UserConfigFile.GetValue("General.UpdateChannel"));

			string currentVersion = Version?.CurrentVersion ?? AppVersion.Current;

			Version = await VersionCheck
				.RunAsync(currentVersion, channel, time.HasInternetConnection, HttpClientForTesting, cancellationToken)
				.ConfigureAwait(false);

			RaiseChanged();
		}
		catch
		{
			// Best-effort; the individual checks log their own failures.
		}
	}

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
		News = null;
		LaunchCompleted = false;
		Changed = null;
		HttpClientForTesting = null;
	}
}
