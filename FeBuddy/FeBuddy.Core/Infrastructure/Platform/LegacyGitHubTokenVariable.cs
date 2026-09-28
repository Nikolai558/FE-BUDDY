using System.Security;

using Microsoft.Win32;

namespace FeBuddy.Core.Infrastructure.Platform;

/// <summary>
/// FE-Buddy 2.x's <c>FEBUDDY_GITHUB_TOKEN</c> environment variable. 2.x told users to keep a GitHub
/// token in it (<c>setx FEBUDDY_GITHUB_TOKEN …</c>), where Windows stores it as plain text that every
/// program the user runs can read. FE-Buddy 3 never uses it - its tokens live in Windows Credential
/// Manager - so it only finds out whether the variable is still set, to tell the user.
/// </summary>
/// <remarks>
/// Only the names of the variables Windows keeps in the registry are read - the user's under
/// <c>HKCU\Environment</c>, the whole PC's under <see cref="MachineKeyPath"/> - never the variable's
/// value. <see cref="Environment.GetEnvironmentVariable(string)"/> is not used for that reason.
/// </remarks>
public static class LegacyGitHubTokenVariable
{
	/// <summary>The variable's name.</summary>
	public const string Name = "FEBUDDY_GITHUB_TOKEN";

	/// <summary>Where Windows keeps this user's environment variables, under <c>HKEY_CURRENT_USER</c>.</summary>
	internal const string UserKeyPath = "Environment";

	/// <summary>Where Windows keeps the whole PC's environment variables, under <c>HKEY_LOCAL_MACHINE</c>.</summary>
	internal const string MachineKeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

	/// <summary>Where the variable is set: for this Windows user, for everyone on this PC, or both.</summary>
	/// <returns><see cref="EnvironmentVariableTarget.User"/> and/or <see cref="EnvironmentVariableTarget.Machine"/>; empty when it is not set.</returns>
	public static IReadOnlyList<EnvironmentVariableTarget> FindTargets() =>
		FindTargets(Registry.CurrentUser, UserKeyPath, Registry.LocalMachine, MachineKeyPath);

	/// <summary><see cref="FindTargets()"/> over other registry keys. Unit tests only.</summary>
	/// <param name="userRoot">The root holding the user's variables.</param>
	/// <param name="userPath">The key under <paramref name="userRoot"/> holding them.</param>
	/// <param name="machineRoot">The root holding the whole PC's variables.</param>
	/// <param name="machinePath">The key under <paramref name="machineRoot"/> holding them.</param>
	/// <returns>Where the variable is set.</returns>
	internal static IReadOnlyList<EnvironmentVariableTarget> FindTargets(RegistryKey userRoot, string userPath, RegistryKey machineRoot, string machinePath)
	{
		List<EnvironmentVariableTarget> found = [];

		if (IsNamedIn(userRoot, userPath))
		{
			found.Add(EnvironmentVariableTarget.User);
		}

		if (IsNamedIn(machineRoot, machinePath))
		{
			found.Add(EnvironmentVariableTarget.Machine);
		}

		return found;
	}

	/// <summary>
	/// Whether the key holds a value named <see cref="Name"/>, going by the names alone. Environment
	/// variable names are not case-sensitive. A key that is missing or cannot be read counts as not.
	/// </summary>
	private static bool IsNamedIn(RegistryKey root, string path)
	{
		try
		{
			using RegistryKey? key = root.OpenSubKey(path, writable: false);
			return key is not null && key.GetValueNames().Contains(Name, StringComparer.OrdinalIgnoreCase);
		}
		catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
		{
			return false;
		}
	}
}
