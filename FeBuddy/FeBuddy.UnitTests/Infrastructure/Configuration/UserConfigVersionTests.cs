using System.Text.RegularExpressions;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Platform;
using FeBuddy.Versioning;
using FeBuddy.Wpf.Shell;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Covers <see cref="UserConfigVersion"/>: which layout each release reads (<see cref="UserConfigVersion.Layouts"/>),
/// kept in step with the developer docs' layout table and this version, and <see cref="UserConfigVersion.Oldest"/>
/// never going up.
/// </summary>
public sealed partial class UserConfigVersionTests
{
	[Theory]
	[InlineData("3.0.0-beta.6", 2)]
	[InlineData("3.0.0-beta.13", 2)]
	[InlineData("3.0.0", 2)]
	[InlineData("v3.1.0", 2)]
	[InlineData("3.0.0-beta.5", null)]
	[InlineData("3.0.0-alpha.9", null)]
	[InlineData("2.9.3", null)]
	[InlineData("dev", null)]
	[InlineData("", null)]
	[InlineData(null, null)]
	public void for_release_gives_the_layout_a_release_reads(string? version, int? layout)
	{
		Assert.Equal(layout, UserConfigVersion.ForRelease(version));
	}

	/// <summary>
	/// Raising <see cref="UserConfigVersion.Oldest"/> would cut off anyone who skipped the releases in between:
	/// their settings would start on defaults. Keep every step instead.
	/// </summary>
	[Fact]
	public void the_oldest_layout_never_goes_up()
	{
		int oldest = UserConfigVersion.Oldest;

		Assert.True(oldest == 2,
			"UserConfigVersion.Oldest must stay 2 (3.0.0's layout), or a user who skipped releases loses their settings. Keep the old steps instead.");
	}

	[Fact]
	public void layouts_run_oldest_first_from_the_oldest_to_the_current_one_with_releases_in_order()
	{
		IReadOnlyList<(int Layout, string FirstSavedBy)> layouts = UserConfigVersion.Layouts;

		Assert.Equal(UserConfigVersion.Oldest, layouts[0].Layout);
		Assert.Equal(UserConfigVersion.Current, layouts[^1].Layout);
		Assert.Equal(layouts.Select(l => l.Layout).Distinct().Order(), layouts.Select(l => l.Layout));

		for (int i = 1; i < layouts.Count; i++)
		{
			Assert.True(ProductVersion.Parse(layouts[i - 1].FirstSavedBy).ComparePrecedenceTo(ProductVersion.Parse(layouts[i].FirstSavedBy)) < 0,
				$"Layout {layouts[i].Layout} must be first saved by a release after layout {layouts[i - 1].Layout}'s.");
		}
	}

	/// <summary>This FE-Buddy's own version reads the current layout: a new layout needs its row in <see cref="UserConfigVersion.Layouts"/>.</summary>
	[Fact]
	public void this_version_reads_the_current_layout()
	{
		Assert.Equal(UserConfigVersion.Current, UserConfigVersion.ForRelease(AppVersion.Of(typeof(InstalledUpdate).Assembly)));
	}

	/// <summary>The layout table in <c>docs/Developers/Settings-Reference.md</c> lists the same layouts and releases.</summary>
	[Fact]
	public void layouts_match_the_table_in_the_settings_reference()
	{
		string page = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "Developers", "Settings-Reference.md"));
		int table = page.IndexOf("| Layout | First saved by |", StringComparison.Ordinal);
		Assert.True(table >= 0, "Settings-Reference.md has no '| Layout | First saved by |' table.");

		List<(int, string)> rows = [];

		foreach (string line in page[table..].Split('\n').Skip(2).Select(line => line.TrimEnd('\r')).TakeWhile(line => line.StartsWith('|')))
		{
			Match row = TableRow().Match(line);
			Assert.True(row.Success, $"Can't read the layout table row '{line}'.");
			rows.Add((int.Parse(row.Groups["layout"].Value, System.Globalization.CultureInfo.InvariantCulture), row.Groups["release"].Value));
		}

		Assert.Equal(UserConfigVersion.Layouts.Select(l => (l.Layout, l.FirstSavedBy)), rows);
	}

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

	[GeneratedRegex(@"^\|\s*(?<layout>\d+)\s*\|\s*(?<release>[^|\s]+)\s*\|")]
	private static partial Regex TableRow();
}
