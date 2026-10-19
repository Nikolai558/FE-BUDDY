using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.Platform;

/// <summary>
/// Watches the running FE-Buddy's own program file for an update installed while it is open. Windows
/// Installer closes FE-Buddy only in the account that installs; a copy left open in another Windows account
/// keeps running, with the files it hasn't loaded yet already the new version's. Nothing is read until Windows
/// reports a change. Once things have been quiet for <see cref="DefaultSettle"/> and Windows Installer has
/// finished - the install, or its rollback - the version on disk is compared with the running one, and
/// <see cref="Updated"/> is raised - once - when they differ.
/// </summary>
/// <remarks>
/// An update first uninstalls the old version: Windows Installer moves the files in use out to
/// <c>C:\Config.Msi</c>, deletes the emptied install folder, then creates it again for the new files. A watch
/// on the folder alone would be left on the deleted one, so the folder above it is watched too, and the
/// folder's watch is set up again on the new folder as soon as it is created. While the file is missing -
/// mid-install - it is looked for again, for up to <see cref="MaxMissingChecks"/> checks in a row; a new
/// install folder starts them over. While Windows Installer runs anything (its <c>Global\_MSIExecute</c>
/// mutex is held), nothing is compared: it is looked at again once the install has finished.
/// <para>
/// The version is read from the file's own bytes (<see cref="VersionOf"/>), not through Windows' version
/// information: that answers from the copy this process already has loaded, so a running FE-Buddy asking about its
/// own <c>FE-BUDDY.dll</c> would only ever hear its own version back.
/// </para>
/// </remarks>
public sealed class InstalledVersionWatcher : IDisposable
{
	/// <summary>How many times in a row a missing file is looked for again before waiting for the next change.</summary>
	internal const int MaxMissingChecks = 100;

	/// <summary>The mutex Windows Installer holds while it installs anything, a rollback included.</summary>
	private const string InstallerMutex = @"Global\_MSIExecute";

	private const string LogSource = "Update";

	private readonly Lock _gate = new();
	private readonly string _path;
	private readonly string _folder;
	private readonly string _fileName;
	private readonly string _runningVersion;
	private readonly TimeSpan _settle;
	private readonly Func<bool> _installRunning;
	private readonly Func<string, FileSystemWatcher> _createWatcher;
	private readonly int _maxMissingChecks;
	private readonly FileSystemWatcher? _parentWatcher;
	private readonly Timer _timer;
	private FileSystemWatcher? _folderWatcher;
	private int _missingChecks;

	// 1 while the folder's watch may be on a folder that has gone: it is set up again before anything is compared.
	private int _rewatch;
	private int _raised;
	private bool _disposed;

	/// <summary>Starts watching <paramref name="path"/>.</summary>
	/// <param name="path">The running program's own file, e.g. <c>FE-BUDDY.dll</c> in the install folder.</param>
	/// <param name="runningVersion">The running version (<see cref="AppVersion.Current"/>).</param>
	/// <param name="settle">How long things must be quiet before the file is read; <see cref="DefaultSettle"/> when not given.</param>
	/// <exception cref="ArgumentException">The path is blank, or its folder doesn't exist.</exception>
	public InstalledVersionWatcher(string path, string runningVersion, TimeSpan? settle = null)
		: this(path, runningVersion, settle, IsInstallRunning, folder => new FileSystemWatcher(folder), MaxMissingChecks)
	{
	}

	/// <summary>
	/// Same as <see cref="InstalledVersionWatcher(string, string, TimeSpan?)"/>, with Windows Installer, the folder's
	/// watch and the limit supplied. Unit tests stand in for an install running and a folder that can't be watched.
	/// </summary>
	/// <param name="path">The running program's own file.</param>
	/// <param name="runningVersion">The running version.</param>
	/// <param name="settle">How long things must be quiet before the file is read.</param>
	/// <param name="installRunning">Whether Windows Installer is installing anything now.</param>
	/// <param name="createWatcher">Makes the watch of a folder.</param>
	/// <param name="maxMissingChecks">How many times in a row a missing file is looked for again.</param>
	internal InstalledVersionWatcher(
		string path,
		string runningVersion,
		TimeSpan? settle,
		Func<bool> installRunning,
		Func<string, FileSystemWatcher> createWatcher,
		int maxMissingChecks)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		ArgumentNullException.ThrowIfNull(runningVersion);

