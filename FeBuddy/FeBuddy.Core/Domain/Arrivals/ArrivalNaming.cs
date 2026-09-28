using System.Text.RegularExpressions;

using FeBuddy.Core.Domain.Arrivals.Models;

namespace FeBuddy.Core.Domain.Arrivals;

/// <summary>
/// Derives the identifier FE-Buddy uses for an arrival procedure in file names and alias
/// commands (<see cref="ArrivalProcedure.CodeId"/>).
/// </summary>
/// <remarks>
/// <para>
/// The primary source is the FAA computer code, and a STAR code is the reverse of a departure's:
/// it reads <c>TRANSITION.PROCEDURE</c>. <c>AALAN.BLAID2</c> is the procedure <c>BLAID</c>,
/// amendment <c>2</c>, transition <c>AALAN</c>; <c>FIM.FERN7</c> is <c>FERN</c>, amendment
/// <c>7</c>, transition <c>FIM</c>; <c>MRB.EMI7</c> is <c>EMI</c>, amendment <c>7</c>, transition
/// <c>MRB</c>. The code is preferred over the published name because it is what a controller
/// types and what the chart prints in brackets, and because it is always a clean identifier
/// where the name is often not: <c>FIM.FERN7</c> is published as <c>FERNANDO</c>, and
/// <c>MRB.EMI7</c> as <c>WESTMINSTER</c>.
/// </para>
/// <para>
/// The amendment digit is removed by matching it against <c>AMENDMENT_NO</c>, not by cutting at
/// the first digit, the same rule as a departure: an identifier with digits of its own (as some
/// departures have, e.g. <c>1U7</c>) keeps them.
/// </para>
/// <para>
/// When there is no usable code - it is <c>NOT ASSIGNED</c>, or its digit does not match the
/// amendment - the published arrival name is used with everything but letters and digits removed
/// (<c>WILKES-BARRE</c> becomes <c>WILKESBARRE</c>). Either way the result contains only letters and digits,
/// which is what a CRC alias name (<c>\.\w+</c>) and a file name both need.
/// </para>
/// </remarks>
internal static class ArrivalNaming
{
	/// <summary>The literal value NASR uses in <c>STAR_COMPUTER_CODE</c> when the FAA has not assigned one.</summary>
	internal const string NotAssigned = "NOT ASSIGNED";

	private static readonly Regex NonAlphanumeric = new("[^A-Za-z0-9]", RegexOptions.Compiled);

	private static readonly Dictionary<string, int> AmendmentNumbers = new(StringComparer.OrdinalIgnoreCase)
	{
		["ONE"] = 1,
		["TWO"] = 2,
		["THREE"] = 3,
		["FOUR"] = 4,
		["FIVE"] = 5,
		["SIX"] = 6,
		["SEVEN"] = 7,
		["EIGHT"] = 8,
		["NINE"] = 9,
		["TEN"] = 10,
		["ELEVEN"] = 11,
		["TWELVE"] = 12,
		["THIRTEEN"] = 13,
		["FOURTEEN"] = 14,
		["FIFTEEN"] = 15,
		["SIXTEEN"] = 16,
		["SEVENTEEN"] = 17,
		["EIGHTEEN"] = 18,
		["NINETEEN"] = 19,
		["TWENTY"] = 20,
	};

	/// <summary>
	/// Returns the identifier for a procedure.
	/// </summary>
	/// <param name="computerCode">The published <c>STAR_COMPUTER_CODE</c>, e.g. <c>AALAN.BLAID2</c>.</param>
	/// <param name="amendmentNo">The published <c>AMENDMENT_NO</c>, e.g. <c>TWO</c>.</param>
	/// <param name="arrivalName">The published <c>ARRIVAL_NAME</c>, used when the code cannot be.</param>
	/// <param name="usedFallback"><see langword="true"/> when the result came from <paramref name="arrivalName"/>.</param>
	/// <returns>An upper-case identifier containing only letters and digits.</returns>
	internal static string CodeIdFor(string? computerCode, string? amendmentNo, string arrivalName, out bool usedFallback)
	{
		string? fromCode = FromComputerCode(computerCode, amendmentNo);

		if (fromCode is not null)
		{
			usedFallback = false;
			return fromCode;
		}

		usedFallback = true;
		return Clean(arrivalName);
	}

	/// <summary>Removes everything but letters and digits and upper-cases the rest.</summary>
	/// <param name="value">The raw text.</param>
	/// <returns>The cleaned identifier.</returns>
	internal static string Clean(string value) =>
		NonAlphanumeric.Replace(value ?? string.Empty, string.Empty).ToUpperInvariant();

	private static string? FromComputerCode(string? computerCode, string? amendmentNo)
	{
		string code = computerCode?.Trim() ?? string.Empty;

		if (code.Length == 0 || code.Equals(NotAssigned, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		if (amendmentNo is null || !AmendmentNumbers.TryGetValue(amendmentNo.Trim(), out int amendment))
		{
			return null;
		}

		int dot = code.IndexOf('.');
		string procedurePart = dot >= 0 ? code[(dot + 1)..] : code;
		string suffix = amendment.ToString(System.Globalization.CultureInfo.InvariantCulture);

		if (!procedurePart.EndsWith(suffix, StringComparison.Ordinal))
		{
			return null;
		}

		string core = Clean(procedurePart[..^suffix.Length]);
		return core.Length > 0 ? core : null;
	}
}
