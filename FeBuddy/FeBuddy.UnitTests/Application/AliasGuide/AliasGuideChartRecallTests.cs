using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.ChartRecall;
using FeBuddy.Core.Domain.Procedures.ChartRecall.Models;

namespace FeBuddy.UnitTests.Application.AliasGuide;

/// <summary>
/// Proves the guide's chart recall examples are what FE-Buddy really writes: each real chart is
/// run through <see cref="ChartRecallCodes"/>, and every command it gets must be the one the guide
/// shows. The other way round too - every command the Chart Recall section shows is one of these
/// charts' - so an example cannot be added to the guide without a chart to prove it.
/// </summary>
public sealed class AliasGuideChartRecallTests
{
	/// <summary>For a chart type that never reads the airport's name.</summary>
	private const string AnyAirport = "TEST AIRPORT";

	/// <summary>
	/// A multi-page chart's second page: the alias builder numbers the pages (see
	/// <c>ChartRecallAliasBuilderTests</c>), not <see cref="ChartRecallCodes"/>.
	/// </summary>
	private const string PagedExample = ".dtwCLVINc2";

	/// <summary>
	/// Each chart: its type, name, computer code and airport's name as the d-TPP Metafile gives them,
	/// the airport's FAA ID, and the commands (comma-separated, in order) the guide shows for it.
	/// </summary>
	public static TheoryData<string, string, string?, string, string, string> Examples => new()
	{
		{ ProcedureChartTypes.Iap, "ILS OR LOC RWY 22L", null, AnyAirport, "dtw", ".dtwI22Lc,.dtwL22Lc" },
		{ ProcedureChartTypes.Iap, "ILS Z OR LOC RWY 04L", null, AnyAirport, "dtw", ".dtwIZ04Lc,.dtwLZ04Lc" },
		{ ProcedureChartTypes.Iap, "RNAV (GPS) Y RWY 24L", null, AnyAirport, "lax", ".laxRY24Lc" },
		{ ProcedureChartTypes.Iap, "RNAV (RNP) Z RWY 07R", null, AnyAirport, "lax", ".laxRZ07Rc" },
		{ ProcedureChartTypes.Iap, "QUIET BRIDGE VISUAL RWY 28R", null, AnyAirport, "sfo", ".sfovQUIETBRIDGE28Rc" },
		{ ProcedureChartTypes.Iap, "RACEWAY VISUAL RWY 28L", null, AnyAirport, "mry", ".mryvRACEWAY28Lc" },
		{ ProcedureChartTypes.Iap, "LA RIVER VISUAL RWY 12", null, AnyAirport, "lgb", ".lgbvLARIVER12c" },
		{ ProcedureChartTypes.Iap, "TIPP TOE VISUAL RWY 28L/R", null, AnyAirport, "sfo", ".sfovTIPPTOE28Lc,.sfovTIPPTOE28Rc" },
		{ ProcedureChartTypes.Iap, "VOR-A", null, AnyAirport, "pdx", ".pdxOAc" },
		{ ProcedureChartTypes.Iap, "VOR RWY 15", null, AnyAirport, "cdb", ".cdbO15c" },
		{ ProcedureChartTypes.Iap, "NDB RWY 36", null, AnyAirport, "ili", ".iliN36c" },
		{ ProcedureChartTypes.Iap, "LDA RWY 19", null, AnyAirport, "dca", ".dcaD19c" },
		{ ProcedureChartTypes.Iap, "GPS RWY 11", null, AnyAirport, "fot", ".fotG11c" },
		{ ProcedureChartTypes.Iap, "TACAN RWY 28L", null, AnyAirport, "pdx", ".pdxT28Lc" },
		{ ProcedureChartTypes.Iap, "LOC/DME RWY 24", null, AnyAirport, "ful", ".fulLD24c" },
		{ ProcedureChartTypes.Iap, "VOR/DME RWY 07", null, AnyAirport, "tal", ".talOD07c" },
		{ ProcedureChartTypes.Iap, "NDB/DME RWY 23", null, AnyAirport, "adk", ".adkND23c" },
		{ ProcedureChartTypes.Iap, "LDA/DME RWY 24", null, AnyAirport, "eko", ".ekoDD24c" },
		{ ProcedureChartTypes.Iap, "LOC BC RWY 33", null, AnyAirport, "cdb", ".cdbLBC33c" },
		{ ProcedureChartTypes.Iap, "LOC/DME BC RWY 17", null, AnyAirport, "gri", ".griLDBC17c" },
		{ ProcedureChartTypes.Dp, "CLVIN THREE (RNAV)", "CLVIN3.CLVIN", "DETROIT METRO WAYNE COUNTY", "dtw", ".dtwCLVINc" },
		{ ProcedureChartTypes.Star, "GRAYT TWO (RNAV)", "FERRL.GRAYT2", "DETROIT METRO WAYNE COUNTY", "dtw", ".dtwGRAYTc" },
		{ ProcedureChartTypes.Dp, "TURNAGAIN EIGHT", null, "TED STEVENS ANCHORAGE INTL", "anc", ".ancTURNAGAINc" },
		{ ProcedureChartTypes.Apd, "AIRPORT DIAGRAM", null, AnyAirport, "dtw", ".dtwAPDc" },
		{ ProcedureChartTypes.Min, "TAKEOFF MINIMUMS", null, AnyAirport, "dtw", ".dtwTMc" },
		{ ProcedureChartTypes.Min, "DIVERSE VECTOR AREA", null, AnyAirport, "lax", ".laxDVAc" },
		{ ProcedureChartTypes.Min, "RADAR MINIMUMS", null, AnyAirport, "hsv", ".hsvRMc" },
		{ ProcedureChartTypes.Hot, "HOT SPOT", null, AnyAirport, "lax", ".laxHSc" },
		{ ProcedureChartTypes.Lah, "LAHSO", null, AnyAirport, "bur", ".burLAHSOc" },
	};