		_path = Path.GetFullPath(path);
		_folder = Path.GetDirectoryName(_path)!;
		_fileName = Path.GetFileName(_path);
		_runningVersion = runningVersion;
		_settle = settle ?? DefaultSettle;
		_installRunning = installRunning;
		_createWatcher = createWatcher;
		_maxMissingChecks = maxMissingChecks;

		if (!Directory.Exists(_folder))
		{
			throw new ArgumentException($"The folder '{_folder}' doesn't exist.", nameof(path));
		}

		_timer = new Timer(_ => CheckSafely());
		FileSystemWatcher? parentWatcher = null;

		try
		{
			WatchFolder();

			// The install folder itself being deleted and created again, as an update does.
			if (Path.GetDirectoryName(_folder) is { } parent && Directory.Exists(parent))
			{
				parentWatcher = new FileSystemWatcher(parent, Path.GetFileName(_folder)) { NotifyFilter = NotifyFilters.DirectoryName };
				parentWatcher.Created += (_, _) => OnFolderCreated();
				parentWatcher.Deleted += (_, _) => OnFolderGone();
				parentWatcher.Renamed += (_, _) => OnFolderGone();
				parentWatcher.Error += (_, _) => OnFolderGone();
				parentWatcher.EnableRaisingEvents = true;
			}
		}
		catch
		{
			parentWatcher?.Dispose();
			_folderWatcher?.Dispose();
			_timer.Dispose();
			throw;
		}

