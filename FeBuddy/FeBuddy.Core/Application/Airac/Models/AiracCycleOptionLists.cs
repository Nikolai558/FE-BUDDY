using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// The options the AIRAC Service's tabs offer for one cycle, worked out from its NASR data by
/// <see cref="AiracCycleOptionListBuilder"/>, each list in the order its tab shows it. A table the
/// cycle's data doesn't hold gives an empty list.
/// </summary>
public sealed class AiracCycleOptionLists
{
	/// <summary>Airway designations (<c>J</c>, <c>Q</c>, <c>V</c>, ...), the letters <c>AWY_BASE.AWY_ID</c> starts with, sorted.</summary>
	public required IReadOnlyList<string> AirwayDesignations { get; init; }

	/// <summary>The ARTCCs a STAR belongs to (<c>STAR_BASE.ARTCC</c>, which can name several), sorted.</summary>
	public required IReadOnlyList<string> StarArtccs { get; init; }

	/// <summary>The ARTCCs a departure belongs to (<c>DP_BASE.ARTCC</c>), sorted.</summary>
	public required IReadOnlyList<string> DepartureArtccs { get; init; }

	/// <summary>The types of the NAVAIDs that aren't shut down: the known types first, in their usual order, then the rest by name.</summary>
	public required IReadOnlyList<string> NavaidTypes { get; init; }

	/// <summary>The ARTCC boundary location IDs (<c>ARB_SEG.LOCATION_ID</c>), sorted.</summary>
	public required IReadOnlyList<string> BoundaryLocations { get; init; }

	/// <summary>Each location's boundary altitudes, as NASR spells them: by location, then HIGH, LOW, UNLIMITED.</summary>
	public required IReadOnlyList<(string LocationId, string Altitude)> BoundaryLocationAltitudes { get; init; }

	/// <summary>The fix uses: the known ones first, in their usual order, then the rest by name.</summary>
	public required IReadOnlyList<string> FixUseNames { get; init; }

	/// <summary>The charts fixes are on, each with its token, sorted by name.</summary>
	public required IReadOnlyList<(string Token, string Name)> FixCharts { get; init; }

	/// <summary>Whether a fix is on no chart.</summary>
	public required bool HasFixOnNoChart { get; init; }

	/// <summary>The ARTCCs responsible for an airport (<c>APT_BASE.RESP_ARTCC_ID</c>), sorted.</summary>
	public required IReadOnlyList<string> AirportFacilities { get; init; }

	/// <summary>Each airport's FAA ID, by its FAA ID and by its ICAO ID, ignoring case.</summary>
	public required IReadOnlyDictionary<string, string> AirportFaaIdsByIdent { get; init; }

	/// <summary>Each airport's <c>APT_BASE</c> row, by its FAA ID, ignoring case.</summary>
	public required IReadOnlyDictionary<string, AptCsvDataModel.AptBase> AirportsByFaaId { get; init; }
}
