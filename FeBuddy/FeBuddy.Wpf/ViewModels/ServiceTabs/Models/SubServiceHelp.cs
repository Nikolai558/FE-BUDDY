namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>
/// What a sub-service and each of its outputs are, in a sentence or two each: the General tab's
/// tooltips, and the tooltip of its tab in the rail while it is not included.
/// </summary>
/// <param name="Summary">What the sub-service makes, and from what.</param>
/// <param name="Alias">Its alias file and what narrows it, or <see langword="null"/> when it has none.</param>
/// <param name="Geojson">Its GeoJSON files and what narrows them, or <see langword="null"/> when it has none.</param>
/// <param name="ProcedureChanges">Procedures' <c>Procedure_Changes.md</c>, or <see langword="null"/>.</param>
/// <param name="ProceduresJson">Procedures' <c>Procedures.json</c>, or <see langword="null"/>.</param>
public sealed record SubServiceHelp(
	string Summary,
	string? Alias = null,
	string? Geojson = null,
	string? ProcedureChanges = null,
	string? ProceduresJson = null);
