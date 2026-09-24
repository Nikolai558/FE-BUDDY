using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>
/// A sub-service run's messages that share one heading - a severity such as <c>Warning</c>, or
/// a subject such as an airway ID - so the Review tab does not show one flat wall of text.
/// </summary>
/// <param name="Title">The heading: the severity or subject these messages share.</param>
/// <param name="Level">The highest severity among them, which orders the groups.</param>
/// <param name="Messages">The message texts, in the order the service emitted them.</param>
public sealed record SubServiceMessageGroup(string Title, LogLevel Level, IReadOnlyList<string> Messages)
{
	/// <summary>How many messages are in this group.</summary>
	public int Count => Messages.Count;
}
