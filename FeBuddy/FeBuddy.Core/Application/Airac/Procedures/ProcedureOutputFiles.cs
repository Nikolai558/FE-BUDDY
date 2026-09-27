namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// The files the Procedures sub-service writes: two documents inside
/// <see cref="AiracOutputPaths.PublicationDocsFolder"/> - never the vNAS folder, since neither is a
/// file vNAS takes - and the FAA Chart Recall alias file, which goes where every alias file goes
/// (<see cref="AiracOutputPaths.AliasDirectory"/>).
/// </summary>
public static class ProcedureOutputFiles
{
	/// <summary>The Markdown changes document (<c>ProcedureChangesMarkdownWriter</c>).</summary>
	public const string Changes = "Procedure_Changes.md";

	/// <summary>The JSON procedures document (<c>ProceduresJsonWriter</c>).</summary>
	public const string Json = "Procedures.json";

	/// <summary>
	/// The FAA Chart Recall alias file (<c>ChartRecallAliasWriter</c>) - also its file key on the
	/// Upload to vNAS card.
	/// </summary>
	public const string Alias = "FAA_CHART_RECALL.txt";
}
