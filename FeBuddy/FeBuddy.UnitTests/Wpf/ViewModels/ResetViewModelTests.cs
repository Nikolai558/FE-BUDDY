using System.ComponentModel;

using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Infrastructure.FileSystem.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="ResetViewModel"/>: keeping is the default for the settings and the
/// credentials, a copy is offered only while the settings are being deleted, a choice about
/// something that isn't there changes nothing, and the request says what the user picked.
/// </summary>
public sealed class ResetViewModelTests
{
	[Fact]
	public void by_default_the_reset_keeps_the_settings_and_credentials()
	{
		ResetViewModel reset = new(hasSettings: true, credentialCount: 2, unfinishedWork: []);

		Assert.True(reset.KeepSettings);
		Assert.False(reset.DeleteSettings);
		Assert.False(reset.DeletesSettings);
		Assert.True(reset.SaveCopyFirst);
		Assert.False(reset.SavesCopy);
		Assert.True(reset.KeepCredentials);
		Assert.Equal(new AppDataResetRequest(KeepSettings: true, DeleteCredentials: false, WaitForProcessId: 42), reset.BuildRequest(42));
	}

	[Fact]
	public void deleting_the_settings_offers_a_copy_first()
	{
		ResetViewModel reset = new(hasSettings: true, credentialCount: 0, unfinishedWork: []);
		List<string?> changed = [];
		((INotifyPropertyChanged)reset).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		reset.DeleteSettings = true;

		Assert.False(reset.KeepSettings);
		Assert.True(reset.DeletesSettings);
		Assert.True(reset.SavesCopy);
		Assert.False(reset.BuildRequest(1).KeepSettings);
		Assert.Contains(nameof(ResetViewModel.DeleteSettings), changed);
		Assert.Contains(nameof(ResetViewModel.SavesCopy), changed);

		reset.SaveCopyFirst = false;

		Assert.False(reset.SavesCopy);
	}

	[Fact]
	public void deleting_the_credentials_is_in_the_request()
	{
		ResetViewModel reset = new(hasSettings: true, credentialCount: 3, unfinishedWork: []) { DeleteCredentials = true };

		Assert.True(reset.HasCredentials);
		Assert.Equal("Saved Credentials (3)", reset.CredentialsHeader);
		Assert.False(reset.KeepCredentials);
		Assert.True(reset.BuildRequest(1).DeleteCredentials);
	}

	/// <summary>With no settings file or no credentials, those sections are hidden and nothing is deleted for them.</summary>
	[Fact]
	public void a_choice_about_something_not_there_changes_nothing()
	{
		ResetViewModel reset = new(hasSettings: false, credentialCount: -1, unfinishedWork: [])
		{
			DeleteSettings = true,
			DeleteCredentials = true,
		};

		Assert.False(reset.DeletesSettings);
		Assert.False(reset.SavesCopy);
		Assert.False(reset.HasCredentials);
		Assert.Equal(0, reset.CredentialCount);
		Assert.Equal(new AppDataResetRequest(KeepSettings: true, DeleteCredentials: false, WaitForProcessId: 7), reset.BuildRequest(7));
	}

	[Fact]
	public void unfinished_work_is_shown()
	{
		Assert.False(new ResetViewModel(true, 0, []).HasUnfinishedWork);
		Assert.True(new ResetViewModel(true, 0, ["Settings has unsaved changes."]).HasUnfinishedWork);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void either_button_closes_the_window_and_says_which(bool confirm)
	{
		ResetViewModel reset = new(hasSettings: true, credentialCount: 0, unfinishedWork: []);
		int closes = 0;
		reset.CloseRequested += (_, _) => closes++;

		(confirm ? reset.ConfirmCommand : reset.CancelCommand).Execute(null);

		Assert.Equal(1, closes);
		Assert.Equal(confirm, reset.Confirmed);
	}

	[Fact]
	public void every_reset_lists_what_always_goes()
	{
		Assert.Equal(4, ResetViewModel.AlwaysDeleted.Count);
		Assert.Throws<ArgumentNullException>(() => new ResetViewModel(true, 0, null!));
	}
}
