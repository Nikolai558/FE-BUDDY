using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;
using FeBuddy.Core.Domain.Arrivals;
using FeBuddy.Core.Domain.Departures;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.ChartRecall;
using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

namespace FeBuddy.UnitTests.Application.AliasGuide;

/// <summary>
/// Covers <see cref="AliasPracticeContent"/>: every chart question's commands are what
/// <see cref="ChartRecallCodes"/> gives its d-TPP chart, every Data Display procedure's is what the
/// alias writers' naming gives its NASR procedure, every question is well formed, every action and
/// subject is practised, and each part of a command and each wrong name is explained.
/// </summary>
public sealed class AliasPracticeContentTests
{
	private static IReadOnlyList<PracticeQuestion> Questions => AliasPracticeContent.Questions;

	private static string Flat(string markup) => CommandMarkup.Flatten(CommandMarkup.Parse(markup));

	private static PracticeQuestion Question(PracticeAction action, string name) =>
		Assert.Single(Questions, question => question.Action == action && question.Name == name);

	/// <summary>
	/// Each Data Display procedure as NASR lists it (its name, amendment and computer code), and the
	/// command the guide's practice expects for it.
	/// </summary>
	public static TheoryData<PracticeSubject, string, string, string, string, string> Procedures => new()
	{
		{ PracticeSubject.Departure, "DTW", "CLVIN", "THREE", "CLVIN3.CLVIN", ".dtwCLVINf" },
		{ PracticeSubject.Departure, "SLC", "SALT LAKE", "FOUR", "SLC4.TCH", ".slcSLCf" },
		{ PracticeSubject.Arrival, "SLC", "QWENN", "SEVEN", "JAMMN.QWENN7", ".slcQWENNf" },
		{ PracticeSubject.Arrival, "SLC", "BRIGHAM CITY", "FIVE", "LHO.LHO5", ".slcLHOf" },
		{ PracticeSubject.Departure, "ANC", "TURNAGAIN", "EIGHT", "NOT ASSIGNED", ".ancTURNAGAINf" },
	};

	[Fact]
	public void every_chart_questions_commands_are_what_fe_buddy_writes_for_its_chart()
	{
		PracticeQuestion[] charts = [.. Questions.Where(question => question.Action == PracticeAction.RecallChart)];

		Assert.NotEmpty(charts);
		Assert.All(charts, question =>
		{
			PracticeChart chart = Assert.IsType<PracticeChart>(question.Chart);
			ChartRecallCodeResult result = ChartRecallCodes.For(chart.ChartCode, chart.ChartName, chart.ComputerCode, chart.AirportName);
			int page = ProcedureNaming.PageNumber(chart.ChartName);
			string[] written = [.. result.Codes.Select(code => $".{chart.AirportId.ToLowerInvariant()}{code}c{(page > 1 ? page : string.Empty)}")];

			Assert.Equal(written, question.Answers.Select(Flat));
		});
	}

	[Theory]
	[MemberData(nameof(Procedures))]
	public void each_data_display_procedures_command_is_named_as_the_alias_writers_name_it(
		PracticeSubject subject, string airportId, string nasrName, string amendment, string computerCode, string expected)
	{
		string codeId = subject == PracticeSubject.Arrival
			? ArrivalNaming.CodeIdFor(computerCode, amendment, nasrName, out _)
			: DepartureNaming.CodeIdFor(computerCode, amendment, nasrName, out _);

		Assert.Equal(expected, $".{airportId.ToLowerInvariant()}{codeId}f");
		Assert.Contains(Questions, question =>
			question.Action == PracticeAction.DisplayFixes && question.Subject == subject && question.Answers.Select(Flat).SequenceEqual([expected]));
	}

	[Fact]
	public void every_data_display_procedure_is_proven_above()
	{
		HashSet<string> proven = [.. Procedures.Select(row => (string)row[5]!)];
		string[] shown = [.. Questions
			.Where(question => question.Action == PracticeAction.DisplayFixes && question.Subject != PracticeSubject.Airway)
			.Select(question => Flat(question.Answers[0]))];

		Assert.Equal(proven.Order(), shown.Order());
	}

