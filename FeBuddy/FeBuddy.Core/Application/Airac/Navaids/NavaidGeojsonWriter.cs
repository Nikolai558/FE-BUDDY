using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Domain.Navaids;
using FeBuddy.Core.Domain.Navaids.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging.Models;

using NetTopologySuite.Features;

namespace FeBuddy.Core.Application.Airac.Navaids;

/// <summary>
/// Generates the NAVAIDs GeoJSON output: Symbols and Text only, one Point per NAVAID. There is no
/// Lines file.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NavaidOutputBy.All"/> writes at most <c>NAVAIDs_Symbols.geojson</c> and
/// <c>NAVAIDs_Text.geojson</c>; <see cref="NavaidOutputBy.Type"/> writes one Symbols/Text pair per
/// NAVAID type present among the NAVAIDs passed in (see <see cref="NavaidOutputFiles"/>). Either
/// way, a file that would hold no NAVAIDs is not written at all (see <see cref="GeojsonFileSet"/>).
/// </para>
/// <para>
/// The ROI limits this output only: the caller filters with <see cref="FilterToRoi"/> before
/// calling <see cref="Generate"/>, while <see cref="NavaidAliasWriter"/> covers every included
/// NAVAID regardless of the ROI. Each file goes in the GeoJSON folder, or the vNAS one when the
/// user marked it for vNAS. Only a file chosen for CRC-ERAM defaults gets an isDefaults Feature.
/// </para>
/// <para>
/// A merged <see cref="NavaidOutputBy.All"/> Symbols file that gets CRC-ERAM defaults and has
/// <see cref="NavaidSettings.SymbolStyleBy"/> set to <see cref="NavaidSymbolStyleBy.Type"/> gives
/// each Feature its own <c>style</c> - see <see cref="NavaidTypes.SymbolStyleFor"/> - since one
/// file then draws every kind of NAVAID; a type with no mapped style (<c>CONSOLAN</c>, or a type
/// FE-Buddy does not recognize) is left with none, and a warning is reported once per such type.
/// </para>
/// </remarks>
public static class NavaidGeojsonWriter
{
	private const string LogSource = "NavaidGeojsonWriter";

	/// <summary>
	/// Keeps the NAVAIDs whose own coordinates fall inside the ROI.
	/// </summary>
	/// <param name="navaids">Every NAVAID, after <see cref="NavaidFilter.ExcludeTypes"/>.</param>
	/// <param name="roi">The ROI, or <see langword="null"/> for no filtering.</param>
	/// <returns>The NAVAIDs the GeoJSON output covers.</returns>
	public static IReadOnlyList<Navaid> FilterToRoi(IReadOnlyList<Navaid> navaids, RegionOfInterest? roi) =>
		roi is null
			? navaids
			: [.. navaids.Where(navaid => RoiFilter.Contains(roi, navaid.Latitude, navaid.Longitude))];

	/// <summary>
	/// Generates every GeoJSON file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="navaids">The NAVAIDs in scope - already ROI-filtered by the caller.</param>
	/// <param name="settings">The parsed NAVAIDs settings.</param>
	/// <returns>The files written, how many rendered Features each holds, and any messages collected.</returns>
	public static NavaidGeojsonGenerateResult Generate(IReadOnlyList<Navaid> navaids, NavaidSettings settings)
	{
		ArgumentNullException.ThrowIfNull(navaids);
		ArgumentNullException.ThrowIfNull(settings);

		GeojsonFileSet files = new(settings.CoordinatePrecision);
		List<ServiceMessage> messages = [];

		if (!settings.GenerateGeojson || navaids.Count == 0)
		{
			return new NavaidGeojsonGenerateResult(files, messages);
		}

		if (settings.OutputBy == NavaidOutputBy.All)
		{
			if (settings.EmitSymbols)
			{
				GenerateSymbols(navaids, settings, NavaidOutputFiles.AllClass, NavaidOutputFiles.Symbols, files, messages);
			}

			if (settings.EmitText)
			{
				GenerateText(navaids, settings, NavaidOutputFiles.AllClass, NavaidOutputFiles.Text, files);
			}
		}
		else
		{
			foreach (IGrouping<string, Navaid> group in navaids
				.GroupBy(navaid => navaid.NavType, StringComparer.OrdinalIgnoreCase)
				.OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
			{
				List<Navaid> typeNavaids = [.. group];
				string token = NavaidTypes.Token(group.Key);

				if (settings.EmitSymbols)
				{
					GenerateSymbols(typeNavaids, settings, token, NavaidOutputFiles.TypeKey(group.Key, CrcFeatureKind.Symbol), files, messages);
				}

				if (settings.EmitText)
				{
					GenerateText(typeNavaids, settings, token, NavaidOutputFiles.TypeKey(group.Key, CrcFeatureKind.Text), files);
				}
			}
		}

		return new NavaidGeojsonGenerateResult(files, messages);
	}

	/// <summary>Writes one file, into the GeoJSON or vNAS folder as the user chose.</summary>
	private static void WriteFile(
		FeatureCollection collection,
		int renderedCount,
		NavaidSettings settings,
		string fileKey,
		GeojsonFileSet files)
	{
		string directory = AiracOutputPaths.FileDirectory(settings.OutputDirectory, isGeojson: true, settings.Vnas.IsUploaded(fileKey));
		files.Write(collection, renderedCount, directory, $"{fileKey}.geojson");
	}

	private static void GenerateSymbols(
		IReadOnlyList<Navaid> navaids,
		NavaidSettings settings,
		string crcClass,
		string fileKey,
		GeojsonFileSet files,
		List<ServiceMessage> messages)
	{
		FeatureCollection collection = [];
		bool crcDefaults = settings.Vnas.HasCrcDefaults(fileKey);

		if (crcDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.SymbolDefaults[crcClass]));
		}

