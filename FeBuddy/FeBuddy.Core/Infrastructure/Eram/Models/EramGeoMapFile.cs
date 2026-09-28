namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>What an ERAM <c>Geomaps.xml</c> (<c>Geomaps_Records</c>) holds, as read by <c>EramGeoMapReader</c>.</summary>
/// <param name="SourcePath">The file it was read from (or the name given to text read directly).</param>
/// <param name="Maps">Every <c>GeoMapRecord</c>, in file order.</param>
/// <param name="Problems">
/// Each element or value that could not be used, with its line number. The rest of the file is
/// still read.
/// </param>
public sealed record EramGeoMapFile(
	string SourcePath,
	IReadOnlyList<EramGeoMap> Maps,
	IReadOnlyList<string> Problems);
