using System.Diagnostics;

namespace FeBuddy.Core.Infrastructure.Platform;

/// <summary>
/// Watches the running FE-Buddy's own program file for an update installed while it is open. Windows
/// Installer closes FE-Buddy only in the account that installs; a copy left open in another Windows account
/// keeps running, with the files it hasn't loaded yet already the new version's. Nothing is read until Windows
/// reports a change; once things have been quiet for <see cref="DefaultSettle"/> (an install replaces files in
/// steps), the version on disk is compared with the running one, and <see cref="Updated"/> is raised - once -
/// when they differ.
/// </summary>
/// <remarks>
/// An update first uninstalls the old version: Windows Installer moves the files in use out to
/// <c>C:\Config.Msi</c>, deletes the emptied install folder, then creates it again for the new files. A watch
/// on the folder alone would be left on the deleted one, so the folder above it is watched too, and the
/// folder's watch is set up again on whichever folder holds the file at each check. While the file is
/// missing - mid-install - it is looked for again, for up to <see cref="MaxMissingChecks"/> checks.
/// </remarks>
public sealed class InstalledVersionWatcher : IDisposable
{
	/// <summary>How many times in a row a missing file is looked for again before waiting for the next change.</summary>
	internal const int MaxMissingChecks = 100;

	private readonly Lock _gate = new();
	private readonly string _path;
	private readonly string _folder;
	private readonly string _fileName;
	private readonly string _runningVersion;
	private readonly TimeSpan _settle;
	private readonly FileSystemWatcher? _parentWatcher;
	private readonly Timer _timer;
	private FileSystemWatcher? _folderWatcher;
	private int _missingChecks;
	private int _raised;
	private bool _disposed;

	/// <summary>Starts watching <paramref name="path"/>.</summary>
	/// <param name="path">The running program's own file, e.g. <c>FE-BUDDY.dll</c> in the install folder.</param>
	/// <param name="runningVersion">The running version (<see cref="AppVersion.Current"/>).</param>
	/// <param name="settle">How long things must be quiet before the file is read; <see cref="DefaultSettle"/> when not given.</param>
	/// <exception cref="ArgumentException">The path is blank, or its folder doesn't exist.</exception>
	public InstalledVersionWatcher(string path, string runningVersion, TimeSpan? settle = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		ArgumentNullException.ThrowIfNull(runningVersion);

		_path = Path.GetFullPath(path);
		_folder = Path.GetDirectoryName(_path)!;
		_fileName = Path.GetFileName(_path);
		_runningVersion = runningVersion;
		_settle = settle ?? DefaultSettle;

		if (!Directory.Exists(_folder))
		{
			throw new ArgumentException($"The folder '{_folder}' doesn't exist.", nameof(path));
		}

		_timer = new Timer(_ => Check());
		WatchFolder();

		// The install folder itself being deleted and created again, as an update does.
		if (Path.GetDirectoryName(_folder) is { } parent && Directory.Exists(parent))
		{
			_parentWatcher = new FileSystemWatcher(parent, Path.GetFileName(_folder)) { NotifyFilter = NotifyFilters.DirectoryName };
			_parentWatcher.Created += (_, _) => Schedule();
			_parentWatcher.Deleted += (_, _) => Schedule();
			_parentWatcher.Renamed += (_, _) => Schedule();
			_parentWatcher.Error += (_, _) => Schedule();
			_parentWatcher.EnableRaisingEvents = true;
		}
	}

	/// <summary>How long things must be quiet before the file is read: an install writes it, and the files around it, in steps.</summary>
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
		lock (_gate)
		{
			_disposed = true;
			_folderWatcher?.Dispose();
			_parentWatcher?.Dispose();
			_timer.Dispose();
		}
	}

	/// <summary>Watches the install folder as it is now - after an update, a new folder of the same name.</summary>
	private void WatchFolder()
	{
		lock (_gate)
		{
			if (_disposed || !Directory.Exists(_folder))
			{
				return;
			}

			FileSystemWatcher watcher;
			try
			{
				watcher = new FileSystemWatcher(_folder)
				{
					NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
				};
			}
			catch (ArgumentException)
			{
				// Gone again meanwhile: the next check tries again.
				return;
			}

			watcher.Created += OnChanged;
			watcher.Changed += OnChanged;
			watcher.Deleted += OnChanged;
			watcher.Renamed += OnRenamed;
			watcher.Error += (_, _) => Schedule();
			watcher.EnableRaisingEvents = true;

			_folderWatcher?.Dispose();
			_folderWatcher = watcher;
		}
	}

	private void OnChanged(object sender, FileSystemEventArgs e)
	{
		if (IsWatched(e.Name))
		{
			Schedule();
		}
	}

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
		if (Volatile.Read(ref _raised) != 0)
		{
			return;
		}

		if (VersionOf(_path) is null)
		{
			// Mid-install: the folder or the file isn't back yet. Look again shortly.
			if (Interlocked.Increment(ref _missingChecks) <= MaxMissingChecks)
			{
				Schedule();
			}

			return;
		}

		Interlocked.Exchange(ref _missingChecks, 0);
		WatchFolder();

		if (ChangedVersion() is { } version && Interlocked.Exchange(ref _raised, 1) == 0)
		{
			lock (_gate)
			{
				if (_folderWatcher is not null)
				{
					_folderWatcher.EnableRaisingEvents = false;
				}

				if (_parentWatcher is not null)
				{
					_parentWatcher.EnableRaisingEvents = false;
				}
			}

			Updated?.Invoke(this, version);
		}
	}
}
