using System.Net;

using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Add Custom Aliases tab (<see cref="ConcatenateAliasesViewModel"/>): the custom alias
/// files are added to start, and the choice is saved; what the run is sent while they aren't added; a
/// GitHub address shown as its Raw link, the help <b>Check</b> offers when GitHub refuses a file, and a
/// file on this PC with no alias commands - against a throwaway config, an empty credential store and
/// canned downloads.
/// </summary>
[Collection("AppLog")]
public sealed class ConcatenateAliasesViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Concatenate_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public ConcatenateAliasesViewModelTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	private static ConcatenateAliasesViewModel NewTab() => new(new CredentialStore(new InMemoryCredentialVault()));

	/// <summary>A tab with one custom alias file, a web address.</summary>
	private static ConcatenateAliasesViewModel TabWithCustomFile(string url)
	{
		ConcatenateAliasesViewModel tab = NewTab();
		tab.AddUrlCommand.Execute(null);
		tab.Sources[0].Location = url;
		return tab;
	}

	[Fact]
	public void the_custom_files_are_added_to_start_and_the_run_gets_them()
	{
		ConcatenateAliasesViewModel tab = TabWithCustomFile("https://example.com/ZOB-Alias.txt");

		Assert.True(tab.AddCustomAliasFiles);
		Assert.Equal("Add Custom Aliases", tab.Title);
		Assert.Equal(@"Aliases\Combined_Alias.txt", ConcatenateAliasesViewModel.OutputFile);

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock();
		Assert.Equal("Y", block["AddCustomAliasFiles"]);
		Assert.Equal("https://example.com/ZOB-Alias.txt", block["Sources.1.Url"]);
		Assert.Equal(
			[@"Added to `Aliases\Combined_Alias.txt`, after every FE-Buddy alias file this run writes", "https://example.com/ZOB-Alias.txt, with no credential"],
			Assert.Single(tab.BuildPreviewSummary()).Rows.Select(row => row.Value));
	}

	/// <summary>Not added, the custom files are neither sent nor checked: nothing would read them.</summary>
	[Fact]
	public void with_the_custom_files_not_added_they_are_not_sent_or_checked()
	{
		ConcatenateAliasesViewModel tab = TabWithCustomFile(string.Empty);
		Assert.Equal("Custom alias file 1: Enter the file's web address.", tab.ValidationError);

		tab.AddCustomAliasFiles = false;

		Assert.Null(tab.ValidationError);
		Assert.Null(tab.Sources[0].Error);
		Assert.Equal(new Dictionary<string, string> { ["AddCustomAliasFiles"] = "N" }, tab.BuildSettingsBlock());
		Assert.Equal(
			@"Not added: `Aliases\Combined_Alias.txt` holds only FE-Buddy's alias files",
			Assert.Single(Assert.Single(tab.BuildPreviewSummary()).Rows).Value);

		AiracServiceResult result = new()
		{
			OutputDirectory = _root,
			Messages = [],
			Elapsed = TimeSpan.Zero,
			CombinedAlias = new CombinedAliasResult
			{
				FilePath = Path.Combine(_root, "Aliases", "Combined_Alias.txt"),
				FeBuddyFiles = ["Airports.txt"],
				FeBuddyCommandCount = 3,
				Messages = [],
				Elapsed = TimeSpan.Zero,
			},
		};

		Assert.Equal(
			"Combined_Alias.txt: 3 command(s) from Airports.txt. Your custom alias files weren't added",
			tab.DescribeRunResult(result)?.Summary);
		Assert.Null(tab.DescribeRunResult(new AiracServiceResult { OutputDirectory = _root, Messages = [], Elapsed = TimeSpan.Zero }));
	}

	/// <summary>The choice is saved under its first name, CombineAliasFiles, so an earlier "off" carries over.</summary>
	[Fact]
	public void the_choice_is_saved_and_read_back()
	{
		ConcatenateAliasesViewModel tab = NewTab();
		tab.AddCustomAliasFiles = false;

		Assert.True(tab.Save());

		Assert.Equal("N", UserConfigFile.GetValue("Services.AiracService.ConcatenateAliases.CombineAliasFiles"));
		Assert.False(NewTab().AddCustomAliasFiles);
	}

	/// <summary>Whatever form a GitHub address is pasted in, leaving the box shows the file's Raw link.</summary>
	[Theory]
	[InlineData("https://github.com/vZOB/facility/blob/main/ZOB-Alias.txt", "https://github.com/vZOB/facility/raw/refs/heads/main/ZOB-Alias.txt")]
	[InlineData(" https://raw.githubusercontent.com/vZOB/facility/main/ZOB-Alias.txt ", "https://github.com/vZOB/facility/raw/refs/heads/main/ZOB-Alias.txt")]
	[InlineData("https://example.com/ZOB-Alias.txt", "https://example.com/ZOB-Alias.txt")]
	[InlineData("https://raw.githubusercontent.com/o/r/main/a.txt?token=GHSAT0AAA", "https://raw.githubusercontent.com/o/r/main/a.txt?token=GHSAT0AAA")]
	public void a_github_address_is_shown_as_its_raw_link(string pasted, string shown)
	{
		ConcatenateAliasesViewModel tab = TabWithCustomFile(pasted);

		tab.Sources[0].TidyLocation();

		Assert.Equal(shown, tab.Sources[0].Location);
	}

	/// <summary>
	/// With no credential, GitHub answers "not found" for a private repository, so the row asks
	/// whether it is private, and points to the token guide or to the address.
	/// </summary>
	[Fact]
	public async Task a_github_file_it_cannot_see_asks_whether_the_repository_is_private()
	{
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
		ConcatenateAliasesViewModel tab = new(new CredentialStore(new InMemoryCredentialVault()), client);
		tab.AddUrlCommand.Execute(null);
		AliasSourceRow row = tab.Sources[0];
		row.Location = "https://github.com/vZOB/facility/blob/main/ZOB-Alias.txt";

		await tab.CheckAsync(row);

		Assert.Equal("https://github.com/vZOB/facility/raw/refs/heads/main/ZOB-Alias.txt", row.Location);
		Assert.True(row.CheckFailed);
		Assert.True(row.AsksIfPrivate);

		row.AnswerPrivateCommand.Execute(null);
		Assert.True(row.ShowsPrivateHelp);
		Assert.False(row.AsksIfPrivate);

		row.AnswerPublicCommand.Execute(null);
		Assert.True(row.ShowsPublicHelp);

		// Any edit starts over.
		row.Location += " ";
		Assert.Equal(AliasTroubleshooting.None, row.Troubleshooting);
	}

	/// <summary>With a credential chosen, the question is moot: the row says what to check about the token.</summary>
	[Fact]
	public async Task a_github_file_its_credential_cannot_read_points_to_the_guide()
	{
		CredentialStore store = new(new InMemoryCredentialVault());
		Guid id = store.Save(new CredentialDraft(null, "ZOB GitHub", CredentialKind.GitHubToken, null, "github_pat_test", CredentialHosts.GitHubDefaults)).Id;
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)));
		ConcatenateAliasesViewModel tab = new(store, client);
		tab.AddUrlCommand.Execute(null);
		AliasSourceRow row = tab.Sources[0];
		row.Location = "https://github.com/vZOB/facility/raw/refs/heads/main/ZOB-Alias.txt";
		row.CredentialId = id;

		await tab.CheckAsync(row);

		Assert.True(row.ShowsCredentialHelp);
		Assert.StartsWith("GitHub refused the credential 'ZOB GitHub'.", row.CheckMessage, StringComparison.Ordinal);
	}

	/// <summary>Only GitHub's refusals get the GitHub help; a file that is read gets none.</summary>
	[Theory]
	[InlineData("https://example.com/ZOB-Alias.txt", HttpStatusCode.NotFound)]
	[InlineData("https://github.com/vZOB/facility/blob/main/ZOB-Alias.txt", HttpStatusCode.OK)]
	public async Task other_results_offer_no_github_help(string url, HttpStatusCode status)
	{
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(status) { Content = new StringContent(".zob .msg ZOB") }));
		ConcatenateAliasesViewModel tab = new(new CredentialStore(new InMemoryCredentialVault()), client);
		tab.AddUrlCommand.Execute(null);
		tab.Sources[0].Location = url;

		await tab.CheckAsync(tab.Sources[0]);

		Assert.Equal(AliasTroubleshooting.None, tab.Sources[0].Troubleshooting);
	}

	/// <summary>
	/// A file on this PC with no alias command in it isn't an alias file, so the tab can't be saved with
	/// it. The files are looked at off the UI thread; the tab holds what was found against the user.
	/// </summary>
	[Fact]
	public async Task a_file_on_this_pc_with_no_alias_commands_cannot_be_saved()
	{
		Directory.CreateDirectory(_root);
		string notes = Path.Combine(_root, "Notes.txt");
		File.WriteAllText(notes, "Remember to upload the aliases.\r\n");
		UserConfigFile.TrySetValue("Services.AiracService.ConcatenateAliases.Sources.1.FilePath", notes);
		ConcatenateAliasesViewModel tab = NewTab();
		await tab.CheckLocalFilesAsync();

		Assert.StartsWith("It has no alias commands", tab.Sources[0].Error, StringComparison.Ordinal);
		Assert.StartsWith("Custom alias file 1: It has no alias commands", tab.ValidationError, StringComparison.Ordinal);
		Assert.False(tab.Save());

		File.WriteAllText(notes, ".zob .msg ZOB\r\n");
		await tab.CheckLocalFilesAsync();

		Assert.Null(tab.Sources[0].Error);
		Assert.True(tab.Save());
	}

	/// <summary>
	/// Once the user has fixed the file, <b>Check</b> looks at the files on this PC again: the row's error
	/// goes, and the tab can be saved.
	/// </summary>
	[Fact]
	public async Task checking_a_fixed_file_clears_its_no_alias_commands_error()
	{
		Directory.CreateDirectory(_root);
		string notes = Path.Combine(_root, "Notes.txt");
		File.WriteAllText(notes, "Remember to upload the aliases.\r\n");
		UserConfigFile.TrySetValue("Services.AiracService.ConcatenateAliases.Sources.1.FilePath", notes);
		ConcatenateAliasesViewModel tab = NewTab();
		await tab.CheckLocalFilesAsync();
		Assert.False(tab.Save());

		File.WriteAllText(notes, ".zob .msg ZOB\r\n");
		await tab.CheckAsync(tab.Sources[0]);

		Assert.Equal("Read 1 alias command(s).", tab.Sources[0].CheckMessage);
		Assert.Null(tab.Sources[0].Error);
		Assert.True(tab.Save());
	}

	/// <summary>Two checks of the files can run at once; an older one that finishes last doesn't undo the newer one.</summary>
	[Fact]
	public async Task an_older_check_of_the_files_that_finishes_last_is_not_shown()
	{
		UserConfigFile.TrySetValue("Services.AiracService.ConcatenateAliases.Sources.1.FilePath", Path.Combine(_root, "ZOB-Alias.txt"));
		ConcatenateAliasesViewModel tab = NewTab();
		using ManualResetEventSlim release = new();
		tab.InspectLocalFile = _ =>
		{
			release.Wait();
			return LocalAliasFileState.NoAliasCommands;
		};
		Task older = tab.CheckLocalFilesAsync();

		tab.InspectLocalFile = _ => LocalAliasFileState.HasAliasCommands;
		await tab.CheckLocalFilesAsync();
		release.Set();
		await older;

		Assert.Null(tab.Sources[0].Error);
		Assert.True(tab.Save());
	}

	/// <summary>
	/// Typing a path looks at no file on the UI thread: until the check runs, the tab says nothing about
	/// a file it hasn't looked at, rather than stat or read it on every keystroke.
	/// </summary>
	[Fact]
	public async Task typing_a_path_looks_at_no_file_until_the_check_runs()
	{
		ConcatenateAliasesViewModel tab = NewTab();
		AliasSourceRow row = new(tab, AliasSourceKind.File, string.Empty, Guid.Empty);
		tab.Sources.Add(row);

		row.Location = Path.Combine(_root, "nowhere", "ZOB-Alias.txt");

		Assert.Null(row.Notice);

		await tab.CheckLocalFilesAsync();

		Assert.Equal("This file is not on this PC. It may have been moved or renamed.", row.Notice);
	}

	// ---- the one-line rows (issue #326) ----

	/// <summary>A saved file loads closed: one line with its name, and what's wrong with it in amber.</summary>
	[Fact]
	public async Task a_saved_file_is_one_line_with_its_name_and_status()
	{
		string missing = Path.Combine(_root, "gone", "ZOB-Alias.txt");
		UserConfigFile.TrySetValue("Services.AiracService.ConcatenateAliases.Sources.1.FilePath", missing);
		ConcatenateAliasesViewModel tab = NewTab();
		await tab.CheckLocalFilesAsync();

		AliasSourceRow row = Assert.Single(tab.Sources);

		Assert.False(row.IsExpanded);
		Assert.Equal("ZOB-Alias.txt", row.Title);
		Assert.Equal("1 · ZOB-Alias.txt", row.Header);
		Assert.Equal("This file is not on this PC. It may have been moved or renamed.", row.Status);
		Assert.Equal(AliasSourceTone.Warn, row.StatusTone);
	}

	/// <summary>A new web address opens, ready to be typed in; once it is, the line names its file.</summary>
	[Fact]
	public void a_new_web_address_opens_and_its_line_follows_what_is_typed()
	{
		AliasSourceRow row = TabWithCustomFile(string.Empty).Sources[0];
		List<string?> changed = [];
		row.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		Assert.True(row.IsExpanded);
		Assert.Equal("No address yet", row.Title);
		Assert.Equal("Enter the file's web address.", row.Status);
		Assert.Equal(AliasSourceTone.Danger, row.StatusTone);

		row.Location = "https://github.com/vZOB/facility/raw/refs/heads/main/ZOB%20Alias.txt";

		Assert.Equal("ZOB Alias.txt", row.Title);
		Assert.Equal("Web address", row.Status);
		Assert.Equal(AliasSourceTone.Neutral, row.StatusTone);
		Assert.Contains(nameof(AliasSourceRow.Title), changed);
		Assert.Contains(nameof(AliasSourceRow.Status), changed);
	}

	/// <summary>An address with nothing after the site is shown whole.</summary>
	[Fact]
	public void an_address_with_no_file_name_is_shown_whole()
	{
		Assert.Equal("https://example.com/", TabWithCustomFile("https://example.com/").Sources[0].Title);
	}

	[Fact]
	public void the_line_names_the_credential_and_what_a_check_found()
	{
		CredentialStore store = new(new InMemoryCredentialVault());
		Guid id = store.Save(new CredentialDraft(null, "ZOB GitHub", CredentialKind.GitHubToken, null, "github_pat_test", CredentialHosts.GitHubDefaults)).Id;
		ConcatenateAliasesViewModel tab = new(store);
		tab.AddUrlCommand.Execute(null);
		AliasSourceRow row = tab.Sources[0];
		row.Location = "https://github.com/vZOB/facility/raw/refs/heads/main/ZOB-Alias.txt";
		row.CredentialId = id;
		row.IsExpanded = false;

		Assert.Equal("Web address, read with ZOB GitHub", row.Status);

		row.SetCheck(true, "Read 12 alias command(s).");
		Assert.Equal(("Read 12 alias command(s).", AliasSourceTone.Positive), (row.Status, row.StatusTone));
		Assert.False(row.IsExpanded);

		// The help is in the opened row, so a check that has some opens it.
		row.SetCheck(false, "GitHub refused the credential.", AliasTroubleshooting.CredentialRefused);
		Assert.Equal(AliasSourceTone.Danger, row.StatusTone);
		Assert.True(row.IsExpanded);
	}
}
