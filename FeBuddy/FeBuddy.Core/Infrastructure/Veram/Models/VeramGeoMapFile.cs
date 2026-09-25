namespace FeBuddy.Core.Infrastructure.Veram.Models;

/// <summary>What a vERAM GeoMaps XML file (<c>GeoMapSet</c>) holds, as read by <c>VeramGeoMapReader</c>.</summary>
/// <param name="SourcePath">The file it was read from (or the name given to text read directly).</param>
/// <param name="Maps">Every <c>GeoMap</c>, in file order.</param>
/// <param name="Problems">
/// Each element or value that could not be used, with its line number. The rest of the file is
/// still read.
/// </param>
public sealed record VeramGeoMapFile(
	string SourcePath,
	IReadOnlyList<VeramGeoMap> Maps,
	IReadOnlyList<string> Problems);
