using FeBuddy.Core.Application.AliasGuide.Models;
using FeBuddy.Core.Infrastructure.GitHub;

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
/// It is written to be scanned, not read through: a short point in bold, the bullets under it, and
/// each example command on a line of its own after a lead-in such as "is both:". A list in a
/// command's description (<see cref="GuideCommand.Details"/>) does the same for what a card shows.
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

	/// <summary>
	/// What plain text in a command means, where parts to replace are coloured pills. Each format
	/// follows it with <c>".apt"</c>, in quotes.
	/// </summary>
	public const string TypedLegend = "Plain text, typed exactly as shown. For example:";

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
	/// <returns>The guide's content.</returns>
	public static AliasGuideDocument Build() => new(
		Title,
		$"These alias commands are made by [FE-Buddy]({GitHubRepository.WebUrl}) every AIRAC cycle.",
		[
			Item("**Commands are not case-sensitive**",
				Item("These two work the same:",
					Item("`.apt{a:dtw}`"),
					Item("`.apt{a:DTW}`"))),
			Item("**Airport IDs**",
				Item("Use the FAA ID, not the ICAO ID, unless the command says otherwise. For example, `{a:DTW}`, not `{a:KDTW}`.")),
		],
		[InScopeReference(), DataDisplay(), ChartRecall()]);

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

	/// <summary>A bullet, with the bullets under it.</summary>
	private static GuideListItem Item(string text, params GuideListItem[] items) => new(text, items);

	private static GuideSection InScopeReference() => new(
		"isr",
		"In-Scope Reference (ISR)",
		"Information cards for airports, NAVAIDs and airlines (may include Virtual Airlines, if your FE has set it up).",
		[
			new GuideCommandTable(
			[
				new GuideCommand(
					[".apt", "[a:FAA or ICAO airport ID]"],
					"Shows the airport's card:",
					[
						"FAA and ICAO IDs, name, tower type and ARTCC",
						"Longest runway, elevation and traffic pattern altitude",
						"FSS, CTAF and weather frequency",
						"Attended hours (for towered airspace only)",
						"Class of airspace, with the hours it is in effect",
					],
					[],
					[".apt{a:DTW}", ".apt{a:KDTW}"]),
				new GuideCommand(
					[".nav", "[i:NAVAID ID or name]"],
					"Shows the NAVAID's card:",
					[
						"ID, name, type and frequency",
						"The ARTCCs it is in, for high and low altitude airspace",
					],
					[
						"When entering the name, leave out spaces and special characters.",
						"When several NAVAIDs share the ID or name, the card lists each of them.",
					],
					[".nav{i:CGT}", ".nav{i:CHICAGOHEIGHTS}"]),
				new GuideCommand(
					[".id", "[i:operator 3LD or telephony]"],
					"Shows the aircraft operator's card:",
					[
						"Three-letter designator (3LD), telephony, company and country",
						"A U.S. special call sign: its agency and expiration date instead",
						"A virtual airline your facility added: marked `--VA--`, with its virtual organization",
					],
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
					[".[i:airway ID]", "f"],
					"Shows every fix on the airway, NAVAIDs and airports included.",
					[],
					["CRC STARS & ERAM."],
					[".{i:J60}F"]),
				new GuideCommand(
					[".[a:airport ID]", "[i:departure]", "f"],
					"Shows every fix on the departure procedure (a SID or an obstacle departure), NAVAIDs included, with "
					+ "all of its transitions.",
					[],
					["CRC STARS & ERAM."],
					[".{a:dtw}{i:CLVIN}f"]),
				new GuideCommand(
					[".[a:airport ID]", "[i:arrival]", "f"],
					"Shows every fix on the arrival procedure (STAR), NAVAIDs included, with all of its transitions.",
					[],
					["CRC STARS & ERAM."],
					[".{a:dtw}{i:GRAYT}f"]),
			]),
			new GuideParagraph("**Procedure Names**"),
			ProcedureNames('f'),
		]);

	/// <summary>
	/// How a departure or an arrival is named in a command, shared by Data Display and Chart Recall;
	/// its last example is that section's own command, ending in <paramref name="suffix"/>.
	/// </summary>
	private static GuideList ProcedureNames(char suffix) => new(
	[
		Item("A departure name is the first part of its FAA computer code without the version number. For example: "
			+ "`ROG4.RZC` is `ROG`."),
		Item("An arrival name is the second part of its FAA computer code, without the version number. For example: "
			+ "`AALAN.BLAID2` is `BLAID`."),
		Item("A chart with no computer code is spelled out in full instead, without its version number, bracketed "
			+ "words, or the words RNAV, OBSTACLE and COPTER; spaces and punctuation are also removed. For example, "
			+ $"`TURNAGAIN EIGHT` at ANC is `.{{a:anc}}{{i:TURNAGAIN}}{suffix}`."),
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
					[".[a:airport ID]", "[t:approach type]", "[v?:variant]", "[r:runway]", "c"],
					"An instrument approach. The approach type codes are below.",
					[],
					[],
					[".{a:dtw}{t:I}{r:22L}c", ".{a:dtw}{t:L}{v:Z}{r:04L}c", ".{a:lax}{t:R}{v:Y}{r:24L}c"]),
				new GuideCommand(
					[".[a:airport ID]", "v", "[i:visual name]", "[r:runway]", "c"],
					"A charted visual approach: a lower-case `v` (for \"Visual\"), then the approach's name with spaces "
					+ "and punctuation left out.",
					[],
					[],
					[".{a:sfo}v{i:QUIETBRIDGE}{r:28R}c", ".{a:mry}v{i:RACEWAY}{r:28L}c"]),
				new GuideCommand(
					[".[a:airport ID]", "[i:procedure]", "c"],
					"A departure, an obstacle departure or an arrival (STAR).",
					[],
					[],
					[".{a:dtw}{i:CLVIN}c", ".{a:dtw}{i:GRAYT}c", ".{a:anc}{i:TURNAGAIN}c"]),
				new GuideCommand(
					[".[a:airport ID]", "[i:chart]", "c"],
					"Another of the airport's charts, such as its airport diagram. The chart codes are below.",
					[],
					[],
					[".{a:dtw}{i:APD}c", ".{a:lax}{i:HS}c"]),
				new GuideCommand(
					[".[a:airport ID]", "[i:chart code]", "c", "[p?:page]"],
					"Page 2 or later of a chart with more than one page: the page number goes after the `c`.",
					[],
					[],
					[".{a:dtw}{i:CLVIN}c{p:2}"]),
			]),

			new GuideHeading("Approach type codes"),
			new GuideParagraph(
				"FE-Buddy uses the eight approach types in common use across the FAA.\nA `/DME` approach adds `D` to its "
				+ "type's code, while a back course approach adds `BC`."),
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
				Item("**One command per approach**",
					Item("A chart for more than one approach has a command for each:",
						Item("ILS OR LOC RWY 22L at DTW is both:",
							Item("`.{a:dtw}{t:I}{r:22L}c`"),
							Item("`.{a:dtw}{t:L}{r:22L}c`")))),
				Item("**Variant letters** (X, Y, Z...)",
					Item("Come after the type code."),
					Item("If the FAA indicates the variant on only one of the approaches on the same chart, the command "
						+ "applies the variant to both:",
						Item("ILS Z OR LOC RWY 04L is both:",
							Item("`.{a:dtw}{t:I}{v:Z}{r:04L}c`"),
							Item("`.{a:dtw}{t:L}{v:Z}{r:04L}c`")))),
				Item("**Runways** are written exactly as the chart's name prints them",
					Item("RNAV (RNP) Z RWY 07R at LAX is:",
						Item("`.{a:lax}{t:R}{v:Z}{r:07R}c`")),
					Item("A chart for two runways has a command for each",
						Item("TIPP TOE VISUAL RWY 28L/R at SFO is both:",
							Item("`.{a:sfo}v{i:TIPPTOE}{r:28L}c`"),
							Item("`.{a:sfo}v{i:TIPPTOE}{r:28R}c`")))),
				Item("**Circling approaches**",
					Item("Keep their letter where the runway would be, for example VOR-A at PDX is:",
						Item("`.{a:pdx}{t:O}{v:A}c`"))),
				Item("**RNAV**",
					Item("`{t:R}` = RNAV, regardless of what the brackets say, (GPS) or (RNP)"),
					Item("Only a GPS approach with no \"RNAV\" in its name is `{t:G}`.")),
			]),

			new GuideHeading("Charted visual approaches"),
			new GuideList(
			[
				Item("The approach's name is spelled out in full, without the words VISUAL and RWY; spaces and punctuation "
					+ "are removed."),
				Item("Its runway comes right after the name."),
			]),
			new GuideTable(
				["Airport", "Chart", "Command"],
				[
					["SFO", "QUIET BRIDGE VISUAL RWY 28R", "`.{a:sfo}v{i:QUIETBRIDGE}{r:28R}c`"],
					["MRY", "RACEWAY VISUAL RWY 28L", "`.{a:mry}v{i:RACEWAY}{r:28L}c`"],
					["LGB", "LA RIVER VISUAL RWY 12", "`.{a:lgb}v{i:LARIVER}{r:12}c`"],
				]),

			new GuideHeading("Departures, obstacle departures and arrivals"),
			ProcedureNames('c'),

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
				Item("High-altitude (HI-) and COPTER charts"),
				Item("PRM approaches"),
				Item("Category II and III approaches, and other special-authorization approaches"),
				Item("CONVERGING approaches"),
				Item("GLS approaches (the other approaches on the same chart still have a command)"),
				Item("Numbered approaches, such as VOR-1"),
				Item("Attention All Users pages (AAUP)"),
				Item("Alternate minimums"),
			]),
		]);
}
