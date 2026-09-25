using System.Text;

using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Navaids;
using FeBuddy.Core.Domain.Navaids.Models;

namespace FeBuddy.Core.Application.Airac.Navaids;

/// <summary>
/// Generates the <c>NAVAIDs.txt</c> alias file: an <c>.echo</c> command per NAVAID that prints its
/// identifier, name, type, frequency and ARTCC boundaries.
/// </summary>
/// <remarks>
/// <para>
/// Like <c>AirportAliasWriter</c>, column alignment and line breaks are literal <c>\t</c> and
/// <c>\n</c> escapes - two characters each, never real tabs or newlines - because CRC tokenizes an
/// alias's replacement text on whitespace and rejoins it with single spaces.
/// </para>
/// <para>
/// The file covers every included NAVAID (after <see cref="NavaidFilter.ExcludeTypes"/>) and is
/// deliberately never ROI-filtered, the same as every other alias writer.
/// </para>
/// <para>
/// Each NAVAID contributes an ID command (<c>.nav&lt;NavId&gt;</c>) and, when its name yields a
/// different one, a name command (<c>.nav&lt;name with only letters/digits&gt;</c>). Several
/// NAVAIDs can land on the same command - duplicate identifiers are normal in NASR data, and two
/// different NAVAIDs can alias to the same name - so a command already seen is not overwritten:
/// its new block is appended to the existing one, joined by <c>\n---</c>, and the command is
/// written once, in the order its command name was first seen.
/// </para>
/// </remarks>
public static class NavaidAliasWriter
{
	/// <summary>The literal two-character escape CRC expands into a line break.</summary>
	private const string NewLineEscape = @"\n";

	/// <summary>The literal two-character escape CRC expands into a tab stop.</summary>
	private const string TabEscape = @"\t";

	/// <summary>The literal two-character escape CRC expands into a single space.</summary>
	private const string SpaceEscape = @"\s";

	/// <summary>The literal text joining two NAVAIDs' blocks under one shared command.</summary>
	private const string BlockSeparator = @"\n---";

	/// <summary>
	/// Writes the alias file for every included NAVAID.
	/// </summary>
	/// <param name="navaids">Every included NAVAID (after <see cref="NavaidFilter.ExcludeTypes"/>), ROI-independent.</param>
	/// <param name="settings">The parsed NAVAIDs settings.</param>
	/// <returns>The path written (or <see langword="null"/> when there was nothing to write), the command count, and any messages.</returns>
	public static NavaidAliasGenerateResult Generate(IReadOnlyList<Navaid> navaids, NavaidSettings settings)
	{
		ArgumentNullException.ThrowIfNull(navaids);
		ArgumentNullException.ThrowIfNull(settings);

		List<ServiceMessage> messages = [];

		if (navaids.Count == 0)
		{
			return new NavaidAliasGenerateResult(null, 0, messages);
		}

		Dictionary<string, List<string>> blocksByCommand = new(StringComparer.OrdinalIgnoreCase);
		List<string> commandOrder = [];

		foreach (Navaid navaid in navaids)
		{
			string block = BuildBlock(navaid);
			string idCommand = $".nav{navaid.NavId}";

			AddBlock(blocksByCommand, commandOrder, idCommand, block);

			string aliasNavName = string.Concat(navaid.Name.Where(char.IsAsciiLetterOrDigit));

			if (aliasNavName.Length > 0)
			{
				string nameCommand = $".nav{aliasNavName}";

				// A NAVAID whose name aliases to the same command as its own ID (e.g. ELY = ELY)
				// contributes only once.
				if (!nameCommand.Equals(idCommand, StringComparison.OrdinalIgnoreCase))
				{
					AddBlock(blocksByCommand, commandOrder, nameCommand, block);
				}
			}
		}

		StringBuilder builder = new();

		foreach (string command in commandOrder)
		{
			string body = string.Join(BlockSeparator, blocksByCommand[command]);
			builder.Append(command).Append(" .echo ").Append(body).AppendLine();
		}

		string directory = AiracOutputPaths.FileDirectory(
			settings.OutputDirectory, isGeojson: false, settings.Vnas.IsUploaded(NavaidOutputFiles.Alias));
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, NavaidOutputFiles.Alias);

		// UTF-8 without a BOM, like every other alias writer.
		File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		return new NavaidAliasGenerateResult(path, commandOrder.Count, messages);
	}

	/// <summary>Appends a block to a command's list, recording the command's first-seen order.</summary>
	private static void AddBlock(Dictionary<string, List<string>> blocksByCommand, List<string> commandOrder, string command, string block)
	{
		if (!blocksByCommand.TryGetValue(command, out List<string>? blocks))
		{
			blocks = [];
			blocksByCommand[command] = blocks;
			commandOrder.Add(command);
		}

		blocks.Add(block);
	}

	/// <summary>
	/// Builds one NAVAID's block: <c>\nNAVAID:\t\t\s{NavId}\s-\s{Name}\n\t\t\t\t{NavType}\nFREQ:\t\t\s\s\s{Freq}\nARTCC\sHIGH:\t\s{HighAltArtccId}\nARTCC\sLOW:\t\s\s{LowAltArtccId}</c>.
	/// </summary>
	/// <param name="navaid">The NAVAID to describe.</param>
	/// <returns>The block, escapes included, with no leading command or <c>.echo</c>.</returns>
	private static string BuildBlock(Navaid navaid)
	{
		string tab2 = TabEscape + TabEscape;
		string tab4 = tab2 + tab2;
		string space3 = SpaceEscape + SpaceEscape + SpaceEscape;

		StringBuilder block = new();

		block.Append(NewLineEscape);
		block.Append("NAVAID:").Append(tab2).Append(SpaceEscape).Append(navaid.NavId)
			.Append(SpaceEscape).Append('-').Append(SpaceEscape).Append(navaid.Name).Append(NewLineEscape);
		block.Append(tab4).Append(navaid.NavType).Append(NewLineEscape);
		block.Append("FREQ:").Append(tab2).Append(space3).Append(NavaidTypes.FormatFrequency(navaid.NavType, navaid.Freq)).Append(NewLineEscape);
		block.Append("ARTCC").Append(SpaceEscape).Append("HIGH:").Append(TabEscape).Append(SpaceEscape).Append(navaid.HighAltArtccId).Append(NewLineEscape);
		block.Append("ARTCC").Append(SpaceEscape).Append("LOW:").Append(TabEscape).Append(SpaceEscape).Append(SpaceEscape).Append(navaid.LowAltArtccId);

		return block.ToString();
	}
}
