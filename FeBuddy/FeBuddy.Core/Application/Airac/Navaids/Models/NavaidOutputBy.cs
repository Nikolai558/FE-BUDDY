namespace FeBuddy.Core.Application.Airac.Navaids.Models;

/// <summary>How the NAVAIDs sub-service groups its GeoJSON output into files.</summary>
public enum NavaidOutputBy
{
	/// <summary>One Symbols file and one Text file for every included NAVAID, whatever its type.</summary>
	All = 0,

	/// <summary>One Symbols file and one Text file per NAVAID type present (see <see cref="NavaidOutputFiles.TypeKey"/>).</summary>
	Type = 1,
}
