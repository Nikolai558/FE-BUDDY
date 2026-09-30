using System.ComponentModel;

using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="UninstallViewModel"/>: what the uninstall lists as removed, the copy of the
/// settings offered (on by default, only when there are settings), and which button closed it.
/// </summary>
public sealed class UninstallViewModelTests
{
	[Fact]
	public void settings_and_credentials_are_listed_as_removed_when_there_are_some()
	{
		UninstallViewModel uninstall = new(hasSettings: true, credentialCount: 3, unfinishedWork: []);

		Assert.Equal(4, uninstall.Removed.Count);
		Assert.Contains("Your settings and their backups", uninstall.Removed);
		Assert.Contains("Your 3 saved credentials, from Windows Credential Manager", uninstall.Removed);
		Assert.Equal(3, uninstall.CredentialCount);
	}

	[Fact]
	public void one_credential_is_named_in_the_singular()
	{
		UninstallViewModel uninstall = new(hasSettings: true, credentialCount: 1, unfinishedWork: []);

		Assert.Contains("Your saved credential, from Windows Credential Manager", uninstall.Removed);
	}

	/// <summary>With no settings file and no credentials, only FE-Buddy and its data are listed, and no copy is offered.</summary>
	[Fact]
	public void nothing_is_listed_or_copied_that_is_not_there()
	{
		UninstallViewModel uninstall = new(hasSettings: false, credentialCount: -1, unfinishedWork: []);

		Assert.Equal(2, uninstall.Removed.Count);
		Assert.Equal(0, uninstall.CredentialCount);
		Assert.True(uninstall.SaveCopyFirst);
		Assert.False(uninstall.SavesCopy);
	}

	[Fact]
	public void a_copy_of_the_settings_is_saved_first_unless_the_user_says_not()
	{
		UninstallViewModel uninstall = new(hasSettings: true, credentialCount: 0, unfinishedWork: []);
		List<string?> changed = [];
		((INotifyPropertyChanged)uninstall).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		Assert.True(uninstall.SavesCopy);

		uninstall.SaveCopyFirst = false;
		uninstall.SaveCopyFirst = false;

		Assert.False(uninstall.SavesCopy);
		Assert.Equal([nameof(UninstallViewModel.SaveCopyFirst), nameof(UninstallViewModel.SavesCopy)], changed);
	}

	[Fact]
	public void unfinished_work_is_shown()
	{
		Assert.False(new UninstallViewModel(true, 0, []).HasUnfinishedWork);
		Assert.True(new UninstallViewModel(true, 0, ["Settings has unsaved changes."]).HasUnfinishedWork);
		Assert.Throws<ArgumentNullException>(() => new UninstallViewModel(true, 0, null!));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void either_button_closes_the_window_and_says_which(bool confirm)
	{
		UninstallViewModel uninstall = new(hasSettings: true, credentialCount: 0, unfinishedWork: []);
		int closes = 0;
		uninstall.CloseRequested += (_, _) => closes++;

		(confirm ? uninstall.ConfirmCommand : uninstall.CancelCommand).Execute(null);

		Assert.Equal(1, closes);
		Assert.Equal(confirm, uninstall.Confirmed);
	}

	/// <summary>The window always says the output folder, and other Windows accounts' FE-Buddy data, are kept.</summary>
	[Fact]
	public void every_uninstall_lists_what_it_leaves()
	{
		Assert.Equal(2, UninstallViewModel.Kept.Count);
		Assert.Contains(UninstallViewModel.Kept, k => k.Contains("output folder", StringComparison.Ordinal));
		Assert.Contains(UninstallViewModel.Kept, k => k.Contains("other people's Windows accounts", StringComparison.Ordinal));
	}
}
