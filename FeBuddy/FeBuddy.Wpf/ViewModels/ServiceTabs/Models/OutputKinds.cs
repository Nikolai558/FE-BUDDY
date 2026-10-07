namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>Every <see cref="SubServiceOutputKinds"/>, as the output tags on the AIRAC Service's cards use them.</summary>
public static class OutputKinds
{
	/// <summary>Every kind of output: what a tab built on its own (a test) has on.</summary>
	public const SubServiceOutputKinds Every =
		SubServiceOutputKinds.Alias | SubServiceOutputKinds.Geojson | SubServiceOutputKinds.ProcedureChanges | SubServiceOutputKinds.ProceduresJson;

	/// <summary>The kinds in the order the General tab's columns and every card's tags list them.</summary>
	public static IReadOnlyList<SubServiceOutputKinds> InOrder { get; } =
		[SubServiceOutputKinds.Alias, SubServiceOutputKinds.Geojson, SubServiceOutputKinds.ProcedureChanges, SubServiceOutputKinds.ProceduresJson];
}
