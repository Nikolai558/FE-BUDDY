namespace FeBuddy.Core.Models.Services.General;

/// <summary>
/// Where the UTC time in a <see cref="UtcTimeCheckResult"/> came from, in the order the
/// launch check tries them.
/// </summary>
public enum UtcTimeSource
{
	/// <summary>The timeapi.io UTC endpoint responded with a parseable time.</summary>
	TimeApi = 0,

	/// <summary>timeapi.io failed, but a plain HTTP request's <c>Date</c> response header was usable.</summary>
	HttpDateHeader = 1,

	/// <summary>No network time was available; the local machine clock was used.</summary>
	LocalClock = 2,
}

/// <summary>
/// The outcome of the launch-time UTC clock and connectivity check.
/// </summary>
/// <param name="UtcNow">The best available current UTC time.</param>
/// <param name="Source">Which source <paramref name="UtcNow"/> came from.</param>
/// <param name="HasInternetConnection">
/// <see langword="true"/> if any network source responded. When <see langword="false"/>, every
/// internet-dependent feature (AIRAC download, version check, News) is expected to degrade
/// gracefully rather than error.
/// </param>
public record UtcTimeCheckResult(DateTime UtcNow, UtcTimeSource Source, bool HasInternetConnection);
