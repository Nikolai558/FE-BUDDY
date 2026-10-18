using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Microsoft.Win32;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.ConcatenateAliases;
using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Add Custom Aliases</b> sub-service tab inside the AIRAC Service screen (Concatenate Aliases,
/// as the code still calls it): whether to add the facility's own custom alias files - on this PC, or
/// on the web (GitHub, private or public) - to <c>Aliases\Combined_Alias.txt</c>, after every alias file
/// the run writes, so theirs win; and which files.
/// </summary>
/// <remarks>
/// <para>
/// vNAS takes one alias file, so every run that writes an alias file writes <c>Combined_Alias.txt</c>
/// (see <see cref="AiracService"/>); this tab decides only whether the facility's own aliases go in
/// too. They do until the user says no. The run reads each custom file (see
/// <see cref="AliasSourceLoader"/>); one that cannot be read is left out with a warning, and the rest
/// are merged. The tab is greyed out while no included sub-service makes an alias file.
/// </para>
/// <para>
/// A web address may name a saved credential (Settings ▸ Credentials). Only its id is saved with the
/// tab, so the secret never reaches <c>UserConfig.json</c>, and a settings export leaves even the id
/// out (<c>ConfigKeyScope.CredentialChoice</c>). One credential serves as many files as need it - a
/// row with none is offered the one an earlier row on the same website uses. <b>Check</b> reads a
/// file straight away, so a mistyped address or a refused token shows up here rather than in the run.
/// </para>
/// <para>
/// The list is saved as numbered keys, <c>Sources.1.FilePath</c>, <c>Sources.2.Url</c>,
/// <c>Sources.2.CredentialId</c> and so on (see <see cref="ConcatenateAliasesSettingsParser"/>); the
/// settings block the run gets is the same keys.
/// </para>
/// </remarks>
public sealed class ConcatenateAliasesViewModel : SubServiceSettingsViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.ConcatenateAliases";
	private const string SourcesKey = "Sources";
	private const string LogSource = "ConcatenateAliases";

	/// <summary>
	/// Where <see cref="AddCustomAliasFiles"/> is saved: under its first name, from when it switched the
	/// whole combined file on and off, so the choice carries over. Off then kept the custom files out,
	/// as it does now.
	/// </summary>
	private const string AddCustomAliasFilesConfigKey = "CombineAliasFiles";

	private const string NoAliasCommandsMessage =
		"It has no alias commands (lines starting with a dot), so it isn't an alias file. Choose your facility's alias file.";

	private readonly CredentialStore _store;
	private readonly HttpClient? _httpClient;
	private readonly Dispatcher _dispatcher;

	// What each file on this PC was found to be, by path. Found off the UI thread
	// (CheckLocalFilesAsync); validating and the rows' notices only read it.
	private readonly Dictionary<string, LocalAliasFileState> _localFiles = new(StringComparer.OrdinalIgnoreCase);

	// Checks the files once the user stops typing a path, rather than on every keystroke: a half-typed
	// path on a share that isn't there would tie up a check for seconds each time.
	private readonly DispatcherTimer _checkAfterTyping;
	private bool _loading;
	private bool _addCustomAliasFiles = true;

	// The saved credentials, read once per change to the store rather than on every keystroke.
	private Dictionary<Guid, CredentialInfo> _savedCredentials = [];
	private string? _credentialsError;

	/// <summary>Builds the tab over this user's credentials and restores its saved settings.</summary>
	public ConcatenateAliasesViewModel()
		: this(CredentialStore.Default)
	{
	}

	/// <summary>Builds the tab over <paramref name="store"/> and restores its saved settings.</summary>
	/// <param name="store">The credentials a web address can be downloaded with.</param>
	/// <param name="httpClient">What <b>Check</b> downloads with; <see langword="null"/> makes one per check.</param>
	internal ConcatenateAliasesViewModel(CredentialStore store, HttpClient? httpClient = null)
	{
		_store = store;
		_httpClient = httpClient;
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		_checkAfterTyping = new DispatcherTimer(DispatcherPriority.Background, _dispatcher) { Interval = TimeSpan.FromMilliseconds(400) };
		_checkAfterTyping.Tick += (_, _) =>
		{
			_checkAfterTyping.Stop();
			_ = CheckLocalFilesAsync();
		};

		AddFileCommand = new RelayCommand(AddFiles);
		// A new web address opens expanded: its box is where the address goes.
		AddUrlCommand = new RelayCommand(() => Add(new AliasSourceRow(this, AliasSourceKind.Url, string.Empty, Guid.Empty) { IsExpanded = true }));

		RefreshCredentials();
		_store.Changed += (_, _) =>
		{
			if (_dispatcher.CheckAccess())
			{
				RefreshCredentials();
			}
			else
			{
				_dispatcher.BeginInvoke(RefreshCredentials);
			}
		};

		Sources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSources));

		LoadFromConfig();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Add Custom Aliases";

	/// <summary>Where the combined file goes, inside the cycle folder.</summary>
	public static string OutputFile => $@"{AiracOutputPaths.AliasFolder}\{AiracOutputPaths.CombinedAliasFileName}";

	/// <summary>
	/// Whether to add the custom alias files to <c>Combined_Alias.txt</c>, after FE-Buddy's: "Yes" to the
	/// tab's question. On by default; off, the combined file holds only FE-Buddy's alias files and the
	/// custom alias files are not used.
	/// </summary>
	public bool AddCustomAliasFiles
	{
		get => _addCustomAliasFiles;
		set
		{
			if (SetProperty(ref _addCustomAliasFiles, value))
			{
				MarkDirty();
			}
		}
	}

	/// <summary>The custom alias files, in merge order.</summary>
	public ObservableCollection<AliasSourceRow> Sources { get; } = [];

	/// <summary>Whether there is at least one custom alias file.</summary>
	public bool HasSources => Sources.Count > 0;

	/// <summary>"None", then every saved credential, for a web address's drop-down.</summary>
	public ObservableCollection<CredentialChoice> Credentials { get; } = [];

	/// <summary>Adds files on this PC, chosen in a file dialog: <b>Add local file</b>.</summary>
	public ICommand AddFileCommand { get; }

	/// <summary>Adds an empty web address row: <b>Add remote file</b>.</summary>
	public ICommand AddUrlCommand { get; }

	private static Window? Owner => Application.Current?.MainWindow;

	// ================= ISubServiceRunTarget =================

	/// <inheritdoc />
	/// <remarks>
	/// The block goes on <see cref="AiracServiceSettings.ConcatenateAliases"/>. The custom alias files go in
	/// only while they're added: nothing else reads them.
	/// </remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> block = new(StringComparer.OrdinalIgnoreCase)
		{
			[ConcatenateAliasesSettingsParser.AddCustomAliasFilesKey] = YesNo(AddCustomAliasFiles),
		};

		if (!AddCustomAliasFiles)
		{
			return block;
		}

		foreach (AliasSourceRow row in Sources)
		{
			foreach ((string key, string value) in KeysOf(row))
			{
				block[key] = value;
			}
		}

		return block;
	}

	/// <inheritdoc />
	/// <remarks>Does nothing: the custom alias files do not come from the AIRAC data.</remarks>
	public void SetReadiness(bool ready, string? unavailableReason = null)
	{
	}

	/// <inheritdoc />
	/// <remarks>Does nothing: nothing on this tab is built from the selected cycle.</remarks>
	public void LoadCycleDependentLists(AiracCycleOptionLists lists)
	{
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		if (result.CombinedAlias is not { } merged)
		{
			return null;
		}

		string feBuddy = merged.FeBuddyFiles.Count > 0
			? $"{merged.FeBuddyCommandCount:N0} command(s) from {string.Join(", ", merged.FeBuddyFiles)}"
			: "no FE-Buddy alias file";

		// Named as it was written: the user may have renamed it on the File Names tab. In the
		// file's own order: FE-Buddy's aliases, then the custom files.
		string summary = merged.FilePath is null
			? $"{AiracOutputPaths.CombinedAliasFileName} not written"
			: !AddCustomAliasFiles
				? $"{Path.GetFileName(merged.FilePath)}: {feBuddy}. Your custom alias files weren't added"
				: $"{Path.GetFileName(merged.FilePath)}: {feBuddy}, then {merged.CustomCommandCount:N0} command(s) from " +
					$"{merged.CustomFilesMerged} of {merged.CustomFileCount} custom alias file(s)";

		return new SubServiceRunResult(Title, summary, merged.Messages);
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		if (!AddCustomAliasFiles)
		{
			return [new ServicePreviewSection(Title, [new ServicePreviewRow("Custom alias files", $"Not added: `{OutputFile}` holds only FE-Buddy's alias files")])];
		}

		List<ServicePreviewRow> rows =
		[
			new ServicePreviewRow("Custom alias files", $"Added to `{OutputFile}`, after every FE-Buddy alias file this run writes"),
		];

		if (Sources.Count == 0)
		{
			rows.Add(new ServicePreviewRow("Custom alias file", "None yet"));
		}

		foreach (AliasSourceRow row in Sources)
		{
			rows.Add(new ServicePreviewRow($"Custom alias file {row.Number}", Describe(row)));
		}

		return [new ServicePreviewSection(Title, rows)];
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		_loading = true;

		try
		{
			Sources.Clear();

			string prefix = $"{NodePath}.{SourcesKey}.";
			SortedDictionary<int, Dictionary<string, string>> byNumber = [];

			foreach ((string key, string value) in UserConfigFile.SnapshotValues())
			{
				if (!key.StartsWith(prefix, StringComparison.Ordinal))
				{
					continue;
				}

				string[] parts = key[prefix.Length..].Split('.');

				if (parts.Length == 2 && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int number))
				{
					if (!byNumber.TryGetValue(number, out Dictionary<string, string>? fields))
					{
						fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
						byNumber[number] = fields;
					}

					fields[parts[1]] = value.Trim();
				}
			}

			foreach (Dictionary<string, string> fields in byNumber.Values)
			{
				string filePath = fields.GetValueOrDefault(ConcatenateAliasesSettingsParser.FilePathKey, string.Empty);
				string url = fields.GetValueOrDefault(ConcatenateAliasesSettingsParser.UrlKey, string.Empty);
				Guid credentialId = Guid.TryParse(fields.GetValueOrDefault(ConcatenateAliasesSettingsParser.CredentialIdKey), out Guid id) ? id : Guid.Empty;

				// A row whose file did not come across in a settings import has nothing left to read.
				if (filePath.Length > 0)
				{
					Sources.Add(new AliasSourceRow(this, AliasSourceKind.File, filePath, Guid.Empty));
				}
				else if (url.Length > 0)
				{
					Sources.Add(new AliasSourceRow(this, AliasSourceKind.Url, url, credentialId));
				}
			}

			Renumber();
			_addCustomAliasFiles = GetBool(AddCustomAliasFilesConfigKey, true);
			OnPropertyChanged(nameof(AddCustomAliasFiles));
		}
		finally
		{
			_loading = false;
		}

		RefreshRowHints();
		ClearDirty();
		_ = CheckLocalFilesAsync();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set(AddCustomAliasFilesConfigKey, YesNo(AddCustomAliasFiles));

		// The list is written whole: a removed row must not leave its numbered keys behind.
		RemoveSubtree(SourcesKey);

		foreach (AliasSourceRow row in Sources)
		{
			foreach ((string key, string value) in KeysOf(row))
			{
				Set(key, value);
			}
		}
	}

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation)
	{
		// The custom alias files are only read while they're added, so only then held against the user.
		foreach (AliasSourceRow row in Sources)
		{
			row.Error = AddCustomAliasFiles ? ErrorOf(row) ?? LocalFileErrorOf(row) : null;

			if (row.Error is { } error)
			{
				validation.AddArea(ServiceAreas.CustomAliasFiles, $"Custom alias file {row.Number}: {error}");
			}
		}
	}

	// ================= rows =================

	/// <summary>A row's location or credential changed.</summary>
	internal void RowChanged()
	{
		if (_loading)
		{
			return;
		}

		RefreshRowHints();
		MarkDirty();
		_checkAfterTyping.Stop();
		_checkAfterTyping.Start();
	}

	/// <summary>
	/// Finds what each local file the rows name is - there, readable, an alias file - off the UI thread,
	/// then shows it: the rows' notices and the tab's validation only read what was found. Runs when
	/// the tab loads, when the list changes, and once the user stops typing a path.
	/// </summary>
	/// <returns>A task that completes when the rows show what was found.</returns>
	internal async Task CheckLocalFilesAsync()
	{
		string[] paths = [.. Sources
			.Where(row => row.IsFile)
			.Select(row => row.Location.Trim())
			.Where(Path.IsPathFullyQualified)
			.Distinct(StringComparer.OrdinalIgnoreCase)];

		if (paths.Length == 0)
		{
			return;
		}

		try
		{
			Dictionary<string, LocalAliasFileState> found = await Task.Run(() =>
				paths.ToDictionary(path => path, AliasSourceLoader.InspectLocalFile, StringComparer.OrdinalIgnoreCase));

			foreach ((string path, LocalAliasFileState state) in found)
			{
				_localFiles[path] = state;
			}

			RefreshRowHints();
			Revalidate();
		}
		catch (Exception ex)
		{
			// A path Windows refuses outright (too long, a reserved name) is the row's to say; this keeps
			// anything unexpected from taking FE-Buddy down, since nothing waits for the check.
			AppLog.Warning(LogSource, $"Checking the custom alias files failed: {ex.Message}");
		}
	}

	/// <summary>Picks the file for a file row.</summary>
	/// <param name="row">The row.</param>
	internal void Browse(AliasSourceRow row)
	{
		if (PickFiles(multiselect: false, row.Location) is [string path])
		{
			row.Location = path;
		}
	}

	/// <summary>
	/// Reads a row's file now, and shows what the run would get - and, when GitHub refused or hid it,
	/// what to try (<see cref="AliasSourceRow.Troubleshooting"/>). A GitHub address is turned into its
	/// Raw link first. A result for an address or credential the user has changed since is dropped,
	/// so it never shows against the new one.
	/// </summary>
	/// <param name="row">The row.</param>
	/// <returns>A task that completes when the row shows the result.</returns>
	internal async Task CheckAsync(AliasSourceRow row)
	{
		row.TidyLocation();

		if (ErrorOf(row) is { } error)
		{
			row.SetCheck(false, error);
			return;
		}

		AliasSource source = row.ToSource();
		row.IsChecking = true;

		try
		{
			// Off the UI thread: a file is read, or looked for, before the loader's first wait.
			AliasSourceLoad load = await Task.Run(() => AliasSourceLoader.LoadAsync(source, _store, _httpClient));

			if (IsStill(row, source))
			{
				row.SetCheck(
					load.Succeeded,
					load.Succeeded
						? $"Read {load.CommandCount:N0} alias command(s)." + (load.CredentialWarning is { } warning ? " " + warning : string.Empty)
						: load.Problem ?? "It could not be read.",
					TroubleshootingFor(load));
			}
		}
		catch (Exception ex)
		{
			// The loader never throws for a file it cannot read; this keeps anything unexpected from
			// taking FE-Buddy down, since the button's command does not wait for the check.
			AppLog.Warning(LogSource, $"Checking {source.DisplayName} failed: {ex.Message}");

			if (IsStill(row, source))
			{
				row.SetCheck(false, $"It could not be checked: {ex.Message}");
			}
		}
		finally
		{
			row.IsChecking = false;
		}
	}

	/// <summary>
	/// What to offer after a check: when GitHub refused or hid the file, whether the repository is
	/// private (no credential chosen) or what to check about the credential; otherwise nothing.
	/// </summary>
	private static AliasTroubleshooting TroubleshootingFor(AliasSourceLoad load)
	{
		bool onGitHub = Uri.TryCreate(load.Source.Location, UriKind.Absolute, out Uri? url) && GitHubFileUrl.ToContentsApi(url) is not null;

		return !load.IsAccessDenied || !onGitHub ? AliasTroubleshooting.None
			: load.Source.CredentialId is null ? AliasTroubleshooting.AskIfPrivate
			: AliasTroubleshooting.CredentialRefused;
	}

	/// <summary>Whether the row still points at the file it had when a check started.</summary>
	private static bool IsStill(AliasSourceRow row, AliasSource source)
	{
		AliasSource now = row.ToSource();
		return now.Location == source.Location && now.CredentialId == source.CredentialId;
	}

	/// <summary>Adds a credential in the credential editor and chooses it for a row.</summary>
	/// <param name="row">The row.</param>
	internal void NewCredential(AliasSourceRow row)
	{
		if (CredentialEditorWindow.Edit(Owner, _store, null) is { } saved)
		{
			RefreshCredentials();
			row.CredentialId = saved.Id;
		}
	}

	/// <summary>Moves a row one place earlier or later.</summary>
	/// <param name="row">The row.</param>
	/// <param name="offset">-1 for earlier, +1 for later.</param>
	internal void Move(AliasSourceRow row, int offset)
	{
		int from = Sources.IndexOf(row);
		int to = from + offset;

		if (from < 0 || to < 0 || to >= Sources.Count)
		{
			return;
		}

		Sources.Move(from, to);
		ListChanged();
	}

	/// <summary>Takes a row off the list.</summary>
	/// <param name="row">The row.</param>
	internal void Remove(AliasSourceRow row)
	{
		if (Sources.Remove(row))
		{
			ListChanged();
		}
	}

	private void AddFiles()
	{
		foreach (string path in PickFiles(multiselect: true, initial: null))
		{
			Add(new AliasSourceRow(this, AliasSourceKind.File, path, Guid.Empty));
		}
	}

	private void Add(AliasSourceRow row)
	{
		Sources.Add(row);
		ListChanged();
	}

	private void ListChanged()
	{
		Renumber();
		RefreshRowHints();
		MarkDirty();
		CommandManager.InvalidateRequerySuggested();
		_ = CheckLocalFilesAsync();
	}

	private void Renumber()
	{
		for (int i = 0; i < Sources.Count; i++)
		{
			Sources[i].Number = i + 1;
		}
	}

	private static string[] PickFiles(bool multiselect, string? initial)
	{
		OpenFileDialog dialog = new()
		{
			Title = "Choose your custom alias file",
			Filter = "Alias files (*.txt)|*.txt|All files (*.*)|*.*",
			Multiselect = multiselect,
			InitialDirectory = OutputPreferences.BrowseDirectory(
				!string.IsNullOrWhiteSpace(initial) && Path.IsPathFullyQualified(initial) ? Path.GetDirectoryName(initial) : null),
		};

		return dialog.ShowDialog(Owner) == true ? dialog.FileNames : [];
	}

	/// <summary>The keys a row is saved as, relative to the tab's node.</summary>
	private static IEnumerable<(string Key, string Value)> KeysOf(AliasSourceRow row)
	{
		string prefix = $"{SourcesKey}.{row.Number.ToString(CultureInfo.InvariantCulture)}.";

		if (row.IsFile)
		{
			yield return (prefix + ConcatenateAliasesSettingsParser.FilePathKey, row.Location.Trim());
			yield break;
		}

		yield return (prefix + ConcatenateAliasesSettingsParser.UrlKey, row.Location.Trim());

		if (row.CredentialId != Guid.Empty)
		{
			yield return (prefix + ConcatenateAliasesSettingsParser.CredentialIdKey, row.CredentialId.ToString("N"));
		}
	}

	/// <summary>Why a row cannot be saved - the same rules <see cref="ConcatenateAliasesSettingsParser"/> applies - or <see langword="null"/>.</summary>
	private static string? ErrorOf(AliasSourceRow row)
	{
		string location = row.Location.Trim();

		if (row.IsFile)
		{
			return location.Length == 0 ? "Choose the file."
				: !Path.IsPathFullyQualified(location) ? @"Give the file's full path, such as C:\Users\me\Documents\ZOB-Alias.txt."
				: null;
		}

		if (location.Length == 0)
		{
			return "Enter the file's web address.";
		}

		if (!Uri.TryCreate(location, UriKind.Absolute, out Uri? url) || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
		{
			return "Enter a web address starting with https://.";
		}

		if (GitHubFileUrl.IsPageButNotFile(url))
		{
			return "This is a GitHub page, not a file. Open the alias file itself on GitHub and copy its address, or its Raw link.";
		}

		if (UrlSecrets.Describe(url) is { } secret)
		{
			return $"This address has {secret} in it. Remove it and choose a credential instead: web addresses are saved " +
				"in FE-Buddy's settings and in settings exports.";
		}

		// A file on GitHub is downloaded from GitHub's API over https whatever the address says.
		if (row.CredentialId != Guid.Empty && url.Scheme != Uri.UriSchemeHttps && GitHubFileUrl.ToContentsApi(url) is null)
		{
			return "A credential is only ever sent to an https:// address.";
		}

		return null;
	}

	/// <summary>
	/// Why a file on this PC can't be saved for what it holds: a file with no alias command in it is
	/// not an alias file. One that isn't there, or can't be read, only gets a notice (see
	/// <see cref="RefreshRowHints"/>): the run says what happened to it. Reads only what
	/// <see cref="CheckLocalFilesAsync"/> found: nothing until it has looked.
	/// </summary>
	private string? LocalFileErrorOf(AliasSourceRow row) =>
		row.IsFile && LocalFileState(row) == LocalAliasFileState.NoAliasCommands ? NoAliasCommandsMessage : null;

	/// <summary>What <see cref="CheckLocalFilesAsync"/> found the row's file to be, or <see langword="null"/> before it has looked.</summary>
	private LocalAliasFileState? LocalFileState(AliasSourceRow row) =>
		_localFiles.TryGetValue(row.Location.Trim(), out LocalAliasFileState state) ? state : null;

	/// <summary>
	/// Updates what each row warns about without stopping a save - a file that is not on this PC, a
	/// credential that is not - and the credential an earlier row could lend it.
	/// </summary>
	private void RefreshRowHints()
	{
		foreach (AliasSourceRow row in Sources)
		{
			string location = row.Location.Trim();

			row.Notice =
				row.IsFile && LocalFileState(row) == LocalAliasFileState.Missing
					? "This file is not on this PC. It may have been moved or renamed."
				: row.IsUrl && row.CredentialId != Guid.Empty && _credentialsError is not null
					? $"Windows Credential Manager could not be read, so its credential cannot be checked: {_credentialsError}"
				: row.IsUrl && row.CredentialId != Guid.Empty && !_savedCredentials.ContainsKey(row.CredentialId)
					? "Its credential is not on this PC: it was removed, or the settings came from another PC. Choose one of yours."
				: null;

			// Its status line names its credential, which may have been renamed.
			row.RefreshStatus();
			row.SetSuggestion(null, null);

			if (!row.IsUrl || row.CredentialId != Guid.Empty || RequestHost(location) is not { } host)
			{
				continue;
			}

			AliasSourceRow? lender = Sources
				.TakeWhile(other => !ReferenceEquals(other, row))
				.FirstOrDefault(other => other.IsUrl
					&& _savedCredentials.TryGetValue(other.CredentialId, out CredentialInfo? info)
					&& CredentialHosts.Allows(info.Hosts, host));

			if (lender is not null && Credentials.FirstOrDefault(c => c.Id == lender.CredentialId) is { } choice)
			{
				row.SetSuggestion(choice, $"file {lender.Number}");
			}
		}
	}

	/// <summary>The website a web address is downloaded from - GitHub's API for a file on GitHub.</summary>
	private static string? RequestHost(string location) =>
		Uri.TryCreate(location, UriKind.Absolute, out Uri? url)
			? (GitHubFileUrl.ToContentsApi(url) ?? url).Host
			: null;

	/// <summary>
	/// Re-reads the saved credentials and brings the drop-down up to date without clearing it, so a
	/// row's chosen credential stays chosen while the list changes around it. When Credential
	/// Manager cannot be read, the tab still opens: the drop-down offers only None, and each row
	/// that names a credential says why it cannot be checked.
	/// </summary>
	private void RefreshCredentials()
	{
		IReadOnlyList<CredentialInfo> saved;

		try
		{
			saved = _store.List();
			_credentialsError = null;
		}
		catch (Win32Exception ex)
		{
			AppLog.Warning(LogSource, $"Could not read Windows Credential Manager: {ex.Message}");
			saved = [];
			_credentialsError = ex.Message;
		}

		_savedCredentials = saved.ToDictionary(info => info.Id);
		CredentialChoice.Sync(Credentials, [CredentialChoice.None, .. saved.Select(CredentialChoice.For)]);
		RefreshRowHints();
	}

	/// <summary>The name of a saved credential, for a row's status line.</summary>
	/// <param name="id">The credential's id.</param>
	/// <returns>Its name, or <see langword="null"/> for none or one that is not on this PC.</returns>
	internal string? CredentialName(Guid id) =>
		id == Guid.Empty ? null : Credentials.FirstOrDefault(c => c.Id == id)?.Name;

	/// <summary>How the Preview Settings tab describes a row.</summary>
	private string Describe(AliasSourceRow row)
	{
		if (row.IsFile)
		{
			return $"{row.Location} (this PC)";
		}

		string credential = row.CredentialId == Guid.Empty
			? "no credential"
			: Credentials.FirstOrDefault(c => c.Id == row.CredentialId) is { } choice
				? $"credential '{choice.Name}'"
				: "a credential that is not on this PC";

		return $"{row.Location}, with {credential}";
	}
}
