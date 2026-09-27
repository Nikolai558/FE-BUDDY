using System.Globalization;

using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

namespace FeBuddy.Core.Domain.Procedures.ChartRecall;

/// <summary>
/// Works out the FAA Chart Recall code for a departure (<c>DP</c>), obstacle departure
/// (<c>ODP</c>) or STAR (<c>STR</c>) chart.
/// </summary>
/// <remarks>
/// <para>
/// The code comes from the chart's computer code (<c>faanfd18</c>) whenever it has one, the way
/// FE-Buddy 2.x made it: a departure's first half and a STAR's second half, less the version
/// number - <c>JALEX3.JALEX</c> is <c>JALEX</c>, <c>BRODE.GRUUB1</c> is <c>GRUUB</c>. The version is
/// the one the chart name spells out (<c>THREE</c>), so a procedure named after an airport whose
/// identifier ends in a digit keeps it: <c>L711.LHS</c> (<c>CALIFORNIA CITY ONE</c>) is <c>L71</c>,
/// not <c>L</c>. When the name has no version, a single trailing digit is dropped.
/// </para>
/// <para>
/// A chart with no usable computer code is named from its chart name instead, with the version,
/// every bracketed word and the words <c>RNAV</c>, <c>OBSTACLE</c> and <c>COPTER</c> dropped, and
/// only letters and digits kept: <c>KNIK THREE</c> is <c>KNIK</c>, <c>CAPE LISBURNE EIGHT
/// (OBSTACLE)</c> is <c>CAPELISBURNE</c>. A runway in the name is kept, without <c>RWY</c>:
/// <c>TIN CITY FIVE RWY 17</c> is <c>TINCITY17</c>. When what is left names the airport the chart
/// serves (<c>TATALINA</c> at <c>TATALINA LRRS</c>), <see cref="ChartRecallCodeResult.NamesItsAirport"/>
/// tells the caller to use the airport's identifier instead.
/// </para>
/// </remarks>
internal static class SidStarCodes
{
	/// <summary>The spelled-out version numbers the FAA uses in procedure names.</summary>
	private static readonly Dictionary<string, int> VersionWords = new(StringComparer.OrdinalIgnoreCase)
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

	/// <summary>Words that describe a procedure rather than name it, dropped from a name-based code.</summary>
	private static readonly HashSet<string> DescriptiveWords = new(StringComparer.OrdinalIgnoreCase) { "RNAV", "OBSTACLE", "COPTER" };

	/// <summary>
	/// Works out the code for one departure, obstacle departure or STAR chart.
	/// </summary>
	/// <param name="isStar">Whether the chart is a STAR, whose code is the computer code's second half rather than its first.</param>
	/// <param name="name">The chart's base name - no <c>, CONT.n</c> - in upper case, e.g. <c>JALEX THREE (RNAV)</c>.</param>
	/// <param name="computerCode">The chart's computer code (<c>faanfd18</c>), e.g. <c>JALEX3.JALEX</c>; blank when it has none.</param>
	/// <param name="airportName">The name of the airport the chart is listed under, e.g. <c>TATALINA LRRS</c>.</param>
	/// <returns>The code, or what was not recognized.</returns>
	internal static ChartRecallCodeResult For(bool isStar, string name, string? computerCode, string airportName)
	{
		(IReadOnlyList<string> nameWords, string runway, int? version) = SplitName(name);

		if (FromComputerCode(isStar, computerCode, version) is { } fromCode)
		{
			return new ChartRecallCodeResult { Codes = [fromCode] };
		}

		string fromName = string.Concat(nameWords) + runway;

		if (fromName.Length == 0)
		{
			return new ChartRecallCodeResult { Unrecognized = [$"a name to make a command from in '{name}'"] };
		}

		return new ChartRecallCodeResult
		{
			Codes = [fromName],
			IsNamedFromChartName = true,
			NamesItsAirport = nameWords.Count > 0 && ContainsRun(ChartRecallText.Words(airportName), nameWords),
		};
	}

	/// <summary>
	/// Splits a chart name into the words that name the procedure, the runway it is limited to (if
	/// any, without <c>RWY</c>), and its version number (if the name ends in one).
	/// </summary>
	/// <remarks>
	/// <c>TATALINA FOUR (OBSTACLE) (RNAV)</c> gives <c>[TATALINA]</c>, no runway, version 4;
	/// <c>TIN CITY FIVE RWY 17</c> gives <c>[TIN, CITY]</c>, <c>17</c>, version 5; <c>DEVLN 1</c> gives
	/// <c>[DEVLN]</c>, no runway, version 1.
	/// </remarks>
	private static (IReadOnlyList<string> Words, string Runway, int? Version) SplitName(string name)
	{
		List<string> words = [.. ChartRecallText.Words(ChartRecallText.WithoutBrackets(name))
			.Where(word => !DescriptiveWords.Contains(word))];

		string runway = string.Empty;
		int runwayIndex = words.IndexOf("RWY");

		if (runwayIndex >= 0)
		{
			runway = string.Concat(words.Skip(runwayIndex + 1));
			words.RemoveRange(runwayIndex, words.Count - runwayIndex);
		}

		int? version = null;

		if (words.Count > 0 && VersionOf(words[^1]) is { } number)
		{
			version = number;
			words.RemoveAt(words.Count - 1);
		}

		return (words, runway, version);
	}

	/// <summary>A version word's number: <c>THREE</c> is 3, and so is a plain <c>3</c>; anything else is <see langword="null"/>.</summary>
	private static int? VersionOf(string word)
	{
		if (VersionWords.TryGetValue(word, out int number))
		{
			return number;
		}

		return word.All(char.IsAsciiDigit) && int.TryParse(word, NumberStyles.None, CultureInfo.InvariantCulture, out number)
			? number
			: null;
	}

	/// <summary>
	/// The code from the computer code - the first half for a departure, the second for a STAR, less
	/// its version - or <see langword="null"/> when there is no usable computer code.
	/// </summary>
	private static string? FromComputerCode(bool isStar, string? computerCode, int? version)
	{
		string[] halves = (computerCode ?? string.Empty).Trim().Split('.');

		if (halves.Length != 2)
		{
			return null;
		}

		string half = ChartRecallText.LettersAndDigits(halves[isStar ? 1 : 0]);
		string versionDigits = version?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

		if (versionDigits.Length > 0 && half.Length > versionDigits.Length && half.EndsWith(versionDigits, StringComparison.Ordinal))
		{
			half = half[..^versionDigits.Length];
		}
		else if (half.Length > 1 && char.IsAsciiDigit(half[^1]))
		{
			half = half[..^1];
		}

		return half.Length > 0 ? half : null;
	}

	/// <summary>Whether <paramref name="run"/> appears in <paramref name="words"/> as consecutive words.</summary>
	private static bool ContainsRun(IReadOnlyList<string> words, IReadOnlyList<string> run)
	{
		for (int start = 0; start + run.Count <= words.Count; start++)
		{
			bool matches = true;

			for (int offset = 0; offset < run.Count && matches; offset++)
			{
				matches = words[start + offset].Equals(run[offset], StringComparison.Ordinal);
			}

			if (matches)
			{
				return true;
			}
		}

		return false;
	}
}
