using System.Text;

using FeBuddy.Core.Models.Services.Airac.Airways;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Core.Services.Airac.Airways;

/// <summary>
/// Generates the <c>Airways.txt</c> alias file: one <c>.FF</c> alias command per airway that
/// draws every waypoint on it (e.g. <c>.J3F .FF OAK RBL LKV IMB GEG</c>).
/// </summary>
public static class AirwayAliasService
{
	private const string FileName = "Airways.txt";

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

		IEnumerable<Airway> candidates = airways.Where(a => a.Points.Count > 0);

		// RoiAirways scope: keep exactly the airways the GeoJSON draws - those whose LINE crosses
		// the ROI, even with no waypoint inside it - then write ALL of that airway's waypoints,
		// because the alias is meant to draw the whole airway. The crossing test is the one the
		// builder runs for every airway whether or not GeoJSON is being written, so an
		// alias-only run gets the same answer.
		if (settings.AliasRoiScope == AliasRoiScope.RoiAirways && settings.Roi is not null)
		{
			candidates = candidates.Where(a => a.CrossesRoi);
		}

		List<Airway> airwaysWithPoints = candidates
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

		string directory = AirwayOutputPaths.Resolve(settings, "Alias");
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, FileName);
		File.WriteAllText(path, builder.ToString());

		return new AirwayAliasGenerateResult(path, airwaysWithPoints.Count);
	}
}
