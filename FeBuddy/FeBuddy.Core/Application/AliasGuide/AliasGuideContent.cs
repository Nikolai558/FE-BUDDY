using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// What the alias command guide says: a controller's explanation of every alias command FE-Buddy
/// writes, grouped the way a facility's own command reference usually is - In-Scope Reference,
/// Data Display and Chart Recall.
/// </summary>
/// <remarks>
/// <para>
/// The rules here are the alias writers' own (<c>AirportAliasWriter</c>, <c>NavaidAliasWriter</c>,
/// <c>TelephonyAliasWriter</c>, <c>AirwayAliasWriter</c>, <c>DepartureAliasWriter</c>,
/// <c>ArrivalAliasWriter</c> and <c>ChartRecallCodes</c>), written for the controller who types the
/// commands rather than the engineer who makes them; docs/Users/User-Guide.md has the engineer's
/// version. Change both together.
/// </para>
/// <para>
/// Every example is a real command from AIRAC 2609's files. The tests check each chart recall
/// example against <c>ChartRecallCodes</c>, so a change to the codes that the guide does not follow
/// fails a test.
/// </para>
/// <para>
/// Three things lay it out: <c>AliasGuideHtmlWriter</c> and <c>AliasGuideMarkdownWriter</c> for the
/// export, and the app's Info ▸ Alias Command Guide page. The legend text below is shared by the web
/// page and the app, which both show a part to replace as a coloured pill.
/// </para>
/// </remarks>
public static class AliasGuideContent
{
	/// <summary>The guide's title.</summary>
	public const string Title = "FE-Buddy Alias Command Guide";

	/// <summary>The heading of the part that explains how a command is shown.</summary>
	public const string NotationTitle = "How to read this guide";

	/// <summary>What plain text in a command means, where parts to replace are coloured pills.</summary>
	public const string TypedLegend = "Plain text: type it exactly as shown.";

	/// <summary>What a coloured pill means; the colour key follows it.</summary>
	public const string PlaceholderLegend = "Replace it with the real value. Its colour shows what goes there:";

	/// <summary>What a pill with a dashed edge means.</summary>
	public const string OptionalLegend = "Optional.";

	/// <summary>The part kinds a controller replaces, in the order the colour key lists them.</summary>
	public static IReadOnlyList<CommandPartKind> KeyKinds { get; } =
	[
		CommandPartKind.Airport,
		CommandPartKind.Identifier,
		CommandPartKind.ApproachType,
		CommandPartKind.Variant,
		CommandPartKind.Runway,
		CommandPartKind.Page,
	];

	/// <summary>Builds the guide.</summary>
	/// <param name="facility">
	/// The user's facility, e.g. <c>ZOB</c>, or <see langword="null"/>. Only its letters and digits
	/// are used, so nothing in it can read as markup.
	/// </param>
	/// <returns>The guide's content.</returns>
	public static AliasGuideDocument Build(string? facility)
	{
		string facilityId = string.Concat((facility ?? string.Empty).Where(char.IsAsciiLetterOrDigit)).ToUpperInvariant();
		string aliasFile = facilityId.Length == 0
			? "your facility's alias file"
			: $"the {facilityId} alias file";

		return new AliasGuideDocument(
			Title,
			$"These alias commands are made by FE-Buddy every AIRAC cycle and merged into {aliasFile}.",
			About(),
			[
				"Commands are not case-sensitive: `.apt{a:dtw}` works the same as `.apt{a:DTW}`. The capitals in this guide only make each part easier to see.",
				"An airport ID is its FAA ID (`{a:DTW}`), not its ICAO ID (`{a:KDTW}`), unless the command says otherwise.",
				"Finish every command with Enter.",
			],
			[InScopeReference(), DataDisplay(), ChartRecall()]);
	}

