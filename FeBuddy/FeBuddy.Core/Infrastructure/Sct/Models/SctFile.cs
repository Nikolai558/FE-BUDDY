namespace FeBuddy.Core.Infrastructure.Sct.Models;

/// <summary>
/// The drawable content of a VRC sector file (<c>.sct2</c> / <c>.sct</c>), as read by
/// <c>SctFileReader</c>. Every coordinate is resolved, including those given as a fix, navaid or
/// airport name.
/// </summary>
/// <param name="SourcePath">The file it was read from (or the name given to text read directly).</param>
/// <param name="Lines">The segments of each line section, in file order; a section with none is absent.</param>
/// <param name="Sids">The <c>[SID]</c> segments, each named after its diagram, in file order.</param>
/// <param name="Stars">The <c>[STAR]</c> segments, each named after its diagram, in file order.</param>
/// <param name="Labels">The <c>[LABELS]</c> entries.</param>
/// <param name="Regions">The <c>[REGIONS]</c> entries.</param>
/// <param name="Problems">Each record that could not be used, with its line number. The rest of the file is still read.</param>
public sealed record SctFile(
	string SourcePath,
	IReadOnlyDictionary<SctLineSection, IReadOnlyList<SctSegment>> Lines,
	IReadOnlyList<SctSegment> Sids,
	IReadOnlyList<SctSegment> Stars,
	IReadOnlyList<SctLabel> Labels,
	IReadOnlyList<SctRegion> Regions,
	IReadOnlyList<string> Problems);
