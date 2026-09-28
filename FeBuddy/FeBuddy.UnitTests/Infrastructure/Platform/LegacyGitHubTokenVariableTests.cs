using Microsoft.Win32;

using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.UnitTests.Infrastructure.Platform;

/// <summary>
/// Covers <see cref="LegacyGitHubTokenVariable"/> against throwaway registry keys under
/// <c>HKCU\Software</c> standing in for the user's and the PC's environment (and once against the
/// real ones, read-only).
/// </summary>
public sealed class LegacyGitHubTokenVariableTests : IDisposable
{
	private readonly string _root = @"Software\FeBuddyTests_LegacyToken_" + Guid.NewGuid().ToString("N");

	private string UserPath => _root + @"\User";

	private string MachinePath => _root + @"\Machine";

	/// <summary>Deletes the throwaway keys.</summary>
	public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(_root, throwOnMissingSubKey: false);

	/// <summary>The variable is found where it is set - the user's variables, the PC's, or both - whatever its case.</summary>
	[Theory]
	[InlineData(null, null, new EnvironmentVariableTarget[0])]
	[InlineData("FEBUDDY_GITHUB_TOKEN", null, new[] { EnvironmentVariableTarget.User })]
	[InlineData(null, "febuddy_github_token", new[] { EnvironmentVariableTarget.Machine })]
	[InlineData("FeBuddy_GitHub_Token", "FEBUDDY_GITHUB_TOKEN", new[] { EnvironmentVariableTarget.User, EnvironmentVariableTarget.Machine })]
	public void finds_the_variable_where_it_is_set(string? userName, string? machineName, EnvironmentVariableTarget[] expected)
	{
		Plant(UserPath, userName);
		Plant(MachinePath, machineName);

		Assert.Equal(expected, Find());
	}

	/// <summary>Other variables, and a key that is not there at all, find nothing.</summary>
	[Fact]
	public void other_variables_or_missing_keys_find_nothing()
	{
		Plant(UserPath, "FEBUDDY_GITHUB_TOKEN_OLD");

		Assert.Empty(Find());
		Assert.Empty(LegacyGitHubTokenVariable.FindTargets(Registry.CurrentUser, _root + @"\Missing", Registry.CurrentUser, _root + @"\AlsoMissing"));
	}

	/// <summary>The real lookup reads Windows' own keys without failing, whatever this PC holds.</summary>
	[Fact]
	public void the_real_lookup_reads_windows_keys() =>
		Assert.All(LegacyGitHubTokenVariable.FindTargets(), t => Assert.True(t is EnvironmentVariableTarget.User or EnvironmentVariableTarget.Machine));

	private IReadOnlyList<EnvironmentVariableTarget> Find() =>
		LegacyGitHubTokenVariable.FindTargets(Registry.CurrentUser, UserPath, Registry.CurrentUser, MachinePath);

	/// <summary>Creates the key with an unrelated variable, plus one named <paramref name="name"/> when given.</summary>
	private static void Plant(string path, string? name)
	{
		using RegistryKey key = Registry.CurrentUser.CreateSubKey(path);
		key.SetValue("PATH", "not it");

		if (name is not null)
		{
			key.SetValue(name, "a value the lookup never reads");
		}
	}
}
