using System.Globalization;

using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Telephony;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Telephony;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the Telephony sub-service into a typed, validated <see cref="TelephonySettings"/>.
/// </summary>
/// <remarks>
/// <para>
/// Telephony's only output is its alias file, so there is little to read: where to write, the
/// user's virtual airlines - numbered, merged in number order - and whether the virtual airline
/// list is merged too:
/// </para>
/// <code>
/// VirtualAirlines.1.Designator      = DVA
/// VirtualAirlines.1.Telephony       = DELTA
/// VirtualAirlines.1.Organization    = Delta Virtual
/// IncludeVatsimRadarVirtualAirlines = Y
/// </code>
/// <para>
/// The shared keys every sub-service tab sends (region of interest, coordinate precision, FE-Buddy
/// properties) are accepted and ignored - Telephony writes no GeoJSON and is not limited to a region.
/// </para>
/// </remarks>
public static class TelephonySettingsParser
{
	/// <summary>The start of every virtual airline's keys, then its number and a field: <c>VirtualAirlines.1.Designator</c>.</summary>
	public const string VirtualAirlinesPrefix = "VirtualAirlines.";

	/// <summary>A virtual airline's three-letter designator, under <see cref="VirtualAirlinesPrefix"/> and its number.</summary>
	public const string DesignatorKey = "Designator";

	/// <summary>A virtual airline's telephony, under <see cref="VirtualAirlinesPrefix"/> and its number.</summary>
	public const string TelephonyKey = "Telephony";

	/// <summary>A virtual airline's virtual organization, under <see cref="VirtualAirlinesPrefix"/> and its number.</summary>
	public const string OrganizationKey = "Organization";

	/// <summary>Whether the virtual airline list is written too: <c>Y</c> or <c>N</c> (the default).</summary>
	public const string IncludeVatsimRadarKey = "IncludeVatsimRadarVirtualAirlines";

	private const string LogSource = "TelephonySettingsParser";

