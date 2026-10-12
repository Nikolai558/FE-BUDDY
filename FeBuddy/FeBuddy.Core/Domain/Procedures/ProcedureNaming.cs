using System.Globalization;
using System.Text.RegularExpressions;

namespace FeBuddy.Core.Domain.Procedures;

/// <summary>
/// Derives a procedure's base chart name - the identity <c>ProcedureBuilder</c> groups a chart's
/// pages by - from its raw <c>chart_name</c> text, and a departure or arrival's root name, which
/// <c>ProcedureSelection</c> matches the user's picks against.
/// </summary>
/// <remarks>
/// A multi-page procedure's continuation pages are named e.g. <c>"GRUUB ONE (RNAV), CONT.1"</c>,
/// <c>"GRUUB ONE (RNAV), CONT.2"</c>; stripping that suffix recovers the base name every page of
/// the same chart shares. Public (rather than <see langword="internal"/>, like the rest of this
/// sub-service): the Procedures GUI tab uses both members directly to group the d-TPP Metafile's
/// raw records into the same per-airport procedure lists <c>ProcedureBuilder</c> would produce,
/// without pulling in the full NASR join that building a <c>ProcedureAirport</c> requires.
/// </remarks>
public static partial class ProcedureNaming
{
	/// <summary>
	/// Strips a trailing <c>, CONT.n</c> continuation marker, trims, and collapses internal
	/// whitespace, so <c>"GRUUB ONE (RNAV), CONT.1"</c> and <c>"GRUUB ONE (RNAV)"</c> both become
	/// <c>"GRUUB ONE (RNAV)"</c>.
	/// </summary>
	/// <param name="chartName">The raw <c>chart_name</c> (or a user-typed procedure name).</param>
	/// <returns>The normalized base name.</returns>
	public static string BaseName(string chartName)
	{
		ArgumentNullException.ThrowIfNull(chartName);

		string withoutContinuation = ContinuationSuffix().Replace(chartName, string.Empty);
		return CollapseWhitespace().Replace(withoutContinuation.Trim(), " ");
	}

	/// <summary>
	/// The name a departure or arrival keeps through its amendments: <see cref="BaseName"/> without
	/// its trailing qualifiers (<c>(RNAV)</c>, <c>(OBSTACLE)</c>) and then the number the FAA changes
	/// at each amendment, so <c>"BRWNZ FOUR (RNAV)"</c>, <c>"BRWNZ FIVE (RNAV)"</c> and
	/// <c>"BRWNZ FIVE"</c> are all <c>"BRWNZ"</c>. A name with no number at the end keeps its words,
	/// less any qualifiers. The Procedure Charts cards that list departures and arrivals match by it,
	/// so a procedure stays listed when the FAA renumbers it.
	/// </summary>
	/// <param name="name">A chart name, or a name the user listed.</param>
	/// <returns>The root name, e.g. <c>BRWNZ</c>.</returns>
	public static string RootName(string name)
	{
		string baseName = BaseName(name);
		string withoutQualifiers = TrailingQualifiers().Replace(baseName, string.Empty);
		Match number = TrailingNumber().Match(withoutQualifiers);
		string root = number.Success ? withoutQualifiers[..number.Index] : withoutQualifiers;

		return root.Length > 0 ? root : baseName;
	}

	/// <summary>Whether a chart name carries a <c>, CONT.n</c> continuation marker.</summary>
	/// <param name="chartName">The raw <c>chart_name</c>.</param>
	/// <returns><see langword="true"/> for a continuation page.</returns>
	public static bool IsContinuation(string chartName)
	{
		ArgumentNullException.ThrowIfNull(chartName);
		return ContinuationSuffix().IsMatch(chartName);
	}

	/// <summary>
	/// Which page of its procedure a chart is: 1 for the first page, and n + 1 for the
	/// <c>, CONT.n</c> continuation page - <c>"GRUUB ONE (RNAV), CONT.1"</c> is page 2.
	/// </summary>
	/// <param name="chartName">The raw <c>chart_name</c>.</param>
	/// <returns>The page number, from 1.</returns>
	public static int PageNumber(string chartName)
	{
		ArgumentNullException.ThrowIfNull(chartName);

		Match match = ContinuationSuffix().Match(chartName);

		return match.Success && int.TryParse(match.Groups["page"].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out int continuation)
			? continuation + 1
			: 1;
	}

	[GeneratedRegex(@",\s*CONT\.(?<page>\d+)\s*$", RegexOptions.IgnoreCase)]
	private static partial Regex ContinuationSuffix();

	[GeneratedRegex(@"\s+")]
	private static partial Regex CollapseWhitespace();

	/// <summary>One or more <c>(…)</c> qualifiers at the end of a name: <c>(RNAV)</c>, <c>(OBSTACLE)</c>, <c>(VECTOR)</c>, <c>(COPTER)</c>.</summary>
	[GeneratedRegex(@"(\s*\([^()]*\))+\s*$")]
	private static partial Regex TrailingQualifiers();

	/// <summary>
	/// The number at the end of a departure or arrival's name, spelled out as the FAA does (it wraps
	/// back to ONE after NINE) or, rarely, in digits (<c>LGTNG 1</c>) - but never a runway's number,
	/// which ends an approach's name (<c>RNAV (GPS) RWY 10</c>).
	/// </summary>
	[GeneratedRegex(@"(?<!\bRWY)\s+(ONE|TWO|THREE|FOUR|FIVE|SIX|SEVEN|EIGHT|NINE|TEN|ELEVEN|TWELVE|\d+)$", RegexOptions.IgnoreCase)]
	private static partial Regex TrailingNumber();
}
