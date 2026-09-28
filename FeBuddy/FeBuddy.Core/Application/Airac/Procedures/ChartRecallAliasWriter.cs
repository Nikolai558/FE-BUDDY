using System.Text;

using FeBuddy.Core.Application.Airac.Procedures.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Writes <c>Faa_Chart_Recall.txt</c>, the FAA Chart Recall alias file, from the lines
/// <see cref="ChartRecallAliasBuilder"/> built.
/// </summary>
/// <remarks>
/// The file goes in the cycle folder's <c>Aliases</c> folder (see
/// <see cref="AiracOutputPaths.AliasDirectory"/>), like every other alias file; when the user marked it
/// for vNAS it is also copied into <c>vNAS_Alias.txt</c>.
/// </remarks>
public static class ChartRecallAliasWriter
{
	/// <summary>
	/// Writes the alias file.
	/// </summary>
	/// <param name="lines">The command lines, in the order to write them.</param>
	/// <param name="settings">The parsed Procedures settings; <see cref="ProcedureSettings.OutputDirectory"/> and <see cref="ProcedureSettings.Vnas"/> are read.</param>
	/// <returns>The path written (or <see langword="null"/> when there was no line to write) and the command count.</returns>
	public static ChartRecallAliasWriteResult Generate(IReadOnlyList<ChartRecallAliasLine> lines, ProcedureSettings settings)
	{
		ArgumentNullException.ThrowIfNull(lines);
		ArgumentNullException.ThrowIfNull(settings);

		if (lines.Count == 0)
		{
			return new ChartRecallAliasWriteResult(null, 0);
		}

		StringBuilder builder = new();

		foreach (ChartRecallAliasLine line in lines)
		{
			builder.Append(line.Text).AppendLine();
		}

		string directory = AiracOutputPaths.AliasDirectory(settings.OutputDirectory);
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, settings.FileNames.FileName(ProcedureOutputFiles.Alias));

		// UTF-8 without a BOM, like every other alias file.
		File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		return new ChartRecallAliasWriteResult(path, lines.Count);
	}
}
