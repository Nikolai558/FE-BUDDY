namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// A card of the service's own on its Preview Settings tab, above the run button
/// (<see cref="ServicePreviewTabViewModel.RunOptions"/>): a choice about the run itself rather than
/// any one tab. The tab refreshes it each time it opens.
/// </summary>
public interface IPreviewOptions
{
	/// <summary>Re-reads what the card shows.</summary>
	void Refresh();
}
