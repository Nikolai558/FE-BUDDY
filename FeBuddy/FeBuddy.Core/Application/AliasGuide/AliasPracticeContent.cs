using FeBuddy.Core.Application.AliasGuide.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.ChartRecall;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// What the Alias Command Practice asks: real charts, procedures, airports, NAVAIDs, operators and
/// airways, each with an action - display the ISR, display the fixes, recall the chart - and every
/// command that does it, with how each part of it is built and what is worth knowing about it.
/// </summary>
/// <remarks>
/// <para>
/// Every chart and procedure is real, from AIRAC 2610's d-TPP Metafile and NASR. The tests work
/// each chart question's answers out again with <see cref="ChartRecallCodes"/>, and each Data
/// Display procedure's with the alias writers' own naming, so a change to the rules that the
/// practice does not follow fails a test.
/// </para>
/// <para>
/// The questions are chosen for what trips controllers up: a chart that is two approaches, a
/// variant, a leading zero, a circling letter, a back course, a /DME approach, a name that is not
/// the chart's title, an arrival's second code, a chart with no computer code, a second page.
/// <c>AliasPracticeWriter</c> lays them out as a web page whose script reads what a controller
/// typed and says what is missing or out of place.
/// </para>
/// </remarks>
public static class AliasPracticeContent
{
	/// <summary>The practice's title.</summary>
	public const string Title = "FE-Buddy Alias Command Practice";

	/// <summary>What each approach type code stands for, as <see cref="ApproachCodes"/> makes them.</summary>
	public static IReadOnlyDictionary<string, string> ApproachTypeNames { get; } = new Dictionary<string, string>
	{
		["R"] = "RNAV",
		["I"] = "ILS",
		["L"] = "LOC",
		["O"] = "VOR",
		["N"] = "NDB",
		["D"] = "LDA",
		["G"] = "GPS",
		["T"] = "TACAN",
		["LD"] = "LOC/DME",
		["OD"] = "VOR/DME",
		["ND"] = "NDB/DME",
		["DD"] = "LDA/DME",
		["LBC"] = "LOC BC",
		["LDBC"] = "LOC/DME BC",
	};

	private const string Slc = "SALT LAKE CITY INTL";
	private const string Dtw = "DETROIT METRO WAYNE COUNTY";

