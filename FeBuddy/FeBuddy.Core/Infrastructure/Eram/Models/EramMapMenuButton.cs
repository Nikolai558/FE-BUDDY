namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>
/// One button on a map menu: a <c>MapBCGButton</c> or a <c>MapFilterButton</c>. Values are kept as
/// the file writes them, trimmed.
/// </summary>
/// <param name="Position">Its <c>MenuPosition</c>, or <see langword="null"/> when it has none.</param>
/// <param name="LabelLine1">
/// Its label: a BCG button's <c>Label</c>, or a filter button's <c>LabelLine1</c>;
/// <see langword="null"/> when it has none.
/// </param>
/// <param name="LabelLine2">A filter button's <c>LabelLine2</c>; <see langword="null"/> for a BCG button, or when it has none.</param>
/// <param name="Groups">The groups it switches: its <c>MapBCGGroup</c>s or <c>MapFilterGroup</c>s, in file order.</param>
public sealed record EramMapMenuButton(
	string? Position,
	string? LabelLine1,
	string? LabelLine2,
	IReadOnlyList<string> Groups);
