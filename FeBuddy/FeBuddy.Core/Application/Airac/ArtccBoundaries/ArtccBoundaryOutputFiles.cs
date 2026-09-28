using System.Text.RegularExpressions;

using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.ArtccBoundaries;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;

namespace FeBuddy.Core.Application.Airac.ArtccBoundaries;

/// <summary>
/// The files the ARTCC Boundaries sub-service writes, by file key - the name the
/// <c>UploadToVnas</c> and <c>CrcDefaultsFor</c> settings use (see <see cref="VnasFileChoices"/>).
/// </summary>
/// <remarks>
/// A file's key is its name without <c>.geojson</c>. There is no alias file.
/// <para>
/// <see cref="ArtccBoundaryOutputBy.HighLow"/> and <see cref="ArtccBoundaryOutputBy.HighLowUnlimited"/>
/// both key their files by a fixed group name - <see cref="HighClass"/>, <see cref="LowClass"/> and,
/// in HighLowUnlimited mode only, <see cref="UnlimitedClass"/> - via <see cref="KeyFor(string)"/>,
/// e.g. <c>ARTCC-Boundary_High_Lines</c>. The group name doubles as the file's CRC class name.
/// </para>
/// <para>
/// <see cref="ArtccBoundaryOutputBy.ArtccAltitude"/> instead keys each file by its LocationId and
/// altitude via <see cref="KeyFor(string, string)"/>, e.g. <c>ARTCC-Boundary_ZOB-HIGH_Lines</c> for
/// CRC class <c>ZOB-HIGH</c> (see <see cref="ClassFor(string, string)"/>).
/// </para>
/// </remarks>
public static partial class ArtccBoundaryOutputFiles
{
	/// <summary>The CRC class (and file group) for every HIGH ring, in HighLow and HighLowUnlimited modes.</summary>
	public const string HighClass = nameof(ArtccBoundaryAltitude.High);

	/// <summary>The CRC class (and file group) for every LOW ring, in HighLow and HighLowUnlimited modes.</summary>
	public const string LowClass = nameof(ArtccBoundaryAltitude.Low);

	/// <summary>The CRC class (and file group) for every UNLIMITED ring, in HighLowUnlimited mode.</summary>
	public const string UnlimitedClass = nameof(ArtccBoundaryAltitude.Unlimited);

	/// <summary>A HighLow/HighLowUnlimited-mode file's key, e.g. <c>ARTCC-Boundary_High_Lines</c>.</summary>
	/// <param name="group">The file's group: <see cref="HighClass"/>, <see cref="LowClass"/> or <see cref="UnlimitedClass"/>.</param>
	/// <returns>The file key.</returns>
	public static string KeyFor(string group) => $"ARTCC-Boundary_{group}_Lines";

	/// <summary>An ArtccAltitude-mode file's key, e.g. <c>ARTCC-Boundary_ZOB-HIGH_Lines</c>.</summary>
	/// <param name="locationId">The location's identifier.</param>
	/// <param name="altitudeToken">The altitude, as NASR writes it (see <see cref="ArtccBoundaryAltitudes.NasrToken"/>).</param>
	/// <returns>The file key.</returns>
	public static string KeyFor(string locationId, string altitudeToken) => KeyFor(ClassFor(locationId, altitudeToken));

	/// <summary>A HighLow/HighLowUnlimited-mode file's CRC class name: its group, unchanged.</summary>
	/// <param name="group">The file's group: <see cref="HighClass"/>, <see cref="LowClass"/> or <see cref="UnlimitedClass"/>.</param>
	/// <returns>The CRC class name.</returns>
	public static string ClassFor(string group) => group;

	/// <summary>An ArtccAltitude-mode file's CRC class name, e.g. <c>ZOB-HIGH</c>.</summary>
	/// <param name="locationId">The location's identifier.</param>
	/// <param name="altitudeToken">The altitude, as NASR writes it.</param>
	/// <returns>The CRC class name.</returns>
	public static string ClassFor(string locationId, string altitudeToken) => $"{locationId}-{altitudeToken}";

	/// <summary>Whether a key names one of the ARTCC Boundaries GeoJSON files, ignoring case.</summary>
	/// <param name="key">The file key.</param>
	/// <returns><see langword="true"/> for e.g. <c>ARTCC-Boundary_High_Lines</c> or <c>ARTCC-Boundary_ZOB-HIGH_Lines</c>.</returns>
	public static bool IsGeojsonKey(string key) => GeojsonKeyPattern().IsMatch(key);

	/// <summary>
	/// Reads a key back into the CRC class its file uses: <see cref="HighClass"/>,
	/// <see cref="LowClass"/> or <see cref="UnlimitedClass"/> for a HighLow/HighLowUnlimited-mode
	/// key, or <c>LocationId-ALTITUDE</c> (e.g. <c>ZOB-HIGH</c>) for an ArtccAltitude-mode key.
	/// </summary>
	/// <param name="key">The file key.</param>
	/// <param name="className">The CRC class, when <paramref name="key"/> is recognized.</param>
	/// <returns><see langword="true"/> for a recognized ARTCC Boundaries GeoJSON key.</returns>
	public static bool TryParseKey(string key, out string className)
	{
		Match match = GeojsonKeyPattern().Match(key);

		className = match.Success ? match.Groups["class"].Value : string.Empty;
		return match.Success;
	}

	[GeneratedRegex(@"^ARTCC-Boundary_(?<class>High|Low|Unlimited|[A-Za-z0-9]+-(?:HIGH|LOW|UNLIMITED))_Lines$", RegexOptions.IgnoreCase)]
	private static partial Regex GeojsonKeyPattern();
}
