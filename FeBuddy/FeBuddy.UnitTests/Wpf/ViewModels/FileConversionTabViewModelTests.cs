using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers what <see cref="FileConversionTabViewModel"/> does for every conversion page, through a stand-in
/// page: a folder count that fails for a reason Core doesn't catch - against a throwaway config and log.
/// </summary>
[Collection("AppLog")]
public sealed class FileConversionTabViewModelTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_ConversionTab_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public FileConversionTabViewModelTests()
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

	/// <summary>
	/// A count that fails for a reason Core doesn't catch says the folder can't be read, and why, rather than
	/// counting for good with the run held back.
	/// </summary>
	[Fact]
	public async Task a_count_that_fails_says_the_folder_cant_be_read()
	{
		FailingCountPage page = new() { SourceFolder = _root };

		await page.CountFolderAsync();

		Assert.Equal("This folder can't be read: boom", page.FolderSummary);
		Assert.Equal("The source folder can't be read: boom", page.RunBlocker);
	}

	/// <summary>A conversion page whose folder count throws.</summary>
	private sealed class FailingCountPage() : FileConversionTabViewModel("VideoMap", writesSymbols: false, writesText: false)
	{
		public override string NodePath => "Services.FileConversions.Test";

		public override string Title => "Test";

		public override string RunLabel => "Convert";

		public override string FileTypeLabel => ".test";

		protected override IReadOnlyList<string> Extensions => [".test"];

		protected override string FileDescription => "test files";

		protected override SourceFolderCount CountSourceFiles(string folder, CancellationToken cancellationToken) =>
			throw new InvalidOperationException("boom");

		public override ServiceResult Execute(IReadOnlyDictionary<string, string> settings, Action<string, string, bool> reportStep) =>
			throw new NotSupportedException();
	}
}
