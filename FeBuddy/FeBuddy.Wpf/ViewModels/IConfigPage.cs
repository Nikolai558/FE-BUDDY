namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// A page whose values come from <c>UserConfig</c> and live for the whole session, so it has to
/// re-read them when a settings import replaces the file underneath it (see <see cref="ConfigPages"/>).
/// </summary>
public interface IConfigPage
{
	/// <summary>The page's name in the "unsaved changes will be lost" warning, e.g. <c>Airways</c>.</summary>
	string ConfigPageName { get; }

	/// <summary>Whether the page has edits that are not saved yet.</summary>
	bool IsDirty { get; }

	/// <summary>Re-reads every value from <c>UserConfig</c>, dropping unsaved edits.</summary>
	void ReloadFromConfig();
}