		// A per-Feature style only makes sense for the merged All-mode Symbols file with
		// SymbolStyleBy=Type: a Type-mode file already holds one NAVAID type, and an All-mode
		// file with SymbolStyleBy=File draws every NAVAID with the file's own default style.
		bool needsPerFeatureStyle = crcDefaults
			&& settings.OutputBy == NavaidOutputBy.All
			&& settings.SymbolStyleBy == NavaidSymbolStyleBy.Type;

		HashSet<string> warnedTypesWithNoStyle = new(StringComparer.OrdinalIgnoreCase);

		foreach (Navaid navaid in navaids)
		{
			AttributesTable attributes = [];
			AddFebProperties(attributes, navaid, settings, forTextFile: false);

			if (needsPerFeatureStyle)
			{
				string? style = navaid.NavType.Equals(NavaidTypes.FanMarker, StringComparison.OrdinalIgnoreCase)
					? settings.FanMarkerStyle
					: NavaidTypes.SymbolStyleFor(navaid.NavType);

				if (style is not null)
				{
					attributes.Add("style", style);
				}
				else if (warnedTypesWithNoStyle.Add(navaid.NavType))
				{
					messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
						navaid.NavType.Equals(NavaidTypes.FanMarker, StringComparison.OrdinalIgnoreCase)
							? "No fan marker style was chosen (FanMarkerStyle), so CRC will draw fan markers with its fallback symbol."
							: $"NAVAID type '{navaid.NavType}' has no CRC symbol style assigned. CRC will draw them with its fallback symbol."));
				}
			}

			collection.Add(new Feature(Wgs84.Point(navaid.Latitude, navaid.Longitude), attributes));
		}

		WriteFile(collection, navaids.Count, settings, fileKey, files);
	}

	private static void GenerateText(
		IReadOnlyList<Navaid> navaids,
		NavaidSettings settings,
		string crcClass,
		string fileKey,
		GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.Vnas.HasCrcDefaults(fileKey))
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.TextDefaults[crcClass]));
		}

		foreach (Navaid navaid in navaids)
		{
			AttributesTable attributes = new()
			{
				// Two rendered lines: the identifier, then the NAVAID's name and type.
				{ "text", new[] { navaid.NavId, $"{navaid.Name} {navaid.NavType}" } }
			};

			AddFebProperties(attributes, navaid, settings, forTextFile: true);

			collection.Add(new Feature(Wgs84.Point(navaid.Latitude, navaid.Longitude), attributes));
		}

		WriteFile(collection, navaids.Count, settings, fileKey, files);
	}

	/// <summary>
	/// Adds the selected <c>feb.*</c> properties to a Feature.
	/// </summary>
	/// <param name="attributes">The Feature's attribute table.</param>
	/// <param name="navaid">The NAVAID being written.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="forTextFile">
	/// <see langword="true"/> for the Text file, which skips the identifier, type and name - its
	/// <c>text</c> array already carries all three.
	/// </param>
	private static void AddFebProperties(
		AttributesTable attributes,
		Navaid navaid,
		NavaidSettings settings,
		bool forTextFile)
	{
		FebProperties.Add(attributes, settings.IncludeFebCustomProperties, settings.FebProperties, property =>
			forTextFile && property is NavaidFebProperty.NavId or NavaidFebProperty.NavType or NavaidFebProperty.Name
				? null
				: ValueFor(navaid, property));
	}

	private static object? ValueFor(Navaid navaid, NavaidFebProperty property) => property switch
	{
		NavaidFebProperty.NavId => navaid.NavId,
		NavaidFebProperty.NavType => navaid.NavType,
		NavaidFebProperty.Name => navaid.Name,
		NavaidFebProperty.Freq => navaid.Freq,
		NavaidFebProperty.LowAltArtccId => NullIfBlank(navaid.LowAltArtccId),
		NavaidFebProperty.HighAltArtccId => NullIfBlank(navaid.HighAltArtccId),
		_ => null,
	};

	private static string? NullIfBlank(string value) => value.Length == 0 ? null : value;
}
