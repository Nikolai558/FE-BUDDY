using System.Globalization;
using FeBuddy.Core.Application.Updates.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One release's section in the update window: its version header and its notes.</summary>
public sealed class ReleaseNotesItem
{
	/// <summary>Creates the section for <paramref name="release"/>.</summary>
	/// <param name="release">The release.</param>
	/// <param name="isFirst"><see langword="true"/> for the top section, which has no divider above it.</param>
	public ReleaseNotesItem(ReleaseSummary release, bool isFirst)
	{
		ArgumentNullException.ThrowIfNull(release);

		Version = "v" + release.Version;
		PublishedOn = release.PublishedAt?.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.CurrentCulture);
		IsPrerelease = release.IsPrerelease;
		Notes = string.IsNullOrWhiteSpace(release.Notes) ? "No release notes were provided for this version." : release.Notes;
		IsFirst = isFirst;
	}

	/// <summary>The version header, e.g. <c>v2.9.0</c>.</summary>
	public string Version { get; }

	/// <summary>The publish date, e.g. <c>Aug 30, 2026</c>, or <see langword="null"/> when unknown.</summary>
	public string? PublishedOn { get; }

	/// <summary><see langword="true"/> for an alpha or beta release (shows a "Pre-release" chip).</summary>
	public bool IsPrerelease { get; }

	/// <summary>The release notes (Markdown).</summary>
	public string Notes { get; }

	/// <summary><see langword="true"/> for the newest release, which has no divider above it.</summary>
	public bool IsFirst { get; }
}
