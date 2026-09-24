using FeBuddy.Versioning.Models;

using System;

using Semver;

namespace FeBuddy.Versioning;

/// <summary>
/// A parsed, real FE-Buddy release version (e.g. <c>3.0.0</c> or <c>3.0.0-alpha.1</c>): strict
/// SemVer 2.0, compared by SemVer precedence. Wraps <see cref="SemVersion"/> so nothing else
/// hand-parses or hand-compares version strings.
/// </summary>
/// <remarks>
/// This is the version the csproj's <c>&lt;Version&gt;</c> holds, the exe's Product version, the
/// GitHub release tag and the installer's <c>ProductSemVer</c> - never the numeric-only
/// assembly or MSI version, which cannot carry a pre-release tag (see docs/Developers/VERSIONING.md).
/// </remarks>
public sealed class ProductVersion
{
	private ProductVersion(SemVersion semVersion)
	{
		SemVersion = semVersion;
	}

	/// <summary>The underlying SemVer value.</summary>
	public SemVersion SemVersion { get; }

	/// <summary><see langword="true"/> when the version carries a pre-release tag (<c>-alpha.1</c>, <c>-rc.2</c>, ...).</summary>
	public bool IsPrerelease => SemVersion.IsPrerelease;

	/// <summary>
	/// The channel implied by the pre-release tag's first identifier: <c>rc</c> is
	/// <see cref="ReleaseChannel.ReleaseCandidate"/>, <c>beta</c> is <see cref="ReleaseChannel.Beta"/>,
	/// no tag is <see cref="ReleaseChannel.Stable"/>, and <c>alpha</c> - or anything unrecognised -
	/// is <see cref="ReleaseChannel.Alpha"/>, the least trusted channel.
	/// </summary>
	public ReleaseChannel Channel
	{
		get
		{
			if (!IsPrerelease)
			{
				return ReleaseChannel.Stable;
			}

			string tag = SemVersion.PrereleaseIdentifiers[0].Value;

			if (tag.Equals("rc", StringComparison.OrdinalIgnoreCase))
			{
				return ReleaseChannel.ReleaseCandidate;
			}

			if (tag.Equals("beta", StringComparison.OrdinalIgnoreCase))
			{
				return ReleaseChannel.Beta;
			}

			return ReleaseChannel.Alpha;
		}
	}

	/// <summary>Parses a strict SemVer 2.0 version string.</summary>
	/// <param name="versionText">The version, e.g. <c>3.0.0-beta.2</c>. No leading <c>v</c>.</param>
	/// <returns>The parsed version.</returns>
	/// <exception cref="FormatException"><paramref name="versionText"/> is not strict SemVer 2.0.</exception>
	public static ProductVersion Parse(string versionText)
	{
		if (string.IsNullOrWhiteSpace(versionText))
		{
			throw new FormatException("Version string is null or empty.");
		}

		return new ProductVersion(SemVersion.Parse(versionText, SemVersionStyles.Strict));
	}

	/// <summary>Parses a strict SemVer 2.0 version string without throwing.</summary>
	/// <param name="versionText">The version, e.g. <c>3.0.0-beta.2</c>. No leading <c>v</c>.</param>
	/// <param name="version">The parsed version, or <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="versionText"/> parsed.</returns>
	public static bool TryParse(string? versionText, out ProductVersion? version)
	{
		version = null;

		if (string.IsNullOrWhiteSpace(versionText)
			|| !SemVersion.TryParse(versionText, SemVersionStyles.Strict, out SemVersion? parsed))
		{
			return false;
		}

		version = new ProductVersion(parsed!);
		return true;
	}

	/// <summary>
	/// Parses a GitHub release tag: strict SemVer, optionally prefixed with <c>v</c> or <c>V</c>
	/// (older FE-Buddy tags such as <c>V2.2.0</c> have one), with surrounding whitespace ignored.
	/// </summary>
	/// <param name="tag">The tag, e.g. <c>3.0.0-rc.1</c> or <c>v2.9.0</c>.</param>
	/// <param name="version">The parsed version, or <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="tag"/> parsed.</returns>
	public static bool TryParseTag(string? tag, out ProductVersion? version)
	{
		string? trimmed = tag?.Trim();
		if (trimmed is { Length: > 1 } && (trimmed[0] == 'v' || trimmed[0] == 'V') && char.IsDigit(trimmed[1]))
		{
			trimmed = trimmed.TrimStart('v', 'V');
		}

		return TryParse(trimmed, out version);
	}

	/// <summary>SemVer 2.0 precedence comparison (build metadata ignored, per the spec).</summary>
	/// <param name="other">The version to compare against.</param>
	/// <returns>Negative if this is lower, zero if equal precedence, positive if higher.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
	public int ComparePrecedenceTo(ProductVersion other)
	{
		if (other is null)
		{
			throw new ArgumentNullException(nameof(other));
		}

		return SemVersion.ComparePrecedenceTo(other.SemVersion);
	}

	/// <summary>The version as written, e.g. <c>3.0.0-alpha.1</c>.</summary>
	/// <returns>The version string.</returns>
	public override string ToString() => SemVersion.ToString();
}
