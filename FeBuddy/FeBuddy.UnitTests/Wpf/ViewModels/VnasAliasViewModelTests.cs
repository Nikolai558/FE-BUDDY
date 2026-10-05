using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Concatenate Aliases tab (<see cref="VnasAliasViewModel"/>): combining is on to start
/// and saved, which FE-Buddy alias files go into <c>Combined_Alias.txt</c>, and what the run is sent
/// while combining is off - against a throwaway config and an empty credential store.
/// </summary>
[Collection("AppLog")]
public sealed class VnasAliasViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Concatenate_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public VnasAliasViewModelTests()
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

	private static VnasAliasViewModel NewTab() => new(new CredentialStore(new InMemoryCredentialVault()));

	/// <summary>A tab with one custom alias file, a web address.</summary>
	private static VnasAliasViewModel TabWithCustomFile(string url)
	{
		VnasAliasViewModel tab = NewTab();
		tab.AddUrlCommand.Execute(null);
		tab.Sources[0].Location = url;
		return tab;
	}

	[Fact]
	public void combining_is_on_to_start_and_the_run_gets_the_custom_files()
	{
		VnasAliasViewModel tab = TabWithCustomFile("https://example.com/ZOB-Alias.txt");

		Assert.True(tab.CombineAliasFiles);
		Assert.Equal("Concatenate Aliases", tab.Title);
		Assert.Equal(@"Aliases\Combined_Alias.txt", VnasAliasViewModel.OutputFile);

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock();
		Assert.Equal("Y", block["CombineAliasFiles"]);
		Assert.Equal("https://example.com/ZOB-Alias.txt", block["Sources.1.Url"]);
	}

	/// <summary>Off, the custom files are neither sent nor checked: nothing would read them.</summary>
	[Fact]
	public void with_combining_off_the_custom_files_are_not_sent_or_checked()
	{
		VnasAliasViewModel tab = TabWithCustomFile(string.Empty);
		Assert.Equal("Custom alias file 1: Enter the file's web address.", tab.ValidationError);

		tab.CombineAliasFiles = false;

		Assert.Null(tab.ValidationError);
		Assert.Null(tab.Sources[0].Error);
		Assert.Equal(new Dictionary<string, string> { ["CombineAliasFiles"] = "N" }, tab.BuildSettingsBlock());
		Assert.Equal(
			@"No: each alias file stays on its own in Aliases\",
			Assert.Single(Assert.Single(tab.BuildPreviewSummary()).Rows).Value);

		SubServiceRunResult? review = tab.DescribeRunResult(new AiracServiceResult { OutputDirectory = _root, Messages = [], Elapsed = TimeSpan.Zero });
		Assert.Equal("Combining is off, so Combined_Alias.txt was not written", review?.Summary);
	}

	[Fact]
	public void the_choice_is_saved_and_read_back()
	{
		VnasAliasViewModel tab = NewTab();
		tab.CombineAliasFiles = false;

		Assert.True(tab.Save());

		Assert.Equal("N", UserConfigFile.GetValue("Services.AiracService.VnasAlias.CombineAliasFiles"));
		Assert.False(NewTab().CombineAliasFiles);
	}

	/// <summary>Each FE-Buddy alias file says whether it goes in, and if not, why not.</summary>
	[Fact]
	public void each_fe_buddy_alias_file_says_whether_it_goes_in()
	{
		AiracGeneralTabViewModel general = new();
		AirportsViewModel airports = new();
		airports.AttachOutputs(general.RowFor(AiracSubServices.AirportsKey)!);
		ArrivalsViewModel arrivals = new();
		arrivals.AttachOutputs(general.RowFor(AiracSubServices.ArrivalsKey)!);
		general.RowFor(AiracSubServices.ArrivalsKey)!.Alias = false;

		VnasAliasViewModel tab = NewTab();
		tab.AttachToService(
			d => d.Key switch
			{
				AiracSubServices.AirportsKey => airports,
				AiracSubServices.ArrivalsKey => arrivals,
				_ => null,
			},
			_ => { });

		Assert.Equal(("Goes into Combined_Alias.txt", true), Status(tab, "Airports.txt"));
		Assert.Equal(("Alias file turned off on the General tab", false), Status(tab, "Arrivals.txt"));
		Assert.Equal(("Not included on the General tab", false), Status(tab, "Telephony.txt"));
		Assert.Equal("1 of 7 FE-Buddy alias files go in, ahead of your custom aliases.", tab.FeBuddyAliasSummary);
		Assert.False(tab.HasNoFeBuddyAliasFiles);

		tab.CombineAliasFiles = false;

		Assert.Equal(("Written on its own: combining is off", false), Status(tab, "Airports.txt"));
		Assert.Equal(@"Combining is off, so each alias file stays on its own in Aliases\.", tab.FeBuddyAliasSummary);
		Assert.False(tab.HasNoFeBuddyAliasFiles);
	}

	private static (string Status, bool IsAdded) Status(VnasAliasViewModel tab, string fileName)
	{
		FeBuddyAliasFileRow row = tab.FeBuddyAliasFiles.Single(file => file.FileName == fileName);
		return (row.Status, row.IsAdded);
	}
}
