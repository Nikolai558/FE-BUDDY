using System.Diagnostics;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.SctToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Sct;
using FeBuddy.Core.Infrastructure.Sct.Models;

namespace FeBuddy.Core.Application.Conversions.SctToGeojson;

/// <summary>
/// Public entry point for the SCT2 to GeoJSON conversion: turns VRC sector files into
/// CRC-ready GeoJSON, a folder of files per sector file (see <see cref="SctGeojsonWriter"/>).
/// </summary>
/// <remarks>
/// Each file is converted on its own (<see cref="ConversionFiles"/>): one that cannot be read is
/// reported as an error in the result and the rest still convert. Only a bad setting - which would
/// fail every file the same way - stops the run, by throwing.
/// </remarks>
public static class SctToGeojsonService
{
	/// <summary>The extensions a source folder's files must have to be converted.</summary>
	public static readonly IReadOnlyList<string> Extensions = [".sct2", ".sct"];

	private const string LogSource = "SctToGeojsonService";

	/// <summary>Runs the conversion.</summary>
	/// <param name="settings">The raw settings dictionary (see <see cref="SctToGeojsonSettingsParser"/>).</param>
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

		SctToGeojsonSettingsParseResult parseResult = SctToGeojsonSettingsParser.Parse(settings);
		messages.AddRange(parseResult.Messages);
		SctToGeojsonSettings parsed = parseResult.Settings;

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
			OutputDirectory = SctGeojsonWriter.OutputDirectory(parsed),
		};
	}

	private static SourceFileConversion Convert(
		string source,
		SctToGeojsonSettings settings,
		GeojsonFileSet files,
		List<ServiceMessage> messages)
	{
		string name = Path.GetFileName(source);
		SctFile sctFile = SctFileReader.Read(source);

		foreach (string problem in sctFile.Problems)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, $"{name}: {problem}"));
		}

		(IReadOnlyList<string> paths, int featureCount) = SctGeojsonWriter.Write(sctFile, settings, files);

		if (paths.Count == 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{name} has nothing FE-Buddy converts (lines, SIDs, STARs, labels or regions), so no GeoJSON was written for it.")
			{
				IsAdvisory = true
			});
		}

		return new SourceFileConversion
		{
			SourcePath = source,
			OutputPaths = paths,
			FeaturesWritten = featureCount,
			RecordsSkipped = sctFile.Problems.Count,
		};
	}

	private static string Describe(SourceFileConversion conversion) => conversion switch
	{
		{ Error: { } error } => error,
		{ OutputPaths.Count: 0 } => "nothing to write",
		_ => $"{conversion.OutputPaths.Count:N0} GeoJSON file(s) written",
	};
}
