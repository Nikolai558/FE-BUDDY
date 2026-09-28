using System;
using System.Collections.Generic;
using System.Linq;

using Semver;

namespace FeBuddy.Versioning;

/// <summary>
/// The release-numbering rule the release pre-flight enforces: a new release must be exactly one
/// step after the latest published one, so a typo such as <c>3.0.0-alpha.4</c> after
/// <c>3.0.0-alpha.1</c> cannot ship. See docs/Developers/RELEASING.md.
/// </summary>
/// <remarks>
/// After a stable <c>X.Y.Z</c>, the next release is the next patch, minor or major version -
/// stable, or its first <c>-alpha.1</c>, <c>-beta.1</c> or <c>-rc.1</c>. After a pre-release
/// <c>X.Y.Z-label.N</c> it is the same label's <c>N+1</c>, a later label's <c>.1</c>, or the
/// stable <c>X.Y.Z</c> itself. Stages may be skipped (alpha straight to rc); numbers may not.
/// </remarks>
public static class ReleaseStep
{
	/// <summary>The pre-release labels a release may carry, lowest first.</summary>
	public static IReadOnlyList<string> PrereleaseLabels { get; } = ["alpha", "beta", "rc"];

	/// <summary>
	/// Is <paramref name="version"/> something FE-Buddy may be released as: <c>X.Y.Z</c>, or
	/// <c>X.Y.Z-alpha.N</c> / <c>-beta.N</c> / <c>-rc.N</c> with <c>N</c> of 1 or more, and no
	/// <c>+metadata</c>? A development version such as <c>3.0.0-dev</c> is not.
	/// </summary>
	/// <param name="version">The version.</param>
	/// <returns><see langword="true"/> if it is release-shaped.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="version"/> is <see langword="null"/>.</exception>
	public static bool IsReleaseShaped(ProductVersion version)
	{
		if (version is null)
		{
			throw new ArgumentNullException(nameof(version));
		}

		SemVersion semVersion = version.SemVersion;

		if (semVersion.MetadataIdentifiers.Count > 0)
		{
			return false;
		}

		if (!semVersion.IsPrerelease)
		{
			return true;
		}

		return semVersion.PrereleaseIdentifiers.Count == 2
			&& PrereleaseLabels.Contains(semVersion.PrereleaseIdentifiers[0].Value)
			&& semVersion.PrereleaseIdentifiers[1].NumericValue is { } number
			&& number >= 1;
	}

	/// <summary>Every version that may be released next after <paramref name="previous"/>, lowest first.</summary>
	/// <param name="previous">The latest published release.</param>
	/// <returns>The allowed next versions.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="previous"/> is <see langword="null"/>.</exception>
	public static IReadOnlyList<ProductVersion> AllowedNext(ProductVersion previous)
	{
		if (previous is null)
		{
			throw new ArgumentNullException(nameof(previous));
		}

		SemVersion semVersion = previous.SemVersion;
		List<string> candidates = [];

		if (semVersion.IsPrerelease)
		{
			string core = $"{semVersion.Major}.{semVersion.Minor}.{semVersion.Patch}";

			if (IsReleaseShaped(previous))
			{
				candidates.Add($"{core}-{semVersion.PrereleaseIdentifiers[0].Value}.{semVersion.PrereleaseIdentifiers[1].NumericValue + 1}");
			}

			// A later label's first release (earlier labels drop out below, as lower than previous).
			candidates.AddRange(PrereleaseLabels.Select(label => $"{core}-{label}.1"));
			candidates.Add(core);
		}
		else
		{
			string[] cores =
			[
				$"{semVersion.Major}.{semVersion.Minor}.{semVersion.Patch + 1}",
				$"{semVersion.Major}.{semVersion.Minor + 1}.0",
				$"{semVersion.Major + 1}.0.0",
			];

			foreach (string core in cores)
			{
				candidates.AddRange(PrereleaseLabels.Select(label => $"{core}-{label}.1"));
				candidates.Add(core);
			}
		}

		return [.. candidates
			.Select(ProductVersion.Parse)
			.Where(candidate => candidate.ComparePrecedenceTo(previous) > 0)
			.OrderBy(candidate => candidate.SemVersion, SemVersion.PrecedenceComparer)];
	}

	/// <summary>Is <paramref name="next"/> an allowed release right after <paramref name="previous"/>?</summary>
	/// <param name="previous">The latest published release, or <see langword="null"/> when there is none.</param>
	/// <param name="next">The version about to be released.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="next"/> is release-shaped and one of
	/// <see cref="AllowedNext"/> - or, with nothing released yet, release-shaped at all.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
	public static bool IsNextStep(ProductVersion? previous, ProductVersion next)
	{
		if (!IsReleaseShaped(next))
		{
			return false;
		}

		return previous is null
			|| AllowedNext(previous).Any(allowed => allowed.ComparePrecedenceTo(next) == 0);
	}

	/// <summary>The highest version among <paramref name="tags"/>, skipping any that are not SemVer.</summary>
	/// <param name="tags">Release tags, e.g. <c>2.9.3</c> or <c>v2.2.0</c> (see <see cref="ProductVersion.TryParseTag"/>).</param>
	/// <returns>The highest version, or <see langword="null"/> when none parse.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="tags"/> is <see langword="null"/>.</exception>
	public static ProductVersion? Latest(IEnumerable<string?> tags)
	{
		if (tags is null)
		{
			throw new ArgumentNullException(nameof(tags));
		}

		ProductVersion? latest = null;

		foreach (string? tag in tags)
		{
			if (ProductVersion.TryParseTag(tag, out ProductVersion? version)
				&& (latest is null || version!.ComparePrecedenceTo(latest) > 0))
			{
				latest = version;
			}
		}

		return latest;
	}
}
