namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// The files the Procedures sub-service writes, by file key (the name the File Names tab uses): two
/// documents inside <see cref="AiracOutputPaths.PublicationDocsFolder"/>, and the FAA Chart Recall
/// alias file, which goes where every alias file goes (<see cref="AiracOutputPaths.AliasDirectory"/>).
/// </summary>
public static class ProcedureOutputFiles
{
	/// <summary>The Markdown changes document (<c>ProcedureChangesMarkdownWriter</c>).</summary>
	public const string Changes = "Procedure_Changes.md";

	/// <summary>The JSON procedures document (<c>ProceduresJsonWriter</c>).</summary>
	public const string Json = "Procedures.json";

	/// <summary>The FAA Chart Recall alias file (<c>ChartRecallAliasWriter</c>).</summary>
	public const string Alias = "Faa_Chart_Recall.txt";
}
