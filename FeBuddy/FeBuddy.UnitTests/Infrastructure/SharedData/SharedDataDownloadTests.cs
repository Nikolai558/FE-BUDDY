using System.Net;
using System.Text;

using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.SharedData;
using FeBuddy.Core.Infrastructure.SharedData.Models;

namespace FeBuddy.UnitTests.Infrastructure.SharedData;

/// <summary>
/// Exercises <see cref="SharedDataDownload.RefreshAsync"/> directly against a local
/// <see cref="HttpListener"/>: a successful download replacing and stamping the kept copy, a
/// throwing <c>prepare</c> or <c>validate</c> keeping the old copy, an HTTP error with no earlier
/// copy, argument validation, cancellation, best-effort temp cleanup, <see cref="SharedDataDownload.SharedDataDirectory"/>,
/// and the <see cref="SharedDataRefreshResult"/> truth table.
/// </summary>
[Collection("AppLog")]
public sealed class SharedDataDownloadTests : IDisposable
{
	private readonly string _testRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_SharedDataDl_" + Guid.NewGuid().ToString("N"));

	public SharedDataDownloadTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_testRoot, "logs"));
		TempWorkspace.ConfigureForTesting(Path.Combine(_testRoot, "temp"));
	}

	public void Dispose()
	{
		TempWorkspace.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			if (Directory.Exists(_testRoot))
			{
				Directory.Delete(_testRoot, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	/// <summary>A prepare step that just copies the download as-is - the shape most real callers use.</summary>
	private static void CopyAsIs(string downloadedPath, string preparedPath) => File.Copy(downloadedPath, preparedPath, overwrite: true);

	/// <summary>A validate step that accepts anything.</summary>
	private static void AcceptAnything(string preparedPath)
	{
	}

	/// <summary>
	/// Starts a minimal local HTTP server that serves <paramref name="payload"/> on every request,
	/// or a 500 for every request when <paramref name="fail"/> is set.
	/// </summary>
	private static (HttpListener Listener, string Url) StartServer(byte[]? payload, bool fail = false)
	{
		HttpListener listener = new();
		string url;

		while (true)
		{
			int port = GetFreeTcpPort();
			url = $"http://127.0.0.1:{port}/data";
			listener.Prefixes.Clear();
			listener.Prefixes.Add($"http://127.0.0.1:{port}/");

			try
			{
				listener.Start();
				break;
			}
			catch (HttpListenerException)
			{
				// Port was taken between GetFreeTcpPort() and Start(); try another.
			}
		}

		_ = Task.Run(async () =>
		{
			try
			{
				while (listener.IsListening)
				{
					HttpListenerContext context = await listener.GetContextAsync();

					if (fail)
					{
						context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
						context.Response.OutputStream.Close();
						continue;
					}

					context.Response.ContentLength64 = payload!.Length;
					await context.Response.OutputStream.WriteAsync(payload);
					context.Response.OutputStream.Close();
				}
			}
			catch (Exception) when (!listener.IsListening)
			{
				// Expected once the listener is stopped mid-GetContextAsync.
			}
		});

		return (listener, url);
	}

	private static int GetFreeTcpPort()
	{
		System.Net.Sockets.TcpListener tcpListener = new(IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}

	// ---- success ----

	[Fact]
	public async Task a_successful_refresh_replaces_the_destination_and_stamps_its_last_write_time_with_the_download()
	{
		string destination = Path.Combine(_testRoot, "kept", "data.txt");
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		File.WriteAllText(destination, "old copy");
		File.SetLastWriteTimeUtc(destination, DateTime.UtcNow.AddDays(-5));

		(HttpListener listener, string url) = StartServer(Encoding.UTF8.GetBytes("new payload"));

		try
		{
			DateTime before = DateTime.UtcNow;
			SharedDataRefreshResult result = await SharedDataDownload.RefreshAsync(
				url, destination, CopyAsIs, AcceptAnything, "test data", CancellationToken.None);
			DateTime after = DateTime.UtcNow;

			Assert.True(result.IsFresh);
			Assert.True(result.HasCopy);
			Assert.Equal(destination, result.FilePath);
			Assert.Null(result.FailureReason);
			Assert.InRange(result.DownloadedUtc!.Value, before.AddSeconds(-1), after.AddSeconds(1));
			Assert.Equal(File.GetLastWriteTimeUtc(destination), result.DownloadedUtc);
			Assert.Equal("new payload", File.ReadAllText(destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- prepare / validate throwing ----

	[Fact]
	public async Task a_prepare_that_throws_keeps_the_old_copy_and_reports_why()
	{
		string destination = Path.Combine(_testRoot, "kept", "prepare-fail.txt");
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		File.WriteAllText(destination, "kept copy");
		DateTime oldWriteUtc = DateTime.UtcNow.AddDays(-2);
		File.SetLastWriteTimeUtc(destination, oldWriteUtc);

		(HttpListener listener, string url) = StartServer(Encoding.UTF8.GetBytes("payload"));

		try
		{
			SharedDataRefreshResult result = await SharedDataDownload.RefreshAsync(
				url, destination,
				prepare: (_, _) => throw new InvalidDataException("cannot prepare"),
				validate: AcceptAnything,
				"test data", CancellationToken.None);

			Assert.False(result.IsFresh);
			Assert.True(result.HasCopy);
			Assert.Equal(destination, result.FilePath);
			Assert.Equal(oldWriteUtc, result.DownloadedUtc);
			Assert.Contains("cannot prepare", result.FailureReason, StringComparison.Ordinal);
			Assert.Equal("kept copy", File.ReadAllText(destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public async Task a_validate_that_throws_keeps_the_old_copy_and_reports_why()
	{
		string destination = Path.Combine(_testRoot, "kept", "validate-fail.txt");
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		File.WriteAllText(destination, "kept copy");
		DateTime oldWriteUtc = DateTime.UtcNow.AddDays(-1);
		File.SetLastWriteTimeUtc(destination, oldWriteUtc);

		(HttpListener listener, string url) = StartServer(Encoding.UTF8.GetBytes("payload"));

		try
		{
			SharedDataRefreshResult result = await SharedDataDownload.RefreshAsync(
				url, destination,
				prepare: CopyAsIs,
				validate: _ => throw new InvalidDataException("cannot validate"),
				"test data", CancellationToken.None);

			Assert.False(result.IsFresh);
			Assert.True(result.HasCopy);
			Assert.Equal(destination, result.FilePath);
			Assert.Equal(oldWriteUtc, result.DownloadedUtc);
			Assert.Contains("cannot validate", result.FailureReason, StringComparison.Ordinal);
			Assert.Equal("kept copy", File.ReadAllText(destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- HTTP error ----

	[Fact]
	public async Task an_http_error_with_no_previous_copy_returns_no_file()
	{
		string destination = Path.Combine(_testRoot, "kept", "http-fail.txt");
		(HttpListener listener, string url) = StartServer(payload: null, fail: true);

		try
		{
			SharedDataRefreshResult result = await SharedDataDownload.RefreshAsync(
				url, destination, CopyAsIs, AcceptAnything, "test data", CancellationToken.None);

			Assert.False(result.IsFresh);
			Assert.False(result.HasCopy);
			Assert.Null(result.FilePath);
			Assert.Null(result.DownloadedUtc);
			Assert.NotNull(result.FailureReason);
			Assert.False(File.Exists(destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	// ---- argument validation ----

	[Fact]
	public async Task refresh_async_rejects_a_null_url() =>
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => SharedDataDownload.RefreshAsync(null!, "x", CopyAsIs, AcceptAnything, "test data", CancellationToken.None));

	[Fact]
	public async Task refresh_async_rejects_a_blank_url() =>
		await Assert.ThrowsAsync<ArgumentException>(
			() => SharedDataDownload.RefreshAsync(" ", "x", CopyAsIs, AcceptAnything, "test data", CancellationToken.None));

	[Fact]
	public async Task refresh_async_rejects_a_null_destination_path() =>
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => SharedDataDownload.RefreshAsync("http://example.test/x", null!, CopyAsIs, AcceptAnything, "test data", CancellationToken.None));

	[Fact]
	public async Task refresh_async_rejects_a_blank_destination_path() =>
		await Assert.ThrowsAsync<ArgumentException>(
			() => SharedDataDownload.RefreshAsync("http://example.test/x", " ", CopyAsIs, AcceptAnything, "test data", CancellationToken.None));

	[Fact]
	public async Task refresh_async_rejects_a_null_prepare() =>
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => SharedDataDownload.RefreshAsync("http://example.test/x", "x", null!, AcceptAnything, "test data", CancellationToken.None));

	[Fact]
	public async Task refresh_async_rejects_a_null_validate() =>
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => SharedDataDownload.RefreshAsync("http://example.test/x", "x", CopyAsIs, null!, "test data", CancellationToken.None));

	[Fact]
	public async Task refresh_async_rejects_a_null_description() =>
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => SharedDataDownload.RefreshAsync("http://example.test/x", "x", CopyAsIs, AcceptAnything, null!, CancellationToken.None));

	[Fact]
	public async Task refresh_async_rejects_a_blank_description() =>
		await Assert.ThrowsAsync<ArgumentException>(
			() => SharedDataDownload.RefreshAsync("http://example.test/x", "x", CopyAsIs, AcceptAnything, " ", CancellationToken.None));

	// ---- cancellation ----

	[Fact]
	public async Task cancellation_is_not_swallowed_as_a_failure() =>
		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => SharedDataDownload.RefreshAsync(
				"http://127.0.0.1:1/unused", Path.Combine(_testRoot, "cancel.txt"),
				CopyAsIs, AcceptAnything, "test data", new CancellationToken(canceled: true)));

	// ---- best-effort temp cleanup ----

	[Fact]
	public async Task a_downloaded_temp_file_locked_against_deletion_does_not_fail_the_refresh()
	{
		string destination = Path.Combine(_testRoot, "kept", "locked.txt");
		(HttpListener listener, string url) = StartServer(Encoding.UTF8.GetBytes("payload"));
		FileStream? lockHandle = null;

		try
		{
			// Opened without FileShare.Delete, so SharedDataDownload's own best-effort cleanup of the
			// downloaded temp file fails with IOException - which must be swallowed, not surfaced.
			void PrepareAndLockDownload(string downloadedPath, string preparedPath)
			{
				lockHandle = new FileStream(downloadedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
				File.Copy(downloadedPath, preparedPath, overwrite: true);
			}

			SharedDataRefreshResult result = await SharedDataDownload.RefreshAsync(
				url, destination, PrepareAndLockDownload, AcceptAnything, "test data", CancellationToken.None);

			Assert.True(result.IsFresh);
			Assert.Equal("payload", File.ReadAllText(destination));
		}
		finally
		{
			lockHandle?.Dispose();
			listener.Stop();
			listener.Close();
		}
	}

	// ---- reusing a new enough copy ----

	/// <summary>A copy younger than the age asked for is used as it is - nothing downloaded, nothing replaced - and logged as such.</summary>
	[Fact]
	public async Task a_copy_younger_than_the_age_asked_for_is_used_without_downloading()
	{
		string destination = Path.Combine(_testRoot, "kept", "recent.txt");
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		File.WriteAllText(destination, "kept copy");
		DateTime keptUtc = DateTime.UtcNow.AddHours(-2);
		File.SetLastWriteTimeUtc(destination, keptUtc);
		bool validated = false;

		SharedDataRefreshResult result = await SharedDataDownload.RefreshAsync(
			"http://127.0.0.1:1/never-asked", destination, CopyAsIs, _ => validated = true, "test data",
			reuseCopyYoungerThan: TimeSpan.FromHours(24), CancellationToken.None);

		Assert.True(result.Reused);
		Assert.False(result.IsFresh);
		Assert.True(result.HasCopy);
		Assert.Null(result.FailureReason);
		Assert.Equal(keptUtc, result.DownloadedUtc);
		Assert.False(validated);
		Assert.Equal("kept copy", File.ReadAllText(destination));
		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith("Used FE-Buddy's copy of the test data from ", StringComparison.Ordinal)
			&& e.Message.EndsWith("without downloading it: it is under 24 hours old.", StringComparison.Ordinal));
	}

	/// <summary>A copy as old as the age asked for or older - or dated in the future - is downloaded again.</summary>
	[Theory]
	[InlineData(-25.0)]
	[InlineData(3.0)]
	public async Task a_copy_not_younger_than_the_age_asked_for_is_downloaded_again(double hoursFromNow)
	{
		string destination = Path.Combine(_testRoot, "kept", "old.txt");
		Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
		File.WriteAllText(destination, "kept copy");
		File.SetLastWriteTimeUtc(destination, DateTime.UtcNow.AddHours(hoursFromNow));

		(HttpListener listener, string url) = StartServer(Encoding.UTF8.GetBytes("new payload"));

		try
		{
			SharedDataRefreshResult result = await SharedDataDownload.RefreshAsync(
				url, destination, CopyAsIs, AcceptAnything, "test data", reuseCopyYoungerThan: TimeSpan.FromHours(24), CancellationToken.None);

			Assert.True(result.IsFresh);
			Assert.False(result.Reused);
			Assert.Equal("new payload", File.ReadAllText(destination));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Fact]
	public void a_reused_result_has_a_copy_but_is_not_fresh()
	{
		SharedDataRefreshResult result = new("C:\\kept\\file.txt", DateTime.UtcNow, FailureReason: null) { Reused = true };

		Assert.False(result.IsFresh);
		Assert.True(result.HasCopy);
	}

	// ---- why a download failed ----

	/// <summary>The innermost cause is logged and reported too - .NET's own "see inner exception" says nothing (issue #338).</summary>
	[Fact]
	public async Task a_failure_names_its_innermost_cause_in_the_log_and_the_reason()
	{
		string destination = Path.Combine(_testRoot, "kept", "inner.txt");
		(HttpListener listener, string url) = StartServer(Encoding.UTF8.GetBytes("payload"));

		try
		{
			SharedDataRefreshResult result = await SharedDataDownload.RefreshAsync(
				url, destination, CopyAsIs,
				validate: _ => throw new HttpRequestException(
					"The SSL connection could not be established, see inner exception.",
					new System.Security.Authentication.AuthenticationException(
						"Authentication failed, see inner exception.",
						new System.ComponentModel.Win32Exception(unchecked((int)0x80090331), "The client and server cannot communicate, because they do not possess a common algorithm."))),
				"test data", CancellationToken.None);

			const string Expected = "The SSL connection could not be established: The client and server cannot communicate, because they do not possess a common algorithm.";
			Assert.Equal(Expected, result.FailureReason);
			Assert.Contains(AppLog.Entries, e => e.Message.EndsWith(Expected, StringComparison.Ordinal));
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	[Theory]
	[InlineData("Response status code does not indicate success: 500 (Internal Server Error).", null, "Response status code does not indicate success: 500 (Internal Server Error).")]
	[InlineData("Outer problem.", "The real cause.", "Outer problem: The real cause.")]
	[InlineData("Outer problem, see inner exception.", "The real cause.", "Outer problem: The real cause.")]
	[InlineData("Outer problem: The real cause.", "The real cause.", "Outer problem: The real cause.")]
	[InlineData("Outer problem.", "", "Outer problem.")]
	public void the_reason_is_the_message_and_the_innermost_cause_when_it_says_more(string outer, string? inner, string expected)
	{
		Exception ex = inner is null ? new InvalidOperationException(outer) : new InvalidOperationException(outer, new IOException(inner));

		Assert.Equal(expected, SharedDataDownload.DescribeFailure(ex));
		Assert.Throws<ArgumentNullException>(() => SharedDataDownload.DescribeFailure(null!));
	}

	// ---- SharedDataDirectory ----

	[Fact]
	public void shared_data_directory_ends_with_the_fe_buddy_folder_and_the_given_name()
	{
		string directory = SharedDataDownload.SharedDataDirectory("X");

		Assert.EndsWith(@"FE-Buddy\X", directory, StringComparison.Ordinal);
	}

	// ---- SharedDataRefreshResult truth table ----

	[Fact]
	public void a_fresh_result_has_a_copy_and_is_fresh()
	{
		SharedDataRefreshResult result = new("C:\\kept\\file.txt", DateTime.UtcNow, FailureReason: null);

		Assert.True(result.IsFresh);
		Assert.True(result.HasCopy);
	}

	[Fact]
	public void a_fallback_result_has_a_copy_but_is_not_fresh()
	{
		SharedDataRefreshResult result = new("C:\\kept\\file.txt", DateTime.UtcNow.AddDays(-1), FailureReason: "network down");

		Assert.False(result.IsFresh);
		Assert.True(result.HasCopy);
	}

	[Fact]
	public void a_result_with_no_file_path_has_no_copy_and_is_not_fresh_even_without_a_failure_reason()
	{
		SharedDataRefreshResult result = new(FilePath: null, DownloadedUtc: null, FailureReason: null);

		Assert.False(result.IsFresh);
		Assert.False(result.HasCopy);
	}

	[Fact]
	public void a_result_with_no_file_path_and_a_failure_reason_has_no_copy_and_is_not_fresh()
	{
		SharedDataRefreshResult result = new(FilePath: null, DownloadedUtc: null, FailureReason: "network down");

		Assert.False(result.IsFresh);
		Assert.False(result.HasCopy);
	}
}
