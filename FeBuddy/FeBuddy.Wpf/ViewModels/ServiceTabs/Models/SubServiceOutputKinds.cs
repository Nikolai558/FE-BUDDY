namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>
/// The kinds of output a sub-service can make: the columns of the General tab's table. A
/// sub-service offers one or more; the user turns each on or off there.
/// </summary>
[Flags]
public enum SubServiceOutputKinds
{
	/// <summary>Nothing of its own (Add Custom Aliases, which adds custom aliases to the others' combined alias file).</summary>
	None = 0,

	/// <summary>An alias file, e.g. <c>Airports.txt</c>.</summary>
	Alias = 1,

	/// <summary>GeoJSON files.</summary>
	Geojson = 2,

	/// <summary>Procedures' <c>Procedure_Changes.md</c>.</summary>
	ProcedureChanges = 4,

	/// <summary>Procedures' <c>Procedures.json</c>.</summary>
	ProceduresJson = 8,
}
