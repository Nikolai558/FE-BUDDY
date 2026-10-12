namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// What the launch pipeline does with the previous, current and next cycles. A normal launch does
/// everything (<see cref="Everything"/>); a dev launch can leave any part out, and a cycle it doesn't
/// load is loaded when something asks for it.
/// </summary>
/// <param name="Download">
/// Check whether the FAA has published each cycle, and download its NASR data and d-TPP Metafile when
/// they aren't on disk. Without it nothing touches the network, and only the cycles already on disk
/// can be parsed.
/// </param>
/// <param name="Parse">Parse each cycle on disk.</param>
/// <param name="Prune">Delete every cycle on disk but the three.</param>
public sealed record AiracLaunchPlan(bool Download, bool Parse, bool Prune)
{
	/// <summary>Download, parse and prune: a normal launch.</summary>
	public static AiracLaunchPlan Everything { get; } = new(Download: true, Parse: true, Prune: true);

	/// <summary>Whether this plan can leave a cycle unloaded, for something to load later.</summary>
	public bool LeavesCyclesToLoad => !(Download && Parse);
}
