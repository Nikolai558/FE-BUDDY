using System.Net;

using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Versioning;

namespace FeBuddy.UnitTests.Application.Updates;

/// <summary>
/// Covers <see cref="UpdateInstaller"/> (the download into the temporary workspace, its token
/// fallback and clean-up, and the msiexec arguments) and how <see cref="VersionCheck"/> finds a
/// release's MSI.
/// </summary>
[Collection("AppLog")]
public sealed class UpdateInstallerTests : IDisposable
{
	private static readonly byte[] Payload = [.. Enumerable.Range(0, 200_000).Select(i => (byte)i)];

	private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Update_" + Guid.NewGuid().ToString("N"));

	public UpdateInstallerTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_tempRoot, "logs"));
		TempWorkspace.ConfigureForTesting(_tempRoot);
		Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, null);
	}

	public void Dispose()
	{
		Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, null);
		TempWorkspace.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);
		try
		{
			Directory.Delete(_tempRoot, recursive: true);
		}
		catch (IOException)
		{
		}
	}

	[Fact]
	public void updates_directory_is_in_the_temporary_workspace()
	{
		Assert.Equal(Path.Combine(_tempRoot, "Updates"), UpdateInstaller.UpdatesDirectory);
	}

	[Fact]
	public async Task download_async_writes_the_file_and_reports_progress()
	{
		var progress = new RecordingProgress();
		using HttpClient client = new(new StubHttpHandler(_ => Ok(Payload)));

		string path = await UpdateInstaller.DownloadAsync(Installer(Payload.Length), progress, client);

		Assert.Equal(Path.Combine(UpdateInstaller.UpdatesDirectory, "FE-BUDDY-3.0.0.msi"), path);
		Assert.Equal(Payload, await File.ReadAllBytesAsync(path));
		Assert.NotEmpty(progress.Reports);
		Assert.Equal(new DownloadProgress(Payload.Length, Payload.Length), progress.Reports[^1]);
		Assert.Equal(100, progress.Reports[^1].Percent);
	}

	[Fact]
	public async Task download_async_unknown_length_uses_the_reported_size()
	{
		var progress = new RecordingProgress();
		using HttpClient client = new(new StubHttpHandler(_ => OkWithoutLength(Payload)));

		await UpdateInstaller.DownloadAsync(Installer(Payload.Length), progress, client);

		Assert.Equal(Payload.Length, progress.Reports[^1].TotalBytes);
	}

	[Fact]
	public async Task download_async_no_length_anywhere_still_downloads()
	{
		var progress = new RecordingProgress();
		using HttpClient client = new(new StubHttpHandler(_ => OkWithoutLength(Payload)));

		string path = await UpdateInstaller.DownloadAsync(Installer(sizeBytes: 0), progress, client);

		Assert.Equal(Payload.Length, new FileInfo(path).Length);
		Assert.Null(progress.Reports[^1].TotalBytes);
		Assert.Null(progress.Reports[^1].Percent);
	}

	[Fact]
	public async Task download_async_short_download_fails_and_leaves_no_file()
	{
		using HttpClient client = new(new StubHttpHandler(_ => OkWithoutLength(Payload)));

		await Assert.ThrowsAsync<IOException>(() => UpdateInstaller.DownloadAsync(Installer(Payload.Length + 10), httpClient: client));

		Assert.False(File.Exists(Path.Combine(UpdateInstaller.UpdatesDirectory, "FE-BUDDY-3.0.0.msi")));
	}

	[Fact]
	public async Task download_async_fails_without_a_token_and_does_not_retry()
	{
		int calls = 0;
		using HttpClient client = new(new StubHttpHandler(_ =>
		{
			calls++;
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		}));

		await Assert.ThrowsAsync<HttpRequestException>(() => UpdateInstaller.DownloadAsync(Installer(Payload.Length), httpClient: client));

		Assert.Equal(1, calls);
		Assert.False(File.Exists(Path.Combine(UpdateInstaller.UpdatesDirectory, "FE-BUDDY-3.0.0.msi")));
	}

	[Fact]
	public async Task download_async_public_url_fails_retries_through_the_assets_api_with_the_token()
	{
		Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, "test-token");
		HttpRequestMessage? retry = null;
		using HttpClient client = new(new StubHttpHandler(request =>
		{
			if (request.Headers.Authorization is null)
			{
				return new HttpResponseMessage(HttpStatusCode.NotFound);
			}

			retry = request;
			return Ok(Payload);
		}));

		string path = await UpdateInstaller.DownloadAsync(Installer(Payload.Length), httpClient: client);

		Assert.Equal(Payload, await File.ReadAllBytesAsync(path));
		Assert.Equal("https://api.github.com/repos/Nikolai558/FE-BUDDY/releases/assets/42", retry!.RequestUri!.ToString());
		Assert.Equal("test-token", retry.Headers.Authorization!.Parameter);
		Assert.Contains(retry.Headers.Accept, a => a.MediaType == "application/octet-stream");
	}

	[Fact]
	public async Task download_async_no_asset_id_does_not_retry_with_the_token()
	{
		Environment.SetEnvironmentVariable(GitHubAuth.EnvironmentVariableName, "test-token");
		int calls = 0;
		using HttpClient client = new(new StubHttpHandler(_ =>
		{
			calls++;
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		}));

		await Assert.ThrowsAsync<HttpRequestException>(() =>
			UpdateInstaller.DownloadAsync(Installer(Payload.Length) with { AssetId = 0 }, httpClient: client));

		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task download_async_cancelled_leaves_no_file()
	{
		using CancellationTokenSource cts = new();
		var progress = new CancellingProgress(cts);
		using HttpClient client = new(new StubHttpHandler(_ => Ok(Payload)));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			UpdateInstaller.DownloadAsync(Installer(Payload.Length), progress, client, cts.Token));

		Assert.False(File.Exists(Path.Combine(UpdateInstaller.UpdatesDirectory, "FE-BUDDY-3.0.0.msi")));
	}

	[Theory]
	[InlineData("")]
	[InlineData("FE-BUDDY-3.0.0.exe")]
	[InlineData(@"..\FE-BUDDY-3.0.0.msi")]
	[InlineData("sub/FE-BUDDY-3.0.0.msi")]
	public async Task download_async_rejects_anything_but_a_plain_msi_name(string fileName)
	{
		await Assert.ThrowsAsync<ArgumentException>(() =>
			UpdateInstaller.DownloadAsync(Installer(Payload.Length) with { FileName = fileName }));
	}

	[Fact]
	public async Task download_async_null_installer_throws()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() => UpdateInstaller.DownloadAsync(null!));
	}

	[Fact]
	public void installer_arguments_install_with_every_file_copied()
	{
		Assert.Equal(
			"/i \"C:\\Temp\\FE-Buddy\\Updates\\FE-BUDDY-3.0.0.msi\" REINSTALLMODE=amus",
			UpdateInstaller.InstallerArguments(@"C:\Temp\FE-Buddy\Updates\FE-BUDDY-3.0.0.msi"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData(" ")]
	public void installer_arguments_need_a_path(string? path)
	{
		Assert.ThrowsAny<ArgumentException>(() => UpdateInstaller.InstallerArguments(path!));
	}

	/// <summary>
	/// The latest release's first plain .msi asset is reported; other assets, unsafe names and
	/// missing fields are skipped; a release without one reports none.
	/// </summary>
	[Theory]
	[InlineData("""[ { "name": "FE-BUDDYSetup.exe", "browser_download_url": "https://x.test/setup.exe" }, { "name": "FE-BUDDY-3.1.0.msi", "browser_download_url": "https://x.test/a.msi", "id": 7, "size": 99 } ]""", "FE-BUDDY-3.1.0.msi", 7L, 99L)]
	[InlineData("""[ { "name": "..\\evil.msi", "browser_download_url": "https://x.test/e.msi" }, { "name": "FE-BUDDY.MSI", "browser_download_url": "https://x.test/b.msi" } ]""", "FE-BUDDY.MSI", 0L, 0L)]
	[InlineData("""[ { "name": "no-url.msi" }, { "browser_download_url": "https://x.test/no-name.msi" } ]""", null, 0L, 0L)]
	[InlineData("""[]""", null, 0L, 0L)]
	[InlineData("null", null, 0L, 0L)]
	public async Task version_check_finds_the_latest_releases_installer(string assetsJson, string? expectedName, long expectedId, long expectedSize)
	{
		string releasesJson = $$"""
		[
		  { "tag_name": "3.1.0", "draft": false, "assets": {{assetsJson}} },
		  { "tag_name": "3.0.5", "draft": false, "assets": [ { "name": "old.msi", "browser_download_url": "https://x.test/old.msi" } ] }
		]
		""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.Equal(expectedName, result.LatestInstaller?.FileName);
		if (expectedName is not null)
		{
			Assert.Equal(expectedId, result.LatestInstaller!.AssetId);
			Assert.Equal(expectedSize, result.LatestInstaller.SizeBytes);
		}
	}

	[Fact]
	public async Task version_check_release_without_assets_has_no_installer()
	{
		const string releasesJson = """[ { "tag_name": "3.1.0", "draft": false } ]""";
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releasesJson) }));

		VersionCheckResult result = await VersionCheck.RunAsync("3.0.0", ReleaseChannel.Stable, hasInternetConnection: true, client);

		Assert.True(result.UpdateAvailable);
		Assert.Null(result.LatestInstaller);
	}

	private static ReleaseInstaller Installer(long sizeBytes) =>
		new("FE-BUDDY-3.0.0.msi", "https://github.com/Nikolai558/FE-BUDDY/releases/download/3.0.0/FE-BUDDY-3.0.0.msi", 42, sizeBytes);

	private static HttpResponseMessage Ok(byte[] body) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(body) };

	// A body whose length is not known up front, like a chunked response.
	private static HttpResponseMessage OkWithoutLength(byte[] body) =>
		new(HttpStatusCode.OK) { Content = new StreamContent(new UnseekableStream(body)) };

	private sealed class RecordingProgress : IProgress<DownloadProgress>
	{
		public List<DownloadProgress> Reports { get; } = [];

		public void Report(DownloadProgress value) => Reports.Add(value);
	}

	private sealed class CancellingProgress(CancellationTokenSource cts) : IProgress<DownloadProgress>
	{
		public void Report(DownloadProgress value) => cts.Cancel();
	}

	private sealed class UnseekableStream(byte[] data) : MemoryStream(data)
	{
		public override bool CanSeek => false;
	}
}
