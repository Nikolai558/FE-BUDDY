using System.Reflection;
using System.Text.RegularExpressions;

using FeBuddy.Wpf.Shell;

namespace FeBuddy.UnitTests.Repository;

/// <summary>
/// Checks the links the app opens into the repository's own docs (<see cref="Links"/>): each page is
/// still at its path, and each <c>#section</c> still names one of its headings - so moving a page or
/// renaming a heading can't leave a button in the app pointing nowhere.
/// </summary>
public sealed partial class DocLinkTests
{
	private const string DocsBase = "https://github.com/Nikolai558/FE-BUDDY/blob/v3-development/";

	public static TheoryData<string> doc_links() => [.. DocUrls()];

	[Fact]
	public void the_app_links_to_a_section_of_its_docs() =>
		Assert.Contains(DocUrls(), url => url.Contains('#', StringComparison.Ordinal));

	[Theory]
	[MemberData(nameof(doc_links))]
	public void each_doc_link_names_a_page_and_a_heading_that_exist(string url)
	{
		string[] parts = url[DocsBase.Length..].Split('#', 2);
		string page = Path.Combine(FindRepositoryRoot(), parts[0].Replace('/', Path.DirectorySeparatorChar));

		Assert.True(File.Exists(page), $"{url} names a page that isn't in the repository.");

		if (parts.Length == 2)
		{
			IEnumerable<string> anchors = File.ReadLines(page)
				.Select(line => HeadingPattern().Match(line))
				.Where(match => match.Success)
				.Select(match => GitHubAnchor(match.Groups["text"].Value));

			Assert.Contains(parts[1], anchors);
		}
	}

	/// <summary>Every link in <see cref="Links"/> into the repository's docs.</summary>
	private static IEnumerable<string> DocUrls() =>
		typeof(Links).GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(field => field.IsLiteral)
			.Select(field => field.GetRawConstantValue())
			.OfType<string>()
			.Where(url => url.StartsWith(DocsBase, StringComparison.Ordinal));

	/// <summary>The anchor GitHub gives a heading: lower case, punctuation dropped, spaces as hyphens.</summary>
	private static string GitHubAnchor(string heading) =>
		AnchorDropPattern().Replace(heading.Trim().ToLowerInvariant(), string.Empty).Replace(' ', '-');

	/// <summary>The folder holding <c>FeBuddy\FeBuddy.sln</c>, found by walking up from the test's own folder.</summary>
	private static string FindRepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "FeBuddy", "FeBuddy.sln")))
			{
				return folder.FullName;
			}
		}

		throw new DirectoryNotFoundException($"No repository root (holding FeBuddy\\FeBuddy.sln) above {AppContext.BaseDirectory}.");
	}

	[GeneratedRegex(@"^#{1,6}\s+(?<text>.+)$")]
	private static partial Regex HeadingPattern();

	[GeneratedRegex(@"[^\p{L}\p{Nd} _-]")]
	private static partial Regex AnchorDropPattern();
}
