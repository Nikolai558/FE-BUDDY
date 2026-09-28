namespace FeBuddy.Core.Infrastructure.Sct.Models;

/// <summary>The sector-file sections made of plain line segments.</summary>
public enum SctLineSection
{
	/// <summary><c>[ARTCC]</c> - center boundaries.</summary>
	Artcc,

	/// <summary><c>[ARTCC HIGH]</c> - high-altitude boundaries.</summary>
	ArtccHigh,

	/// <summary><c>[ARTCC LOW]</c> - low-altitude boundaries.</summary>
	ArtccLow,

	/// <summary><c>[LOW AIRWAY]</c> - low airways.</summary>
	LowAirway,

	/// <summary><c>[HIGH AIRWAY]</c> - high airways.</summary>
	HighAirway,

	/// <summary><c>[GEO]</c> - coastlines, runways and other geography.</summary>
	Geo,
}
