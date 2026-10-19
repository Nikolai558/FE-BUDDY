namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>
/// How a line of a tab's "What You'll Get" summary joins the lines above it: the words on its chip.
/// Read top to bottom, chips and all, a block is one sentence: <c>Every STAR</c> · <c>for</c>
/// <c>airports in ZOB</c> · <c>and only</c> <c>those amended this cycle</c>.
/// </summary>
public enum SummaryJoin
{
	/// <summary>No chip: a block's first line, what the rest narrow down or add to.</summary>
	First = 0,

	/// <summary><c>for</c>: the place the first line is for, e.g. <c>airports in ZOB</c>.</summary>
	For = 1,

	/// <summary><c>in</c>: the place the first line is in, e.g. <c>ZLA and ZOA</c>.</summary>
	In = 2,

	/// <summary>
	/// <c>but only</c>: narrows the first line, right below it. <see cref="SummaryLines"/> words a
	/// narrowing this way or as <see cref="AndOnly"/> by where it falls.
	/// </summary>
	ButOnly = 3,

	/// <summary><c>and only</c>: narrows everything above, below another line that already does.</summary>
	AndOnly = 4,

	/// <summary><c>with only</c>: under "Outputs include", what a file keeps of everything above.</summary>
	WithOnly = 5,

	/// <summary><c>along with</c>: added to what the lines above get, whatever they say.</summary>
	AlongWith = 6,
}
