namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// Which file an airway designation's airways go in when Airways writes High and Low files
/// (<see cref="AirwayGeojsonOutputBy.HighLow"/>). The user chooses it per designation.
/// </summary>
public enum AirwayStratum
{
	/// <summary>The <c>Airways_High</c> files only.</summary>
	High,

	/// <summary>The <c>Airways_Low</c> files only.</summary>
	Low,

	/// <summary>Both the <c>Airways_High</c> and the <c>Airways_Low</c> files.</summary>
	Both,
}
