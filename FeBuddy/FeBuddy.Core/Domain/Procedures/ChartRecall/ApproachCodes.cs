using System.Text.RegularExpressions;

using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

namespace FeBuddy.Core.Domain.Procedures.ChartRecall;

/// <summary>
/// Works out the FAA Chart Recall codes for an instrument approach chart (chart type <c>IAP</c>),
/// charted visual approaches included.
/// </summary>
/// <remarks>
/// <para>
/// A code is the approach type's code, then <c>BC</c> for a back course, then the variant letter,
/// then the runway exactly as the FAA prints it:
/// </para>
/// <list type="table">
///   <item><term><c>ILS OR LOC RWY 22L</c></term><description><c>I22L</c>, <c>L22L</c> - one code per "OR" part</description></item>
///   <item><term><c>ILS Y OR LOC Y RWY 22L</c></term><description><c>IY22L</c>, <c>LY22L</c></description></item>
///   <item><term><c>ILS Z OR LOC RWY 23</c></term><description><c>IZ23</c>, <c>LZ23</c> - a variant on one part applies to every part</description></item>
///   <item><term><c>LOC/DME BC RWY 18</c></term><description><c>LDBC18</c></description></item>
///   <item><term><c>VOR RWY 26L/R</c></term><description><c>O26L</c>, <c>O26R</c> - one code per runway</description></item>
///   <item><term><c>VOR OR GPS-A</c></term><description><c>OA</c>, <c>GA</c> - a circling approach keeps its letter</description></item>
///   <item><term><c>RIVER VISUAL RWY 19</c></term><description><c>vRIVER19</c> - a charted visual is a lower-case <c>v</c>, its name and its runway</description></item>
/// </list>
/// <para>
/// The type codes: <c>RNAV (anything)</c> R, <c>ILS</c> I, <c>LOC</c> L, <c>LOC/DME</c> LD,
/// <c>VOR</c> O, <c>VOR/DME</c> OD, <c>NDB</c> N, <c>NDB/DME</c> ND, <c>LDA</c> D, <c>LDA/DME</c> DD,
/// <c>GPS</c> G (a GPS approach, not the <c>(GPS)</c> of an RNAV approach), <c>TACAN</c> T.
/// </para>
/// <para>
/// A variant on one "OR" part applies to all of them because the FAA prints it once for the whole
/// chart; when it prints it on one part only, that is an FAA slip, and the part without it is still
/// the same chart.
/// </para>
/// <para>
/// Some charts get no command at all (<see cref="ChartRecallSkipReason"/>): a name starting with
/// <c>HI-</c> or <c>COPTER</c> (its "OR" parts included), holding the word <c>PRM</c>, a bracket
/// holding <c>CAT</c>, <c>CONVERGING</c>, ending in <c>AAUP</c>, or a numbered approach such as
/// <c>VOR-1</c>. A <c>GLS</c> or <c>LOC/NDB</c> part gets no command either, but the chart's other
/// parts still do. Anything else FE-Buddy cannot read - a new approach type, an odd runway - is
/// returned in <see cref="ChartRecallCodeResult.Unrecognized"/> for the caller to warn about.
/// </para>
/// </remarks>
internal static partial class ApproachCodes
{
	/// <summary>The code of each approach type FE-Buddy makes a command for, apart from RNAV (see <see cref="RnavCode"/>).</summary>
	private static readonly Dictionary<string, string> TypeCodes = new(StringComparer.OrdinalIgnoreCase)
	{
		["ILS"] = "I",
		["LOC"] = "L",
		["LOC/DME"] = "LD",
		["VOR"] = "O",
		["VOR/DME"] = "OD",
		["NDB"] = "N",
		["NDB/DME"] = "ND",
		["LDA"] = "D",
		["LDA/DME"] = "DD",
		["GPS"] = "G",
		["TACAN"] = "T",
	};

