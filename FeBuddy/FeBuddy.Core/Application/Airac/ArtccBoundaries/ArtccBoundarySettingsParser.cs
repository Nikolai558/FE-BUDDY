using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.ArtccBoundaries;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the ARTCC Boundaries sub-service into a typed, validated
/// <see cref="ArtccBoundarySettings"/>.
/// </summary>
/// <remarks>
/// The only place in the ARTCC Boundaries sub-service that touches the raw dictionary; everything
/// downstream works with <see cref="ArtccBoundarySettings"/>. Unlike every other AIRAC sub-service,
/// there is no <c>GenerateGeojson</c> toggle and no <c>GenerateAliasFile</c>: the sub-service
/// always writes GeoJSON Lines and has no alias file.
/// </remarks>
public static class ArtccBoundarySettingsParser
{
	private const string LogSource = "ArtccBoundarySettingsParser";

	/// <summary>The keys only ARTCC Boundaries reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"OutputBy", "LocationFilter", "SplitAtAntimeridian",
	};

	/// <summary>
	/// The CRC-defaults classes always known, whatever <c>CrcDefaultsFor</c> names: the three
	/// fixed groups. Every one draws Line only.
	/// </summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> BaseCrcKindsByClass =
		new[] { ArtccBoundaryOutputFiles.HighClass, ArtccBoundaryOutputFiles.LowClass, ArtccBoundaryOutputFiles.UnlimitedClass }
			.ToDictionary(
				className => className,
				_ => new[] { CrcFeatureKind.Line },
				StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Parses and validates <paramref name="artccBoundarySettings"/> into a typed
	/// <see cref="ArtccBoundarySettings"/>.
	/// </summary>
	/// <param name="artccBoundarySettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or a value is invalid.</exception>
	public static ArtccBoundarySettingsParseResult Parse(IReadOnlyDictionary<string, string> artccBoundarySettings)
	{
		ArgumentNullException.ThrowIfNull(artccBoundarySettings);

		string outputDirectory = SettingsValueReader.RequiredString(artccBoundarySettings, "OutputDirectory");
		ArtccBoundaryOutputBy outputBy = SettingsValueReader.OptionalEnum(artccBoundarySettings, "OutputBy", ArtccBoundaryOutputBy.HighLow);

		HashSet<string> locationFilter = SettingsValueReader.StringList(artccBoundarySettings, "LocationFilter")
			.Select(locationId => locationId.ToUpperInvariant())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		bool splitAtAntimeridian = SettingsValueReader.YesNo(artccBoundarySettings, "SplitAtAntimeridian", defaultValue: true);

		(bool includeFebProperties, IReadOnlyList<ArtccBoundaryFebProperty> febProperties) =
			SubServiceSettingsReader.ReadFebProperties<ArtccBoundaryFebProperty>(
				artccBoundarySettings, example: "locationId,locationName,altitude,type");

		RegionOfInterest? roi = SubServiceSettingsReader.ReadRoi(artccBoundarySettings);
		int coordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(artccBoundarySettings);

		// No alias file, so aliasFileKey is null: no UploadToVnas/CrcDefaultsFor entry can name one.
		VnasFileChoices vnas = SubServiceSettingsReader.ReadVnasFiles(
			artccBoundarySettings, aliasFileKey: null, ArtccBoundaryOutputFiles.IsGeojsonKey,
			example: $"{ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.HighClass)}, {ArtccBoundaryOutputFiles.KeyFor(ArtccBoundaryOutputFiles.LowClass)}");

		// Read from the keys chosen for CRC-ERAM defaults, whatever OutputBy is: a class not
		// actually written under the current mode simply goes unused, same as an unused
		// UploadToVnas entry.
		Dictionary<string, CrcLineDefaults> lineDefaults = new(StringComparer.OrdinalIgnoreCase);

		foreach (string key in vnas.CrcDefaultsFiles)
		{
			if (ArtccBoundaryOutputFiles.TryParseKey(key, out string className))
			{
				lineDefaults[className] = CrcDefaultsReader.ReadLine(artccBoundarySettings, $"Crc.{className}.Line");
			}
		}

		Dictionary<string, CrcFeatureKind[]> crcKindsByClass = new(BaseCrcKindsByClass, StringComparer.OrdinalIgnoreCase);

		foreach (string className in lineDefaults.Keys)
		{
			crcKindsByClass.TryAdd(className, [CrcFeatureKind.Line]);
		}

		List<ServiceMessage> messages = [.. SubServiceSettingsReader.UnknownKeyWarnings(
			artccBoundarySettings, OwnKeys, crcKindsByClass, LogSource,
			labelSource: "an ARTCC boundary line carries no label")];

		ArtccBoundarySettings settings = new()
		{
			OutputDirectory = outputDirectory,
			OutputBy = outputBy,
			LocationFilter = locationFilter,
			SplitAtAntimeridian = splitAtAntimeridian,
			IncludeFebCustomProperties = includeFebProperties,
			FebProperties = febProperties,
			Vnas = vnas,
			Roi = roi,
			CoordinatePrecision = coordinatePrecision,
			LineDefaults = lineDefaults,
		};

		return new ArtccBoundarySettingsParseResult(settings, messages);
	}
}