	/// <summary>Every question, grouped by action; the page shuffles them.</summary>
	public static IReadOnlyList<PracticeQuestion> Questions { get; } =
	[
		// ---- In-Scope Reference ----
		Card(PracticeSubject.Airport, Dtw, "FAA ID DTW · ICAO ID KDTW", [".apt{a:DTW}", ".apt{a:KDTW}"],
			"Either ID works: the FAA ID `{a:DTW}` or the ICAO ID `{a:KDTW}`.",
			"Commands are not case-sensitive, so `.apt{a:dtw}` works too."),
		Card(PracticeSubject.Airport, Slc, "FAA ID SLC · ICAO ID KSLC", [".apt{a:SLC}", ".apt{a:KSLC}"],
			"`.apt` is the one command that takes an airport's ICAO ID as well as its FAA ID."),
		Card(PracticeSubject.Navaid, "CHICAGO HEIGHTS VORTAC", "ID CGT", [".nav{i:CGT}", ".nav{i:CHICAGOHEIGHTS}"],
			"Its name works too, with the space left out: `.nav{i:CHICAGOHEIGHTS}`.",
			"When several NAVAIDs share an ID or a name, the card lists each of them."),
		Card(PracticeSubject.Navaid, "BONNEVILLE VORTAC", "ID BVL", [".nav{i:BVL}", ".nav{i:BONNEVILLE}"],
			"Its name works too: `.nav{i:BONNEVILLE}`."),
		Card(PracticeSubject.Operator, "DELTA AIR LINES", "3LD DAL · telephony DELTA", [".id{i:DAL}", ".id{i:DELTA}"],
			"The telephony works too: `.id{i:DELTA}`.",
			"Leave out spaces and special characters when you type a telephony."),
		Card(PracticeSubject.Operator, "SOUTHWEST AIRLINES", "3LD SWA · telephony SOUTHWEST", [".id{i:SWA}", ".id{i:SOUTHWEST}"],
			"The telephony works too: `.id{i:SOUTHWEST}`."),
		Card(PracticeSubject.Operator, "NASA", "U.S. special call sign", [".id{i:NASA}"],
			"A U.S. special call sign's card shows its agency and expiration date."),

		// ---- Data Display ----
		Airway("J60", "Jet route",
			"Data Display commands work in CRC STARS and ERAM."),
		Airway("Q35", "RNAV route",
			"RNAV routes (Q and T) work the same way as Victor and Jet airways."),
		Procedure(PracticeAction.DisplayFixes, PracticeSubject.Departure, "DTW", Dtw, "CLVIN THREE (RNAV)", "CLVIN3.CLVIN", ".{a:dtw}{i:CLVIN}f",
			"A departure's name is the first part of its computer code, without the version number: `CLVIN3` is `CLVIN`.",
			"It shows every fix on the departure, NAVAIDs included, with all of its transitions."),
		Procedure(PracticeAction.DisplayFixes, PracticeSubject.Departure, "SLC", Slc, "SALT LAKE FOUR", "SLC4.TCH", ".{a:slc}{i:SLC}f",
			"The name comes from the computer code, not the chart's title: SALT LAKE FOUR (`SLC4.TCH`) is `SLC`."),
		Procedure(PracticeAction.DisplayFixes, PracticeSubject.Arrival, "SLC", Slc, "QWENN SEVEN (RNAV)", "JAMMN.QWENN7", ".{a:slc}{i:QWENN}f",
			"An arrival takes the second part of its computer code: `JAMMN` is a transition, and `QWENN7` without its version number is `QWENN`."),
		Procedure(PracticeAction.DisplayFixes, PracticeSubject.Arrival, "SLC", Slc, "BRIGHAM CITY FIVE", "LHO.LHO5", ".{a:slc}{i:LHO}f",
			"The name comes from the computer code, not the chart's title: BRIGHAM CITY FIVE (`LHO.LHO5`) is `LHO`."),
		Procedure(PracticeAction.DisplayFixes, PracticeSubject.Departure, "ANC", "TED STEVENS ANCHORAGE INTL", "TURNAGAIN EIGHT", null, ".{a:anc}{i:TURNAGAIN}f",
			"With no computer code, the name is spelled out in full, without its version number: TURNAGAIN EIGHT is `TURNAGAIN`."),

		// ---- Chart Recall: instrument approaches ----
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "SLC", Slc, "ILS OR LOC RWY 16R", [".{a:slc}{t:I}{r:16R}c", ".{a:slc}{t:L}{r:16R}c"],
			"This chart is for two approaches, so it has a command for each: `.{a:slc}{t:I}{r:16R}c` and `.{a:slc}{t:L}{r:16R}c` both open it.",
			"The airport's ILS RWY 16R (SA CAT I) and (CAT II - III) charts have no command."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "SLC", Slc, "RNAV (GPS) Y RWY 16R", [".{a:slc}{t:R}{v:Y}{r:16R}c"],
			"RNAV is `{t:R}` whatever its brackets say, (GPS) or (RNP).",
			"The variant `{v:Y}` comes straight after the type code."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "SLC", Slc, "RNAV (RNP) Z RWY 34L", [".{a:slc}{t:R}{v:Z}{r:34L}c"],
			"(RNP) is still RNAV, `{t:R}`. The airport's RNAV (GPS) Y RWY 34L is `.{a:slc}{t:R}{v:Y}{r:34L}c`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "SLC", Slc, "RNAV (GPS) RWY 35", [".{a:slc}{t:R}{r:35}c"],
			"No variant letter on this one, so the runway comes straight after the type code."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "SLC", Slc, "LDA RWY 35", [".{a:slc}{t:D}{r:35}c"],
			"LDA is `{t:D}`. `{t:L}` is a LOC."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "DTW", Dtw, "ILS Z OR LOC RWY 04L", [".{a:dtw}{t:I}{v:Z}{r:04L}c", ".{a:dtw}{t:L}{v:Z}{r:04L}c"],
			"The FAA prints the `{v:Z}` on the ILS only, but it applies to both approaches on the chart.",
			"Keep the runway's leading zero: `{r:04L}`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "LAX", "LOS ANGELES INTL", "RNAV (RNP) Z RWY 07R", [".{a:lax}{t:R}{v:Z}{r:07R}c"],
			"Runways are written exactly as the chart prints them, leading zero included: `{r:07R}`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "PDX", "PORTLAND INTL", "VOR-A", [".{a:pdx}{t:O}{v:A}c"],
			"A circling approach has no runway: its letter `{v:A}` goes where the runway would be.",
			"VOR is `{t:O}`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "POC", "BRACKETT FLD", "VOR OR GPS-A", [".{a:poc}{t:O}{v:A}c", ".{a:poc}{t:G}{v:A}c"],
			"Two approaches on one chart: `.{a:poc}{t:O}{v:A}c` and `.{a:poc}{t:G}{v:A}c` both open it.",
			"A GPS approach with no RNAV in its name is `{t:G}`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "LAS", "HARRY REID INTL", "VOR RWY 26L/R", [".{a:las}{t:O}{r:26L}c", ".{a:las}{t:O}{r:26R}c"],
			"A chart for two runways has a command for each: `{r:26L}` and `{r:26R}` both open it."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "CDB", "COLD BAY", "LOC BC RWY 33", [".{a:cdb}{t:LBC}{r:33}c"],
			"A back course adds `BC` to its type's code: LOC BC is `{t:LBC}`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "GRI", "CENTRAL NEBRASKA RGNL", "LOC/DME BC RWY 17", [".{a:gri}{t:LDBC}{r:17}c"],
			"A /DME approach adds `D` to its type's code, and a back course adds `BC`: LOC/DME BC is `{t:LDBC}`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "TAL", "RALPH M CALHOUN MEML", "VOR/DME RWY 07", [".{a:tal}{t:OD}{r:07}c"],
			"VOR/DME is `{t:OD}`: VOR's `O`, then the `D` a /DME approach adds.",
			"Keep the runway's leading zero: `{r:07}`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "FUL", "FULLERTON MUNI", "LOC/DME RWY 24", [".{a:ful}{t:LD}{r:24}c"],
			"LOC/DME is `{t:LD}`: LOC's `L`, then the `D` a /DME approach adds."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "ILI", "ILIAMNA", "NDB RWY 36", [".{a:ili}{t:N}{r:36}c"],
			"NDB is `{t:N}`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "FOT", "ROHNERVILLE", "GPS RWY 11", [".{a:fot}{t:G}{r:11}c"],
			"A GPS approach with no RNAV in its name is `{t:G}`. An RNAV (GPS) approach is `{t:R}`."),
		Chart(PracticeSubject.Approach, ProcedureChartTypes.Iap, "PDX", "PORTLAND INTL", "TACAN RWY 28L", [".{a:pdx}{t:T}{r:28L}c"],
			"TACAN is `{t:T}`."),

