namespace FeBuddy.Core.Application.Map.Models;

/// <summary>The live layers the Map screen can build from a parsed AIRAC cycle (<see cref="AiracMapData"/>).</summary>
public enum AiracLayerKind
{
	/// <summary>Every ARTCC boundary ring, High, Low and Unlimited together.</summary>
	ArtccBoundaries,

	/// <summary>Airports with a control tower, plus their runways once zoomed in.</summary>
	ToweredAirports,

	/// <summary>VOR, VOR/DME, VORTAC and TACAN stations.</summary>
	Navaids,
}