	/// <summary>The colour key's name for each kind of part a controller replaces.</summary>
	/// <param name="kind">The kind.</param>
	/// <returns>e.g. <c>Airport ID</c>.</returns>
	public static string KindLabel(CommandPartKind kind) => kind switch
	{
		CommandPartKind.Airport => "Airport ID",
		CommandPartKind.Identifier => "ID or name",
		CommandPartKind.ApproachType => "Approach type",
		CommandPartKind.Variant => "Variant or circling letter",
		CommandPartKind.Runway => "Runway",
		CommandPartKind.Page => "Page number",
		_ => "Typed as shown",
	};

	private static GuideSection About() => new(
		"about",
		"About alias commands",
		null,
		[
			new GuideParagraph("An alias command is a shortcut you type into CRC. Every one starts with a period."),
			new GuideParagraph(
				"CRC also has its own built-in commands, which this guide does not cover; the CRC documentation explains those. "
				+ "Your facility may have custom commands of its own as well. When one has the same name as an FE-Buddy command, "
				+ "your facility's command is the one you get."),
			new GuideParagraph(
				"Which of the commands below you have depends on the FE-Buddy files your facility uploads, so some may not be there."),
		]);

	private static GuideSection InScopeReference() => new(
		"isr",
		"In-Scope Reference (ISR)",
		"Information cards for airports, NAVAIDs and aircraft operators. They are not limited to your facility's area.",
		[
			new GuideCommandTable(
			[
				new GuideCommand(
					".apt[a:FAA or ICAO airport ID]",
					"Shows the airport's card: its FAA and ICAO IDs, name, tower type, ARTCC, longest runway, elevation, "
					+ "traffic pattern altitude, FSS, CTAF, weather frequency, attended hours, and its class of airspace "
					+ "with the hours it is in effect.",
					[],
					[".apt{a:DTW}", ".apt{a:KDTW}"]),
				new GuideCommand(
					".nav[i:NAVAID ID or name]",
					"Shows the NAVAID's card: its ID, name, type and frequency, and the ARTCCs it is in for high and low "
					+ "altitude airspace.",
					[
						"When entering the name, leave out spaces and special characters.",
						"When several NAVAIDs share the ID or name, the card lists each of them.",
					],
					[".nav{i:CGT}", ".nav{i:CHICAGOHEIGHTS}"]),
				new GuideCommand(
					".id[i:operator 3LD or telephony]",
					"Shows the aircraft operator's card: its three-letter designator (3LD), telephony, company and country. "
					+ "A U.S. special call sign shows its agency and expiration date instead, and a virtual airline your "
					+ "facility added is marked `--VA--` and shows its virtual organization.",
					[
						"When entering the telephony, leave out spaces and special characters.",
						"When several operators match, the card lists each of them.",
					],
					[".id{i:DAL}", ".id{i:DELTA}", ".id{i:NASA}"]),
			]),
		]);

	private static GuideSection DataDisplay() => new(
		"data-display",
		"Data Display",
		"Draws an airway's or a procedure's fixes on your scope. Your facility may include only the airways and "
		+ "procedures in its own area.",
		[
			new GuideCommandTable(
			[
				new GuideCommand(
					".[i:airway ID]F",
					"Shows every fix on the airway, NAVAIDs and airports included.",
					["CRC STARS & ERAM."],
					[".{i:J60}F"]),
				new GuideCommand(
					".[a:airport ID][i:departure]f",
					"Shows every fix on the departure procedure (a SID or an obstacle departure), NAVAIDs included, with "
					+ "all of its transitions.",
					["CRC STARS & ERAM."],
					[".{a:dtw}{i:CLVIN}f"]),
				new GuideCommand(
					".[a:airport ID][i:arrival]f",
					"Shows every fix on the arrival procedure (STAR), NAVAIDs included, with all of its transitions.",
					["CRC STARS & ERAM."],
					[".{a:dtw}{i:GRAYT}f"]),
			]),
			new GuideParagraph(
				"**Procedure names.** A departure goes by the first part of its FAA computer code and an arrival by the "
				+ "second, without the version number: `DOTSS2.DOTSS` is `DOTSS`, and `AALAN.BLAID2` is `BLAID`. A "
				+ "procedure with no computer code goes by its name, letters and digits only."),
		]);

