using System.Text;

using FEBuddyLibrary.Models.Services.Airways;

namespace FEBuddyLibrary.Services.Airways;

/// <summary>
/// Generates the <c>Draw_Airway_Points.txt</c> alias file: one <c>.FF</c> alias command per
/// airway that draws every waypoint on it (e.g. <c>.J3F .FF OAK RBL LKV IMB GEG</c>).
/// </summary>
public static class AirwayAliasService
{
	private const string FileName = "Draw_Airway_Points.txt";

	/// <summary>
	/// Writes the alias file for every airway that has at least one resolved waypoint.
	/// </summary>
	/// <param name="airways">
	/// The built airways. Each airway's full (never ROI-truncated) <c>Points</c> list is used -
	/// a controller aliasing <c>.J3F</c> wants the entire airway drawn even if the GeoJSON
	/// Lines file was clipped to an ROI.
	/// </param>
	/// <param name="settings">The parsed Airways settings.</param>
	/// <returns>The path written (or <see langword="null"/> if nothing to write) and the line count.</returns>
	/// <remarks>
	/// Written whenever <see cref="AirwaySettings.GenerateAliasFile"/> is
	/// <see langword="true"/>, independently of <see cref="AirwaySettings.OutputBy"/> - the
	/// alias file is a separate user choice from GeoJSON generation.
	/// </remarks>
	public static AirwayAliasGenerateResult Generate(IReadOnlyList<Airway> airways, AirwaySettings settings)
	{
		ArgumentNullException.ThrowIfNull(airways);
		ArgumentNullException.ThrowIfNull(settings);

		List<Airway> airwaysWithPoints = airways
			.Where(a => a.Points.Count > 0)
			.OrderBy(a => a.AwyId, StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (airwaysWithPoints.Count == 0)
		{
			return new AirwayAliasGenerateResult(null, 0);
		}

		StringBuilder builder = new();

		foreach (Airway airway in airwaysWithPoints)
		{
			string pointIds = string.Join(' ', airway.Points.Select(p => p.PointId));
			builder.AppendLine($".{airway.AwyId}F .FF {pointIds}");
		}

		string directory = Path.Combine(settings.OutputDirectory, "FE-Buddy_Output", "Airways", "Alias");
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, FileName);
		File.WriteAllText(path, builder.ToString());

		return new AirwayAliasGenerateResult(path, airwaysWithPoints.Count);
	}
}
