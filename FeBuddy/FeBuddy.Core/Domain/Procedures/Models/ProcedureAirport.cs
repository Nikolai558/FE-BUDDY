namespace FeBuddy.Core.Domain.Procedures.Models;

/// <summary>
/// One airport the d-TPP Metafile lists, joined to its NASR <c>APT_BASE</c>/<c>CLS_ARSP</c> data
/// where available, with every procedure <c>ProcedureBuilder</c> found for it.
/// </summary>
public sealed record ProcedureAirport
{
	/// <summary>The FAA identifier (metafile <c>apt_ident</c>).</summary>
	public required string AptIdent { get; init; }

	/// <summary>
	/// The ICAO identifier: the metafile's <c>icao_ident</c> when it has one, else NASR
	/// <c>APT_BASE.ICAO_ID</c>, else <see langword="null"/>.
	/// </summary>
	public string? IcaoIdent { get; init; }

	/// <summary>The airport name (metafile <c>airport_name</c>).</summary>
	public required string Name { get; init; }

	/// <summary>The city name (metafile <c>city_name</c>).</summary>
	public required string City { get; init; }

	/// <summary>The state post office code (metafile <c>state_code</c>).</summary>
	public required string State { get; init; }

	/// <summary>Whether the metafile flags the airport military (<c>military</c> = <c>"M"</c>).</summary>
	public required bool IsMilitary { get; init; }

	/// <summary>The metafile's Approach and Landing number (<c>alnum</c>).</summary>
	public required int Alnum { get; init; }

	/// <summary>
	/// The responsible ARTCC (NASR <c>APT_BASE.RESP_ARTCC_ID</c>), or <see langword="null"/> when the
	/// airport is not in this cycle's NASR data.
	/// </summary>
	public string? ResponsibleArtcc { get; init; }

	/// <summary>The highest class airspace overlying the airport (NASR <c>CLS_ARSP</c>).</summary>
	public ProcedureAirspaceClass AirspaceClass { get; init; } = ProcedureAirspaceClass.None;

	/// <summary>Airport reference point latitude in decimal degrees (NASR), or <see langword="null"/> when not in NASR.</summary>
	public double? Latitude { get; init; }

	/// <summary>Airport reference point longitude in decimal degrees (NASR), or <see langword="null"/> when not in NASR.</summary>
	public double? Longitude { get; init; }

	/// <summary>Every procedure the metafile lists for this airport, in chartseq (file) order.</summary>
	public required IReadOnlyList<Procedure> Procedures { get; init; }
}
