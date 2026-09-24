namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>A titled group of <see cref="ServicePreviewRow"/> on the Preview Settings tab.</summary>
/// <param name="Title">The group heading, usually the tab's title.</param>
/// <param name="Rows">The rows, in display order.</param>
/// <param name="Note">Optional line under the heading, e.g. why a tab contributes nothing.</param>
public sealed record ServicePreviewSection(string Title, IReadOnlyList<ServicePreviewRow> Rows, string? Note = null);
