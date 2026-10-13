using FeBuddy.Core.Application.Airac.Procedures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ChartRecallHelpCommand"/>: the line is exactly the note FE-Buddy was given, under
/// the name it's given, and a typed name loses its dots and is checked for spaces.
/// </summary>
public sealed class ChartRecallHelpCommandTests
{
	/// <summary>The command as it was handed over, word for word.</summary>
	private const string Expected =
		@".helpCHARTRECALL .note :::::::::::::::::::::::::::::\n\s\sHelp:\sChart\sRecall\s(IAPs)\n:::::::::::::::::::::::::::::\n\nSyntax:\nAptID\sAppTypeCode\sAppVariant\sRwyID/CircleCode\sC\n\nAppType\s\sCode\s\s\sDME\s\sBACK-CRS\n-------\s\s----\s\s\s---\s\s--------\nRNAV\t\sR\t\s\s\nILS\t\s\sI\t\s\s\nLOC\t\s\sL\t\s\sLD\s\s\sLBC\s/\sLDBC\nVOR\t\s\sO\t\s\sOD\nNDB\t\s\sN\t\s\sND\nLDA\t\s\sD\t\s\sDD\nGPS\t\s\sG\t\s\s\nTACAN\tT\t\s\s\n\naaaL27Rc\t\t\s\s|\s\saaaNAc\n\s\sAppType\s\s\sLOC\s\s\s|\tAppType\s\s\sNDB\n\s\sRwy\t\s\s\s27R\s\s\s|\tCircle\tA\n\t\t\t\t\s\s|\s\s\naaaRY27Rc\t\t\s|\s\saaaLDBC27Rc\n\s\sAppType\s\s\sRNAV\s\s|\tAppType\s\s\sLOC/DME\sBack\sCourse\n\s\sRwy\t\s\s\s27R\s\s\s|\tRwy\t\s\s\s27R\n\s\sVariant\s\s\sY\t\s|\n\nNote:\nFor other types of Chart Recalls, consult\nyour Alias Command documentation or\nthe FE-Buddy Command Guide.";

	[Fact]
	public void the_default_line_is_the_command_as_given()
	{
		Assert.Equal(Expected, ChartRecallHelpCommand.Line(ChartRecallHelpCommand.DefaultName));
	}

	[Fact]
	public void the_line_is_one_line_of_text()
	{
		Assert.DoesNotContain('\n', ChartRecallHelpCommand.Line("helpCR"));
		Assert.DoesNotContain('\r', ChartRecallHelpCommand.Line("helpCR"));
	}

	[Fact]
	public void a_renamed_command_keeps_the_note()
	{
		Assert.Equal(".helpCR .note " + ChartRecallHelpCommand.Note, ChartRecallHelpCommand.Line("helpCR"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	public void a_line_needs_a_name(string? name) =>
		Assert.ThrowsAny<ArgumentException>(() => ChartRecallHelpCommand.Line(name!));

	[Theory]
	[InlineData("helpCR", "helpCR")]
	[InlineData(".helpCR", "helpCR")]
	[InlineData("..helpCR", "helpCR")]
	[InlineData("  .helpCR  ", "helpCR")]
	[InlineData(".", "")]
	[InlineData(null, "")]
	public void a_typed_name_is_trimmed_and_loses_its_dots(string? typed, string expected) =>
		Assert.Equal(expected, ChartRecallHelpCommand.CleanName(typed));

	[Theory]
	[InlineData("helpCR", false)]
	[InlineData("help.CR", false)]
	[InlineData("help CR", true)]
	[InlineData("help\tCR", true)]
	public void a_name_with_a_space_in_it_cannot_be_used(string name, bool hasSpace) =>
		Assert.Equal(hasSpace, ChartRecallHelpCommand.HasSpace(name));

	[Fact]
	public void has_space_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => ChartRecallHelpCommand.HasSpace(null!));
}