		_parentWatcher = parentWatcher;
	}

	/// <summary>
	/// How long things must be quiet before the file is read: an install writes it, and the files around it, in
	/// steps. The file is read only once Windows Installer has finished, too.
	/// </summary>
	public static TimeSpan DefaultSettle { get; } = TimeSpan.FromSeconds(3);

	/// <summary>Raised once, on a thread-pool thread, with the version now on disk, when it differs from the running one.</summary>
	public event EventHandler<string>? Updated;

	/// <summary>
	/// The FE-Buddy version an assembly file holds, as <see cref="AppVersion"/> reads the running one: its informational
	/// version without build metadata, else its numeric version's first three parts. Read from the file's metadata,
	/// never through a copy of it already loaded.
	/// </summary>
	/// <param name="path">The file.</param>
	/// <returns>The version, or <see langword="null"/> when the file isn't there, can't be read, or isn't an assembly.</returns>
	public static string? VersionOf(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}

			using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using PEReader pe = new(stream);
			if (!pe.HasMetadata)
			{
				return null;
			}

			MetadataReader metadata = pe.GetMetadataReader();
			AssemblyDefinition assembly = metadata.GetAssemblyDefinition();

			return InformationalVersion(metadata, assembly) ?? assembly.Version.ToString(3);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException or InvalidOperationException)
		{
			return null;
		}
	}

	/// <summary>The assembly's <c>AssemblyInformationalVersionAttribute</c>, without build metadata.</summary>
	private static string? InformationalVersion(MetadataReader metadata, AssemblyDefinition assembly)
	{
		foreach (CustomAttributeHandle handle in assembly.GetCustomAttributes())
		{
			CustomAttribute attribute = metadata.GetCustomAttribute(handle);
			if (attribute.Constructor.Kind != HandleKind.MemberReference)
			{
				continue;
			}

			EntityHandle type = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
			if (type.Kind != HandleKind.TypeReference)
			{
				continue;
			}

			TypeReference reference = metadata.GetTypeReference((TypeReferenceHandle)type);
			if (metadata.StringComparer.Equals(reference.Name, "AssemblyInformationalVersionAttribute")
				&& metadata.StringComparer.Equals(reference.Namespace, "System.Reflection"))
			{
				// The blob: a 0x0001 prolog, then the one string argument.
				BlobReader value = metadata.GetBlobReader(attribute.Value);
				return value.ReadUInt16() == 1 ? AppVersion.Clean(value.ReadSerializedString()) : null;
			}
		}

		return null;
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

	/// <summary>
	/// Whether Windows Installer is installing anything - this update, or its rollback - by the mutex it holds
	/// meanwhile. One this process may not open counts as held.
	/// </summary>
	private static bool IsInstallRunning()
	{
		try
		{
			if (!Mutex.TryOpenExisting(InstallerMutex, out Mutex? mutex))
			{
				return false;
			}

			mutex.Dispose();
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			return true;
		}
	}

	/// <summary>
	/// Watches the install folder as it is now - after an update, a new folder of the same name. One that is gone,
	/// or can't be watched for now, isn't: the next check tries again.
	/// </summary>
	/// <returns>Whether the folder is watched.</returns>
	private bool WatchFolder()
	{
		lock (_gate)
		{
			if (_disposed || !Directory.Exists(_folder))
			{
				Volatile.Write(ref _rewatch, 1);
				return false;
			}

			FileSystemWatcher? watcher = null;

			try
			{
				watcher = _createWatcher(_folder);
				watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime;
				watcher.Created += OnChanged;
				watcher.Changed += OnChanged;
				watcher.Deleted += OnChanged;
				watcher.Renamed += OnRenamed;
				watcher.Error += (_, _) => OnFolderGone();
				watcher.EnableRaisingEvents = true;
			}
			catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
			{
				// Gone again meanwhile, or not to be watched for now: the next check tries again.
				watcher?.Dispose();
				Volatile.Write(ref _rewatch, 1);
				return false;
			}

			_folderWatcher?.Dispose();
			_folderWatcher = watcher;
			Volatile.Write(ref _rewatch, 0);
			return true;
		}
	}

	/// <summary>
	/// The install folder is there again - an update about to write its new files: it is watched at once, so the
	/// new file's own changes start the check, and the file is looked for again from the start.
	/// </summary>
	private void OnFolderCreated()
	{
		WatchFolder();
		Interlocked.Exchange(ref _missingChecks, 0);
		Schedule();
	}

	/// <summary>The install folder went, or its watch failed: it is watched again before anything is compared.</summary>
	private void OnFolderGone()
	{
		Volatile.Write(ref _rewatch, 1);
		Schedule();
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

	/// <summary>
	/// <see cref="Check"/>, for the timer: something it doesn't expect is logged, not left to end FE-Buddy on a
	/// thread-pool thread. The next change looks again.
	/// </summary>
	private void CheckSafely()
	{
		try
		{
			Check();
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"Checking {_fileName} for an update failed: {ex.Message}");
		}
	}

	private void Check()
	{
		if (Volatile.Read(ref _raised) != 0)
		{
			return;
		}

		if (_installRunning())
		{
			// Windows Installer is still at work - this update, or its rollback: look again once it has finished.
			Schedule();
			return;
		}

		string? onDisk = VersionOf(_path);

		if (onDisk is null)
		{
			// Mid-install: the folder or the file isn't back yet, and the folder may be a new one by then. Look again shortly.
			Volatile.Write(ref _rewatch, 1);

			if (Interlocked.Increment(ref _missingChecks) <= _maxMissingChecks)
			{
				Schedule();
			}

			return;
		}

		if (Volatile.Read(ref _rewatch) != 0)
		{
			// The folder may be a new one: watched first, then compared once its own changes have settled.
			if (WatchFolder() || Interlocked.Increment(ref _missingChecks) <= _maxMissingChecks)
			{
				Schedule();
			}

			return;
		}

		Interlocked.Exchange(ref _missingChecks, 0);

		if (onDisk.Equals(_runningVersion, StringComparison.OrdinalIgnoreCase))
		{
			AppLog.Info(LogSource, $"{_fileName} was written again, still v{_runningVersion}.");
			return;
		}

		AppLog.Info(LogSource, $"{_fileName} is now v{onDisk} on disk, v{_runningVersion} running.");

		if (Interlocked.Exchange(ref _raised, 1) == 0)
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

			Updated?.Invoke(this, onDisk);
		}
	}
}
