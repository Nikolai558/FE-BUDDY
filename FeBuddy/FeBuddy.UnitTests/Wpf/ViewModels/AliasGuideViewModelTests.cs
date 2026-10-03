using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="AliasGuideViewModel"/>: the page shows the guide the export writes, naming the
/// facility from Settings ▸ Facility Profile, and Back runs what the page was given. The export button
/// opens a dialog, so it is not run here. Uses a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class AliasGuideViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AliasGuidePage_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public AliasGuideViewModelTests()
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

	[Fact]
	public void the_page_shows_the_guide_the_export_writes()
	{
		AliasGuideDocument exported = AliasGuideContent.Build(null);

		AliasGuideViewModel page = new(() => { });

		Assert.Equal(exported.Title, page.Document.Title);
		Assert.Equal(exported.Lead, page.Document.Lead);
		Assert.Equal(exported.ReadingNotes, page.Document.ReadingNotes);
		Assert.Equal(exported.Sections.Select(section => section.Title), page.Document.Sections.Select(section => section.Title));
		Assert.Contains("your facility's alias file", page.Document.Lead, StringComparison.Ordinal);
	}

	[Fact]
	public void the_guide_names_the_facility_from_settings()
	{
		UserConfigFile.TrySetValue(SettingsViewModel.ArtccKey, "ZOB");

		AliasGuideViewModel page = new(() => { });

		Assert.Contains("the ZOB alias file", page.Document.Lead, StringComparison.Ordinal);
		Assert.Equal("ZOB", AliasGuideExport.Facility);
	}

	[Fact]
	public void back_runs_the_action_the_page_was_given()
	{
		int calls = 0;
		AliasGuideViewModel page = new(() => calls++);

		page.BackCommand.Execute(null);

		Assert.Equal(1, calls);
		Assert.True(page.ExportGuideCommand.CanExecute(null));
	}
}
