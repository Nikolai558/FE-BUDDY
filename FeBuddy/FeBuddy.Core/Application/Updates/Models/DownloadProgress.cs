namespace FeBuddy.Core.Application.Updates.Models;

/// <summary>Progress of an installer download.</summary>
/// <param name="BytesReceived">Bytes written so far.</param>
/// <param name="TotalBytes">The expected total, or <see langword="null"/> when unknown.</param>
public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes)
{
	/// <summary>Whole-number percent complete, or <see langword="null"/> when the total is unknown.</summary>
	public int? Percent => TotalBytes is > 0 ? (int)Math.Min(100, BytesReceived * 100 / TotalBytes.Value) : null;
}
