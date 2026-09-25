using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Domain.Navaids;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Navaids;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the NAVAIDs sub-service into a typed, validated <see cref="NavaidSettings"/>.
/// </summary>
/// <remarks>
/// The only place in the NAVAIDs sub-service that touches the raw dictionary; everything
/// downstream works with <see cref="NavaidSettings"/>. Generic value reading lives in
/// <see cref="SettingsValueReader"/>, shared with every other sub-service.
/// </remarks>
public static class NavaidSettingsParser
{
	private const string LogSource = "NavaidSettingsParser";

	/// <summary>The keys only NAVAIDs reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"GenerateGeojson", "EmitSymbols", "EmitText", "OutputBy", "ExcludedTypes", "SymbolStyleBy", "FanMarkerStyle",
	};

	/// <summary>
	/// The CRC-defaults classes NAVAIDs reads: <see cref="NavaidOutputFiles.AllClass"/> for
	/// <see cref="NavaidOutputBy.All"/>, plus every known type's token for
	/// <see cref="NavaidOutputBy.Type"/>. NAVAIDs draws Symbols and Text only, never Lines.
	/// </summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new[] { NavaidOutputFiles.AllClass }
			.Concat(NavaidTypes.All.Select(NavaidTypes.Token))
			.ToDictionary(
				className => className,
				_ => new[] { CrcFeatureKind.Symbol, CrcFeatureKind.Text },
				StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Parses and validates <paramref name="navaidSettings"/> into a typed
	/// <see cref="NavaidSettings"/>.
	/// </summary>
	/// <param name="navaidSettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing, a value is invalid, or the combination of
	/// output choices would produce no output at all.
	/// </exception>
	public static NavaidSettingsParseResult Parse(IReadOnlyDictionary<string, string> navaidSettings)
	{
		ArgumentNullException.ThrowIfNull(navaidSettings);

		string outputDirectory = SettingsValueReader.RequiredString(navaidSettings, "OutputDirectory");

		bool generateGeojson = SettingsValueReader.YesNo(navaidSettings, "GenerateGeojson", defaultValue: true);
		bool generateAliasFile = SettingsValueReader.YesNo(navaidSettings, "GenerateAliasFile", defaultValue: true);

		// Selecting the sub-service and then turning off both of its outputs asks for a run that
		// writes nothing. The GUI blocks this at the tab; the parser is the backstop for the
		// harness and for a hand-edited UserConfig.
		if (!generateGeojson && !generateAliasFile)
		{
			throw new ArgumentException(
				"GenerateGeojson and GenerateAliasFile are both \"N\", so the NAVAIDs sub-service would produce nothing. " +
				"Turn one back on, or deselect NAVAIDs.");
		}

		bool emitSymbols = SettingsValueReader.YesNo(navaidSettings, "EmitSymbols", defaultValue: true);
		bool emitText = SettingsValueReader.YesNo(navaidSettings, "EmitText", defaultValue: true);

		// There is no EmitLines: NAVAIDs has no Lines file.
		if (generateGeojson && !emitSymbols && !emitText)
		{
			throw new ArgumentException(
				"EmitSymbols and EmitText are both \"N\", but GenerateGeojson is \"Y\". " +
				"Turn at least one back on, or set GenerateGeojson to \"N\".");
		}

		NavaidOutputBy outputBy = SettingsValueReader.OptionalEnum(navaidSettings, "OutputBy", NavaidOutputBy.All);

		HashSet<string> excludedTypes = SettingsValueReader.StringList(navaidSettings, "ExcludedTypes")
			.Select(type => type.ToUpperInvariant())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		// A type FE-Buddy does not recognize is still left out - the GUI lists whatever types the
		// cycle holds, so the user can untick one NASR added - but it is said, in case of a typo.
		List<ServiceMessage> typeMessages = [.. excludedTypes
			.Where(type => !NavaidTypes.IsKnown(type))
			.Order(StringComparer.OrdinalIgnoreCase)
			.Select(type => new ServiceMessage(LogLevel.Warning, LogSource,
				$"'ExcludedTypes' entry '{type}' is not a NAVAID type FE-Buddy knows ({string.Join(", ", NavaidTypes.All)}). " +
				"NAVAIDs of that type are still left out."))];

		if (NavaidTypes.All.All(excludedTypes.Contains))
		{
			throw new ArgumentException(
				"'ExcludedTypes' excludes every known NAVAID type, so the NAVAIDs sub-service would have nothing to write. " +
				"Remove at least one type from the list.");
		}

		(bool includeFebProperties, IReadOnlyList<NavaidFebProperty> febProperties) =
			SubServiceSettingsReader.ReadFebProperties<NavaidFebProperty>(navaidSettings, example: "navId,navType,freq");

		RegionOfInterest? roi = SubServiceSettingsReader.ReadRoi(navaidSettings);
		int coordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(navaidSettings);

		VnasFileChoices vnas = SubServiceSettingsReader.ReadVnasFiles(
			navaidSettings, NavaidOutputFiles.Alias, NavaidOutputFiles.IsGeojsonKey,
			example: $"{NavaidOutputFiles.Symbols}, {NavaidOutputFiles.Alias}");

		Dictionary<string, CrcSymbolDefaults> symbolDefaults = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CrcTextDefaults> textDefaults = new(StringComparer.OrdinalIgnoreCase);
		NavaidSymbolStyleBy symbolStyleBy = NavaidSymbolStyleBy.Type;
		string? fanMarkerStyle = null;

		if (outputBy == NavaidOutputBy.All)
		{
			// SymbolStyleBy only means anything for the merged All-mode Symbols file.
			symbolStyleBy = SettingsValueReader.OptionalEnum(navaidSettings, "SymbolStyleBy", NavaidSymbolStyleBy.Type);

			bool symbolsGetCrcDefaults = generateGeojson && emitSymbols && vnas.HasCrcDefaults(NavaidOutputFiles.Symbols);

			if (symbolsGetCrcDefaults)
			{
				symbolDefaults[NavaidOutputFiles.AllClass] = CrcDefaultsReader.ReadSymbol(
					navaidSettings, $"Crc.{NavaidOutputFiles.AllClass}.Symbol", readStyle: symbolStyleBy == NavaidSymbolStyleBy.File);
			}

			if (generateGeojson && emitText && vnas.HasCrcDefaults(NavaidOutputFiles.Text))
			{
				textDefaults[NavaidOutputFiles.AllClass] = CrcDefaultsReader.ReadText(navaidSettings, $"Crc.{NavaidOutputFiles.AllClass}.Text");
			}

			// Read only when a merged Symbols file gives each Feature its own style and fan markers
			// are not excluded. Not required: the cycle may hold no fan markers (the GUI then never
			// asks), so one written without a chosen style gets a warning from the writer instead.
			bool fanMarkerStyleUsed = symbolsGetCrcDefaults
				&& symbolStyleBy == NavaidSymbolStyleBy.Type
				&& !excludedTypes.Contains(NavaidTypes.FanMarker);

			if (fanMarkerStyleUsed && SettingsValueReader.OptionalString(navaidSettings, "FanMarkerStyle") is { } requested)
			{
				string normalized = SettingsValueReader.NormalizeStyle(requested, CrcPropertyValidator.ValidSymbolStyles)!;

				if (!CrcPropertyValidator.ValidSymbolStyles.Contains(normalized, StringComparer.Ordinal))
				{
					throw new ArgumentException(
						$"'FanMarkerStyle' value '{requested}' is invalid. Valid values: {string.Join(", ", CrcPropertyValidator.ValidSymbolStyles)}.");
				}

				fanMarkerStyle = normalized;
			}
		}
		else
		{
			// Read from the keys chosen for CRC-ERAM defaults rather than from the known types, so
			// a type FE-Buddy does not recognize still gets the defaults its file needs.
			HashSet<string> excludedTokens = excludedTypes.Select(NavaidTypes.Token).ToHashSet(StringComparer.OrdinalIgnoreCase);

			foreach (string key in vnas.CrcDefaultsFiles)
			{
				if (!NavaidOutputFiles.TryParseTypeKey(key, out string token, out CrcFeatureKind kind)
					|| excludedTokens.Contains(token)
					|| !generateGeojson)
				{
					continue;
				}

				if (kind == CrcFeatureKind.Symbol && emitSymbols)
				{
					symbolDefaults[token] = CrcDefaultsReader.ReadSymbol(navaidSettings, $"Crc.{token}.Symbol");
				}
				else if (kind == CrcFeatureKind.Text && emitText)
				{
					textDefaults[token] = CrcDefaultsReader.ReadText(navaidSettings, $"Crc.{token}.Text");
				}
			}
		}

		// Every class whose defaults were read counts as known, on top of the fixed ones.
		Dictionary<string, CrcFeatureKind[]> crcKindsByClass = new(CrcKindsByClass, StringComparer.OrdinalIgnoreCase);

		foreach (string className in symbolDefaults.Keys.Concat(textDefaults.Keys))
		{
			crcKindsByClass.TryAdd(className, [CrcFeatureKind.Symbol, CrcFeatureKind.Text]);
		}

		List<ServiceMessage> messages =
		[
			.. typeMessages,
			.. SubServiceSettingsReader.UnknownKeyWarnings(
				navaidSettings, OwnKeys, crcKindsByClass, LogSource,
				labelSource: "a NAVAID's label is always built from its identifier, name and type"),
		];

		NavaidSettings settings = new()
		{
			OutputDirectory = outputDirectory,
			GenerateGeojson = generateGeojson,
			EmitSymbols = emitSymbols,
			EmitText = emitText,
			GenerateAliasFile = generateAliasFile,
			OutputBy = outputBy,
			ExcludedTypes = excludedTypes,
			SymbolStyleBy = symbolStyleBy,
			FanMarkerStyle = fanMarkerStyle,
			IncludeFebCustomProperties = includeFebProperties,
			FebProperties = febProperties,
			Vnas = vnas,
			Roi = roi,
			CoordinatePrecision = coordinatePrecision,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults
		};

		return new NavaidSettingsParseResult(settings, messages);
	}
}
