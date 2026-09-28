namespace FeBuddy.Core.Domain.WxStations;

/// <summary>
/// The country codes the Wx Stations sub-service includes: the United States and its
/// territories.
/// </summary>
public static class WxStationCountries
{
	/// <summary>The included country codes: <c>US</c> plus its territories (Puerto Rico, the U.S.
	/// Virgin Islands, Guam, the Northern Mariana Islands, American Samoa, and the U.S. Minor
	/// Outlying Islands).</summary>
	public static readonly IReadOnlyList<string> Included = ["US", "PR", "VI", "GU", "MP", "AS", "UM"];

	/// <summary>Whether a station's country code is one Wx Stations includes.</summary>
	/// <param name="country">The station's <c>country</c> value, or <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when <paramref name="country"/> is in <see cref="Included"/>, ignoring case.</returns>
	public static bool IsIncluded(string? country) =>
		country is not null && Included.Contains(country, StringComparer.OrdinalIgnoreCase);
}
