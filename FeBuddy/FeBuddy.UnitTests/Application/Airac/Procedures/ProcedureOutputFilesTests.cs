using FeBuddy.Core.Application.Airac.Procedures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>Covers <see cref="ProcedureOutputFiles"/>: the two fixed document names.</summary>
public sealed class ProcedureOutputFilesTests
{
	[Fact]
	public void the_fixed_document_names_are_as_documented()
	{
		Assert.Equal("Procedure_Changes.md", ProcedureOutputFiles.Changes);
		Assert.Equal("Procedures.json", ProcedureOutputFiles.Json);
	}
}