	[Fact]
	public void every_question_is_well_formed()
	{
		Assert.All(Questions, question =>
		{
			Assert.False(string.IsNullOrWhiteSpace(question.Name));
			Assert.False(string.IsNullOrWhiteSpace(question.Detail));
			Assert.NotEmpty(question.Answers);
			Assert.Equal(question.Answers.Count, question.Answers.Select(Flat).Distinct(StringComparer.OrdinalIgnoreCase).Count());
			Assert.All(question.Notes, note => Assert.NotEmpty(GuideInline.Parse(note)));
			Assert.Equal(question.Action != PracticeAction.DisplayIsr && question.Subject != PracticeSubject.Airway, question.Chart is not null);
		});
	}

	[Fact]
	public void every_action_and_subject_is_practised()
	{
		Assert.Equal(Enum.GetValues<PracticeAction>(), Questions.Select(question => question.Action).Distinct().Order());
		Assert.Equal(Enum.GetValues<PracticeSubject>(), Questions.Select(question => question.Subject).Distinct().Order());
	}

	[Fact]
	public void a_procedures_detail_says_what_it_is_its_computer_code_and_its_page()
	{
		Assert.Equal(
			"Departure at SLC · SALT LAKE CITY INTL · computer code FFU9.FFU · page 2",
			Question(PracticeAction.RecallChart, "FAIRFIELD NINE").Detail);
		Assert.Equal(
			"Departure at ANC · TED STEVENS ANCHORAGE INTL · no computer code",
			Question(PracticeAction.DisplayFixes, "TURNAGAIN EIGHT").Detail);
		Assert.Equal("Arrival at SLC · SALT LAKE CITY INTL · computer code LHO.LHO5", Question(PracticeAction.DisplayFixes, "BRIGHAM CITY FIVE").Detail);
		Assert.Equal("SLC · SALT LAKE CITY INTL", Question(PracticeAction.RecallChart, "ILS OR LOC RWY 16R").Detail);
	}

	/// <summary>Each name is the type an approach FE-Buddy makes that code for.</summary>
	[Fact]
	public void each_approach_type_name_is_the_type_fe_buddy_gives_that_code()
	{
		Assert.All(AliasPracticeContent.ApproachTypeNames, pair =>
			Assert.Equal([pair.Key + "9"], ChartRecallCodes.For(ProcedureChartTypes.Iap, $"{pair.Value} RWY 9", null, "X").Codes));
	}

	[Theory]
	[InlineData(PracticeAction.DisplayIsr, "In-Scope Reference", "Display the ISR")]
	[InlineData(PracticeAction.DisplayFixes, "Data Display", "Display the fixes")]
	[InlineData(PracticeAction.RecallChart, "Chart Recall", "Recall the chart")]
	public void each_action_has_its_section_and_label(PracticeAction action, string section, string label)
	{
		Assert.Equal(section, AliasPracticeContent.SectionTitle(action));
		Assert.Equal(label, AliasPracticeContent.ActionLabel(action));
	}

	[Fact]
	public void every_part_of_every_command_is_explained()
	{
		Assert.All(Questions, question => Assert.All(question.Answers, answer =>
		{
			IReadOnlyList<CommandPart> parts = CommandMarkup.Parse(answer);
			bool hasRunway = parts.Any(part => part.Kind == CommandPartKind.Runway);

			Assert.All(parts, part => Assert.EndsWith(".", AliasPracticeContent.PartMeaning(question, part, hasRunway), StringComparison.Ordinal));
		}));
	}

