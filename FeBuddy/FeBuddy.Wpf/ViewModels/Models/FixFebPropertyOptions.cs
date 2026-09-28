using FeBuddy.Core.Application.Airac.Fixes.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties the Fixes tab offers, in display order, with their tooltip text.
/// </summary>
/// <remarks>
/// Each property's name comes from Core (<see cref="Core.Application.Airac.FebProperties.Name{TProperty}"/>),
/// the same name <c>FixSettingsParser</c> accepts, so only the order and the wording live here.
/// </remarks>
public static class FixFebPropertyOptions
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(FixFebProperty Property, string Description)> All { get; } =
	[
		(FixFebProperty.FixId, "Fix identifier. Symbols file only - the Text label already shows it."),
		(FixFebProperty.FixUseCode, "The fix's mapped type of use, e.g. WYPNT."),
		(FixFebProperty.Charts, "The NASR chart names the fix is depicted on, omitted when it has none."),
	];
}
