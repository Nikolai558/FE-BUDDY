namespace FeBuddy.Core.Infrastructure.Telephony.Models;

/// <summary>
/// One virtual airline on the virtual airline list: a row of GNG's list
/// (<see cref="Parsers.GngAirlineParser"/>) or of VATSIM-Radar's (<see cref="Parsers.VatsimRadarAirlineParser"/>),
/// trimmed, otherwise as published.
/// </summary>
/// <param name="Icao">Its <c>icao</c>: the three-letter designator, e.g. <c>DAL</c> - not always three letters.</param>
/// <param name="Name">Its virtual organization, e.g. <c>Fly Delta Virtual</c> (GNG's <c>airline</c>, VATSIM-Radar's <c>name</c>).</param>
/// <param name="Callsign">Its <c>callsign</c>: the telephony, e.g. <c>Delta</c>.</param>
public sealed record VatsimRadarAirline(string Icao, string Name, string Callsign);
