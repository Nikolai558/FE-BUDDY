namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// One settings file being brought forward with the others (<see cref="UserConfigMigrations.MigrateTogether(UserConfigMigratingFile?, IReadOnlyList{UserConfigMigratingFile})"/>):
/// its settings, edited in place, the layout they have reached, and what went wrong if a step failed.
/// </summary>
internal sealed class UserConfigMigratingFile
{
	private readonly Dictionary<string, string> _asRead;

	/// <summary>Takes a file's settings as read.</summary>
	/// <param name="fileName">The file, for the log, e.g. <c>UserConfig.Default.json</c>.</param>
	/// <param name="values">Its settings, by dotted path. Not changed: the file works on a copy.</param>
	/// <param name="version">The layout they are in.</param>
	public UserConfigMigratingFile(string fileName, IReadOnlyDictionary<string, string> values, int version)
	{
		ArgumentNullException.ThrowIfNull(values);

		FileName = fileName;
		_asRead = new Dictionary<string, string>(values, StringComparer.Ordinal);
		Values = new Dictionary<string, string>(values, StringComparer.Ordinal);
		FromVersion = version;
		Version = version;
	}

	/// <summary>The file, for the log.</summary>
	public string FileName { get; }

	/// <summary>Its settings, by dotted path, as the steps so far have left them.</summary>
	public Dictionary<string, string> Values { get; private set; }

	/// <summary>The layout the file was read in.</summary>
	public int FromVersion { get; }

	/// <summary>The layout its settings are in now.</summary>
	public int Version { get; private set; }

	/// <summary>What each step that ran changed, oldest first.</summary>
	public List<string> Applied { get; } = [];

	/// <summary>Why it couldn't be brought forward - a step missing or failing - or <see langword="null"/>.</summary>
	public InvalidOperationException? Error { get; private set; }

	/// <summary>Whether any step ran and none failed: the file has something to write back.</summary>
	public bool Migrated => Error is null && Version > FromVersion;

	/// <summary>Records a step that ran.</summary>
	/// <param name="version">The layout it brought the settings to.</param>
	/// <param name="description">What it changed.</param>
	public void Stepped(int version, string description)
	{
		Version = version;
		Applied.Add(description);
	}

	/// <summary>Stops the file where it was read: its settings go back to how they were, and no more steps run on it.</summary>
	/// <param name="error">Why.</param>
	public void Fail(InvalidOperationException error)
	{
		Error = error;
		Values = new Dictionary<string, string>(_asRead, StringComparer.Ordinal);
		Version = FromVersion;
		Applied.Clear();
	}
}
