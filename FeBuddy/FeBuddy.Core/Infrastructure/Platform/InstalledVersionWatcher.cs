using System.Diagnostics;

namespace FeBuddy.Core.Infrastructure.Platform;

/// <summary>
/// Watches the running FE-Buddy's own program file for an update installed while it is open. Windows
/// Installer closes FE-Buddy only in the account that installs; a copy left open in another Windows account
/// keeps running, with the files it hasn't loaded yet already the new version's. Nothing is read until Windows
/// reports a change to the file; once it has been quiet for <see cref="DefaultSettle"/> (an install replaces
/// it in steps), the version on disk is compared with the running one, and <see cref="Updated"/> is raised -
/// once - when they differ.
/// </summary>
public sealed class InstalledVersionWatcher : IDisposable
{
	private readonly string _path;
	private readonly string _fileName;
	private readonly string _runningVersion;
	private readonly TimeSpan _settle;
	private readonly FileSystemWatcher _watcher;
	private readonly Timer _timer;
	private int _raised;

	/// <summary>Starts watching <paramref name="path"/>.</summary>
	/// <param name="path">The running program's own file, e.g. <c>FE-BUDDY.dll</c> in the install folder.</param>
	/// <param name="runningVersion">The running version (<see cref="AppVersion.Current"/>).</param>
	/// <param name="settle">How long the file must be quiet before it is read; <see cref="DefaultSettle"/> when not given.</param>
	/// <exception cref="ArgumentException">The path is blank, or its folder doesn't exist.</exception>
	public InstalledVersionWatcher(string path, string runningVersion, TimeSpan? settle = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		ArgumentNullException.ThrowIfNull(runningVersion);

		_path = Path.GetFullPath(path);
		_fileName = Path.GetFileName(_path);
		_runningVersion = runningVersion;
		_settle = settle ?? DefaultSettle;
		_timer = new Timer(_ => Check());
		_watcher = new FileSystemWatcher(Path.GetDirectoryName(_path)!)
		{
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
		};
		_watcher.Created += OnChanged;
		_watcher.Changed += OnChanged;
		_watcher.Renamed += OnRenamed;
		_watcher.Error += (_, _) => Schedule();
		_watcher.EnableRaisingEvents = true;
	}

	/// <summary>How long the file must be quiet before it is read: an install writes it, and the files around it, in steps.</summary>
	public static TimeSpan DefaultSettle { get; } = TimeSpan.FromSeconds(3);

	/// <summary>Raised once, on a thread-pool thread, with the version now on disk, when it differs from the running one.</summary>
	public event EventHandler<string>? Updated;

	/// <summary>The FE-Buddy version a file holds: its product version, as <see cref="AppVersion"/> reads the running one.</summary>
	/// <param name="path">The file.</param>
	/// <returns>The version, or <see langword="null"/> when the file isn't there, can't be read, or has none.</returns>
	public static string? VersionOf(string path)
	{
		try
		{
			return File.Exists(path) ? AppVersion.Clean(FileVersionInfo.GetVersionInfo(path).ProductVersion) : null;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return null;
		}
	}

	/// <summary>The version on disk when it isn't the running one (an update, or a rollback to an older one).</summary>
	/// <returns>That version, or <see langword="null"/> when the file is still the running version or can't be read.</returns>
	public string? ChangedVersion() =>
		VersionOf(_path) is { } onDisk && !onDisk.Equals(_runningVersion, StringComparison.OrdinalIgnoreCase) ? onDisk : null;

	/// <inheritdoc />
	public void Dispose()
	{
		_watcher.Dispose();
		_timer.Dispose();
	}

	private void OnChanged(object sender, FileSystemEventArgs e)
	{
		if (IsWatched(e.Name))
		{
			Schedule();
		}
	}

	// Windows Installer moves a file in use out of the way and writes the new one in its place.
	private void OnRenamed(object sender, RenamedEventArgs e)
	{
		if (IsWatched(e.Name) || IsWatched(e.OldName))
		{
			Schedule();
		}
	}

	private bool IsWatched(string? name) => string.Equals(name, _fileName, StringComparison.OrdinalIgnoreCase);

	private void Schedule()
	{
		if (Volatile.Read(ref _raised) != 0)
		{
			return;
		}

		try
		{
			_timer.Change(_settle, Timeout.InfiniteTimeSpan);
		}
		catch (ObjectDisposedException)
		{
			// Stopped meanwhile.
		}
	}

	private void Check()
	{
		if (ChangedVersion() is { } version && Interlocked.Exchange(ref _raised, 1) == 0)
		{
			_watcher.EnableRaisingEvents = false;
			Updated?.Invoke(this, version);
		}
	}
}
