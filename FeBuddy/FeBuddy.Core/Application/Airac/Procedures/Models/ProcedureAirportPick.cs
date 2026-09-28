namespace FeBuddy.Core.Application.Airac.Procedures.Models;

/// <summary>One <c>"APT|PROCEDURE NAME"</c> entry from the <c>AirportProcedures</c> setting.</summary>
/// <param name="Airport">The FAA or ICAO identifier of the airport, as typed.</param>
/// <param name="ProcedureName">The procedure's base chart name, as typed.</param>
public sealed record ProcedureAirportPick(string Airport, string ProcedureName);
