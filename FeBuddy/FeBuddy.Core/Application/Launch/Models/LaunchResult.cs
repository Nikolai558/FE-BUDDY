using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.Platform.Models;

namespace FeBuddy.Core.Application.Launch.Models;

/// <summary>
/// The result of <see cref="LaunchSequence.RunAsync"/>.
/// </summary>
/// <param name="Time">The UTC clock / connectivity check outcome.</param>
/// <param name="Version">The version-check outcome.</param>
/// <param name="TempClearFailures">How many temp entries could not be deleted (0 is normal).</param>
public record LaunchResult(UtcTimeCheckResult Time, VersionCheckResult Version, int TempClearFailures);
