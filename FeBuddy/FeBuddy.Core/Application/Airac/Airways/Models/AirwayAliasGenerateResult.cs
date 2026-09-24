namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// The result of generating the Airways alias file.
/// </summary>
/// <param name="FilePath">
/// The full path written, or <see langword="null"/> when there were no airways with any
/// waypoints to write.
/// </param>
/// <param name="AirwayLineCount">How many airway lines were written to the file.</param>
public sealed record AirwayAliasGenerateResult(string? FilePath, int AirwayLineCount);
