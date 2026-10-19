using System.IO;
using System.Windows.Threading;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// Saves one node's settings a moment after the last change, off the UI thread - for settings that change
/// with every click or drag, such as the map's (which layers are on, the opacity, the files picked).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Set"/> says a key changed, with how to read its value; once nothing has changed for
/// <see cref="Delay"/>, the values are read on the UI thread, from what the page shows, and saved together
/// with <see cref="UserConfigFile.SaveValues"/> on the thread pool, one save after another. Only the keys
/// that changed are written. A save still waiting when FE-Buddy closes is made then.
/// </para>
/// <para>
/// A failed save is logged and the user told, as <see cref="SettingsWrites"/> does; the page keeps what the
/// user chose, so the next change tries again.
/// </para>
/// </remarks>
internal sealed class DeferredSave
{
	/// <summary>How long after the last change the save is made.</summary>
	internal static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(400);

	private readonly string _nodePath;
	private readonly string _what;
	private readonly Dispatcher _dispatcher;
	private readonly DispatcherTimer _timer;
	private readonly Dictionary<string, Func<string>> _changed = new(StringComparer.Ordinal);
	private Task _saving = Task.CompletedTask;

	/// <summary>Creates the save for one node.</summary>
	/// <param name="nodePath">The node, e.g. <c>Services.MapService</c>.</param>
	/// <param name="what">What is being saved, for a failure's message, e.g. <c>map settings</c>.</param>
	/// <param name="dispatcher">The UI thread's dispatcher.</param>
	internal DeferredSave(string nodePath, string what, Dispatcher dispatcher)
	{
		_nodePath = nodePath;
		_what = what;
		_dispatcher = dispatcher;
		_timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher) { Interval = Delay };
		_timer.Tick += (_, _) => _ = SaveNowAsync();
		dispatcher.ShutdownStarted += (_, _) => SaveBeforeClosing();
	}

	/// <summary>Whether a change is waiting to be saved.</summary>
	internal bool IsWaiting => _changed.Count > 0;

	/// <summary>Says a key changed; it is saved once nothing has changed for <see cref="Delay"/>.</summary>
	/// <param name="key">The key, under the node.</param>
	/// <param name="value">Reads its value from the page, when the save is made.</param>
	internal void Set(string key, Func<string> value)
	{
		_changed[key] = value;
		_timer.Stop();
		_timer.Start();
	}

	/// <summary>Drops the changes waiting: the settings were read again from disk (another profile, an import).</summary>
	internal void Forget()
	{
		_timer.Stop();
		_changed.Clear();
	}

	/// <summary>Saves the changes waiting now, rather than once the changes stop.</summary>
	/// <returns><see langword="true"/> when they were saved, or there were none.</returns>
	internal Task<bool> SaveNowAsync()
	{
		_timer.Stop();

		if (TakeChanges() is not { } values)
		{
			return Task.FromResult(true);
		}

		string profile = UserConfigFile.ActiveProfile;
		Task<bool> save = _saving.ContinueWith(_ => Save(values, profile), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
		_saving = save;
		return save;
	}

	/// <summary>The changes' values, read from the page on the UI thread; <see langword="null"/> for none.</summary>
	private Dictionary<string, string>? TakeChanges()
	{
		if (_changed.Count == 0)
		{
			return null;
		}

		Dictionary<string, string> values = _changed.ToDictionary(change => change.Key, change => change.Value(), StringComparer.Ordinal);
		_changed.Clear();
		return values;
	}

	private bool Save(Dictionary<string, string> values, string profile)
	{
		try
		{
			// Quietly: the activity log isn't the place for every map click.
			UserConfigFile.SaveValues(_nodePath, values, profile, quietly: true);
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Error("UserConfig", $"Could not save the {_what}: {ex.Message}");
			_dispatcher.BeginInvoke(() => Toast.Error($"Could not save the {_what}",
				$"{ex.Message} Close anything that may have FE-Buddy's settings open, such as a sync tool, then change it again."));
			return false;
		}
	}

	/// <summary>FE-Buddy is closing: the save running finishes, and a change still waiting is saved.</summary>
	private void SaveBeforeClosing()
	{
		_timer.Stop();
		_saving.Wait(TimeSpan.FromSeconds(5));

		if (TakeChanges() is { } values)
		{
			Save(values, UserConfigFile.ActiveProfile);
		}
	}
}
