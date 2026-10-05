namespace FeBuddy.Core.Application.Airac.VnasAlias.Models;

/// <summary>Where a custom alias file comes from.</summary>
public enum AliasSourceKind
{
	/// <summary>A file on this PC.</summary>
	File = 0,

	/// <summary>A web address, such as a file on GitHub.</summary>
	Url = 1,
}

/// <summary>
/// One of the user's own alias files, merged into <c>Combined_Alias.txt</c> after FE-Buddy's by the
/// Concatenate Aliases sub-service.
/// </summary>
/// <param name="Number">Its number on the tab (1 is merged first).</param>
/// <param name="Kind">Whether it is a file on this PC or a web address.</param>
/// <param name="Location">The file's full path, or the web address.</param>
/// <param name="CredentialId">
/// The saved credential to download it with (see <c>CredentialStore</c>), or <see langword="null"/>
/// to download it without one. Always <see langword="null"/> for a file on this PC.
/// </param>
public sealed record AliasSource(int Number, AliasSourceKind Kind, string Location, Guid? CredentialId = null)
{
	/// <summary>
	/// How messages name it, e.g. <c>custom alias file 2 (ZOB-Alias.txt)</c>: its number, then the
	/// file's name - never the whole address, which may carry more than the user wants in a log.
	/// </summary>
	public string DisplayName => $"custom alias file {Number} ({FileName})";

	/// <summary>The file's name: the last part of its path or address, or the address's website when it has no path.</summary>
	public string FileName
	{
		get
		{
			if (Kind == AliasSourceKind.File)
			{
				return Path.GetFileName(Location);
			}

			if (!Uri.TryCreate(Location, UriKind.Absolute, out Uri? uri))
			{
				return Location;
			}

			string last = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments[^1].Trim('/')) : string.Empty;
			return last.Length > 0 ? last : uri.Host;
		}
	}
}
