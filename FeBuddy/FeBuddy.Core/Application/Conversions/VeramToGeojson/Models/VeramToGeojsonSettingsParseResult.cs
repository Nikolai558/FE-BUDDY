using FeBuddy.Core.Application.Models;

namespace FeBuddy.Core.Application.Conversions.VeramToGeojson.Models;

/// <summary>The outcome of parsing the raw vERAM to GeoJSON settings dictionary.</summary>
/// <param name="Settings">The typed, validated settings.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record VeramToGeojsonSettingsParseResult(VeramToGeojsonSettings Settings, IReadOnlyList<ServiceMessage> Messages);
