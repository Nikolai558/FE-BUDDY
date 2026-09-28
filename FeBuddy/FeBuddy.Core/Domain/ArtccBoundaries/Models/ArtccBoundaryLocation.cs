namespace FeBuddy.Core.Domain.ArtccBoundaries.Models;

/// <summary>
/// One ARTCC/CERAP boundary location, built from its <c>ARB_BASE</c> row.
/// </summary>
/// <remarks>
/// A location can hold boundary rings at more than one <see cref="ArtccBoundaryAltitude"/>
/// (see <see cref="ArtccBoundaryRing"/>), so it is modeled separately from a ring rather than
/// repeated on every one. When a <c>LocationId</c> appears in <c>ARB_SEG</c> with no matching
/// <c>ARB_BASE</c> row, <c>ArtccBoundaryBuilder</c> builds one of these from
/// <c>ARB_SEG.LOCATION_NAME</c> alone, leaving every other field blank.
/// </remarks>
public sealed class ArtccBoundaryLocation
{
	/// <summary>The location identifier, <c>ARB_BASE.LOCATION_ID</c>, trimmed and upper-cased.</summary>
	public required string LocationId { get; init; }

	/// <summary>The center (ARTCC) name, <c>ARB_BASE.LOCATION_NAME</c>, trimmed. Blank when NASR publishes none.</summary>
	public string LocationName { get; init; } = string.Empty;

	/// <summary>The location's computer identifier, <c>ARB_BASE.COMPUTER_ID</c>, trimmed. Blank when there is no <c>ARB_BASE</c> row.</summary>
	public string ComputerId { get; init; } = string.Empty;

	/// <summary>The ICAO identifier, <c>ARB_BASE.ICAO_ID</c>, trimmed. Blank when NASR publishes none, or there is no <c>ARB_BASE</c> row.</summary>
	public string IcaoId { get; init; } = string.Empty;

	/// <summary>The location type (<c>ARTCC</c> or <c>CERAP</c>), <c>ARB_BASE.LOCATION_TYPE</c>, trimmed. Blank when there is no <c>ARB_BASE</c> row.</summary>
	public string LocationType { get; init; } = string.Empty;

	/// <summary>The location's city, <c>ARB_BASE.CITY</c>, trimmed. Blank when there is no <c>ARB_BASE</c> row.</summary>
	public string City { get; init; } = string.Empty;

	/// <summary>The location's country code, <c>ARB_BASE.COUNTRY_CODE</c>, trimmed. Blank when there is no <c>ARB_BASE</c> row.</summary>
	public string CountryCode { get; init; } = string.Empty;
}
