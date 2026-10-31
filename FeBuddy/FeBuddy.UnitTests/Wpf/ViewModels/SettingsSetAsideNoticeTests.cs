using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the notice launch shows when settings a newer FE-Buddy saved were set aside
/// (<see cref="ShellViewModel.DescribeSetAside"/>): where they went, what each has now, and how to get them back.
/// </summary>
public sealed class SettingsSetAsideNoticeTests
{
	[Fact]
	public void the_notice_names_the_folder_each_file_and_what_it_has_now()
	{
		UserConfigSetAside setAside = new(@"C:\AppData\FE-Buddy\Reverted\2026-10-19 120000Z",
		[
			new UserConfigSetAsideFile("ZOB", 3, 2),
			new UserConfigSetAsideFile("ZNY", 3, null),
			new UserConfigSetAsideFile(null, 3, 2),
		]);

		Assert.Equal(
			@"A newer FE-Buddy saved some of your settings in a way this version can't read, so they were moved to `C:\AppData\FE-Buddy\Reverted\2026-10-19 120000Z`. " +
			"In their place:\n\n" +
			"•  Profile ZOB: the last copy this version saved\n" +
			"•  Profile ZNY: every default\n" +
			"•  Settings every profile shares (update channel, window position): the last copy this version saved\n\n" +
			"After you update FE-Buddy again, import a profile from there with Settings ▸ Import….",
			ShellViewModel.DescribeSetAside([setAside]));
	}

	/// <summary>Set aside more than once before the notice showed: every folder is named.</summary>
	[Fact]
	public void the_notice_names_every_folder()
	{
		string text = ShellViewModel.DescribeSetAside(
		[
			new UserConfigSetAside(@"C:\Reverted\one", [new UserConfigSetAsideFile("ZOB", 3, 2)]),
			new UserConfigSetAside(@"C:\Reverted\two", [new UserConfigSetAsideFile("ZOB", 3, 2)]),
		]);

		Assert.Contains(@"moved to `C:\Reverted\one` and `C:\Reverted\two`.", text, StringComparison.Ordinal);
	}
}
