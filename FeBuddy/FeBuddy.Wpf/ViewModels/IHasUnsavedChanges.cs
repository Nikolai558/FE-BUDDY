namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// A page that can hold edits that are not saved yet. Its side-nav row shows the amber unsaved
/// dot while <see cref="HasUnsavedChanges"/> is <see langword="true"/> (see <see cref="NavItem"/>).
/// </summary>
/// <remarks>
/// The page must raise <c>PropertyChanged</c> for <see cref="HasUnsavedChanges"/> whenever it
/// changes; the nav row listens for it.
/// </remarks>
public interface IHasUnsavedChanges
{
	/// <summary>Whether the page has edits that are not saved yet.</summary>
	bool HasUnsavedChanges { get; }
}
