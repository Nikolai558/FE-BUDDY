using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.ChartRecall;
using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="WhatsNewViewModel"/>: every chart recall command the page shows (the examples,
/// the new form of each changed command, each approach type's code) is what <see cref="ChartRecallCodes"/>
/// really gives, and Back and View the guide run what the page was given. The export button opens a
/// dialog, so it is not run here.
/// </summary>
public sealed class WhatsNewViewModelTests
{
	private static WhatsNewViewModel Page() => new(() => { }, () => { });

	/// <summary>The commands a chart gets: a period, the airport in lower case, each code, then <c>c</c>.</summary>
	private static string[] Commands(string airport, ChartRecallCodeResult result) =>
		[.. result.Codes.Select(code => "." + airport.ToLowerInvariant() + code + "c")];

	private static ChartRecallCodeResult Approach(string chartName) =>
		ChartRecallCodes.For(ProcedureChartTypes.Iap, chartName, computerCode: null, airportName: "X");

	private static WhatsNewViewModel.CommandChange Change(string procedurePrefix) =>
		Assert.Single(
			Page().RecallRules.SelectMany(rule => rule.Changes),
			change => change.Procedure.StartsWith(procedurePrefix, StringComparison.Ordinal));

	/// <summary>The text between each pair of backticks.</summary>
	private static string[] Backticked(string text) => [.. text.Split('`').Where((_, index) => index % 2 == 1)];

	/// <summary>Each example's airport, chart and the commands it shows, for <see cref="MemberDataAttribute"/>.</summary>
	public static TheoryData<string, string, string> Examples
	{
		get
		{
			TheoryData<string, string, string> rows = [];

			foreach (WhatsNewViewModel.CommandExample example in Page().Examples)
			{
				rows.Add(example.Airport, example.Procedure, example.Commands);
			}

			return rows;
		}
	}

	/// <summary>Each approach type the page lists, with the code it shows.</summary>
	public static TheoryData<string, string> ApproachTypes
	{
		get
		{
			TheoryData<string, string> rows = [];
			WhatsNewViewModel page = Page();

			foreach (WhatsNewViewModel.TypeCode type in page.BaseTypes.Concat(page.DmeTypes))
			{
				rows.Add(type.Type, type.Code);
			}

			return rows;
		}
	}

	// ---- the examples ----

	[Theory]
	[MemberData(nameof(Examples))]
	public void each_example_shows_the_commands_fe_buddy_writes(string airport, string procedure, string commands)
	{
		// The page marks a charted visual approach's name with *charted*; the chart's own name has no such mark.
		string chartName = procedure.Replace("*charted* ", string.Empty, StringComparison.Ordinal);

		Assert.Equal(Commands(airport, Approach(chartName)), Backticked(commands));
	}

	// ---- the changed commands ----

	[Fact]
	public void the_new_visual_approach_command_is_what_fe_buddy_writes()
	{
		string command = Assert.Single(Commands("aaa", Approach("SOUTH RIVER VISUAL RWY 19")));

		Assert.Equal(".aaavSOUTHRIVER19c", command);
		Assert.Equal(command, Change("SOUTH RIVER VISUAL RWY 19").New);
	}

	[Fact]
	public void the_new_runway_code_is_not_shortened()
	{
		string code = Assert.Single(Approach("ILS RWY 16R").Codes);

		Assert.Equal("I16R", code);
		Assert.Equal(code, Change("ILS RWY 16R").New);
	}

	[Fact]
	public void the_new_departure_command_with_no_computer_code_is_its_full_name()
	{
		ChartRecallCodeResult result = ChartRecallCodes.For(ProcedureChartTypes.Dp, "TURNAGAIN EIGHT", computerCode: null, airportName: "TED STEVENS ANCHORAGE INTL");

		string command = Assert.Single(Commands("anc", result));

		Assert.Equal(".ancTURNAGAINc", command);
		Assert.Equal(command, Change("TURNAGAIN EIGHT").New);
	}

	[Fact]
	public void every_changed_command_differs_from_its_old_form()
	{
		Assert.All(Page().RecallRules.SelectMany(rule => rule.Changes), change => Assert.NotEqual(change.Old, change.New));
	}

	// ---- the approach types ----

	[Fact]
	public void there_are_eight_base_types_and_four_dme_types()
	{
		WhatsNewViewModel page = Page();

		Assert.Equal(["RNAV", "ILS", "LOC", "VOR", "NDB", "LDA", "GPS", "TACAN"], page.BaseTypes.Select(type => type.Type));
		Assert.Equal(["LOC/DME", "VOR/DME", "NDB/DME", "LDA/DME"], page.DmeTypes.Select(type => type.Type));
	}

	[Theory]
	[MemberData(nameof(ApproachTypes))]
	public void each_approach_types_code_is_the_one_fe_buddy_writes(string type, string code)
	{
		// RNAV is only ever written with its bracket.
		string chartName = type == "RNAV" ? "RNAV (GPS) RWY 9" : $"{type} RWY 9";

		Assert.Equal(code + "9", Assert.Single(Approach(chartName).Codes));
	}

	// ---- Back, View the guide and Export ----

	[Fact]
	public void back_runs_the_action_the_page_was_given()
	{
		int calls = 0;
		WhatsNewViewModel page = new(() => calls++, () => { });

		Assert.True(page.BackCommand.CanExecute(null));
		page.BackCommand.Execute(null);

		Assert.Equal(1, calls);
	}

	[Fact]
	public void view_the_guide_runs_the_action_the_page_was_given()
	{
		int calls = 0;
		WhatsNewViewModel page = new(() => { }, () => calls++);

		Assert.True(page.OpenGuideCommand.CanExecute(null));
		page.OpenGuideCommand.Execute(null);

		Assert.Equal(1, calls);
	}

	[Fact]
	public void the_page_has_an_export_command_that_can_run()
	{
		Assert.True(Page().ExportGuideCommand.CanExecute(null));
	}
}
