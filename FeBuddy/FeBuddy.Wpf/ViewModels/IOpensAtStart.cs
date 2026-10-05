namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// A page with places inside it - tabs, or sub-pages - that opens at its start each time it is
/// chosen in the side nav, rather than where the user left it (see <see cref="NavItem"/>).
/// </summary>
/// <remarks>
/// Only the place is reset: the page keeps its settings and unsaved edits, which live on its tabs.
/// </remarks>
public interface IOpensAtStart
{
	/// <summary>Goes back to the page's start: its first tab, or its main page.</summary>
	void ReturnToStart();
}
