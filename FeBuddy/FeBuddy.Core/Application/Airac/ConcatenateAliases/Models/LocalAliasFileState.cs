namespace FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;

/// <summary>What a custom alias file on this PC is, as <see cref="AliasSourceLoader.InspectLocalFile"/> finds it.</summary>
public enum LocalAliasFileState
{
	/// <summary>It isn't there: moved, renamed or deleted.</summary>
	Missing,

	/// <summary>It is there but couldn't be read: locked, or no permission.</summary>
	Unreadable,

	/// <summary>It has no alias command in it (no line starting with a dot), so it isn't an alias file.</summary>
	NoAliasCommands,

	/// <summary>It holds at least one alias command.</summary>
	HasAliasCommands,
}
