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
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Concatenate Aliases</b> sub-service tab inside the AIRAC Service screen: whether to combine
/// every alias file the run writes into <c>Aliases\Combined_Alias.txt</c>, and the facility's own
/// custom alias files - on this PC, or on the web (GitHub, private or public) - to add after them, so
/// theirs win.
/// </summary>
/// <remarks>
/// <para>
/// vNAS takes one alias file, so the facility's own aliases and FE-Buddy's have to be merged before
/// every upload. Combining is on until the user turns it off. The run reads each custom file (see
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

	private const string NoAliasCommandsMessage =
		"It has no alias commands (lines starting with a dot), so it isn't an alias file. Choose your facility's alias file.";

	private readonly CredentialStore _store;
	private readonly HttpClient? _httpClient;
	private readonly Dispatcher _dispatcher;

	// Whether each file on this PC holds an alias command, as last read, with when it was written and
	// its size - so validating as the user types doesn't read every file again.
	private readonly Dictionary<string, (DateTime Written, long Length, bool HasCommands)> _localFileChecks = new(StringComparer.OrdinalIgnoreCase);
	private bool _loading;
	private bool _combineAliasFiles = true;
	private Func<SubServiceDescriptor, ServiceTabViewModel?>? _openTabFor;
	private Action<ServiceTabViewModel>? _showTab;

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

		AddFileCommand = new RelayCommand(AddFiles);
		AddUrlCommand = new RelayCommand(() => Add(new AliasSourceRow(this, AliasSourceKind.Url, string.Empty, Guid.Empty)));

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
	public override string Title => "Concatenate Aliases";

	/// <summary>Where the combined file goes, inside the cycle folder.</summary>
	public static string OutputFile => $@"{AiracOutputPaths.AliasFolder}\{AiracOutputPaths.CombinedAliasFileName}";

	/// <summary>
	/// Whether to combine the alias files into <c>Combined_Alias.txt</c>. On by default; off, each alias
	/// file stays on its own and the custom alias files are not used.
	/// </summary>
	public bool CombineAliasFiles
	{
		get => _combineAliasFiles;
		set
		{
			if (SetProperty(ref _combineAliasFiles, value))
			{
				MarkDirty();
				RefreshFeBuddyAliasFiles();
			}
		}
	}

	/// <summary>The custom alias files, in merge order.</summary>
	public ObservableCollection<AliasSourceRow> Sources { get; } = [];

	/// <summary>Whether there is at least one custom alias file.</summary>
	public bool HasSources => Sources.Count > 0;

	/// <summary>"None", then every saved credential, for a web address's drop-down.</summary>
	public ObservableCollection<CredentialChoice> Credentials { get; } = [];

	/// <summary>
	/// Every FE-Buddy alias file, and whether it goes into <c>Combined_Alias.txt</c> with the settings
	/// on the other tabs right now. Refreshed each time this tab is shown.
	/// </summary>
	public ObservableCollection<FeBuddyAliasFileRow> FeBuddyAliasFiles { get; } = [];

	/// <summary>One line on how many of FE-Buddy's alias files go in, e.g. <c>3 of 7 FE-Buddy alias files go in.</c></summary>
	public string FeBuddyAliasSummary
	{
		get
		{
			int added = FeBuddyAliasFiles.Count(row => row.IsAdded);

			return !CombineAliasFiles
				? $@"Combining is off, so each alias file stays on its own in {AiracOutputPaths.AliasFolder}\."
				: added == 0
					? $"None of FE-Buddy's alias files go into {AiracOutputPaths.CombinedAliasFileName}, so it will hold only your custom aliases. " +
						"To add one, tick its sub-service and its Alias box on the General tab."
					: $"{added} of {FeBuddyAliasFiles.Count} FE-Buddy alias files go in, ahead of your custom aliases.";
		}
	}

	/// <summary>Whether combining is on but no FE-Buddy alias file goes into <c>Combined_Alias.txt</c>.</summary>
	public bool HasNoFeBuddyAliasFiles => CombineAliasFiles && FeBuddyAliasFiles.All(row => !row.IsAdded);

	/// <summary>Adds files on this PC, chosen in a file dialog.</summary>
	public ICommand AddFileCommand { get; }

	/// <summary>Adds an empty web address row.</summary>
	public ICommand AddUrlCommand { get; }

	private static Window? Owner => Application.Current?.MainWindow;

	// ================= ISubServiceRunTarget =================

	/// <inheritdoc />
	/// <remarks>
	/// The block goes on <see cref="AiracServiceSettings.ConcatenateAliases"/>. The custom alias files go in
	/// only while combining: nothing else reads them.
	/// </remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> block = new(StringComparer.OrdinalIgnoreCase)
		{
			[ConcatenateAliasesSettingsParser.CombineAliasFilesKey] = YesNo(CombineAliasFiles),
		};

		if (!CombineAliasFiles)
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
	public void SetReadiness(bool ready)
	{
	}

	/// <inheritdoc />
	/// <remarks>Does nothing: nothing on this tab is built from the selected cycle.</remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		if (result.CombinedAlias is not { } merged)
		{
			return CombineAliasFiles
				? null
				: new SubServiceRunResult(Title, $"Combining is off, so {AiracOutputPaths.CombinedAliasFileName} was not written", []);
		}

		string feBuddy = merged.FeBuddyFiles.Count > 0
			? $"{merged.FeBuddyCommandCount:N0} command(s) from {string.Join(", ", merged.FeBuddyFiles)}"
			: "no FE-Buddy alias file";

		// Named as it was written: the user may have renamed it on the File Names tab. In the
		// file's own order: FE-Buddy's aliases, then the custom files.
		string summary = merged.FilePath is null
			? $"{AiracOutputPaths.CombinedAliasFileName} not written"
			: $"{Path.GetFileName(merged.FilePath)}: {feBuddy}, then {merged.CustomCommandCount:N0} command(s) from " +
				$"{merged.CustomFilesMerged} of {merged.CustomFileCount} custom alias file(s)";

		return new SubServiceRunResult(Title, summary, merged.Messages);
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		if (!CombineAliasFiles)
		{
			return [new ServicePreviewSection(Title, [new ServicePreviewRow("Combine alias files", $@"No: each alias file stays on its own in {AiracOutputPaths.AliasFolder}\")])];
		}

		List<ServicePreviewRow> rows =
		[
			new ServicePreviewRow("Combine alias files", $"Yes: {OutputFile}, every FE-Buddy alias file this run writes, then these files"),
		];

		if (Sources.Count == 0)
		{
			rows.Add(new ServicePreviewRow("Custom alias files", "None"));
		}

		foreach (AliasSourceRow row in Sources)
		{
			rows.Add(new ServicePreviewRow($"Custom alias file {row.Number}", Describe(row)));
		}

		RefreshFeBuddyAliasFiles();

		foreach (FeBuddyAliasFileRow file in FeBuddyAliasFiles)
		{
			rows.Add(new ServicePreviewRow(file.FileName, file.Status));
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
			_combineAliasFiles = GetBool(ConcatenateAliasesSettingsParser.CombineAliasFilesKey, true);
			OnPropertyChanged(nameof(CombineAliasFiles));
		}
		finally
		{
			_loading = false;
		}

		RefreshRowHints();
		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig()
	{
		Set(ConcatenateAliasesSettingsParser.CombineAliasFilesKey, YesNo(CombineAliasFiles));

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
		// The custom alias files are only read while combining, so only then held against the user.
		foreach (AliasSourceRow row in Sources)
		{
			row.Error = CombineAliasFiles ? ErrorOf(row) ?? LocalFileErrorOf(row) : null;

			if (row.Error is { } error)
			{
				validation.Add($"Custom alias file {row.Number}: {error}");
			}
		}
	}

	// ================= FE-Buddy's alias files =================

	/// <summary>
	/// Lets the tab see the AIRAC Service's other tabs, to list which alias files they put into
	/// <c>Combined_Alias.txt</c>, and open one.
	/// </summary>
	/// <param name="openTabFor">The sub-service's tab when it is included on the General tab, otherwise <see langword="null"/>.</param>
	/// <param name="showTab">Shows a tab.</param>
	internal void AttachToService(Func<SubServiceDescriptor, ServiceTabViewModel?> openTabFor, Action<ServiceTabViewModel> showTab)
	{
		_openTabFor = openTabFor;
		_showTab = showTab;
		RefreshFeBuddyAliasFiles();
	}

	/// <summary>Re-reads, from the other tabs, which of FE-Buddy's alias files go into <c>Combined_Alias.txt</c>.</summary>
	internal void RefreshFeBuddyAliasFiles()
	{
		FeBuddyAliasFiles.Clear();

		foreach (SubServiceDescriptor descriptor in AiracSubServices.All.Where(d => d.AliasFileName is not null))
		{
			string fileName = descriptor.AliasFileName!;
			ServiceTabViewModel? tab = _openTabFor?.Invoke(descriptor);

			(string status, bool added) = tab switch
			{
				null => ("Not included on the General tab", false),
				GeojsonSubServiceViewModel { WritesAliasFile: false } => ("Alias file turned off on the General tab", false),
				_ when !CombineAliasFiles => ("Written on its own: combining is off", false),
				_ => ($"Goes into {AiracOutputPaths.CombinedAliasFileName}", true),
			};

			if (tab is { IsDirty: true })
			{
				status += " (that tab has unsaved changes)";
			}

			ICommand? open = tab is not null && _showTab is { } show ? new RelayCommand(() => show(tab)) : null;
			FeBuddyAliasFiles.Add(new FeBuddyAliasFileRow(descriptor.DisplayName, fileName, status, added, open));
		}

		OnPropertyChanged(nameof(FeBuddyAliasSummary));
		OnPropertyChanged(nameof(HasNoFeBuddyAliasFiles));

		// Whether the tab is valid depends on the other tabs too (see Validate).
		Revalidate();
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
			AliasSourceLoad load = await AliasSourceLoader.LoadAsync(source, _store, _httpClient);

			if (IsStill(row, source))
			{
				row.SetCheck(
					load.Succeeded,
					load.Succeeded ? $"Read {load.CommandCount:N0} alias command(s)." : load.Problem ?? "It could not be read.",
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
	/// <see cref="RefreshRowHints"/>): the run says what happened to it.
	/// </summary>
	private string? LocalFileErrorOf(AliasSourceRow row)
	{
		string path = row.Location.Trim();

		if (!row.IsFile || !Path.IsPathFullyQualified(path))
		{
			return null;
		}

		try
		{
			FileInfo file = new(path);

			if (!file.Exists)
			{
				return null;
			}

			if (!_localFileChecks.TryGetValue(path, out (DateTime Written, long Length, bool HasCommands) known)
				|| known.Written != file.LastWriteTimeUtc
				|| known.Length != file.Length)
			{
				known = (file.LastWriteTimeUtc, file.Length, CombinedAliasFileWriter.CountCommands(File.ReadAllText(path)) > 0);
				_localFileChecks[path] = known;
			}

			return known.HasCommands ? null : NoAliasCommandsMessage;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return null;
		}
	}

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
				row.IsFile && Path.IsPathFullyQualified(location) && !File.Exists(location)
					? "This file is not on this PC. It may have been moved or renamed."
				: row.IsUrl && row.CredentialId != Guid.Empty && _credentialsError is not null
					? $"Windows Credential Manager could not be read, so its credential cannot be checked: {_credentialsError}"
				: row.IsUrl && row.CredentialId != Guid.Empty && !_savedCredentials.ContainsKey(row.CredentialId)
					? "Its credential is not on this PC: it was removed, or the settings came from another PC. Choose one of yours."
				: null;

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
