using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Every <see cref="IConfigPage"/> built so far this session. A settings import uses it to warn
/// about unsaved edits it would throw away, then to have each page re-read the imported file.
/// </summary>
/// <remarks>
/// Pages add themselves when they are built. They are held weakly, so a page nothing else keeps
/// is not kept alive by this list. UI thread only.
/// </remarks>
public static class ConfigPages
{
	private static readonly List<WeakReference<IConfigPage>> Pages = [];

	/// <summary>Adds a page. Call from its constructor.</summary>
	/// <param name="page">The page.</param>
	public static void Register(IConfigPage page)
	{
		Pages.RemoveAll(p => !p.TryGetTarget(out _));
		Pages.Add(new WeakReference<IConfigPage>(page));
	}

	/// <summary>The names of the pages that have unsaved edits.</summary>
	/// <returns>The names, in the order the pages were built.</returns>
	public static IReadOnlyList<string> WithUnsavedChanges() =>
		[.. Live().Where(p => p.IsDirty).Select(p => p.ConfigPageName).Distinct(StringComparer.Ordinal)];

	/// <summary>Has every page re-read <c>UserConfig</c>, dropping unsaved edits.</summary>
	/// <remarks>
	/// Works on a copy of the list: a page that reloads can open tabs, which register as they are
	/// built (and read the new file themselves). One page failing to reload is logged, not allowed
	/// to stop the rest.
	/// </remarks>
	public static void ReloadAll()
	{
		foreach (IConfigPage page in Live())
		{
			try
			{
				page.ReloadFromConfig();
			}
			catch (Exception ex)
			{
				AppLog.Warning("Settings", $"Could not reload '{page.ConfigPageName}' after the import: {ex.Message}");
			}
		}
	}

	private static List<IConfigPage> Live() =>
		[.. Pages.Select(p => p.TryGetTarget(out IConfigPage? page) ? page : null).OfType<IConfigPage>()];
}
