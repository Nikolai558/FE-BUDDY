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
/// Public entry point for the ERAM to GeoJSON conversion: turns ERAM <c>Geomaps.xml</c> files
/// (from an ERAM adaptation export) into CRC-ready GeoJSON, a folder per GeoMap (see
/// <see cref="EramGeojsonWriter"/>).
/// </summary>
/// <remarks>
/// Each file is converted on its own (<see cref="ConversionFiles"/>): one that cannot be read, or
/// is not a Geomaps file, is reported as an error in the result and the rest still convert. A
/// source folder may hold a whole adaptation export; only its Geomaps file is picked out. Only a
/// bad setting - which would fail every file the same way - stops the run, by throwing.
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
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid, or the source folder does not exist.</exception>
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

	private static string Describe(SourceFileConversion conversion) => conversion switch
	{
		{ Error: { } error } => error,
		{ OutputPaths.Count: 0 } => "nothing to write",
		_ => $"{conversion.OutputPaths.Count:N0} GeoJSON file(s) written",
	};
}
