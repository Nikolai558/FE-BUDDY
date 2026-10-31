using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers how the sub-service tabs follow the General tab's Alias File Exports card
/// (<see cref="IAliasFileExports"/>): each tab's note that its own alias file isn't exported, the file its
/// What You'll Get lists, and the alias file each sub-service names in the catalogue - against a
/// throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class AliasFileExportsTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AliasFileExports_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public AliasFileExportsTests()
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

	/// <summary>Every sub-service with an alias output names its own alias file, and those are the seven a run can leave unexported.</summary>
	[Fact]
	public void the_catalogue_names_each_sub_services_own_alias_file()
	{
		Assert.All(AiracSubServices.All, descriptor => Assert.Equal(descriptor.Outputs.HasFlag(SubServiceOutputKinds.Alias), descriptor.AliasFile is not null));
		Assert.Equal(
			AiracOutputPaths.SubServiceAliasFileKeys.Order(StringComparer.Ordinal),
			AiracSubServices.All.Select(descriptor => descriptor.AliasFile).OfType<string>().Order(StringComparer.Ordinal));
	}

	/// <summary>
	/// Each tab's note shows only while its alias file is on and Alias File Exports is off, names the tab's
	/// own file and the combined file as the File Names tab has it, and follows the General tab before Save.
	/// </summary>
	[Fact]
	public void each_tabs_note_shows_only_while_its_alias_file_is_on_and_not_exported()
	{
		AiracGeneralTabViewModel general = new();

		foreach (SubServiceDescriptor descriptor in AiracSubServices.All.Where(descriptor => descriptor.AliasFile is not null))
		{
			GeojsonSubServiceViewModel tab = (GeojsonSubServiceViewModel)descriptor.CreateTab();
			SubServiceRow row = general.RowFor(descriptor.Key)!;
			tab.AttachOutputs(row);
			tab.AttachAliasFileExports(general);
			List<string?> changed = Changed(tab);

			Assert.Same(general, tab.AliasFileExports);
			Assert.Equal(
				$"These commands go into `Combined_Alias.txt`. `{descriptor.AliasFile}` itself isn't exported. " +
				"To export it too, turn on **Alias File Exports** on the General tab.",
				tab.UnexportedAliasFileNote);

			general.CombinedAliasFileName = "ZOB vNAS.txt";

			Assert.StartsWith("These commands go into `ZOB vNAS.txt`.", tab.UnexportedAliasFileNote, StringComparison.Ordinal);
			Assert.Contains(nameof(GeojsonSubServiceViewModel.UnexportedAliasFileNote), changed);

			changed.Clear();
			general.ExportIndividualAliasFiles = true;

			Assert.Null(tab.UnexportedAliasFileNote);
			Assert.Contains(nameof(GeojsonSubServiceViewModel.UnexportedAliasFileNote), changed);
			Assert.False(tab.IsDirty);

			general.ExportIndividualAliasFiles = false;
			row.Alias = false;

			Assert.Null(tab.UnexportedAliasFileNote);

			row.Alias = true;
			general.CombinedAliasFileName = AiracOutputPaths.CombinedAliasFileName;

			Assert.NotNull(tab.UnexportedAliasFileNote);
		}
	}

	/// <summary>A tab with no alias file, and one built on its own (which exports it, as before the card), has no note.</summary>
	[Fact]
	public void a_tab_with_no_alias_file_or_built_on_its_own_has_no_note()
	{
		AiracGeneralTabViewModel general = new();
		FixesViewModel fixes = new();
		fixes.AttachOutputs(general.RowFor(AiracSubServices.FixesKey)!);
		fixes.AttachAliasFileExports(general);

		Assert.Null(fixes.UnexportedAliasFileNote);
		Assert.True(new AirportsViewModel().AliasFileExports.ExportIndividualAliasFiles);
		Assert.Equal(AiracOutputPaths.CombinedAliasFileName, new AirportsViewModel().AliasFileExports.CombinedAliasFileName);
		Assert.Null(new AirportsViewModel().UnexportedAliasFileNote);
		Assert.Throws<ArgumentNullException>(() => new AirportsViewModel().AttachAliasFileExports(null!));
	}

	/// <summary>
	/// What You'll Get, and the Preview Settings tab with it, lists the tab's own alias file only while it is
	/// exported; otherwise the combined file its commands go into, by the name the run will write it under.
	/// </summary>
	[Fact]
	public void what_youll_get_lists_the_combined_file_while_the_tabs_own_isnt_exported()
	{
		AiracGeneralTabViewModel general = new();
		AirportsViewModel airports = new() { Area = SubServiceArea.Everything };
		NavaidsViewModel navaids = new();
		airports.AttachOutputs(general.RowFor(AiracSubServices.AirportsKey)!);
		navaids.AttachOutputs(general.RowFor(AiracSubServices.NavaidsKey)!);
		List<string?> changed = Changed(airports);
		airports.AttachAliasFileExports(general);
		navaids.AttachAliasFileExports(general);

		Assert.Equal("Combined_Alias.txt", AliasFile(airports).Name);
		Assert.Contains("`.aptDTW`", AliasFile(airports).Description, StringComparison.Ordinal);
		Assert.Equal("Combined_Alias.txt", AliasFile(navaids).Name);
		Assert.Contains(nameof(ServiceTabViewModel.WhatYoullGet), changed);

		changed.Clear();
		general.ExportIndividualAliasFiles = true;

		Assert.Equal("Airports.txt", AliasFile(airports).Name);
		Assert.Equal("Navaids.txt", AliasFile(navaids).Name);
		Assert.Contains(nameof(ServiceTabViewModel.WhatYoullGet), changed);

		general.ExportIndividualAliasFiles = false;
		general.CombinedAliasFileName = "ZOB vNAS.txt";

		Assert.Equal("ZOB vNAS.txt", AliasFile(airports).Name);
		Assert.Equal(
			"ZOB vNAS.txt",
			airports.BuildPreviewSummary().SelectMany(section => section.WhatYoullGet)
				.Single(block => block.Outputs == SubServiceOutputKinds.Alias).Files.Single().Name);
	}

	/// <summary>The one file a tab's Alias block lists.</summary>
	private static SummaryFile AliasFile(ServiceTabViewModel tab) =>
		tab.WhatYoullGet.Single(block => block.Outputs == SubServiceOutputKinds.Alias).Files.Single();

	/// <summary>Records each property the tab announces.</summary>
	private static List<string?> Changed(ServiceTabViewModel tab)
	{
		List<string?> changed = [];
		tab.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
		return changed;
	}
}
