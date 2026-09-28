using System.Text;

using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Domain.Telephony;
using FeBuddy.Core.Domain.Telephony.Models;

namespace FeBuddy.Core.Application.Airac.Telephony;

/// <summary>
/// Writes <c>Telephony.txt</c>: an <c>.echo</c> command per designator, identifier and telephony
/// that shows the operator's card in CRC, e.g.
/// <c>.idAVA .echo \n3LD:\t\t\tAVA\nTELEPHONY:\t\s\sAVIANCA\nCOMPANY:\t\t...\nCOUNTRY:\t\tCOLOMBIA</c>.
/// </summary>
/// <remarks>
/// <para>
/// Each operator gets two commands: <c>.id</c> + its designator (or U.S. special identifier), and
/// <c>.id</c> + its telephony with only letters and digits (<see cref="TelephonyNaming"/>) - one
/// command when the two are the same (<c>NASA</c>). A controller can type either what is in the
/// data block or what the pilot said.
/// </para>
/// <para>
/// When several operators land on one command - a telephony that spells another operator's
/// designator (<c>AVA</c> is AVIANCA's designator and another operator's telephony), or two
/// telephonies that only differ by spaces (<c>RYAN AIR</c>, <c>RYANAIR</c>) - the command is written
/// once, showing every one of their cards joined by <c>\n---</c>, the way <c>Navaids.txt</c> handles
/// a shared NAVAID identifier. The operator whose designator or identifier the command is comes
/// first, then those whose telephony spells it. Commands are written in alphabetical order.
/// </para>
/// <para>
/// Like <c>Airports.txt</c>, a card's line breaks and column alignment are the literal two-character
/// escapes <c>\n</c>, <c>\t</c> and <c>\s</c>: CRC tokenizes an alias's replacement text on
/// whitespace and rejoins it with single spaces, so real whitespace cannot hold a column. The
/// padding after each label follows the <c>Airports.txt</c> card's, so the values line up the same
/// way in CRC's font.
/// </para>
/// </remarks>
public static class TelephonyAliasWriter
{
	/// <summary>The literal two-character escape CRC expands into a line break.</summary>
	private const string NewLineEscape = @"\n";

	/// <summary>The literal two-character escape CRC expands into a tab stop.</summary>
	private const string TabEscape = @"\t";

	/// <summary>The literal two-character escape CRC expands into a single space.</summary>
	private const string SpaceEscape = @"\s";

	/// <summary>The literal text joining two operators' cards under one shared command.</summary>
	private const string CardSeparator = @"\n---";

	/// <summary>
	/// Writes the alias file for every entry.
	/// </summary>
	/// <param name="entries">Every operator that gets a card (see <see cref="TelephonyBuilder"/>).</param>
	/// <param name="settings">The parsed Telephony settings.</param>
	/// <returns>The path written (or <see langword="null"/> when there was nothing to write), the command count, and how many commands show more than one operator.</returns>
	public static TelephonyAliasGenerateResult Generate(IReadOnlyList<TelephonyEntry> entries, TelephonySettings settings)
	{
		ArgumentNullException.ThrowIfNull(entries);
		ArgumentNullException.ThrowIfNull(settings);

		IReadOnlyDictionary<string, List<string>> cardsByCommand = BuildCommands(entries);

		if (cardsByCommand.Count == 0)
		{
			return new TelephonyAliasGenerateResult(null, 0, 0);
		}

		StringBuilder builder = new();

		foreach ((string command, List<string> cards) in cardsByCommand.OrderBy(pair => pair.Key, StringComparer.Ordinal))
		{
			builder.Append(command).Append(" .echo ").Append(string.Join(CardSeparator, cards)).AppendLine();
		}

		string directory = AiracOutputPaths.AliasDirectory(settings.OutputDirectory);
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, settings.FileNames.FileName(TelephonyOutputFiles.Alias));

		// UTF-8 without a BOM, like every other alias file.
		File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		return new TelephonyAliasGenerateResult(path, cardsByCommand.Count, cardsByCommand.Values.Count(cards => cards.Count > 1));
	}

	/// <summary>
	/// Every command and the cards it shows: first each entry's designator or identifier command,
	/// then each entry's telephony command, so a command's own operator always comes first.
	/// </summary>
	/// <param name="entries">The entries.</param>
	/// <returns>The cards for each command, keyed ignoring case.</returns>
	internal static IReadOnlyDictionary<string, List<string>> BuildCommands(IReadOnlyList<TelephonyEntry> entries)
	{
		Dictionary<string, List<string>> cardsByCommand = new(StringComparer.OrdinalIgnoreCase);

		foreach (TelephonyEntry entry in entries)
		{
			if (TelephonyNaming.CommandName(entry.Identifier) is { } command)
			{
				Add(cardsByCommand, command, BuildCard(entry));
			}
		}

		foreach (TelephonyEntry entry in entries)
		{
			string? identifierCommand = TelephonyNaming.CommandName(entry.Identifier);

			// NASA (NASA), FEMA (FEMA): the telephony spells the identifier's own command.
			if (TelephonyNaming.CommandName(entry.Telephony) is { } command
				&& !command.Equals(identifierCommand, StringComparison.OrdinalIgnoreCase))
			{
				Add(cardsByCommand, command, BuildCard(entry));
			}
		}

		return cardsByCommand;
	}

	/// <summary>
	/// One operator's card, starting with a line break so it sits below the command in CRC:
	/// <c>3LD</c> / <c>TELEPHONY</c> / <c>COMPANY</c> / <c>COUNTRY</c> for an ICAO assignment, and
	/// <c>ID</c> / <c>TELEPHONY</c> / <c>AGENCY</c> / <c>EXPIRES</c> for a U.S. special call sign.
	/// </summary>
	/// <param name="entry">The operator.</param>
	/// <returns>The card, escapes included.</returns>
	internal static string BuildCard(TelephonyEntry entry)
	{
		string tab = TabEscape;
		string tab2 = tab + tab;
		string tab3 = tab2 + tab;
		string space2 = SpaceEscape + SpaceEscape;

		StringBuilder card = new();

		if (entry.Kind == TelephonyEntryKind.IcaoAssignment)
		{
			AppendLine(card, "3LD:", tab3, entry.Identifier);
			AppendLine(card, "TELEPHONY:", tab + space2, entry.Telephony);
			AppendLine(card, "COMPANY:", tab2, entry.Organization);
			AppendLine(card, "COUNTRY:", tab2, entry.Detail);
		}
		else
		{
			AppendLine(card, "ID:", tab3 + SpaceEscape, entry.Identifier);
			AppendLine(card, "TELEPHONY:", tab + space2, entry.Telephony);
			AppendLine(card, "AGENCY:", tab2 + SpaceEscape, entry.Organization);
			AppendLine(card, "EXPIRES:", tab2, entry.Detail);
		}

		return card.ToString();
	}

	/// <summary>Appends <c>\n</c>, the label, its padding and the value.</summary>
	private static void AppendLine(StringBuilder card, string label, string padding, string value) =>
		card.Append(NewLineEscape).Append(label).Append(padding).Append(value);

	/// <summary>Adds a card to a command's list, starting the list the first time the command is seen.</summary>
	private static void Add(Dictionary<string, List<string>> cardsByCommand, string command, string card)
	{
		if (!cardsByCommand.TryGetValue(command, out List<string>? cards))
		{
			cards = [];
			cardsByCommand[command] = cards;
		}

		cards.Add(card);
	}
}