	private static GuideSection ChartRecall() => new(
		"chart-recall",
		"Chart Recall",
		"Opens an FAA chart in your web browser, straight from the FAA's d-TPP. Every airport the FAA publishes "
		+ "charts for is included. A command is a period, the airport's FAA ID, the chart's code, then `c`.",
		[
			new GuideCommandTable(
			[
				new GuideCommand(
					".[a:airport ID][t:approach type][v?:variant][r:runway]c",
					"An instrument approach. The approach type codes are below.",
					[],
					[".{a:dtw}{t:I}{r:22L}c", ".{a:dtw}{t:L}{v:Z}{r:04L}c", ".{a:lax}{t:R}{v:Y}{r:24L}c"]),
				new GuideCommand(
					".[a:airport ID]v[i:visual name][r:runway]c",
					"A charted visual approach: a lower-case `v`, then the approach's name with spaces and punctuation "
					+ "left out.",
					[],
					[".{a:sfo}v{i:QUIETBRIDGE}{r:28R}c", ".{a:mry}v{i:RACEWAY}{r:28L}c"]),
				new GuideCommand(
					".[a:airport ID][i:procedure]c",
					"A departure, an obstacle departure or an arrival (STAR).",
					[],
					[".{a:dtw}{i:CLVIN}c", ".{a:dtw}{i:GRAYT}c", ".{a:anc}{i:TURNAGAIN}c"]),
				new GuideCommand(
					".[a:airport ID][i:chart]c",
					"Another of the airport's charts, such as its airport diagram. The chart codes are below.",
					[],
					[".{a:dtw}{i:APD}c", ".{a:lax}{i:HS}c"]),
				new GuideCommand(
					".[a:airport ID][i:chart code]c[p?:page]",
					"Page 2 or later of a chart with more than one page: the page number goes after the `c`.",
					[],
					[".{a:dtw}{i:CLVIN}c{p:2}"]),
			]),

			new GuideHeading("Approach type codes"),
			new GuideParagraph(
				"FE-Buddy uses the eight approach types in common use across the FAA. A `/DME` approach adds `D` to its "
				+ "type's code, and a back course adds `BC`."),
			new GuideTable(
				["Approach", "Code", "Example"],
				[
					["RNAV (GPS), RNAV (RNP)", "`{t:R}`", "`.{a:lax}{t:R}{v:Z}{r:07R}c`"],
					["ILS", "`{t:I}`", "`.{a:dtw}{t:I}{r:22L}c`"],
					["LOC", "`{t:L}`", "`.{a:dtw}{t:L}{r:22L}c`"],
					["VOR", "`{t:O}`", "`.{a:cdb}{t:O}{r:15}c`"],
					["NDB", "`{t:N}`", "`.{a:ili}{t:N}{r:36}c`"],
					["LDA", "`{t:D}`", "`.{a:dca}{t:D}{r:19}c`"],
					["GPS", "`{t:G}`", "`.{a:fot}{t:G}{r:11}c`"],
					["TACAN", "`{t:T}`", "`.{a:pdx}{t:T}{r:28L}c`"],
					["LOC/DME", "`{t:LD}`", "`.{a:ful}{t:LD}{r:24}c`"],
					["VOR/DME", "`{t:OD}`", "`.{a:tal}{t:OD}{r:07}c`"],
					["NDB/DME", "`{t:ND}`", "`.{a:adk}{t:ND}{r:23}c`"],
					["LDA/DME", "`{t:DD}`", "`.{a:eko}{t:DD}{r:24}c`"],
					["LOC BC", "`{t:LBC}`", "`.{a:cdb}{t:LBC}{r:33}c`"],
					["LOC/DME BC", "`{t:LDBC}`", "`.{a:gri}{t:LDBC}{r:17}c`"],
				]),

			new GuideHeading("Reading an approach's command"),
			new GuideList(
			[
				"**One command per approach.** A chart for more than one approach has a command for each: ILS OR LOC "
				+ "RWY 22L at DTW is `.{a:dtw}{t:I}{r:22L}c` and `.{a:dtw}{t:L}{r:22L}c`.",
				"**Variant letters** (X, Y, Z...) come after the type code. One the FAA prints on only one approach of "
				+ "such a chart applies to all of them: ILS Z OR LOC RWY 04L is `.{a:dtw}{t:I}{v:Z}{r:04L}c` and "
				+ "`.{a:dtw}{t:L}{v:Z}{r:04L}c`.",
				"**Runways** are written exactly as the chart's name prints them: RNAV (RNP) Z RWY 07R at LAX is "
				+ "`.{a:lax}{t:R}{v:Z}{r:07R}c`. A chart for two runways has a command for each: TIPP TOE VISUAL RWY "
				+ "28L/R at SFO is `.{a:sfo}v{i:TIPPTOE}{r:28L}c` and `.{a:sfo}v{i:TIPPTOE}{r:28R}c`.",
				"**Circling approaches** keep their letter where the runway would be: VOR-A at PDX is "
				+ "`.{a:pdx}{t:O}{v:A}c`.",
				"**RNAV** is `{t:R}` whatever its brackets say, (GPS) or (RNP). Only a GPS approach with no RNAV in "
				+ "its name is `{t:G}`.",
			]),

			new GuideHeading("Charted visual approaches"),
			new GuideParagraph(
				"The approach's name in full, without the words VISUAL and RWY, spaces or punctuation, then its runway."),
			new GuideTable(
				["Airport", "Chart", "Command"],
				[
					["SFO", "QUIET BRIDGE VISUAL RWY 28R", "`.{a:sfo}v{i:QUIETBRIDGE}{r:28R}c`"],
					["MRY", "RACEWAY VISUAL RWY 28L", "`.{a:mry}v{i:RACEWAY}{r:28L}c`"],
					["LGB", "LA RIVER VISUAL RWY 12", "`.{a:lgb}v{i:LARIVER}{r:12}c`"],
				]),

			new GuideHeading("Departures, obstacle departures and arrivals"),
			new GuideParagraph(
				"The chart's FAA computer code without its version number: the first part for a departure, the "
				+ "second for an arrival. The CLVIN THREE (RNAV) departure at DTW is `.{a:dtw}{i:CLVIN}c`, and the "
				+ "GRAYT TWO (RNAV) arrival is `.{a:dtw}{i:GRAYT}c`."),
			new GuideParagraph(
				"A chart with no computer code is spelled out in full instead, without its version number, its "
				+ "bracketed words, the words RNAV, OBSTACLE and COPTER, spaces or punctuation: TURNAGAIN EIGHT at ANC "
				+ "is `.{a:anc}{i:TURNAGAIN}c`."),

			new GuideHeading("Other charts"),
			new GuideTable(
				["Chart", "Code", "Example"],
				[
					["Airport diagram", "`{i:APD}`", "`.{a:dtw}{i:APD}c`"],
					["Takeoff minimums", "`{i:TM}`", "`.{a:dtw}{i:TM}c`"],
					["Diverse vector area", "`{i:DVA}`", "`.{a:lax}{i:DVA}c`"],
					["Radar minimums", "`{i:RM}`", "`.{a:hsv}{i:RM}c`"],
					["Hot spots", "`{i:HS}`", "`.{a:lax}{i:HS}c`"],
					["LAHSO", "`{i:LAHSO}`", "`.{a:bur}{i:LAHSO}c`"],
				]),
			new GuideParagraph(
				"Takeoff minimums, diverse vector areas and radar minimums open straight to the airport's own page of "
				+ "the FAA's shared document."),

			new GuideHeading("Charts with no command"),
			new GuideList(
			[
				"High-altitude (HI-) and COPTER charts",
				"PRM approaches",
				"Category II and III approaches, and other special-authorization approaches",
				"CONVERGING approaches",
				"GLS approaches (the other approaches on the same chart still have a command)",
				"Numbered approaches, such as VOR-1",
				"Attention All Users pages (AAUP)",
				"Alternate minimums",
			]),
		]);
}
