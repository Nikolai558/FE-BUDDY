namespace FeBuddy.Core.Application.Airac.Procedures.Models;

/// <summary>
/// One optional field <c>Procedures.json</c> can carry, chosen with the <c>JsonFields</c> setting.
/// A field's name is its enum name with a lower-case first letter (<c>ResponsibleArtcc</c> is
/// chosen as <c>responsibleArtcc</c> and written as the JSON key <c>responsibleArtcc</c>) - see
/// <c>FebProperties</c>, which this reuses for name lookups even though these are not <c>feb.*</c>
/// GeoJSON properties.
/// </summary>
public enum ProcedureJsonField
{
	/// <summary>The airport's ICAO identifier.</summary>
	IcaoId,

	/// <summary>The airport's name.</summary>
	AirportName,

	/// <summary>The airport's city.</summary>
	City,

	/// <summary>The airport's state post office code.</summary>
	State,

	/// <summary>The airport's responsible ARTCC.</summary>
	ResponsibleArtcc,

	/// <summary>The airport's highest class airspace.</summary>
	AirspaceClass,

	/// <summary>Whether the airport is military.</summary>
	Military,

	/// <summary>The procedure's chart type (chart code).</summary>
	ChartType,

	/// <summary>The procedure's chart URL (and, for a multi-page chart, its continuation pages' URLs).</summary>
	ChartUrl,

	/// <summary>The procedure's change this cycle, when it changed.</summary>
	Change,

	/// <summary>The procedure's compare-PDF URL, for a <see cref="Change"/> of "Changed".</summary>
	CompareUrl,

	/// <summary>The procedure's amendment number.</summary>
	Amendment,

	/// <summary>The procedure's amendment date.</summary>
	AmendmentDate,

	/// <summary>The procedure's FAA database identifier.</summary>
	ProcedureUid,

	/// <summary>The procedure's SID/STAR computer code.</summary>
	ComputerCode,

	/// <summary>Who produced the chart (FAA, FAA joint-use, or NGA).</summary>
	Producer,
}