		// ---- Chart Recall: charted visual approaches ----
		Chart(PracticeSubject.Visual, ProcedureChartTypes.Iap, "SFO", "SAN FRANCISCO INTL", "QUIET BRIDGE VISUAL RWY 28R", [".{a:sfo}v{i:QUIETBRIDGE}{r:28R}c"],
			"A charted visual approach is a lower-case `v`, then its name in full without VISUAL, RWY or spaces, then its runway."),
		Chart(PracticeSubject.Visual, ProcedureChartTypes.Iap, "LGB", "LONG BEACH (DAUGHERTY FLD)", "LA RIVER VISUAL RWY 12", [".{a:lgb}v{i:LARIVER}{r:12}c"],
			"Spaces are left out of the name: LA RIVER is `{i:LARIVER}`."),
		Chart(PracticeSubject.Visual, ProcedureChartTypes.Iap, "SFO", "SAN FRANCISCO INTL", "TIPP TOE VISUAL RWY 28L/R", [".{a:sfo}v{i:TIPPTOE}{r:28L}c", ".{a:sfo}v{i:TIPPTOE}{r:28R}c"],
			"A chart for two runways has a command for each: `{r:28L}` and `{r:28R}` both open it."),

		// ---- Chart Recall: departures and arrivals ----
		Procedure(PracticeAction.RecallChart, PracticeSubject.Departure, "DTW", Dtw, "CLVIN THREE (RNAV)", "CLVIN3.CLVIN", ".{a:dtw}{i:CLVIN}c",
			"A departure's name is the first part of its computer code, without the version number: `CLVIN3` is `CLVIN`.",
			"The same name with `f` instead of `c` displays its fixes: `.{a:dtw}{i:CLVIN}f`."),
		Procedure(PracticeAction.RecallChart, PracticeSubject.Departure, "SLC", Slc, "SALT LAKE FOUR", "SLC4.TCH", ".{a:slc}{i:SLC}c",
			"The name comes from the computer code, not the chart's title: SALT LAKE FOUR (`SLC4.TCH`) is `SLC`."),
		Procedure(PracticeAction.RecallChart, PracticeSubject.Arrival, "SLC", Slc, "JAZZZ ONE (RNAV)", "SPANE.JAZZZ1", ".{a:slc}{i:JAZZZ}c",
			"An arrival takes the second part of its computer code, without the version number: `SPANE.JAZZZ1` is `JAZZZ`."),
		Procedure(PracticeAction.RecallChart, PracticeSubject.Departure, "ANC", "TED STEVENS ANCHORAGE INTL", "TURNAGAIN EIGHT", null, ".{a:anc}{i:TURNAGAIN}c",
			"With no computer code, the chart is spelled out in full, without its version number: TURNAGAIN EIGHT is `TURNAGAIN`."),
		Procedure(PracticeAction.RecallChart, PracticeSubject.Departure, "SLC", Slc, "FAIRFIELD NINE, CONT.1", "FFU9.FFU", ".{a:slc}{i:FFU}c{p:2}",
			"Page 2 or later of a chart: its page number goes after the `c`.",
			"FAIRFIELD NINE (`FFU9.FFU`) is `FFU`, and its first page is `.{a:slc}{i:FFU}c`."),

