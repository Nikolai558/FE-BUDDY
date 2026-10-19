using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.WxStations;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// Reads the <c>FileNames</c> block (<see cref="AiracServiceSettings.FileNames"/>): each renamed
/// output file's key, and its new name without an extension - e.g. <c>Airways_High_Lines</c> =
/// <c>ZOB High</c>, <c>Airways.txt</c> = <c>ZOB Airways</c>.
/// </summary>
/// <remarks>
/// Every file a run writes can be renamed except the Departures and Arrivals GeoJSON files, which
/// are named from the FAA's data one per procedure. An entry for any other key is left out with a
/// warning, as is a misspelled setting in a sub-service's block; an entry with no name keeps FE-Buddy's.
/// A name that can't be used stops the run before anything is written.
/// </remarks>
public static class OutputFileNamesParser
{
	private const string LogSource = "OutputFileNamesParser";

	/// <summary>
	/// Every file a run can write that is not GeoJSON, by its key (its name): the alias files, the Procedures
	/// documents, and the two files the run itself writes. A new name may not be one of these files' names in
	/// the same folder, whether or not this run writes it: every run of a cycle writes into the same folder,
	/// so the file may be there from another. The File Names tab checks against them too.
	/// </summary>
	public static IReadOnlySet<string> KeptFileKeys { get; } = new HashSet<string>(
	[
		AirwayOutputFiles.Alias, AirportOutputFiles.Alias, DepartureOutputFiles.Alias, ArrivalOutputFiles.Alias,
		NavaidOutputFiles.Alias, ProcedureOutputFiles.Alias, TelephonyOutputFiles.Alias,
		ProcedureOutputFiles.Changes, ProcedureOutputFiles.Json,
		AiracOutputPaths.CombinedAliasFileName, AiracOutputPaths.DuplicateAliasReportFileName,
	], StringComparer.OrdinalIgnoreCase);

	/// <summary>Reads the block.</summary>
	/// <param name="fileNames">The block, or <see langword="null"/> when no file is renamed.</param>
	/// <returns>The new names, and a warning for each entry left out.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a new name is not a usable file name, or two files would be given the same name
	/// (see <see cref="OutputFileNames"/>).
	/// </exception>
	public static OutputFileNamesParseResult Parse(IReadOnlyDictionary<string, string>? fileNames)
	{
		List<ServiceMessage> messages = [];
		Dictionary<string, string> newNames = new(StringComparer.OrdinalIgnoreCase);

		foreach ((string key, string name) in fileNames ?? new Dictionary<string, string>())
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			if (CanRename(key))
			{
				newNames[key] = name;
				continue;
			}

			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				DepartureOutputFiles.IsGeojsonKey(key) || ArrivalOutputFiles.IsGeojsonKey(key)
					? $"The new name for '{key}' was ignored: Departures and Arrivals name each procedure's GeoJSON file from the FAA's data, so those files keep their names."
					: $"The new name for '{key}' was ignored: it is not a file FE-Buddy writes."));
		}

		// A new name may not be another renamed file's in the same folder, nor that of a file there that keeps
		// FE-Buddy's name - Airways.txt renamed "Airports" would write over Airports.txt. The files checked are
		// every one but the GeoJSON (KeptFileKeys), whether or not this run writes it: every run of a cycle writes
		// into the same folder. The File Names tab checks the same way.
		foreach ((string key, string problem) in OutputFileNames.Clashes(
			newNames.ToDictionary(entry => entry.Key, entry => entry.Value.Trim(), StringComparer.OrdinalIgnoreCase), KeptFileKeys))
		{
			throw new ArgumentException($"The new name for {OutputFileNames.DefaultFileName(key)}, '{newNames[key].Trim()}', can't be used. {problem}", nameof(fileNames));
		}

		return new OutputFileNamesParseResult(new OutputFileNames(newNames), messages);
	}

	/// <summary>Whether a key names a file that can be given a new name.</summary>
	/// <param name="fileKey">The file's key.</param>
	/// <returns><see langword="true"/> for any file a run writes except a Departures or Arrivals GeoJSON file.</returns>
	internal static bool CanRename(string fileKey) =>
		AirwayOutputFiles.IsGeojsonKey(fileKey)
		|| AirportOutputFiles.IsGeojsonKey(fileKey)
		|| NavaidOutputFiles.IsGeojsonKey(fileKey)
		|| ArtccBoundaryOutputFiles.IsGeojsonKey(fileKey)
		|| FixOutputFiles.IsGeojsonKey(fileKey)
		|| WxStationOutputFiles.IsGeojsonKey(fileKey)
		|| KeptFileKeys.Contains(fileKey);
}