	[Fact]
	public void a_part_is_explained_for_what_it_is_in_its_command()
	{
		PracticeQuestion approach = Question(PracticeAction.RecallChart, "ILS OR LOC RWY 16R");
		PracticeQuestion circling = Question(PracticeAction.RecallChart, "VOR-A");
		PracticeQuestion airport = Question(PracticeAction.DisplayIsr, "DETROIT METRO WAYNE COUNTY");
		PracticeQuestion departure = Question(PracticeAction.DisplayFixes, "SALT LAKE FOUR");
		PracticeQuestion noCode = Question(PracticeAction.DisplayFixes, "TURNAGAIN EIGHT");

		Assert.Equal("The approach type code: I is ILS.", Meaning(approach, new("I", CommandPartKind.ApproachType, false, false)));
		Assert.Equal("The variant letter, straight after the approach type code.", Meaning(approach, new("Y", CommandPartKind.Variant, false, false)));
		Assert.Equal("The circling letter, where the runway would be.", Meaning(circling, new("A", CommandPartKind.Variant, false, false), hasRunway: false));
		Assert.Equal("The airport's FAA ID, not its ICAO ID.", Meaning(approach, new("slc", CommandPartKind.Airport, false, false)));
		Assert.Equal("The airport's FAA or ICAO ID.", Meaning(airport, new("DTW", CommandPartKind.Airport, false, false)));
		Assert.Equal("Displays an airport's card.", Meaning(airport, new(".apt", CommandPartKind.Typed, false, false)));
		Assert.Equal("Displays the fixes.", Meaning(departure, new("f", CommandPartKind.Typed, false, false)));
		Assert.Equal(
			"The departure's name: the first part of its computer code (SLC4.TCH), without the version number.",
			Meaning(departure, new("SLC", CommandPartKind.Identifier, false, false)));
		Assert.Equal(
			"The chart's name, spelled out in full, without its version number.",
			Meaning(noCode, new("TURNAGAIN", CommandPartKind.Identifier, false, false)));
	}

	[Fact]
	public void a_wrong_name_is_answered_with_how_this_ones_is_worked_out()
	{
		Assert.Equal(
			"A departure's name is the first part of its computer code, without the version number: `SLC4.TCH` is `SLC`.",
			Hint(PracticeAction.DisplayFixes, "SALT LAKE FOUR"));
		Assert.Equal(
			"An arrival's name is the second part of its computer code, without the version number: `JAMMN.QWENN7` is `QWENN`.",
			Hint(PracticeAction.DisplayFixes, "QWENN SEVEN (RNAV)"));
		Assert.Equal(
			"A chart with no computer code is spelled out in full, without its version number: TURNAGAIN EIGHT is `TURNAGAIN`.",
			Hint(PracticeAction.RecallChart, "TURNAGAIN EIGHT"));
		Assert.Equal(
			"The approach's name is spelled out in full, without the words VISUAL and RWY, spaces or punctuation: `LARIVER`.",
			Hint(PracticeAction.RecallChart, "LA RIVER VISUAL RWY 12"));
		Assert.Equal("The code for this chart is `APD`.", Hint(PracticeAction.RecallChart, "AIRPORT DIAGRAM"));
		Assert.Equal("The airway's ID is `J60`.", Hint(PracticeAction.DisplayFixes, "Airway J60"));
		Assert.Equal("Use `.navCGT` or `.navCHICAGOHEIGHTS`.", Hint(PracticeAction.DisplayIsr, "CHICAGO HEIGHTS VORTAC"));
		Assert.Equal("It is `NASA`.", Hint(PracticeAction.DisplayIsr, "NASA"));
		Assert.Null(Hint(PracticeAction.RecallChart, "ILS OR LOC RWY 16R"));
	}

	[Fact]
	public void the_explanations_reject_a_null_question_or_part()
	{
		PracticeQuestion question = Questions[0];

		Assert.Throws<ArgumentNullException>(() => AliasPracticeContent.PartMeaning(null!, new(".", CommandPartKind.Typed, false, false), false));
		Assert.Throws<ArgumentNullException>(() => AliasPracticeContent.PartMeaning(question, null!, false));
		Assert.Throws<ArgumentNullException>(() => AliasPracticeContent.IdentifierHint(null!));
	}

	private static string Meaning(PracticeQuestion question, CommandPart part, bool hasRunway = true) =>
		AliasPracticeContent.PartMeaning(question, part, hasRunway);

	private static string? Hint(PracticeAction action, string name) => AliasPracticeContent.IdentifierHint(Question(action, name));
}
