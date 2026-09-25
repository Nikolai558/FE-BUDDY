using NetTopologySuite.Geometries;

using Location = FeBuddy.Core.Domain.Geo.Models.Location;

namespace FeBuddy.Core.Infrastructure.Dat.Models;

/// <summary>
/// What an FAA <c>.dat</c> RADAR Video Map (RVM) file holds, as read by <c>DatFileReader</c>.
/// </summary>
/// <param name="SourcePath">The file it was read from (or the name given to text read directly).</param>
/// <param name="PointOfTangency">
/// The map's point of tangency - the <c>9900</c> record in the header, the point the map is
/// drawn around - or <see langword="null"/> when the file has none.
/// </param>
/// <param name="Lines">
/// One LineString per <c>LINE</c> block, in file order. Blocks with fewer than two points draw
/// nothing and are left out (and listed in <paramref name="Problems"/>).
/// </param>
/// <param name="Problems">
/// Each record that could not be used, with its line number, e.g. a coordinate that does not
/// parse. The rest of the file is still read.
/// </param>
public sealed record DatFile(
	string SourcePath,
	Location? PointOfTangency,
	IReadOnlyList<LineString> Lines,
	IReadOnlyList<string> Problems);
