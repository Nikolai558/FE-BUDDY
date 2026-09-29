using System.Diagnostics;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Eram;
using FeBuddy.Core.Infrastructure.Eram.Models;

namespace FeBuddy.Core.Application.Conversions.EramToGeojson;

/// <summary>
/// Public entry point for the ERAM to GeoJSON conversion: turns an ERAM <c>Geomaps.xml</c> (from
/// an ERAM adaptation export) into CRC-ready GeoJSON in <c>ERAM_TO_GEOJSON</c>, laid out as the
/// original ERAM_2_GEOJSON tool laid it out (see <see cref="EramGeojsonWriter"/>).
/// </summary>
/// <remarks>
/// <para>
/// One Geomaps file per run, as the original tool took: the maps' folders and files are named
/// after the maps alone, so two files' maps could land on each other. A source folder may hold a
/// whole adaptation export; only its Geomaps file is picked out. More than one, or a bad setting,
/// stops the run by throwing; a file that cannot be read, or is not a Geomaps file, is reported as
/// an error in the result.
/// </para>
/// <para>
/// Once the file has been read, everything already in <c>ERAM_TO_GEOJSON</c> is deleted, as the
/// original tool emptied its output folder, so it holds only this run's files - the attribute
/// layout's names change with the data, and old files would otherwise pile up beside new ones.
/// The tab asks before a run that would delete anything.
/// </para>
/// </remarks>
public static class EramToGeojsonService
{
	/// <summary>The extensions a source folder's files must have to be converted.</summary>
	public static readonly IReadOnlyList<string> Extensions = [".xml"];

	private const string LogSource = "EramToGeojsonService";

	/// <summary>Runs the conversion.</summary>
	/// <param name="settings">The raw settings dictionary (see <see cref="EramToGeojsonSettingsParser"/>).</param>
	/// <param name="progress">Told as each file starts and finishes; optional.</param>
	/// <returns>What happened to each file, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing or invalid, the source folder does not exist, or
	/// more than one Geomaps file is given.
	/// </exception>
	public static SourceFilesConversionResult Run(
		IReadOnlyDictionary<string, string> settings,
		IProgress<ConversionProgress>? progress = null)
	{
		ArgumentNullException.ThrowIfNull(settings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		EramToGeojsonSettingsParseResult parseResult = EramToGeojsonSettingsParser.Parse(settings);
		messages.AddRange(parseResult.Messages);
		EramToGeojsonSettings parsed = parseResult.Settings;

		// A folder may be a whole ERAM adaptation export: only its Geomaps file is converted.
		IReadOnlyList<string> sources = ConversionFiles.Resolve(parsed, Extensions, LogSource, messages, EramGeoMapReader.IsGeoMapsFile);

		if (sources.Count > 1)
		{
			throw new ArgumentException(
				$"ERAM to GeoJSON converts one Geomaps file per run, and {sources.Count} were given " +
				$"({string.Join(", ", sources.Select(Path.GetFileName))}). Pick one.");
		}

		GeojsonFileSet files = new(parsed.CoordinatePrecision);

		IReadOnlyList<SourceFileConversion> conversions = ConversionFiles.ConvertEach(
			sources,
			source => Convert(source, parsed, files, messages),
			(source, error) => new SourceFileConversion { SourcePath = source, Error = error },
			Describe,
			progress,
			LogSource,
			messages);

		stopwatch.Stop();

		// Every message also flows to the shared application log, so the Dashboard activity log
		// narrates the run.
		foreach (ServiceMessage message in messages)
		{
			AppLog.Write(message.Level, message.Source, message.Text);
		}

		return new SourceFilesConversionResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			Files = conversions,
			OutputDirectory = EramGeojsonWriter.OutputDirectory(parsed),
		};
	}

	private static SourceFileConversion Convert(
		string source,
		EramToGeojsonSettings settings,
		GeojsonFileSet files,
		List<ServiceMessage> messages)
	{
		string name = Path.GetFileName(source);
		EramGeoMapFile geoMaps = EramGeoMapReader.Read(source);

		foreach (string problem in geoMaps.Problems)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, $"{name}: {problem}"));
		}

		// Only now the file is known to be good: a bad one leaves the last run's files alone.
		ClearOutput(EramGeojsonWriter.OutputDirectory(settings), messages);

		(IReadOnlyList<string> paths, int featureCount) = EramGeojsonWriter.Write(geoMaps, settings, files, messages);

		if (paths.Count == 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{name} has no elements to draw, so no GeoJSON was written for it.")
			{
				IsAdvisory = true
			});
		}

		return new SourceFileConversion
		{
			SourcePath = source,
			OutputPaths = paths,
			FeaturesWritten = featureCount,
			RecordsSkipped = geoMaps.Problems.Count,
		};
	}

	/// <summary>
	/// Deletes everything in <paramref name="directory"/>, so it holds only this run's files. What
	/// cannot be deleted (a file open elsewhere) is named in one advisory; the run goes on.
	/// </summary>
	private static void ClearOutput(string directory, List<ServiceMessage> messages)
	{
		if (!Directory.Exists(directory))
		{
			return;
		}

		List<string> kept = [];

		foreach (string entry in Directory.EnumerateFileSystemEntries(directory).ToArray())
		{
			try
			{
				if (Directory.Exists(entry))
				{
					Directory.Delete(entry, recursive: true);
				}
				else
				{
					File.Delete(entry);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				kept.Add(Path.GetFileName(entry));
			}
		}

		if (kept.Count > 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"Could not empty {directory} before the run - {string.Join(", ", kept)} could not be deleted (open elsewhere?), " +
				"so the last run's files may be mixed in with this run's.")
			{
				IsAdvisory = true
			});
		}
	}

	private static string Describe(SourceFileConversion conversion) => conversion switch
	{
		{ Error: { } error } => error,
		{ OutputPaths.Count: 0 } => "nothing to write",
		_ => $"{conversion.OutputPaths.Count:N0} GeoJSON file(s) written",
	};
}
