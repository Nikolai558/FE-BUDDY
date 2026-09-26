using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The AIRAC Service sub-service catalogue: every data topic the service can produce output for,
/// whether or not its backend exists yet.
/// </summary>
/// <remarks>
/// <para>
/// This list is the only place a sub-service is registered. Adding one means adding an entry here
/// and a tab view-model for it; the General tab's picker, the tab rail, the save contract and the
/// Preview Settings tab all pick it up with no further changes. The list is expected to grow to
/// roughly twenty entries.
/// </para>
/// <para>
/// <see cref="SubServiceDescriptor.Key"/> is persisted in <c>UserConfig</c> under
/// <c>Services.AiracService.SelectedSubServices</c>, so a key may not be renamed without migrating
/// that value. <see cref="SubServiceDescriptor.IsImplemented"/> is <see langword="false"/> for a
/// sub-service that has no library code behind it yet: its tab opens and explains itself, and it
/// contributes nothing to a run.
/// </para>
/// </remarks>
public static class AiracSubServices
{
	/// <summary>The Airports sub-service key.</summary>
	public const string AirportsKey = "Airports";

	/// <summary>The Airways sub-service key.</summary>
	public const string AirwaysKey = "Airways";

	/// <summary>The Departures sub-service key.</summary>
	public const string DeparturesKey = "Departures";

	/// <summary>The Arrivals sub-service key.</summary>
	public const string ArrivalsKey = "Arrivals";

	/// <summary>The NAVAIDs sub-service key.</summary>
	public const string NavaidsKey = "Navaids";

	/// <summary>The ARTCC Boundaries sub-service key.</summary>
	public const string ArtccBoundariesKey = "ArtccBoundaries";

	/// <summary>The Fixes sub-service key.</summary>
	public const string FixesKey = "Fixes";

	/// <summary>Every sub-service, in the order the picker and the tab rail show them.</summary>
	public static IReadOnlyList<SubServiceDescriptor> All { get; } =
	[
		new SubServiceDescriptor(AirportsKey, "Airports", 10, true, () => new AirportsViewModel()),
		new SubServiceDescriptor(AirwaysKey, "Airways", 20, true, () => new AirwaysViewModel()),
		new SubServiceDescriptor(DeparturesKey, "Departures", 30, true, () => new DeparturesViewModel()),
		new SubServiceDescriptor(ArrivalsKey, "Arrivals", 40, true, () => new ArrivalsViewModel()),
		new SubServiceDescriptor(NavaidsKey, "NAVAIDs", 50, true, () => new NavaidsViewModel()),
		new SubServiceDescriptor(ArtccBoundariesKey, "ARTCC Boundaries", 60, true, () => new ArtccBoundariesViewModel()),
		new SubServiceDescriptor(FixesKey, "Fixes", 70, true, () => new FixesViewModel()),
	];
}
