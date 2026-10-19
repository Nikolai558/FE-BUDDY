using System.Diagnostics;
using System.Globalization;

using FeBuddy.Core.Application.Conversions.DatToGeojson.Models;
using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Dat;
using FeBuddy.Core.Infrastructure.Dat.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Conversions.DatToGeojson;

/// <summary>
/// Public entry point for the DAT to GeoJSON conversion: turns FAA <c>.dat</c> RADAR Video Maps
/// into CRC-ready GeoJSON, one file per map, optionally cropped to a distance from each map's
/// point of tangency.
/// </summary>
/// <remarks>
/// <para>
/// Each file is converted on its own (<see cref="ConversionFiles"/>): one that cannot be read or
/// cropped is reported as an error in the result and the rest still convert. Only a bad setting -
/// which would fail every file the same way - stops the run, by throwing.
/// </para>
/// <para>
/// Cropping cuts every segment exactly where it crosses the circle (<see cref="RadiusFilter"/>),
/// and lines crossing the antimeridian are split so they do not wrap round the map
/// (<see cref="AntimeridianSplitter"/>).
/// </para>
/// </remarks>
public static class DatToGeojsonService
{
	/// <summary>The extensions a source folder's files must have to be converted.</summary>
	public static readonly IReadOnlyList<string> Extensions = [".dat"];

	/// <summary>
	/// Counts the files in <paramref name="folder"/> (not its sub-folders) a run would convert: every file with a <see cref="Extensions"/> extension,
	/// as the run picks them. For the page that shows the count as the folder is chosen, off the UI thread.
	/// </summary>
	/// <param name="folder">The folder.</param>
	/// <param name="cancellationToken">Stops the count; a newer one has started.</param>
	/// <returns>Whether the folder is there, how many files it has, or why it couldn't be read.</returns>
	/// <exception cref="OperationCanceledException">The count was stopped.</exception>
	public static SourceFolderCount CountSourceFiles(string folder, CancellationToken cancellationToken = default) =>
		ConversionSourceFolder.Count(folder, Extensions, _ => true, cancellationToken);

	private const string LogSource = "DatToGeojsonService";

	/// <summary>Runs the conversion.</summary>
	/// <param name="settings">The raw settings dictionary (see <see cref="DatToGeojsonSettingsParser"/>).</param>
	/// <param name="progress">Told as each file starts and finishes; optional.</param>
	/// <returns>What happened to each file, plus timing and every message collected along the way.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid, or the source folder does not exist.</exception>
	public static DatToGeojsonServiceResult Run(
		IReadOnlyDictionary<string, string> settings,
		IProgress<ConversionProgress>? progress = null)
	{
		ArgumentNullException.ThrowIfNull(settings);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		DatToGeojsonSettingsParseResult parseResult = DatToGeojsonSettingsParser.Parse(settings);
		messages.AddRange(parseResult.Messages);
		DatToGeojsonSettings parsed = parseResult.Settings;

		IReadOnlyList<string> sources = ConversionFiles.Resolve(parsed, Extensions, LogSource, messages);
		GeojsonFileSet files = new(parsed.CoordinatePrecision);

		// Two sources with one name (picked from two folders) would write the same file.
		HashSet<string> outputNames = new(StringComparer.OrdinalIgnoreCase);

		IReadOnlyList<DatFileConversion> conversions = ConversionFiles.ConvertEach(
			sources,
			source => Convert(source, parsed, files, outputNames, messages),
			(source, error) => new DatFileConversion { SourcePath = source, Error = error },
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

		return new DatToGeojsonServiceResult
		{
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
			Files = conversions,
			OutputDirectory = DatGeojsonWriter.OutputDirectory(parsed),
		};
	}

	private static DatFileConversion Convert(
		string source,
		DatToGeojsonSettings settings,
		GeojsonFileSet files,
		HashSet<string> outputNames,
		List<ServiceMessage> messages)
	{
		string name = Path.GetFileName(source);
		DatFile datFile = DatFileReader.Read(source);

		foreach (string problem in datFile.Problems)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, $"{name}: {problem}"));
		}

		// Routine in FAA files (a dot at the point of tangency), so a notice, not a warning.
		if (datFile.ShortLineBlocks > 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				$"{name}: {datFile.ShortLineBlocks:N0} LINE block(s) with fewer than two points draw nothing and were left out."));
		}

		if (datFile.Lines.Count == 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{name} has no lines to draw, so no GeoJSON file was written for it.")
			{
				IsAdvisory = true
			});

			return new DatFileConversion { SourcePath = source };
		}

		IReadOnlyList<LineString> lines = datFile.Lines;

		if (settings.CroppingDistanceNm is { } distance)
		{
			if (datFile.PointOfTangency is not { } center)
			{
				string error = $"{name} has no point of tangency (its 9900 record), so it cannot be cropped. " +
					"Clear the cropping distance to convert it whole.";
				messages.Add(new ServiceMessage(LogLevel.Error, LogSource, error));
				return new DatFileConversion { SourcePath = source, Error = error };
			}

			lines = RadiusFilter.ClipLines(lines, center, distance);

			if (lines.Count == 0)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"Nothing in {name} lies within {distance.ToString("0.##", CultureInfo.InvariantCulture)} NM " +
					"of its point of tangency, so no GeoJSON file was written for it.")
				{
					IsAdvisory = true
				});
			}
		}

		lines = AntimeridianSplitter.Split(lines);
		string? outputName = lines.Count > 0 ? OutputName(source, outputNames, messages) : null;

		return new DatFileConversion
		{
			SourcePath = source,
			OutputPath = DatGeojsonWriter.Write(lines, source, settings, files, outputName),
			LinesWritten = lines.Count,
		};
	}

	/// <summary>The output file's name: the source's, or - taken by another source in the run - the next free one, with a note.</summary>
	private static string OutputName(string source, HashSet<string> outputNames, List<ServiceMessage> messages)
	{
		string own = ConversionFiles.SafeFileName(Path.GetFileNameWithoutExtension(source));
		string name = ConversionFiles.UniqueFileName(own, outputNames);

		if (!string.Equals(name, own, StringComparison.Ordinal))
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				$"{Path.GetFileName(source)}: another file in this run has the same name, so this one was written as {name}.geojson."));
		}

		return name;
	}

	private static string Describe(DatFileConversion conversion) => conversion switch
	{
		{ Error: { } error } => error,
		{ OutputPath: null } => "nothing to write",
		_ => $"{conversion.LinesWritten:N0} line(s) written",
	};
}