	/// <summary>The approach types FE-Buddy knows but makes no command for, as one part of an "OR" chart.</summary>
	private static readonly Dictionary<string, ChartRecallSkipReason> NoCommandTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		["GLS"] = ChartRecallSkipReason.Gls,
		["LOC/NDB"] = ChartRecallSkipReason.LocNdb,
	};

	/// <summary>Every RNAV approach, whatever its bracket says (<c>(GPS)</c>, <c>(RNP)</c>).</summary>
	private const string RnavCode = "R";

	/// <summary>The code of a back-course approach, added straight after the type's code.</summary>
	private const string BackCourseCode = "BC";

	/// <summary>The prefix of a charted visual approach's code; lower case, like the airport and the trailing <c>c</c>.</summary>
	internal const string VisualPrefix = "v";

	/// <summary>
	/// Works out the codes for one approach chart.
	/// </summary>
	/// <param name="name">The chart's base name - no <c>, CONT.n</c> - in upper case, e.g. <c>ILS OR LOC RWY 22L</c>.</param>
	/// <returns>The codes, or why the chart gets none.</returns>
	internal static ChartRecallCodeResult For(string name)
	{
		if (SkipReasonFor(name) is { } skipReason)
		{
			return new ChartRecallCodeResult { SkipReason = skipReason };
		}

		Match visual = VisualName().Match(name);

		return visual.Success
			? ForVisual(name, visual.Groups["name"].Value, visual.Groups["runway"].Value)
			: ForInstrumentApproach(name);
	}

	/// <summary>The rule that skips the whole chart, or <see langword="null"/> when none does. The rules are checked in this order.</summary>
	private static ChartRecallSkipReason? SkipReasonFor(string name)
	{
		if (name.StartsWith("HI-", StringComparison.Ordinal))
		{
			return ChartRecallSkipReason.HighAltitude;
		}

		if (name.StartsWith("COPTER", StringComparison.Ordinal))
		{
			return ChartRecallSkipReason.Copter;
		}

		if (PrmWord().IsMatch(name))
		{
			return ChartRecallSkipReason.Prm;
		}

		if (CategoryBracket().IsMatch(name))
		{
			return ChartRecallSkipReason.CategoryApproach;
		}

		if (name.Contains("CONVERGING", StringComparison.Ordinal))
		{
			return ChartRecallSkipReason.Converging;
		}

		if (AaupWord().IsMatch(name))
		{
			return ChartRecallSkipReason.Aaup;
		}

		// Only the type part is checked, so a runway can never look like a number.
		string typePart = RunwaySuffix().Replace(name, string.Empty);

		return NumberedType().IsMatch(typePart) ? ChartRecallSkipReason.NumberedApproach : null;
	}

	/// <summary>A charted visual approach: <c>v</c>, its name with only letters and digits, then each runway.</summary>
	private static ChartRecallCodeResult ForVisual(string fullName, string visualName, string runwayText)
	{
		string cleanName = ChartRecallText.LettersAndDigits(visualName);

		if (cleanName.Length == 0)
		{
			return Unrecognized($"the charted visual approach name in '{fullName}'");
		}

		if (ExpandRunways(runwayText) is not { } runways)
		{
			return Unrecognized($"the runway in '{fullName}'");
		}

		return new ChartRecallCodeResult { Codes = [.. runways.Select(runway => VisualPrefix + cleanName + runway)] };
	}

	/// <summary>An instrument approach: one code per recognized "OR" part and runway.</summary>
	private static ChartRecallCodeResult ForInstrumentApproach(string name)
	{
		Match runwayMatch = RunwaySuffix().Match(name);
		string typeText = runwayMatch.Success ? name[..runwayMatch.Index] : name;
		string runwayText = runwayMatch.Success ? runwayMatch.Groups["runway"].Value : string.Empty;

		if (ExpandRunways(runwayText) is not { } runways)
		{
			return Unrecognized($"the runway in '{name}'");
		}

		List<(string Code, string Letter)> parts = [];
		List<ChartRecallSkipReason> skippedParts = [];
		List<string> unrecognized = [];

		foreach (string part in typeText.Split(" OR ", StringSplitOptions.TrimEntries))
		{
			Match partMatch = ApproachPart().Match(part);

			if (!partMatch.Success)
			{
				unrecognized.Add($"the approach type '{part}'");
				continue;
			}

			string type = partMatch.Groups["type"].Value;
			string letter = partMatch.Groups["letter"].Value;

			if (NoCommandTypes.TryGetValue(type, out ChartRecallSkipReason reason))
			{
				skippedParts.Add(reason);
				continue;
			}

			string? typeCode = type.StartsWith("RNAV", StringComparison.Ordinal)
				? RnavCode
				: TypeCodes.GetValueOrDefault(type);

			if (typeCode is null)
			{
				unrecognized.Add($"the approach type '{type}'");
				continue;
			}

			if (partMatch.Groups["backCourse"].Success)
			{
				typeCode += BackCourseCode;
			}

			parts.Add((typeCode, letter));
		}

		// The variant (or circling letter) the FAA printed on any part, for the parts it left it off.
		string sharedLetter = parts.Select(part => part.Letter).FirstOrDefault(letter => letter.Length > 0) ?? string.Empty;
		List<string> codes = [];

		foreach ((string code, string letter) in parts)
		{
			string partLetter = letter.Length > 0 ? letter : sharedLetter;

			if (runwayText.Length == 0 && partLetter.Length == 0)
			{
				// With neither a runway nor a circling letter there is nothing to tell this
				// approach from the airport's others.
				unrecognized.Add($"an approach with no runway or circling letter, '{name}'");
				break;
			}

			codes.AddRange(runways.Select(runway => code + partLetter + runway));
		}

		// Every part is one FE-Buddy knowingly skips (GLS RWY 19L): the whole chart is skipped.
		if (codes.Count == 0 && unrecognized.Count == 0 && skippedParts.Count > 0)
		{
			return new ChartRecallCodeResult { SkipReason = skippedParts[0] };
		}

		return new ChartRecallCodeResult { Codes = codes, SkippedParts = skippedParts, Unrecognized = unrecognized };
	}

	/// <summary>
	/// Expands a chart's runway text into one runway per code, exactly as printed: <c>22L</c> to
	/// <c>[22L]</c>, <c>30L/R</c> to <c>[30L, 30R]</c>, <c>16 R/C/L</c> to <c>[16R, 16C, 16L]</c>,
	/// <c>12/30</c> to <c>[12, 30]</c>, and no runway at all (a circling approach) to <c>[""]</c>.
	/// </summary>
	/// <returns>The runways, or <see langword="null"/> when the text is not in a form FE-Buddy can read.</returns>
	internal static IReadOnlyList<string>? ExpandRunways(string runwayText)
	{
		string compact = runwayText.Replace(" ", string.Empty, StringComparison.Ordinal);

		if (compact.Length == 0)
		{
			return [string.Empty];
		}

		List<string> runways = [];
		string number = string.Empty;

		foreach (string item in compact.Split('/'))
		{
			Match match = RunwayItem().Match(item);

			if (item.Length == 0 || !match.Success)
			{
				return null;
			}

			// "30L/R": the R has no number of its own, so it takes the one before it.
			if (match.Groups["number"].Success)
			{
				number = match.Groups["number"].Value;
			}

			if (number.Length == 0)
			{
				return null;
			}

			runways.Add(number + match.Groups["side"].Value);
		}

		return runways;
	}

	private static ChartRecallCodeResult Unrecognized(string what) => new() { Unrecognized = [what] };

	/// <summary><c>RIVER VISUAL RWY 19</c>, <c>BAY VISUAL RWY 16 R/C/L</c>, or a visual with no runway.</summary>
	[GeneratedRegex(@"^(?<name>.+?)\s+VISUAL(?:\s+RWY\s+(?<runway>.+))?$")]
	private static partial Regex VisualName();

	/// <summary>The runway part at the end of a name: <c> RWY 22L</c>.</summary>
	[GeneratedRegex(@"\s+RWY\s+(?<runway>.+)$")]
	private static partial Regex RunwaySuffix();

	/// <summary>One runway of a runway list: <c>22L</c>, <c>30</c>, or just <c>R</c> after a number.</summary>
	[GeneratedRegex(@"^(?<number>\d{1,2})?(?<side>[LCR])?$")]
	private static partial Regex RunwayItem();

	/// <summary>
	/// One "OR" part: its type (<c>RNAV</c> with any bracket, or letters with an optional
	/// <c>/DME</c>-style second half), an optional <c> BC</c>, then an optional variant (<c> Z</c>) or
	/// circling letter (<c>-A</c>).
	/// </summary>
	[GeneratedRegex(@"^(?<type>RNAV(?:\s+\([^)]*\))?|[A-Z]+(?:/[A-Z]+)?)(?<backCourse>\s+BC)?(?:[\s-](?<letter>[A-Z]))?$")]
	private static partial Regex ApproachPart();

	[GeneratedRegex(@"\bPRM\b")]
	private static partial Regex PrmWord();

	[GeneratedRegex(@"\([^)]*\bCAT\b[^)]*\)")]
	private static partial Regex CategoryBracket();

	[GeneratedRegex(@"\bAAUP\b")]
	private static partial Regex AaupWord();

	/// <summary>A type followed by a hyphen and a number, e.g. <c>VOR-1</c>.</summary>
	[GeneratedRegex(@"-\d")]
	private static partial Regex NumberedType();
}
