using FeBuddy.Core.Application.Models;

namespace FeBuddy.Core.Application.Airac.VnasAlias.Models;

/// <summary>The outcome of parsing the raw vNAS Alias Upload settings dictionary.</summary>
/// <param name="Sources">The user's custom alias files, in the order they are merged.</param>
/// <param name="Messages">Non-fatal parsing messages (e.g. unrecognized keys that were ignored).</param>
public sealed record VnasAliasSettingsParseResult(IReadOnlyList<AliasSource> Sources, IReadOnlyList<ServiceMessage> Messages);

/// <summary>
/// What reading one custom alias file produced: its text, or why it could not be read. A problem is
/// written for the user and never holds a secret.
/// </summary>
/// <param name="Source">The file that was read.</param>
/// <param name="Text">Its text, or <see langword="null"/> when it could not be read.</param>
/// <param name="Problem">Why it could not be read, or <see langword="null"/> when it was.</param>
public sealed record AliasSourceLoad(AliasSource Source, string? Text, string? Problem)
{
	/// <summary>Whether the file was read.</summary>
	public bool Succeeded => Text is not null;

	/// <summary>How many alias commands it holds (lines starting with a dot); 0 when it could not be read.</summary>
	public int CommandCount => Text is null ? 0 : VnasAliasFileWriter.CountCommands(Text);

	/// <summary>A file that was read.</summary>
	/// <param name="source">The file.</param>
	/// <param name="text">Its text.</param>
	/// <returns>The load.</returns>
	public static AliasSourceLoad Read(AliasSource source, string text) => new(source, text, null);

	/// <summary>A file that could not be read.</summary>
	/// <param name="source">The file.</param>
	/// <param name="problem">Why, for the user.</param>
	/// <returns>The load.</returns>
	public static AliasSourceLoad Failed(AliasSource source, string problem) => new(source, null, problem);
}

/// <summary>
/// What writing <c>vNAS_Alias.txt</c> produced: the file, what went into it, and every message along
/// the way - including the custom alias files that could not be read and were left out.
/// </summary>
public sealed record VnasAliasResult : ServiceResult
{
	/// <summary>Full path of <c>vNAS_Alias.txt</c>, or <see langword="null"/> when there was nothing to put in it.</summary>
	public string? FilePath { get; init; }

	/// <summary>How many custom alias files there were.</summary>
	public int CustomFileCount { get; init; }

	/// <summary>How many of them were read and merged.</summary>
	public int CustomFilesMerged { get; init; }

	/// <summary>How many alias commands the merged custom files hold.</summary>
	public int CustomCommandCount { get; init; }

	/// <summary>The names of FE-Buddy's alias files marked for vNAS, in the order they were added (e.g. <c>Airways.txt</c>).</summary>
	public IReadOnlyList<string> FeBuddyFiles { get; init; } = [];

	/// <summary>How many alias commands FE-Buddy's files added.</summary>
	public int FeBuddyCommandCount { get; init; }

	/// <summary>How many commands are in more than one of the merged files, so CRC can run only one of each.</summary>
	public int DuplicateCommandCount { get; init; }
}
