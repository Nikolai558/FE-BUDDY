namespace FeBuddy.Core.Models.Services.Airac;

/// <summary>
/// Whether the FAA has actually published a cycle's NASR CSV subscription yet - a separate
/// fact from whether <c>AiracCycleResolver</c> can name the cycle (it names next-cycle
/// identities from a static table long before the FAA publishes the data).
/// </summary>
public enum AiracCyclePublicationState
{
	/// <summary>The cycle's CSV archive responded - it is published and downloadable.</summary>
	Published = 0,

	/// <summary>
	/// The cycle's CSV archive returned "not found" - the FAA has not published it yet. This
	/// is normal for the next cycle for most of a cycle period and is never an error.
	/// </summary>
	NotYetPublished = 1,

	/// <summary>
	/// The probe could not reach the FAA (network error, timeout). Neither published nor
	/// unpublished - the pipeline should retry rather than conclude anything.
	/// </summary>
	Unknown = 2,
}
