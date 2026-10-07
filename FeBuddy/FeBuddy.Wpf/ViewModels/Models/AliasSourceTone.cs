namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>The colour of a custom alias file's status line (<see cref="AliasSourceRow.Status"/>).</summary>
public enum AliasSourceTone
{
	/// <summary>Nothing to report: what kind of file it is.</summary>
	Neutral = 0,

	/// <summary>The last Check read it (green).</summary>
	Positive = 1,

	/// <summary>It will fail to read, but can be saved: the file or the credential isn't on this PC (amber).</summary>
	Warn = 2,

	/// <summary>It can't be saved, or the last Check couldn't read it (red).</summary>
	Danger = 3,
}
