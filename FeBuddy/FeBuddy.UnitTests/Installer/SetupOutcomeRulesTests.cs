using FeBuddy.Installer.UI.Setup;

namespace FeBuddy.UnitTests.Installer;

/// <summary>
/// Covers <see cref="SetupOutcomeRules"/>, which decides the setup window's last page: what Windows
/// Installer said, and, when it ended without a result (the administrator prompt declined), what is
/// installed now, so a declined prompt never reads as "up to date" or "removed".
/// </summary>
public sealed class SetupOutcomeRulesTests
{
	private const string Installed = "3.0.0-beta.12";
	private const string Wanted = "3.0.0-beta.13";

	private static SetupOutcome Decide(
		int installResult,
		bool cancelSent = false,
		bool fatalExit = false,
		bool removing = false,
		string? before = Installed,
		string? after = Installed) =>
		SetupOutcomeRules.Decide(cancelSent, fatalExit, installResult, removing, before, Wanted, () => after);

	[Theory]
	[InlineData(SetupOutcomeRules.ResultSuccess, SetupOutcome.Succeeded)]
	[InlineData(SetupOutcomeRules.ResultUserExit, SetupOutcome.Cancelled)]
	[InlineData(SetupOutcomeRules.ResultFailure, SetupOutcome.Failed)]
	[InlineData(SetupOutcomeRules.ResultNone, SetupOutcome.Failed)]
	public void what_windows_installer_reports_decides(int result, SetupOutcome expected) =>
		Assert.Equal(expected, Decide(result));

	[Fact]
	public void a_cancel_or_a_fatal_error_wins_over_the_result()
	{
		Assert.Equal(SetupOutcome.Cancelled, Decide(SetupOutcomeRules.ResultSuccess, cancelSent: true));
		Assert.Equal(SetupOutcome.Failed, Decide(SetupOutcomeRules.ResultSuccess, fatalExit: true));
	}

	/// <summary>The declined prompt: no result, and nothing installed changed. Neither an update, an install nor an uninstall reads as done.</summary>
	[Fact]
	public void no_result_and_nothing_changed_did_not_run()
	{
		Assert.Equal(SetupOutcome.NotStarted, Decide(SetupOutcomeRules.ResultNotSent));
		Assert.Equal(SetupOutcome.NotStarted, Decide(SetupOutcomeRules.ResultNotSent, before: null, after: null));
		Assert.Equal(SetupOutcome.NotStarted, Decide(SetupOutcomeRules.ResultNotSent, removing: true));
	}

	/// <summary>Should a successful installation ever end without a result, what is installed now still shows it.</summary>
	[Fact]
	public void no_result_but_the_change_is_on_the_machine_succeeded()
	{
		Assert.Equal(SetupOutcome.Succeeded, Decide(SetupOutcomeRules.ResultNotSent, after: Wanted));
		Assert.Equal(SetupOutcome.Succeeded, Decide(SetupOutcomeRules.ResultNotSent, before: null, after: Wanted));
		Assert.Equal(SetupOutcome.Succeeded, Decide(SetupOutcomeRules.ResultNotSent, removing: true, after: null));
	}

	/// <summary>Reinstalling or repairing the same version can't be told from nothing happening.</summary>
	[Fact]
	public void no_result_for_the_same_version_did_not_run()
	{
		Assert.Equal(SetupOutcome.NotStarted, Decide(SetupOutcomeRules.ResultNotSent, before: Wanted, after: Wanted));
	}

	[Fact]
	public void the_machine_is_read_only_when_there_is_no_result()
	{
		bool read = false;

		SetupOutcomeRules.Decide(false, false, SetupOutcomeRules.ResultSuccess, false, Installed, Wanted, () =>
		{
			read = true;
			return null;
		});

		Assert.False(read);
	}
}
