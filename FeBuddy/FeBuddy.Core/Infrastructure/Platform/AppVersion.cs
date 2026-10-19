using System.Reflection;

namespace FeBuddy.Core.Infrastructure.Platform;

/// <summary>
/// The running FE-Buddy's real version - the csproj <c>&lt;Version&gt;</c>, e.g. <c>3.0.0</c> or
/// <c>3.0.0-alpha.1</c> - read from the entry assembly's <see cref="AssemblyInformationalVersionAttribute"/>
/// (Explorer's "Product version").
/// </summary>
/// <remarks>
/// Not <c>AssemblyName.Version</c>: that is numeric-only (<c>3.0.0.0</c>), drops any pre-release
/// tag, and does not compare correctly against a release tag. The installer records this same
/// string as <c>ProductSemVer</c>, because the build reads it from the built exe (see
/// docs/Developers/VERSIONING.md).
/// </remarks>
public static class AppVersion
{
	/// <summary>The entry assembly's real version, or <c>dev</c> when there is no entry assembly.</summary>
	public static string Current => Of(Assembly.GetEntryAssembly());

	/// <summary>The real version of <paramref name="assembly"/>.</summary>
	/// <param name="assembly">The assembly to read, or <see langword="null"/>.</param>
	/// <returns>
	/// The informational version without SemVer build metadata (anything after <c>+</c>, such as a
	/// commit hash); else the numeric version's first three parts; else <c>dev</c>.
	/// </returns>
	public static string Of(Assembly? assembly)
	{
		if (assembly is null)
		{
			return "dev";
		}

		return Clean(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion)
			?? assembly.GetName().Version?.ToString(3)
			?? "dev";
	}

	/// <summary>
	/// An informational version (or a file's product version, which is the same) without its SemVer build
	/// metadata: anything after <c>+</c>, such as a commit hash.
	/// </summary>
	/// <param name="informational">The version as written in the file.</param>
	/// <returns>The real version, or <see langword="null"/> when there is none.</returns>
	internal static string? Clean(string? informational)
	{
		if (string.IsNullOrWhiteSpace(informational))
		{
			return null;
		}

		int metadata = informational.IndexOf('+', StringComparison.Ordinal);
		return (metadata >= 0 ? informational[..metadata] : informational).Trim();
	}
}