	private static string Markdown() =>
		AliasGuideWriter.Write(AliasGuideFormat.Markdown, new AliasGuideOptions("ZOB", "3.0.0", new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc)));

	[Theory]
	[MemberData(nameof(Examples))]
	public void the_guides_chart_recall_example_is_what_fe_buddy_writes(
		string chartCode, string chartName, string? computerCode, string airportName, string airportId, string expected)
	{
		string[] commands = expected.Split(',');
		ChartRecallCodeResult result = ChartRecallCodes.For(chartCode, chartName, computerCode, airportName);
		string[] written = [.. result.Codes.Select(code => "." + airportId.ToLowerInvariant() + code + "c")];

		Assert.Equal(commands, written);

		string guide = Markdown();

		foreach (string command in commands)
		{
			Assert.Contains("`" + command + "`", guide);
		}
	}

	/// <summary>
	/// Every command the Chart Recall section shows - a code span that starts with a period and has
	/// no placeholder in it - belongs to one of the charts above (or is the paged example).
	/// </summary>
	[Fact]
	public void every_chart_recall_command_in_the_guide_has_a_chart_that_proves_it()
	{
		string[] lines = Markdown().Split(Environment.NewLine);
		string section = string.Join(Environment.NewLine, lines.SkipWhile(line => line != "## Chart Recall"));

		// Between the backticks is every odd piece, as the guide always closes a code span.
		HashSet<string> shown = [.. section.Split('`')
			.Where((_, index) => index % 2 == 1)
			.Where(span => span.StartsWith('.') && !span.Contains('<') && !span.Contains('['))];

		HashSet<string> proven = [.. Examples.SelectMany(row => ((string)row[5]!).Split(',')), PagedExample];

		Assert.NotEmpty(shown);
		Assert.Equal(proven.Order(), shown.Order());
	}
}
