using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Every <see cref="IConfigPage"/> built so far this session. A settings import, or a switch to
/// another settings profile, uses it to warn about unsaved edits it would throw away, then to have
/// each page re-read the settings.
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
	/// Works on a copy of the list, so a page that registers while the others reload (and reads the
	/// new file itself as it is built) cannot upset the loop. One page failing to reload is logged,
	/// not allowed to stop the rest, and named in the result so the user can be told.
	/// </remarks>
	/// <returns>
	/// The names of the pages that could not reload. They still show the settings from before, and
	/// saving on one would write those back over the new ones.
	/// </returns>
	public static IReadOnlyList<string> ReloadAll()
	{
		List<string> failed = [];

		foreach (IConfigPage page in Live())
		{
			try
			{
				page.ReloadFromConfig();
			}
			catch (Exception ex)
			{
				AppLog.Warning("Settings", $"Could not reload '{page.ConfigPageName}' after the settings changed: {ex.Message}");
				failed.Add(page.ConfigPageName);
			}
		}

		return [.. failed.Distinct(StringComparer.Ordinal)];
	}

	/// <summary>
	/// Has everything that read <c>UserConfig</c> read it again, after an import or a switch to another
	/// settings profile: the app-wide GeoJSON options, every page (<see cref="ReloadAll"/>), the Map's
	/// layers and the default ROI.
	/// </summary>
	/// <returns>The names of the pages that could not reload, as <see cref="ReloadAll"/> gives them.</returns>
	public static IReadOnlyList<string> ReloadEverything()
	{
		OutputFormatting.LoadFromUserConfig();
		IReadOnlyList<string> notReloaded = ReloadAll();
		MapLayersState.ReloadFromConfigIfCreated();
		DefaultRoiStore.NotifyReloaded();
		return notReloaded;
	}

	private static List<IConfigPage> Live() =>
		[.. Pages.Select(p => p.TryGetTarget(out IConfigPage? page) ? page : null).OfType<IConfigPage>()];
}
