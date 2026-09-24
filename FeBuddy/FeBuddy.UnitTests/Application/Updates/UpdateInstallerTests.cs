using System.Net;
using System.Net.Http;

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
	private static readonly byte[] Payload = Enumerable.Range(0, 200_000).Select(i => (byte)i).ToArray();

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
	public void UpdatesDirectory_IsInTheTemporaryWorkspace()
	{
		Assert.Equal(Path.Combine(_tempRoot, "Updates"), UpdateInstaller.UpdatesDirectory);
	}

	[Fact]
	public async Task DownloadAsync_WritesTheFileAndReportsProgress()
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
	public async Task DownloadAsync_UnknownLength_UsesTheReportedSize()
	{
		var progress = new RecordingProgress();
		using HttpClient client = new(new StubHttpHandler(_ => OkWithoutLength(Payload)));

		await UpdateInstaller.DownloadAsync(Installer(Payload.Length), progress, client);

		Assert.Equal(Payload.Length, progress.Reports[^1].TotalBytes);
	}

	[Fact]
	public async Task DownloadAsync_NoLengthAnywhere_StillDownloads()
	{
		var progress = new RecordingProgress();
		using HttpClient client = new(new StubHttpHandler(_ => OkWithoutLength(Payload)));

		string path = await UpdateInstaller.DownloadAsync(Installer(sizeBytes: 0), progress, client);

		Assert.Equal(Payload.Length, new FileInfo(path).Length);
		Assert.Null(progress.Reports[^1].TotalBytes);
		Assert.Null(progress.Reports[^1].Percent);
	}

	[Fact]
	public async Task DownloadAsync_ShortDownload_FailsAndLeavesNoFile()
	{
		using HttpClient client = new(new StubHttpHandler(_ => OkWithoutLength(Payload)));

		await Assert.ThrowsAsync<IOException>(() => UpdateInstaller.DownloadAsync(Installer(Payload.Length + 10), httpClient: client));

		Assert.False(File.Exists(Path.Combine(UpdateInstaller.UpdatesDirectory, "FE-BUDDY-3.0.0.msi")));
	}

	[Fact]
	public async Task DownloadAsync_FailsWithoutAToken_AndDoesNotRetry()
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
	public async Task DownloadAsync_PublicUrlFails_RetriesThroughTheAssetsApiWithTheToken()
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
	public async Task DownloadAsync_NoAssetId_DoesNotRetryWithTheToken()
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
	public async Task DownloadAsync_Cancelled_LeavesNoFile()
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
	public async Task DownloadAsync_RejectsAnythingButAPlainMsiName(string fileName)
	{
		await Assert.ThrowsAsync<ArgumentException>(() =>
			UpdateInstaller.DownloadAsync(Installer(Payload.Length) with { FileName = fileName }));
	}

	[Fact]
	public async Task DownloadAsync_NullInstaller_Throws()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() => UpdateInstaller.DownloadAsync(null!));
	}

	[Fact]
	public void InstallerArguments_InstallWithEveryFileCopied()
	{
		Assert.Equal(
			"/i \"C:\\Temp\\FE-Buddy\\Updates\\FE-BUDDY-3.0.0.msi\" REINSTALLMODE=amus",
			UpdateInstaller.InstallerArguments(@"C:\Temp\FE-Buddy\Updates\FE-BUDDY-3.0.0.msi"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData(" ")]
	public void InstallerArguments_NeedAPath(string? path)
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
	public async Task VersionCheck_FindsTheLatestReleasesInstaller(string assetsJson, string? expectedName, long expectedId, long expectedSize)
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
	public async Task VersionCheck_ReleaseWithoutAssets_HasNoInstaller()
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
