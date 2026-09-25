using System.Globalization;
using System.Text.RegularExpressions;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Settings;

/// <summary>
/// Reads the settings every AIRAC sub-service shares - coordinate precision, Region of Interest,
/// <c>feb.*</c> properties, the vNAS files, unrecognized keys - so each sub-service's parser only
/// handles what is its own.
/// </summary>
public static partial class SubServiceSettingsReader
{
	/// <summary>The files to upload to vNAS, by file key (see <see cref="VnasFileChoices"/>).</summary>
	public const string UploadToVnasKey = "UploadToVnas";

	/// <summary>The vNAS files that get CRC-ERAM defaults, by file key; each must also be in <see cref="UploadToVnasKey"/>.</summary>
	public const string CrcDefaultsForKey = "CrcDefaultsFor";

	/// <summary>
	/// The keys every sub-service understands. A parser adds its own keys to these when looking
	/// for unrecognized settings.
	/// </summary>
	public static readonly IReadOnlySet<string> CommonKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"OutputDirectory", "CoordinatePrecision", "GenerateAliasFile",
		"IncludeFebCustomProperties", "FebProperties",
		UploadToVnasKey, CrcDefaultsForKey,
		"FilterByRoi", "RoiSwLat", "RoiSwLon", "RoiNeLat", "RoiNeLon",
	};

	/// <summary>Reads <c>CoordinatePrecision</c>: decimal places kept per coordinate, 0-15, default 6.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <returns>The precision.</returns>
	/// <exception cref="ArgumentException">Thrown when the value is not an integer from 0 to 15.</exception>
	public static int ReadCoordinatePrecision(IReadOnlyDictionary<string, string> settings) =>
		SettingsValueReader.IntInRange(settings, "CoordinatePrecision", defaultValue: 6, minimum: 0, maximum: 15);

	/// <summary>
	/// Reads the Region of Interest: <see langword="null"/> unless <c>FilterByRoi</c> is <c>Y</c>,
	/// in which case all four corners are required and validated.
	/// </summary>
	/// <param name="settings">The raw settings block.</param>
	/// <returns>The ROI, or <see langword="null"/> when filtering by ROI is off.</returns>
	/// <exception cref="ArgumentException">Thrown when a corner is missing, malformed, or the corners are the wrong way round.</exception>
	public static RegionOfInterest? ReadRoi(IReadOnlyDictionary<string, string> settings)
	{
		if (!SettingsValueReader.YesNo(settings, "FilterByRoi", defaultValue: false))
		{
			return null;
		}

		string swLatText = SettingsValueReader.RequiredString(settings, "RoiSwLat");
		string swLonText = SettingsValueReader.RequiredString(settings, "RoiSwLon");
		string neLatText = SettingsValueReader.RequiredString(settings, "RoiNeLat");
		string neLonText = SettingsValueReader.RequiredString(settings, "RoiNeLon");

		if (!RoiFilter.IsCoordinateValidFormat(swLatText, swLonText, neLatText, neLonText, out string? formatError))
		{
			throw new ArgumentException($"Invalid Region of Interest: {formatError}");
		}

		double swLat = double.Parse(swLatText, NumberStyles.Float, CultureInfo.InvariantCulture);
		double swLon = double.Parse(swLonText, NumberStyles.Float, CultureInfo.InvariantCulture);
		double neLat = double.Parse(neLatText, NumberStyles.Float, CultureInfo.InvariantCulture);
		double neLon = double.Parse(neLonText, NumberStyles.Float, CultureInfo.InvariantCulture);

		if (!RoiFilter.IsCoordinatesRelativePositionValid(swLat, swLon, neLat, neLon, out string? positionError))
		{
			throw new ArgumentException($"Invalid Region of Interest: {positionError}");
		}

		return new RegionOfInterest(swLat, swLon, neLat, neLon);
	}

	/// <summary>
	/// Reads <c>IncludeFebCustomProperties</c> and, when it is <c>Y</c>, the comma-separated
	/// <c>FebProperties</c> list.
	/// </summary>
	/// <typeparam name="TProperty">The sub-service's property enum.</typeparam>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="example">A sample list for the error message, e.g. <c>awyId,pointId</c>.</param>
	/// <param name="retiredNames">
	/// Names that used to be offered, each with the reason it was withdrawn, so a stale saved
	/// setting gets an explanation rather than "not a known property".
	/// </param>
	/// <returns>Whether properties are included, and which - distinct, in the order listed.</returns>
	/// <exception cref="ArgumentException">Thrown when the list is empty or names an unknown property.</exception>
	public static (bool Include, IReadOnlyList<TProperty> Properties) ReadFebProperties<TProperty>(
		IReadOnlyDictionary<string, string> settings,
		string example,
		IReadOnlyDictionary<string, string>? retiredNames = null)
		where TProperty : struct, Enum
	{
		if (!SettingsValueReader.YesNo(settings, "IncludeFebCustomProperties", defaultValue: false))
		{
			return (false, Array.Empty<TProperty>());
		}

		IReadOnlyList<string> names = SettingsValueReader.StringList(settings, "FebProperties");

		if (names.Count == 0)
		{
			throw new ArgumentException(
				"IncludeFebCustomProperties is \"Y\" but 'FebProperties' names none. " +
				$"List the properties to write, e.g. \"{example}\".");
		}

		List<TProperty> properties = [];

		foreach (string name in names)
		{
			if (retiredNames is not null && retiredNames.TryGetValue(name, out string? reason))
			{
				throw new ArgumentException($"'FebProperties' entry '{name}' is no longer offered: {reason} Remove it from the list.");
			}

			if (!FebProperties.TryParse(name, out TProperty property))
			{
				throw new ArgumentException(
					$"'FebProperties' entry '{name}' is not a known property. Valid values: " +
					string.Join(", ", FebProperties.AllNames<TProperty>()) + ".");
			}

			if (!properties.Contains(property))
			{
				properties.Add(property);
			}
		}

		return (true, properties);
	}

	/// <summary>
	/// Reads <c>UploadToVnas</c> and <c>CrcDefaultsFor</c>: which output files go to vNAS, and
	/// which of those get CRC-ERAM defaults. Both are comma-separated file keys and default to none.
	/// </summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="aliasFileKey">The sub-service's alias file key, e.g. <c>Airways.txt</c>.</param>
	/// <param name="isGeojsonFileKey">Whether a key names one of the sub-service's GeoJSON files (or kinds of file).</param>
	/// <param name="example">Sample keys for the error message, e.g. <c>Airways_High_Lines, Airways.txt</c>.</param>
	/// <returns>The choices.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a key is not a file the sub-service writes, a <c>CrcDefaultsFor</c> file is not
	/// also in <c>UploadToVnas</c>, or <c>CrcDefaultsFor</c> names the alias file.
	/// </exception>
	public static VnasFileChoices ReadVnasFiles(
		IReadOnlyDictionary<string, string> settings,
		string aliasFileKey,
		Func<string, bool> isGeojsonFileKey,
		string example)
	{
		IReadOnlyList<string> uploadFiles = SettingsValueReader.StringList(settings, UploadToVnasKey);
		IReadOnlyList<string> crcFiles = SettingsValueReader.StringList(settings, CrcDefaultsForKey);

		foreach (string key in uploadFiles)
		{
			if (!key.Equals(aliasFileKey, StringComparison.OrdinalIgnoreCase) && !isGeojsonFileKey(key))
			{
				throw new ArgumentException(
					$"'{UploadToVnasKey}' entry '{key}' is not a file this sub-service writes. Entries look like: {example}.");
			}
		}

		foreach (string key in crcFiles)
		{
			if (key.Equals(aliasFileKey, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(
					$"'{CrcDefaultsForKey}' entry '{key}' is the alias file, which has no CRC-ERAM defaults. Remove it from the list.");
			}

			if (!uploadFiles.Contains(key, StringComparer.OrdinalIgnoreCase))
			{
				throw new ArgumentException(
					$"'{CrcDefaultsForKey}' entry '{key}' is not in '{UploadToVnasKey}'. CRC-ERAM defaults are only written to files uploaded to vNAS.");
			}
		}

		return new VnasFileChoices(uploadFiles, crcFiles);
	}

	/// <summary>
	/// Warns about every key the sub-service does not read. A misspelled key would otherwise be
	/// silently ignored and its value never used.
	/// </summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="ownKeys">The sub-service's own keys, beyond <see cref="CommonKeys"/>.</param>
	/// <param name="crcKindsByClass">
	/// The CRC defaults classes the sub-service reads (the <c>High</c> in <c>Crc.High.Line.bcg</c>)
	/// and which kinds each one draws. Must match class names ignoring case.
	/// </param>
	/// <param name="source">The log source for the messages, e.g. <c>AirwaySettingsParser</c>.</param>
	/// <param name="labelSource">
	/// Where a Text Feature's label comes from instead, for the message when someone sets a
	/// <c>Crc.*.Text.text</c> default, e.g. <c>each waypoint is labelled with its own ID</c>.
	/// </param>
	/// <returns>One warning per unused key.</returns>
	public static IReadOnlyList<ServiceMessage> UnknownKeyWarnings(
		IReadOnlyDictionary<string, string> settings,
		IReadOnlySet<string> ownKeys,
		IReadOnlyDictionary<string, CrcFeatureKind[]> crcKindsByClass,
		string source,
		string labelSource)
	{
		List<ServiceMessage> messages = [];

		foreach (string key in settings.Keys)
		{
			if (CommonKeys.Contains(key) || ownKeys.Contains(key))
			{
				continue;
			}

			string? problem = CrcKeyProblem(key, crcKindsByClass, labelSource);

			if (problem is not null)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, source, problem));
			}
		}

		return messages;
	}

	/// <summary>What is wrong with a key that is not a plain setting, or <see langword="null"/> when it is a valid CRC default.</summary>
	private static string? CrcKeyProblem(
		string key,
		IReadOnlyDictionary<string, CrcFeatureKind[]> crcKindsByClass,
		string labelSource)
	{
		Match match = CrcKeyPattern().Match(key);

		if (!match.Success || !crcKindsByClass.TryGetValue(match.Groups["class"].Value, out CrcFeatureKind[]? kinds))
		{
			return $"Unrecognized setting '{key}' was ignored.";
		}

		CrcFeatureKind kind = Enum.Parse<CrcFeatureKind>(match.Groups["kind"].Value, ignoreCase: true);
		string property = match.Groups["property"].Value;

		if (!kinds.Contains(kind))
		{
			return $"'{key}' was ignored: '{match.Groups["class"].Value}' has no {kind} output.";
		}

		if (CrcDefaultsReader.PropertyNames(kind).Contains(property))
		{
			return null;
		}

		return kind == CrcFeatureKind.Text && property.Equals("text", StringComparison.OrdinalIgnoreCase)
			? $"'{key}' was ignored: {labelSource}, so it cannot be set as a class-wide default."
			: $"Unrecognized setting '{key}' was ignored.";
	}

	// A class name can hold a hyphen: NAVAIDs' per-type classes are tokens like VOR-DME.
	[GeneratedRegex(@"^Crc\.(?<class>[\w-]+)\.(?<kind>Line|Symbol|Text)\.(?<property>\w+)$", RegexOptions.IgnoreCase)]
	private static partial Regex CrcKeyPattern();
}
