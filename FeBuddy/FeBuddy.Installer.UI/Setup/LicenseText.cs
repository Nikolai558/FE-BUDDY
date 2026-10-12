using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>One paragraph of the license page.</summary>
/// <param name="Text">The paragraph, as one line (the window wraps it).</param>
/// <param name="IsHeading">A short standalone paragraph ("Preamble", "0. Definitions."), shown bold.</param>
public sealed record LicenseParagraph(string Text, bool IsHeading);

/// <summary>
/// The repository's LICENSE.md (the GNU GPL v3 text), embedded in this assembly, as paragraphs.
/// </summary>
/// <remarks>
/// The text is hard-wrapped at about 72 columns for a fixed-width font. Shown as-is in the window's
/// proportional font those breaks fall mid-line, so each paragraph's lines are joined back into one
/// and the window wraps it to its own width.
/// </remarks>
public static class LicenseText
{
	private const string ResourceName = "FeBuddy.Installer.UI.LICENSE.md";

	// A paragraph of one or two lines this short is a heading ("Preamble", the title and its
	// version line); body paragraphs run longer.
	private const int MaxHeadingLength = 60;
	private const int MaxHeadingLines = 2;

	/// <summary>Reads and reflows the license.</summary>
	/// <returns>Its paragraphs, or a single line pointing to the license online if it is missing.</returns>
	public static IReadOnlyList<LicenseParagraph> Load()
	{
		using Stream? stream = typeof(LicenseText).Assembly.GetManifestResourceStream(ResourceName);
		if (stream is null)
		{
			return [new LicenseParagraph("GNU General Public License v3: https://www.gnu.org/licenses/gpl-3.0.html", false)];
		}

		using StreamReader reader = new(stream, Encoding.UTF8);
		return Reflow(reader.ReadToEnd());
	}

	/// <summary>Joins each blank-line-separated paragraph's lines into one.</summary>
	/// <param name="text">Hard-wrapped plain text.</param>
	/// <returns>The paragraphs.</returns>
	public static IReadOnlyList<LicenseParagraph> Reflow(string text)
	{
		List<LicenseParagraph> paragraphs = [];
		List<string> lines = [];

		void Flush()
		{
			if (lines.Count > 0)
			{
				bool heading = lines.Count <= MaxHeadingLines && lines.All(l => l.Length <= MaxHeadingLength);
				paragraphs.Add(new LicenseParagraph(string.Join(" ", lines), heading));
				lines.Clear();
			}
		}

		foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
		{
			string line = raw.Trim();
			if (line.Length == 0)
			{
				Flush();
			}
			else
			{
				lines.Add(line);
			}
		}

		Flush();
		return [.. paragraphs.Where(p => p.Text.Length > 0)];
	}
}
