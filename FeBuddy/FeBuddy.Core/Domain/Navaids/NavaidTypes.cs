using System.Globalization;
using System.Text;

namespace FeBuddy.Core.Domain.Navaids;

/// <summary>
/// The NASR <c>NAV_TYPE</c> vocabulary (<c>NAV_BASE.NAV_TYPE</c>) and the rules FE-Buddy applies
/// to it: file-naming tokens, CRC symbol styles, and frequency formatting.
/// </summary>
/// <remarks>
/// A NAVAID type FE-Buddy does not recognize (NASR occasionally adds one) is not rejected: it is
/// still built and written, just without a mapped <see cref="SymbolStyleFor"/> - see
/// <see cref="IsKnown"/>.
/// </remarks>
public static class NavaidTypes
{
	/// <summary>The <c>NAV_TYPE</c> for a fan marker, spelled with FE-Buddy's canonical single space.</summary>
	public const string FanMarker = "FAN MARKER";

	/// <summary>
	/// Every NAVAID type FE-Buddy recognizes, in the display order the GUI offers them (e.g. for
	/// <c>ExcludedTypes</c>).
	/// </summary>
	public static IReadOnlyList<string> All { get; } =
	[
		"VOR", "VORTAC", "VOR/DME", "VOT", "TACAN", "DME", "NDB", "NDB/DME",
		"MARINE NDB", "MARINE NDB/DME", "UHF/NDB", FanMarker, "CONSOLAN",
	];

	/// <summary>The NDB-family types, whose frequencies are in kHz rather than MHz. See <see cref="IsNdbFamily"/>.</summary>
	private static readonly IReadOnlySet<string> NdbFamilyTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"NDB", "NDB/DME", "MARINE NDB", "MARINE NDB/DME", "UHF/NDB", "CONSOLAN",
	};

	/// <summary>The CRC symbol style each type renders as by default. See <see cref="SymbolStyleFor"/>.</summary>
	private static readonly IReadOnlyDictionary<string, string> SymbolStyles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["VOR"] = "vor",
		["VORTAC"] = "vor",
		["VOR/DME"] = "vor",
		["VOT"] = "vor",
		["TACAN"] = "tacan",
		["DME"] = "tacan",
		["NDB"] = "ndb",
		["NDB/DME"] = "ndb",
		["MARINE NDB"] = "ndb",
		["MARINE NDB/DME"] = "ndb",
		["UHF/NDB"] = "ndb",
	};

	private static readonly IReadOnlySet<string> KnownTypes = new HashSet<string>(All, StringComparer.OrdinalIgnoreCase);

	/// <summary>Whether <paramref name="type"/> is one of the types in <see cref="All"/>, ignoring case.</summary>
	/// <param name="type">The NASR <c>NAV_TYPE</c> value.</param>
	/// <returns><see langword="true"/> when FE-Buddy recognizes the type.</returns>
	public static bool IsKnown(string type) => KnownTypes.Contains(type);

	/// <summary>
	/// Turns a NAVAID type into the token used in file names and as a per-type CRC class name:
	/// upper-cased, with every run of <c>/</c> and whitespace collapsed to a single <c>-</c>.
	/// </summary>
	/// <param name="type">The NASR <c>NAV_TYPE</c> value, e.g. <c>VOR/DME</c>, <c>FAN MARKER</c>, <c>MARINE NDB/DME</c>.</param>
	/// <returns>The token, e.g. <c>VOR-DME</c>, <c>FAN-MARKER</c>, <c>MARINE-NDB-DME</c>.</returns>
	public static string Token(string type)
	{
		ArgumentNullException.ThrowIfNull(type);

		StringBuilder token = new();
		bool lastWasSeparator = true; // Suppresses a leading '-' from leading whitespace/slashes.

		foreach (char c in type.Trim())
		{
			if (c == '/' || char.IsWhiteSpace(c))
			{
				if (!lastWasSeparator)
				{
					token.Append('-');
					lastWasSeparator = true;
				}
			}
			else
			{
				token.Append(char.ToUpperInvariant(c));
				lastWasSeparator = false;
			}
		}

		// A trailing separator (trailing whitespace/slashes) would otherwise leave a dangling '-'.
		if (token.Length > 0 && token[^1] == '-')
		{
			token.Length--;
		}

		return token.ToString();
	}

	/// <summary>
	/// The CRC symbol style a type renders as by default: <c>vor</c> for VOR, VORTAC, VOR/DME and
	/// VOT; <c>tacan</c> for TACAN and DME; <c>ndb</c> for NDB, NDB/DME, MARINE NDB, MARINE
	/// NDB/DME and UHF/NDB.
	/// </summary>
	/// <param name="type">The NASR <c>NAV_TYPE</c> value.</param>
	/// <returns>
	/// The style, or <see langword="null"/> for <see cref="FanMarker"/> (the user picks its own
	/// style), <c>CONSOLAN</c>, or any type FE-Buddy does not recognize.
	/// </returns>
	public static string? SymbolStyleFor(string type) =>
		SymbolStyles.TryGetValue(type, out string? style) ? style : null;

	/// <summary>
	/// Whether a type's frequency is published in kHz rather than MHz: NDB, NDB/DME, MARINE NDB,
	/// MARINE NDB/DME, UHF/NDB and CONSOLAN.
	/// </summary>
	/// <param name="type">The NASR <c>NAV_TYPE</c> value.</param>
	/// <returns><see langword="true"/> for an NDB-family type.</returns>
	public static bool IsNdbFamily(string type) => NdbFamilyTypes.Contains(type);

	/// <summary>
	/// Formats a NAVAID's frequency for display: NDB-family types (kHz) drop trailing zeros
	/// (<c>365</c> for 365 kHz); every other type (MHz) always shows two decimal places
	/// (<c>114.20</c> for 114.2 MHz, <c>116.65</c> for 116.65 MHz).
	/// </summary>
	/// <param name="type">The NASR <c>NAV_TYPE</c> value.</param>
	/// <param name="freq">The frequency, <c>NAV_BASE.FREQ</c>, or <see langword="null"/> when the NAVAID publishes none.</param>
	/// <returns>The formatted frequency, or an empty string when <paramref name="freq"/> is <see langword="null"/>.</returns>
	public static string FormatFrequency(string type, double? freq)
	{
		if (freq is not { } value)
		{
			return string.Empty;
		}

		return IsNdbFamily(type)
			? value.ToString("0.##", CultureInfo.InvariantCulture)
			: value.ToString("0.00", CultureInfo.InvariantCulture);
	}
}
