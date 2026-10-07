namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// How an alias command that prints cards in CRC lays them out - <c>Airports.txt</c>,
/// <c>Navaids.txt</c> and <c>Telephony.txt</c> all use it: a blank line above the first card, an
/// indented <c>+</c> between blank lines where one command shows more than one card, and a line
/// break after the last card.
/// </summary>
/// <remarks>
/// Every card starts with its own <c>\n</c>, so one more before it leaves the blank line under the
/// command. Two NAVAIDs under <c>.navAA</c> read
/// <c>\n\nNAVAID:…ZTL\n\n\t\t+\n\nNAVAID:…ZMP\n</c>. Like the cards themselves, these are the
/// literal two-character escapes CRC expands, never real line breaks or tabs.
/// </remarks>
internal static class EchoCards
{
	/// <summary>Before the first card, whose own <c>\n</c> follows: a blank line under the command.</summary>
	private const string Start = @"\n";

	/// <summary>Between two cards: a line break, a blank line, the indented <c>+</c>, then the next card's own <c>\n</c> after a line break.</summary>
	private const string Separator = @"\n\n\t\t+\n";

	/// <summary>After the last card.</summary>
	private const string End = @"\n";

	/// <summary>One command's cards, laid out for CRC.</summary>
	/// <param name="cards">The cards, in order, each starting with <c>\n</c>.</param>
	/// <returns>What follows the command's <c>.echo</c> and its space.</returns>
	internal static string Join(IEnumerable<string> cards) => Start + string.Join(Separator, cards) + End;
}
