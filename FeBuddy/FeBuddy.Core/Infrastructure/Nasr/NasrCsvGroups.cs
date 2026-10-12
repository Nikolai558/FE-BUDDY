using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Infrastructure.Nasr;

/// <summary>
/// Which NASR CSV groups there are, and which of them FE-Buddy reads (<see cref="InUse"/>): the only
/// ones the AIRAC cycle cache parses.
/// </summary>
public static class NasrCsvGroups
{
	/// <summary>Every group in a NASR CSV cycle.</summary>
	public static IReadOnlyList<NasrCsvGroup> All { get; } = Enum.GetValues<NasrCsvGroup>();

	/// <summary>
	/// The groups FE-Buddy reads, and so the only ones the AIRAC cycle cache parses for each cycle.
	/// The others stay <see langword="null"/> in its <see cref="NasrCsvDataCollection"/>.
	/// </summary>
	/// <remarks>
	/// To use another group later, add it here: its parser and models are already in place. Reading a
	/// group that isn't here throws <see cref="NotParsed"/>'s error.
	/// </remarks>
	public static IReadOnlyList<NasrCsvGroup> InUse { get; } =
	[
		NasrCsvGroup.Apt,
		NasrCsvGroup.Arb,
		NasrCsvGroup.Awy,
		NasrCsvGroup.ClsArsp,
		NasrCsvGroup.Dp,
		NasrCsvGroup.Fix,
		NasrCsvGroup.Frq,
		NasrCsvGroup.Nav,
		NasrCsvGroup.Star,
	];

	/// <summary>The FAA's code for a group, which its file names start with, e.g. <c>CLS_ARSP</c>.</summary>
	/// <param name="group">The group.</param>
	/// <returns>The code.</returns>
	public static string Code(NasrCsvGroup group) => group switch
	{
		NasrCsvGroup.ClsArsp => "CLS_ARSP",
		NasrCsvGroup.MilOps => "MIL_OPS",
		_ => group.ToString().ToUpperInvariant(),
	};

	/// <summary>
	/// The error for reading a group that wasn't parsed: it names the group and points at
	/// <see cref="InUse"/>.
	/// </summary>
	/// <param name="group">The group.</param>
	/// <param name="consequence">What can't be done without it, e.g. <c>The Airports sub-service cannot run without it.</c></param>
	/// <returns>The exception, to throw.</returns>
	public static InvalidOperationException NotParsed(NasrCsvGroup group, string? consequence = null)
	{
		string code = Code(group);

		return new InvalidOperationException(
			$"The NASR {code} data has not been parsed." +
			(consequence is null ? string.Empty : $" {consequence}") +
			$" FE-Buddy parses only the groups in NasrCsvGroups.InUse; to use {code}, add it there.");
	}
}
