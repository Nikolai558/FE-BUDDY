namespace FeBuddy.Core.Application.Airac.WxStations;

/// <summary>
/// The files the Wx Stations sub-service writes, by file key - the name the <c>UploadToVnas</c>
/// and <c>CrcDefaultsFor</c> settings use (see <see cref="FeBuddy.Core.Application.Airac.Models.VnasFileChoices"/>).
/// </summary>
/// <remarks>
/// A file's key is its name without <c>.geojson</c>. There is no alias file and no per-group
/// layout: Wx Stations always writes at most <see cref="Symbols"/> and <see cref="Text"/>.
/// </remarks>
public static class WxStationOutputFiles
{
	/// <summary>The CRC-defaults class name for the merged Symbols and Text files.</summary>
	public const string AllClass = "Wx";

	/// <summary><c>Wx_Symbols.geojson</c>: a symbol per included station.</summary>
	public const string Symbols = "Wx_Symbols";

	/// <summary><c>Wx_Text.geojson</c>: a label per included station.</summary>
	public const string Text = "Wx_Text";

	/// <summary>Whether a key names one of the Wx Stations GeoJSON files, ignoring case.</summary>
	/// <param name="key">The file key.</param>
	/// <returns><see langword="true"/> for <see cref="Symbols"/> or <see cref="Text"/>.</returns>
	public static bool IsGeojsonKey(string key) =>
		key.Equals(Symbols, StringComparison.OrdinalIgnoreCase)
		|| key.Equals(Text, StringComparison.OrdinalIgnoreCase);
}
