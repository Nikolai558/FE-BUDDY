using FeBuddy.Core.Application.Models;

namespace FeBuddy.Core.Application.Conversions.DatToGeojson.Models;

/// <summary>The outcome of parsing the raw DAT to GeoJSON settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record DatToGeojsonSettingsParseResult(DatToGeojsonSettings Settings, IReadOnlyList<ServiceMessage> Messages);
