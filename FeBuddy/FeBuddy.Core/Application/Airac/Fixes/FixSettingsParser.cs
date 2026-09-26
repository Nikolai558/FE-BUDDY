using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Fixes;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Fixes;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the Fixes sub-service into a typed, validated <see cref="FixSettings"/>.
/// </summary>
/// <remarks>
/// The only place in the Fixes sub-service that touches the raw dictionary; everything downstream
/// works with <see cref="FixSettings"/>. Unlike most other AIRAC sub-services, there is no
/// <c>GenerateGeojson</c> toggle and no <c>GenerateAliasFile</c>: the sub-service always writes
/// GeoJSON and has no alias file.
/// </remarks>
public static class FixSettingsParser
{
	private const string LogSource = "FixSettingsParser";

	/// <summary>The keys only Fixes reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"EmitSymbols", "EmitText", "OutputBy", "ExcludedFixUses", "ExcludedCharts", "Combinations",
	};

	/// <summary>
	/// The CRC-defaults classes always known, whatever <c>CrcDefaultsFor</c> names:
	/// <see cref="FixOutputFiles.AllClass"/>, every known fix use name, and
	/// <see cref="FixCharts.NoChart"/>. Every one draws Symbol and Text.
	/// </summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> BaseCrcKindsByClass =
		new[] { FixOutputFiles.AllClass }
			.Concat(FixUses.All)
			.Append(FixCharts.NoChart)
			.ToDictionary(
				className => className,
				_ => new[] { CrcFeatureKind.Symbol, CrcFeatureKind.Text },
				StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Parses and validates <paramref name="fixSettings"/> into a typed <see cref="FixSettings"/>.
	/// </summary>
	/// <param name="fixSettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing, a value is invalid, or the combination of
	/// output choices would produce no output at all.
	/// </exception>
	public static FixSettingsParseResult Parse(IReadOnlyDictionary<string, string> fixSettings)
	{
		ArgumentNullException.ThrowIfNull(fixSettings);

		string outputDirectory = SettingsValueReader.RequiredString(fixSettings, "OutputDirectory");

		bool emitSymbols = SettingsValueReader.YesNo(fixSettings, "EmitSymbols", defaultValue: true);
		bool emitText = SettingsValueReader.YesNo(fixSettings, "EmitText", defaultValue: true);

		// GeoJSON is the only output the Fixes sub-service has, so turning both off would produce
		// nothing at all.
		if (!emitSymbols && !emitText)
		{
			throw new ArgumentException(
				"'EmitSymbols' and 'EmitText' are both \"N\", so the Fixes sub-service would produce nothing. " +
				"Turn at least one back on, or deselect Fixes.");
		}

		FixOutputBy outputBy = SettingsValueReader.OptionalEnum(fixSettings, "OutputBy", FixOutputBy.All);

		HashSet<string> excludedFixUses = SettingsValueReader.StringList(fixSettings, "ExcludedFixUses")
			.Select(FixUses.Token)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		// Meaningful only in the FixUse file layout - the GUI lists whatever fix uses the cycle
		// holds, so the user can untick one NASR added - but it is said, in case of a typo.
		List<ServiceMessage> fixUseMessages = [.. excludedFixUses
			.Where(token => !FixUses.IsKnown(token))
			.Order(StringComparer.OrdinalIgnoreCase)
			.Select(token => new ServiceMessage(LogLevel.Warning, LogSource,
				$"'ExcludedFixUses' entry '{token}' is not a fix use FE-Buddy knows ({string.Join(", ", FixUses.All)}). " +
				"Fixes with that use are still left out."))];

		if (outputBy == FixOutputBy.FixUse && FixUses.All.All(excludedFixUses.Contains))
		{
			throw new ArgumentException(
				"'ExcludedFixUses' excludes every known fix use, so the Fixes sub-service would have nothing to write " +
				"in the 'FixUse' file layout. Remove at least one fix use from the list, or choose another file layout.");
		}

		HashSet<string> excludedCharts = SettingsValueReader.StringList(fixSettings, "ExcludedCharts")
			.Select(FixCharts.Token)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		(List<FixCombination> combinations, List<ServiceMessage> combinationMessages) = ParseCombinations(fixSettings);

		if (outputBy == FixOutputBy.ChartAndFixUse && combinations.Count == 0)
		{
			throw new ArgumentException(
				"OutputBy is 'ChartAndFixUse' but 'Combinations' names none. " +
				"Add at least one chart + fix use combination, or choose another file layout.");
		}

		(bool includeFebProperties, IReadOnlyList<FixFebProperty> febProperties) =
			SubServiceSettingsReader.ReadFebProperties<FixFebProperty>(fixSettings, example: "fixId,fixUseCode,charts");

		RegionOfInterest? roi = SubServiceSettingsReader.ReadRoi(fixSettings);
		int coordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(fixSettings);

		// No alias file, so aliasFileKey is null: no UploadToVnas/CrcDefaultsFor entry can name one.
		VnasFileChoices vnas = SubServiceSettingsReader.ReadVnasFiles(
			fixSettings, aliasFileKey: null, FixOutputFiles.IsGeojsonKey,
			example: $"{FixOutputFiles.Symbols}, {FixOutputFiles.Text}");

		Dictionary<string, CrcSymbolDefaults> symbolDefaults = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CrcTextDefaults> textDefaults = new(StringComparer.OrdinalIgnoreCase);

		if (outputBy == FixOutputBy.All)
		{
			if (emitSymbols && vnas.HasCrcDefaults(FixOutputFiles.Symbols))
			{
				symbolDefaults[FixOutputFiles.AllClass] = CrcDefaultsReader.ReadSymbol(fixSettings, $"Crc.{FixOutputFiles.AllClass}.Symbol");
			}

			if (emitText && vnas.HasCrcDefaults(FixOutputFiles.Text))
			{
				textDefaults[FixOutputFiles.AllClass] = CrcDefaultsReader.ReadText(fixSettings, $"Crc.{FixOutputFiles.AllClass}.Text");
			}
		}
		else
		{
			// ExcludedFixUses/ExcludedCharts only drop a group from the file layout that actually
			// groups by it; a combination group (ChartAndFixUse) is never excluded this way.
			bool IsExcludedGroup(string group) => outputBy switch
			{
				FixOutputBy.FixUse => excludedFixUses.Contains(group),
				FixOutputBy.Chart => excludedCharts.Contains(group),
				_ => false,
			};

			// Read from the keys chosen for CRC-ERAM defaults rather than from a fixed group list,
			// so a group FE-Buddy did not anticipate (an unrecognized fix use, or any chart name)
			// still gets the defaults its file needs.
			foreach (string key in vnas.CrcDefaultsFiles)
			{
				if (!FixOutputFiles.TryParseGroupKey(key, out string group, out CrcFeatureKind kind) || IsExcludedGroup(group))
				{
					continue;
				}

				if (kind == CrcFeatureKind.Symbol && emitSymbols)
				{
					symbolDefaults[group] = CrcDefaultsReader.ReadSymbol(fixSettings, $"Crc.{group}.Symbol");
				}
				else if (kind == CrcFeatureKind.Text && emitText)
				{
					textDefaults[group] = CrcDefaultsReader.ReadText(fixSettings, $"Crc.{group}.Text");
				}
			}
		}

		// Every combination group and every class whose defaults were read also counts as known,
		// on top of the fixed ones.
		Dictionary<string, CrcFeatureKind[]> crcKindsByClass = new(BaseCrcKindsByClass, StringComparer.OrdinalIgnoreCase);

		foreach (FixCombination combination in combinations)
		{
			crcKindsByClass.TryAdd(combination.Group, [CrcFeatureKind.Symbol, CrcFeatureKind.Text]);
		}

		foreach (string className in symbolDefaults.Keys.Concat(textDefaults.Keys))
		{
			crcKindsByClass.TryAdd(className, [CrcFeatureKind.Symbol, CrcFeatureKind.Text]);
		}

		List<ServiceMessage> messages =
		[
			.. fixUseMessages,
			.. combinationMessages,
			.. SubServiceSettingsReader.UnknownKeyWarnings(
				fixSettings, OwnKeys, crcKindsByClass, LogSource,
				labelSource: "a fix's label is always its identifier"),
		];

		FixSettings settings = new()
		{
			OutputDirectory = outputDirectory,
			EmitSymbols = emitSymbols,
			EmitText = emitText,
			OutputBy = outputBy,
			ExcludedFixUses = excludedFixUses,
			ExcludedCharts = excludedCharts,
			Combinations = combinations,
			IncludeFebCustomProperties = includeFebProperties,
			FebProperties = febProperties,
			Vnas = vnas,
			Roi = roi,
			CoordinatePrecision = coordinatePrecision,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults
		};

		return new FixSettingsParseResult(settings, messages);
	}

	/// <summary>Parses the comma-separated <c>Combinations</c> list into tokenized chart + fix use pairs.</summary>
	private static (List<FixCombination> Combinations, List<ServiceMessage> Messages) ParseCombinations(
		IReadOnlyDictionary<string, string> fixSettings)
	{
		List<FixCombination> combinations = [];
		List<ServiceMessage> messages = [];

		foreach (string entry in SettingsValueReader.StringList(fixSettings, "Combinations"))
		{
			string[] parts = entry.Split('+');
			string chartToken = parts.Length == 2 ? FixCharts.Token(parts[0]) : string.Empty;
			string fixUseToken = parts.Length == 2 ? FixUses.Token(parts[1]) : string.Empty;

			// A side with no letters or digits tokenizes to nothing, which would name no file.
			if (chartToken.Length == 0 || fixUseToken.Length == 0)
			{
				throw new ArgumentException(
					$"'Combinations' entry '{entry}' is not valid. Each entry must be a chart and a fix use joined " +
					"by '+', e.g. \"ENROUTE-LOW+WYPNT\".");
			}

			if (!FixUses.IsKnown(fixUseToken))
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"'Combinations' entry '{entry}' names fix use '{fixUseToken}', which is not a fix use FE-Buddy knows " +
					$"({string.Join(", ", FixUses.All)}). It is still used."));
			}

			FixCombination combination = new(chartToken, fixUseToken);

			if (!combinations.Contains(combination))
			{
				combinations.Add(combination);
			}
		}

		return (combinations, messages);
	}
}
