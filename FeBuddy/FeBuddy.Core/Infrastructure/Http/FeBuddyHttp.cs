using System.Net.Http;

namespace FeBuddy.Core.Infrastructure.Http;

/// <summary>
/// How FE-Buddy talks HTTP: every request names itself (GitHub rejects requests with no
/// User-Agent), and large downloads stream straight to disk with progress.
/// </summary>
/// <remarks>
/// Each class that goes online takes an optional <see cref="HttpClient"/> so tests can supply a
/// stub. When none is given it creates its own and disposes it when done:
/// <code>
/// using HttpClient? owned = httpClient is null ? FeBuddyHttp.CreateClient(timeout) : null;
/// HttpClient client = httpClient ?? owned!;
/// </code>
/// </remarks>
public static class FeBuddyHttp
{
	/// <summary>The User-Agent every FE-Buddy request sends.</summary>
	public const string UserAgent = "FE-Buddy";

	/// <summary>Creates a client that sends <see cref="UserAgent"/>.</summary>
	/// <param name="timeout">How long a request may take before it is abandoned.</param>
	/// <returns>A new client; the caller disposes it.</returns>
	public static HttpClient CreateClient(TimeSpan timeout)
	{
		HttpClient client = new() { Timeout = timeout };
		client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
		return client;
	}

	/// <summary>
	/// Streams a response body to a file, reporting progress after every chunk.
	/// </summary>
	/// <param name="response">A successful response, requested with <see cref="HttpCompletionOption.ResponseHeadersRead"/> so the body is not buffered in memory first.</param>
	/// <param name="destination">The file to create or overwrite.</param>
	/// <param name="expectedBytes">The size to expect when the server sends no <c>Content-Length</c>, or <see langword="null"/> when unknown.</param>
	/// <param name="progress">Called with the bytes received so far and the expected total (<see langword="null"/> when unknown).</param>
	/// <param name="cancellationToken">Cancels the download.</param>
	/// <exception cref="IOException">The body ended before the expected size - a truncated download.</exception>
	public static async Task DownloadToFileAsync(
		HttpResponseMessage response,
		string destination,
		long? expectedBytes,
		Action<long, long?>? progress,
		CancellationToken cancellationToken)
	{
		long? total = response.Content.Headers.ContentLength ?? expectedBytes;

		await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
		await using FileStream file = new(destination, FileMode.Create, FileAccess.Write, FileShare.None);

		byte[] buffer = new byte[81920];
		long received = 0;
		int read;

		while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
		{
			await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
			received += read;
			progress?.Invoke(received, total);
		}

		if (total is long expected && received != expected)
		{
			throw new IOException($"The download ended after {received:N0} of {expected:N0} bytes.");
		}
	}
}
