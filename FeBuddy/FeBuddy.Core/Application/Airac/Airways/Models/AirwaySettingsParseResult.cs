using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// The result of parsing a raw Airways settings dictionary: the typed settings object plus
/// any levelled messages noticed along the way (currently, unrecognized dictionary keys).
/// </summary>
/// <param name="Settings">The fully-parsed, typed settings.</param>
/// <param name="Messages">Levelled messages noticed while parsing, e.g. an unrecognized key at <see cref="LogLevel.Warning"/>.</param>
public sealed record AirwaySettingsParseResult(
	AirwaySettings Settings,
	IReadOnlyList<ServiceMessage> Messages);
