namespace FeBuddy.Core.Infrastructure.Telephony.Models;

/// <summary>
/// One virtual airline on the VATSIM-Radar Virtual Airline List (<see cref="TelephonyFiles.VatsimRadarAirlinesUrl"/>),
/// as <see cref="Parsers.VatsimRadarAirlineParser"/> reads it: trimmed, otherwise as published.
/// </summary>
/// <param name="Icao">Its <c>icao</c>: the three-letter designator, e.g. <c>DAL</c> - not always three letters.</param>
/// <param name="Name">Its <c>name</c>: the virtual organization, e.g. <c>Fly Delta Virtual</c>.</param>
/// <param name="Callsign">Its <c>callsign</c>: the telephony, e.g. <c>Delta</c>.</param>
public sealed record VatsimRadarAirline(string Icao, string Name, string Callsign);