		// ---- Chart Recall: other charts ----
		Chart(PracticeSubject.OtherChart, ProcedureChartTypes.Apd, "DTW", Dtw, "AIRPORT DIAGRAM", [".{a:dtw}{i:APD}c"],
			"The other chart codes: `{i:TM}` takeoff minimums, `{i:DVA}` diverse vector area, `{i:RM}` radar minimums, `{i:HS}` hot spots and `{i:LAHSO}`."),
		Chart(PracticeSubject.OtherChart, ProcedureChartTypes.Hot, "LAX", "LOS ANGELES INTL", "HOT SPOT", [".{a:lax}{i:HS}c"],
			"Hot spots are `{i:HS}`."),
		Chart(PracticeSubject.OtherChart, ProcedureChartTypes.Min, "DTW", Dtw, "TAKEOFF MINIMUMS", [".{a:dtw}{i:TM}c"],
			"Takeoff minimums, diverse vector areas and radar minimums open straight to the airport's own page of the FAA's shared document."),
		Chart(PracticeSubject.OtherChart, ProcedureChartTypes.Min, "HSV", "HUNTSVILLE INTL-CARL T JONES FLD", "RADAR MINIMUMS", [".{a:hsv}{i:RM}c"],
			"Radar minimums are `{i:RM}`, and open straight to the airport's own page of the FAA's shared document."),
		Chart(PracticeSubject.OtherChart, ProcedureChartTypes.Lah, "BUR", "HOLLYWOOD BURBANK", "LAHSO", [".{a:bur}{i:LAHSO}c"],
			"LAHSO is its own code: `{i:LAHSO}`."),
	];

	/// <summary>The section a question's action belongs to, as the guide names it.</summary>
	/// <param name="action">The action.</param>
	/// <returns>e.g. <c>Chart Recall</c>.</returns>
	public static string SectionTitle(PracticeAction action) => action switch
	{
		PracticeAction.DisplayIsr => "In-Scope Reference",
		PracticeAction.DisplayFixes => "Data Display",
		_ => "Chart Recall",
	};

	/// <summary>What a question asks the controller to do.</summary>
	/// <param name="action">The action.</param>
	/// <returns>e.g. <c>Recall the chart</c>.</returns>
	public static string ActionLabel(PracticeAction action) => action switch
	{
		PracticeAction.DisplayIsr => "Display the ISR",
		PracticeAction.DisplayFixes => "Display the fixes",
		_ => "Recall the chart",
	};

	/// <summary>What one part of a question's command is, for the page's "How it's built" list.</summary>
	/// <param name="question">The question.</param>
	/// <param name="part">One part of one of its answers.</param>
	/// <param name="hasRunway">Whether that answer has a runway, which makes a letter a variant rather than a circling letter.</param>
	/// <returns>One plain sentence.</returns>
	public static string PartMeaning(PracticeQuestion question, CommandPart part, bool hasRunway)
	{
		ArgumentNullException.ThrowIfNull(question);
		ArgumentNullException.ThrowIfNull(part);

		return part.Kind switch
		{
			CommandPartKind.Typed => part.Text.ToLowerInvariant() switch
			{
				"." => "Every alias command starts with a period.",
				".apt" => "Displays an airport's card.",
				".nav" => "Displays a NAVAID's card.",
				".id" => "Displays an aircraft operator's card.",
				"v" => "A lower-case v, for Visual.",
				"c" => "Recalls the chart.",
				_ => "Displays the fixes.",
			},
			CommandPartKind.Airport => question.Subject == PracticeSubject.Airport
				? "The airport's FAA or ICAO ID."
				: "The airport's FAA ID, not its ICAO ID.",
			CommandPartKind.ApproachType => $"The approach type code: {part.Text} is {ApproachTypeNames[part.Text]}.",
			CommandPartKind.Variant => hasRunway
				? "The variant letter, straight after the approach type code."
				: "The circling letter, where the runway would be.",
			CommandPartKind.Runway => "The runway, exactly as the chart prints it.",
			CommandPartKind.Page => "The page number, after the c.",
			_ => IdentifierMeaning(question),
		};
	}

	/// <summary>
	/// What to tell a controller whose ID or name part is wrong: how that part is worked out, with
	/// this question's own values.
	/// </summary>
	/// <param name="question">The question.</param>
	/// <returns>Inline text, or <see langword="null"/> when the question's commands have no ID or name part.</returns>
	public static string? IdentifierHint(PracticeQuestion question)
	{
		ArgumentNullException.ThrowIfNull(question);

		IReadOnlyList<CommandPart> parts = CommandMarkup.Parse(question.Answers[0]);

		if (parts.FirstOrDefault(part => part.Kind == CommandPartKind.Identifier) is not { } identifier)
		{
			return null;
		}

		string name = $"`{identifier.Text}`";
		string? code = question.Chart?.ComputerCode;

		return question.Subject switch
		{
			PracticeSubject.Departure when code is null =>
				$"A chart with no computer code is spelled out in full, without its version number: {question.Name} is {name}.",
			PracticeSubject.Departure =>
				$"A departure's name is the first part of its computer code, without the version number: `{code}` is {name}.",
			PracticeSubject.Arrival =>
				$"An arrival's name is the second part of its computer code, without the version number: `{code}` is {name}.",
			PracticeSubject.Visual =>
				$"The approach's name is spelled out in full, without the words VISUAL and RWY, spaces or punctuation: {name}.",
			PracticeSubject.OtherChart =>
				$"The code for this chart is {name}.",
			PracticeSubject.Airway =>
				$"The airway's ID is {name}.",
			_ => question.Answers.Count > 1
				? $"Use {string.Join(" or ", question.Answers.Select(answer => $"`{CommandMarkup.Flatten(CommandMarkup.Parse(answer))}`"))}."
				: $"It is {name}.",
		};
	}

	private static string IdentifierMeaning(PracticeQuestion question) => question.Subject switch
	{
		PracticeSubject.Departure when question.Chart?.ComputerCode is null =>
			"The chart's name, spelled out in full, without its version number.",
		PracticeSubject.Departure =>
			$"The departure's name: the first part of its computer code ({question.Chart!.ComputerCode}), without the version number.",
		PracticeSubject.Arrival =>
			$"The arrival's name: the second part of its computer code ({question.Chart!.ComputerCode}), without the version number.",
		PracticeSubject.Visual => "The approach's name in full, without VISUAL, RWY, spaces or punctuation.",
		PracticeSubject.OtherChart => "The chart's code.",
		PracticeSubject.Airway => "The airway's ID.",
		PracticeSubject.Navaid => "The NAVAID's ID, or its name without spaces.",
		_ => "The operator's three-letter designator (3LD), or its telephony without spaces.",
	};

	/// <summary>An In-Scope Reference question.</summary>
	private static PracticeQuestion Card(PracticeSubject subject, string name, string detail, string[] answers, params string[] notes) =>
		new(PracticeAction.DisplayIsr, subject, name, detail, answers, notes, null);

	/// <summary>An airway's fixes.</summary>
	private static PracticeQuestion Airway(string id, string kind, params string[] notes) =>
		new(PracticeAction.DisplayFixes, PracticeSubject.Airway, $"Airway {id}", kind, [$".{{i:{id}}}F"], notes, null);

	/// <summary>An approach, a visual approach or another of an airport's charts.</summary>
	private static PracticeQuestion Chart(
		PracticeSubject subject, string chartCode, string airportId, string airportName, string chartName, string[] answers, params string[] notes) =>
		new(PracticeAction.RecallChart, subject, chartName, $"{airportId} · {airportName}", answers, notes,
			new PracticeChart(chartCode, airportId, airportName, chartName, null));

	/// <summary>A departure or an arrival, to recall or to display the fixes of.</summary>
	private static PracticeQuestion Procedure(
		PracticeAction action, PracticeSubject subject, string airportId, string airportName, string chartName, string? computerCode, string answer,
		params string[] notes)
	{
		string kind = subject == PracticeSubject.Arrival ? "Arrival" : "Departure";
		string code = computerCode is null ? "no computer code" : $"computer code {computerCode}";
		int page = ProcedureNaming.PageNumber(chartName);
		string detail = $"{kind} at {airportId} · {airportName} · {code}" + (page > 1 ? $" · page {page}" : string.Empty);

		return new(action, subject, ProcedureNaming.BaseName(chartName), detail, [answer], notes,
			new PracticeChart(subject == PracticeSubject.Arrival ? ProcedureChartTypes.Star : ProcedureChartTypes.Dp, airportId, airportName, chartName, computerCode));
	}
}