	/// <summary>The keys only Telephony reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"GenerateAliasFile",
		IncludeVatsimRadarKey,
	};

	/// <summary>Telephony writes no GeoJSON, so it draws from no CRC-ERAM defaults class at all.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Parses and validates <paramref name="telephonySettings"/> into a typed <see cref="TelephonySettings"/>.
	/// </summary>
	/// <param name="telephonySettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing or invalid, the alias file - the only output - is
	/// turned off, or a virtual airline can't be written (<see cref="VirtualAirlineProblem"/>).
	/// </exception>
	public static TelephonySettingsParseResult Parse(IReadOnlyDictionary<string, string> telephonySettings)
	{
		ArgumentNullException.ThrowIfNull(telephonySettings);

		string outputDirectory = SettingsValueReader.RequiredString(telephonySettings, "OutputDirectory");

		if (!SettingsValueReader.YesNo(telephonySettings, "GenerateAliasFile", defaultValue: true))
		{
			throw new ArgumentException(
				"'GenerateAliasFile' is \"N\", but the alias file is the Telephony sub-service's only output, so it would produce " +
				"nothing. Turn it back on, or leave Telephony out of the run.");
		}

		List<ServiceMessage> messages = [];

		if (SettingsValueReader.YesNo(telephonySettings, "IncludeFebCustomProperties", defaultValue: false))
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"'IncludeFebCustomProperties' is \"Y\", but Telephony writes no GeoJSON, so it has no FE-Buddy properties to add; it was ignored."));
		}

		IReadOnlyList<VirtualAirline> virtualAirlines = ReadVirtualAirlines(telephonySettings, messages, out HashSet<string> virtualAirlineKeys);
		bool includeVatsimRadar = SettingsValueReader.YesNo(telephonySettings, IncludeVatsimRadarKey, defaultValue: false);

		messages.AddRange(SubServiceSettingsReader.UnknownKeyWarnings(
			telephonySettings.Where(entry => !virtualAirlineKeys.Contains(entry.Key)).ToDictionary(StringComparer.OrdinalIgnoreCase),
			OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "Telephony writes no GeoJSON, so it has no per-feature CRC output"));

		TelephonySettings settings = new()
		{
			OutputDirectory = outputDirectory,
			VirtualAirlines = virtualAirlines,
			IncludeVatsimRadarVirtualAirlines = includeVatsimRadar,
		};

		return new TelephonySettingsParseResult(settings, messages);
	}

	/// <summary>
	/// Whether a Telephony settings block includes the virtual airline list, so the AIRAC
	/// Service knows to download it before the run. A value that is neither <c>Y</c> nor <c>N</c> counts
	/// as no here; <see cref="Parse"/> is what reports it.
	/// </summary>
	/// <param name="telephonySettings">The raw settings block.</param>
	/// <returns><see langword="true"/> for <c>Y</c>.</returns>
	public static bool IncludesVatsimRadarList(IReadOnlyDictionary<string, string> telephonySettings)
	{
		ArgumentNullException.ThrowIfNull(telephonySettings);

		try
		{
			return SettingsValueReader.YesNo(telephonySettings, IncludeVatsimRadarKey, defaultValue: false);
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	/// <summary>
	/// What stops a virtual airline from being written, or <see langword="null"/> when nothing does.
	/// The Telephony tab asks the same before it adds one.
	/// </summary>
	/// <param name="designator">Its three-letter designator.</param>
	/// <param name="telephony">Its telephony.</param>
	/// <param name="organization">Its virtual organization.</param>
	/// <returns>e.g. <c>its 3LD must be three letters</c>, to follow "Virtual airline 2: ".</returns>
	public static string? VirtualAirlineProblem(string? designator, string? telephony, string? organization)
	{
		if (!TelephonyNaming.IsThreeLetterDesignator(designator ?? string.Empty))
		{
			return "its 3LD must be three letters";
		}

		if (TelephonyNaming.CommandName(telephony ?? string.Empty) is null)
		{
			return "its telephony needs at least one letter or digit";
		}

		return string.IsNullOrWhiteSpace(organization) ? "it has no virtual organization" : null;
	}

	/// <summary>
	/// Reads the numbered <c>VirtualAirlines.&lt;n&gt;.*</c> keys, in number order. A number with
	/// nothing in it is skipped; one listed twice is written once.
	/// </summary>
	/// <param name="settings">The raw settings dictionary.</param>
	/// <param name="messages">Where to add warnings and notes.</param>
	/// <param name="keys">Every key read, to leave out of the unknown-key check.</param>
	/// <returns>The virtual airlines, trimmed.</returns>
	/// <exception cref="ArgumentException">A virtual airline can't be written.</exception>
	private static IReadOnlyList<VirtualAirline> ReadVirtualAirlines(
		IReadOnlyDictionary<string, string> settings,
		List<ServiceMessage> messages,
		out HashSet<string> keys)
	{
		keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		SortedDictionary<int, Dictionary<string, string>> byNumber = [];

		foreach (KeyValuePair<string, string> entry in settings)
		{
			if (!TrySplitVirtualAirlineKey(entry.Key, out int number, out string field))
			{
				continue;
			}

			keys.Add(entry.Key);

			if (!byNumber.TryGetValue(number, out Dictionary<string, string>? fields))
			{
				fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				byNumber[number] = fields;
			}

			fields[field] = entry.Value?.Trim() ?? string.Empty;
		}

		List<VirtualAirline> virtualAirlines = [];
		Dictionary<VirtualAirline, int> firstNumber = [];

		foreach ((int number, Dictionary<string, string> fields) in byNumber)
		{
			foreach (string field in fields.Keys.Where(f => !f.Equals(DesignatorKey, StringComparison.OrdinalIgnoreCase)
				&& !f.Equals(TelephonyKey, StringComparison.OrdinalIgnoreCase)
				&& !f.Equals(OrganizationKey, StringComparison.OrdinalIgnoreCase)))
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"Unknown Telephony setting '{VirtualAirlinesPrefix}{number}.{field}' was ignored."));
			}

			string designator = fields.GetValueOrDefault(DesignatorKey, string.Empty);
			string telephony = fields.GetValueOrDefault(TelephonyKey, string.Empty);
			string organization = fields.GetValueOrDefault(OrganizationKey, string.Empty);

			if (designator.Length == 0 && telephony.Length == 0 && organization.Length == 0)
			{
				continue;
			}

			if (VirtualAirlineProblem(designator, telephony, organization) is { } problem)
			{
				throw new ArgumentException($"Virtual airline {number}: {problem}.");
			}

			// Compared as the card prints them, so a change of case alone is still the same airline.
			VirtualAirline virtualAirline = new(designator.ToUpperInvariant(), telephony.ToUpperInvariant(), organization.ToUpperInvariant());

			if (firstNumber.TryGetValue(virtualAirline, out int earlier))
			{
				messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
					$"Virtual airline {number} is the same as virtual airline {earlier}, so it was written once."));
				continue;
			}

			firstNumber[virtualAirline] = number;
			virtualAirlines.Add(new VirtualAirline(designator, telephony, organization));
		}

		return virtualAirlines;
	}

	/// <summary>Splits <c>VirtualAirlines.2.Telephony</c> into <c>2</c> and <c>Telephony</c>.</summary>
	private static bool TrySplitVirtualAirlineKey(string key, out int number, out string field)
	{
		number = 0;
		field = string.Empty;

		if (!key.StartsWith(VirtualAirlinesPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string[] parts = key[VirtualAirlinesPrefix.Length..].Split('.');

		if (parts.Length != 2
			|| !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out number)
			|| number < 1
			|| parts[1].Length == 0)
		{
			return false;
		}

		field = parts[1];
		return true;
	}
}
