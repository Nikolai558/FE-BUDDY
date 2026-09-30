namespace FeBuddy.Core.Application.Conversions.EramToGeojson.Models;

/// <summary>
/// One thing that keeps an ERAM object's defaults from being CRC defaults: a property CRC's
/// defaults need that they lack, or hold with a value CRC can't draw.
/// </summary>
/// <param name="Property">The CRC property, e.g. <c>bcg</c> or <c>style</c>.</param>
/// <param name="Value">The value CRC can't draw, as the XML gives it (<c>DME</c>), or <see langword="null"/> when the defaults have none.</param>
internal sealed record EramDefaultsGap(string Property, string? Value);
