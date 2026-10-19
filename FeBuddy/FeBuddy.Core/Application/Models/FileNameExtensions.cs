using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Conversions.DatToGeojson;
using FeBuddy.Core.Application.Conversions.EramToGeojson;
using FeBuddy.Core.Application.Conversions.SctToGeojson;

namespace FeBuddy.Core.Application.Models;

/// <summary>
/// Every extension a file FE-Buddy reads or writes has, for the GUI to pick the file names out of a
/// <see cref="ServiceMessage"/>'s text and show them as code. Built from the services' own lists, so a
/// new format is picked out as soon as a service reads or writes it.
/// </summary>
public static class FileNameExtensions
{
	/// <summary>
	/// The extensions, with their dots, longest first so <c>.sct2</c> is tried before <c>.sct</c>: the
	/// AIRAC Service's output, each conversion's sources, the FAA's NASR <c>.csv</c> files and FE-Buddy's
	/// own <c>.log</c> files.
	/// </summary>
	public static IReadOnlyList<string> All { get; } =
		[.. OutputFileNames.OutputExtensions
			.Concat(DatToGeojsonService.Extensions)
			.Concat(SctToGeojsonService.Extensions)
			.Concat(EramToGeojsonService.Extensions)
			.Concat([".csv", ".log"])
			.Select(extension => extension.ToLowerInvariant())
			.Distinct()
			.OrderByDescending(extension => extension.Length)
			.ThenBy(extension => extension, StringComparer.Ordinal)];
}
