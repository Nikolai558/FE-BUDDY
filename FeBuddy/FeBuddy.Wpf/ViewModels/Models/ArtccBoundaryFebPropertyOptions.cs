using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties the ARTCC Boundaries tab offers, in display order, with their
/// tooltip text.
/// </summary>
/// <remarks>
/// Each property's name comes from Core (<see cref="Core.Application.Airac.FebProperties.Name{TProperty}"/>),
/// the same name <c>ArtccBoundarySettingsParser</c> accepts, so only the order and the wording live here.
/// </remarks>
public static class ArtccBoundaryFebPropertyOptions
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(ArtccBoundaryFebProperty Property, string Description)> All { get; } =
	[
		(ArtccBoundaryFebProperty.LocationId, "ARTCC identifier, e.g. ZOB."),
		(ArtccBoundaryFebProperty.LocationName, "ARTCC name, e.g. CLEVELAND."),
		(ArtccBoundaryFebProperty.LocationType, "ARTCC or CERAP."),
		(ArtccBoundaryFebProperty.IcaoId, "ICAO identifier, e.g. KZOB."),
		(ArtccBoundaryFebProperty.ComputerId, "Computer identifier."),
		(ArtccBoundaryFebProperty.Altitude, "HIGH, LOW or UNLIMITED - the altitude the boundary line is for."),
		(ArtccBoundaryFebProperty.Type, "ARTCC, CTA, FIR, CTA/FIR or UTA - tells the overlapping oceanic CTA and FIR lines apart."),
		(ArtccBoundaryFebProperty.City, "City the ARTCC is in."),
		(ArtccBoundaryFebProperty.CountryCode, "Country code."),
	];
}
