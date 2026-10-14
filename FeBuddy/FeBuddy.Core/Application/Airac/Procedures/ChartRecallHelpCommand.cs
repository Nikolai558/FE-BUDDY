namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// The FAA Chart Recall help command: a <c>.note</c> command, the first line of
/// <c>Faa_Chart_Recall.txt</c>, that shows a quick reference for writing an instrument approach's (IAP's)
/// chart recall command, the hardest kind to work out - the syntax, the approach type codes (as
/// <c>ApproachCodes</c> makes them), and four examples - and points to the command guides for any
/// other chart.
/// </summary>
/// <remarks>
/// The note is written as CRC reads it: <c>\n</c> starts a line, <c>\t</c> is a tab and <c>\s</c> a
/// space. It is <c>.helpCHARTRECALL</c> unless the user renames it (<c>HelpCommandName</c>), and
/// <c>Combined_Alias.txt</c> gets it at the top of the file's section, as it copies the file.
/// </remarks>
public static class ChartRecallHelpCommand
{
	/// <summary>The command's name, without its dot, unless the user gives it another.</summary>
	public const string DefaultName = "helpCHARTRECALL";

	/// <summary>The note, one line of it each.</summary>
	private static readonly string[] NoteLines =
	[
		@":::::::::::::::::::::::::::::",
		@"\s\sHelp:\sChart\sRecall\s(IAPs)",
		@":::::::::::::::::::::::::::::",
		@"",
		@"Syntax:",
		@"AptID\sAppTypeCode\sAppVariant\sRwyID/CircleCode\sC",
		@"",
		@"AppType\s\sCode\s\s\sDME\s\sBACK-CRS",
		@"-------\s\s----\s\s\s---\s\s--------",
		@"RNAV\t\sR\t\s\s",
		@"ILS\t\s\sI\t\s\s",
		@"LOC\t\s\sL\t\s\sLD\s\s\sLBC\s/\sLDBC",
		@"VOR\t\s\sO\t\s\sOD",
		@"NDB\t\s\sN\t\s\sND",
		@"LDA\t\s\sD\t\s\sDD",
		@"GPS\t\s\sG\t\s\s",
		@"TACAN\tT\t\s\s",
		@"",
		@"aaaL27Rc\t\t\s\s|\s\saaaNAc",
		@"\s\sAppType\s\s\sLOC\s\s\s|\tAppType\s\s\sNDB",
		@"\s\sRwy\t\s\s\s27R\s\s\s|\tCircle\tA",
		@"\t\t\t\t\s\s|\s\s",
		@"aaaRY27Rc\t\t\s|\s\saaaLDBC27Rc",
		@"\s\sAppType\s\s\sRNAV\s\s|\tAppType\s\s\sLOC/DME\sBack\sCourse",
		@"\s\sRwy\t\s\s\s27R\s\s\s|\tRwy\t\s\s\s27R",
		@"\s\sVariant\s\s\sY\t\s|",
		@"",
		@"Note:",
		@"For other types of Chart Recalls, consult",
		@"your Alias Command documentation or",
		@"the FE-Buddy Command Guide.",
	];

	/// <summary>The note the command shows, its lines joined by CRC's <c>\n</c>.</summary>
	public static string Note { get; } = string.Join(@"\n", NoteLines);

	/// <summary>The command's line in the alias file: <c>.helpCHARTRECALL .note ::::...</c>.</summary>
	/// <param name="name">The command's name, without its dot (see <see cref="CleanName"/>).</param>
	/// <returns>The line, without a line ending.</returns>
	public static string Line(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		return $".{name} .note {Note}";
	}

	/// <summary>
	/// A name as typed, made ready to use: trimmed, and without the dots in front, since FE-Buddy adds
	/// the dot itself. <c>.helpCR</c> is <c>helpCR</c>.
	/// </summary>
	/// <param name="name">The name as typed, or <see langword="null"/>.</param>
	/// <returns>The name; empty when nothing is left.</returns>
	public static string CleanName(string? name) => (name ?? string.Empty).Trim().TrimStart('.');

	/// <summary>
	/// The name the command goes by: the new name, cleaned (see <see cref="CleanName"/>), while the user
	/// renames it and has typed one; otherwise <see cref="DefaultName"/>.
	/// </summary>
	/// <param name="rename">Whether the user renames it: "Yes" on the Chart Recall Help Command card.</param>
	/// <param name="newName">The new name as typed, or <see langword="null"/>.</param>
	/// <returns>The name, without its dot. One that can't be used is returned as it is, for the caller to refuse (see <see cref="IsValidName"/>).</returns>
	public static string Name(bool rename, string? newName) =>
		rename && CleanName(newName) is { Length: > 0 } name ? name : DefaultName;

	/// <summary>
	/// Whether a name can be used: letters and numbers only - no spaces, dots or other symbols, which
	/// the Alias Command Guide would also read as its own markup.
	/// </summary>
	/// <param name="name">The name, cleaned (see <see cref="CleanName"/>).</param>
	/// <returns><see langword="true"/> when it is one or more of A-Z, a-z and 0-9.</returns>
	public static bool IsValidName(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		return name.Length > 0 && name.All(char.IsAsciiLetterOrDigit);
	}
}
