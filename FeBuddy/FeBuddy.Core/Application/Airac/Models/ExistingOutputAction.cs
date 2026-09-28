namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// What an AIRAC Service run does when its cycle folder already holds files from an earlier
/// run of the same cycle.
/// </summary>
public enum ExistingOutputAction
{
	/// <summary>
	/// Write over the old files. A file this run does not write (say, from a setting since
	/// switched off) is left where it is.
	/// </summary>
	Overwrite = 0,

	/// <summary>
	/// Permanently delete the whole cycle folder first, so afterwards it holds only this run's
	/// files.
	/// </summary>
	DeleteExisting = 1,
}
