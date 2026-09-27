namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// The two documents the Procedures sub-service writes, both inside
/// <see cref="AiracOutputPaths.PublicationDocsFolder"/> - never the vNAS folder, since neither is a
/// GeoJSON or alias file another sub-service uploads.
/// </summary>
public static class ProcedureOutputFiles
{
	/// <summary>The Markdown changes document (<c>ProcedureChangesMarkdownWriter</c>).</summary>
	public const string Changes = "Procedure_Changes.md";

	/// <summary>The JSON procedures document (<c>ProceduresJsonWriter</c>).</summary>
	public const string Json = "Procedures.json";
}
