using System.Diagnostics;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.VeramToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Veram;
using FeBuddy.Core.Infrastructure.Veram.Models;

namespace FeBuddy.Core.Application.Conversions.VeramToGeojson;

/// <summary>
/// Public entry point for the vERAM to GeoJSON conversion: turns vERAM GeoMaps XML files into
/// CRC-ready GeoJSON, a folder per GeoMap (see <see cref="VeramGeojsonWriter"/>).
/// </summary>
/// <remarks>
/// Each file is converted on its own (<see cref="ConversionFiles"/>): one that cannot be read, or
/// is not a GeoMaps file, is reported as an error in the result and the rest still convert. Only a
/// bad setting - which would fail every file the same way - stops the run, by throwing.
/// </remarks>
public static class VeramToGeojsonService
{
	/// <summary>The extensions a source folder's files must have to be converted.</summary>
	public static readonly IReadOnlyList<string> Extensions = [".xml"];

	private const string LogSource = "VeramToGeojsonService";

	/// <summary>Runs the conversion.</summary>
	/// <param name="settings">The raw settings dictionary (see <see cref="VeramToGeojsonSettingsParser"/>).</param>
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

		VeramToGeojsonSettingsParseResult parseResult = VeramToGeojsonSettingsParser.Parse(settings);
		messages.AddRange(parseResult.Messages);
		VeramToGeojsonSettings parsed = parseResult.Settings;

		IReadOnlyList<string> sources = ConversionFiles.Resolve(parsed, Extensions, LogSource, messages);
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
			OutputDirectory = VeramGeojsonWriter.OutputDirectory(parsed),
		};
	}

	private static SourceFileConversion Convert(
		string source,
		VeramToGeojsonSettings settings,
		GeojsonFileSet files,
		List<ServiceMessage> messages)
	{
		string name = Path.GetFileName(source);
		VeramGeoMapFile geoMaps = VeramGeoMapReader.Read(source);

		foreach (string problem in geoMaps.Problems)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, $"{name}: {problem}"));
		}

		(IReadOnlyList<string> paths, int featureCount) = VeramGeojsonWriter.Write(geoMaps, settings, files, messages);

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
