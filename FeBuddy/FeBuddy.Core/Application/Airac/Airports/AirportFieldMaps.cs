using System.Text;

using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Airports;

/// <summary>
/// The NASR-code-to-display-value translations the Airports sub-service applies, plus the
/// class-airspace sentence builder.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="AirportBuilder"/> so the mappings can be unit-tested directly and
/// so there is exactly one definition of each. An unrecognized code is passed through
/// unchanged rather than blanked or guessed at: NASR occasionally adds codes, and showing the
/// raw value is more useful to a facility engineer than showing nothing.
/// </remarks>
public static class AirportFieldMaps
{
	private static readonly Dictionary<string, string> TowerTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		["ATCT"] = "TWR",
		["NON-ATCT"] = "No-TWR",
		["ATCT-A/C"] = "TWR provides APP services",
		["ATCT-RAPCON"] = "AirForce TWR / FAA APP",
		["ATCT-RATCF"] = "Navy TWR / FAA APP",
		["ATCT-TRACON"] = "TWR/RADAR APP Up-Down",
	};

	private static readonly Dictionary<string, string> FacilityTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		["A"] = "AIRPORT",
		["B"] = "BALLOONPORT",
		["C"] = "SEAPLANE BASE",
		["G"] = "GLIDERPORT",
		["H"] = "HELIPORT",
		["U"] = "ULTRALIGHT FIELD",
	};

	/// <summary>Translates <c>APT_BASE.TWR_TYPE_CODE</c> to its display form.</summary>
	/// <param name="towerTypeCode">The raw NASR code.</param>
	/// <returns>The display form, or the trimmed raw code when it is not a code we translate.</returns>
	public static string MapTowerType(string? towerTypeCode)
	{
		string trimmed = towerTypeCode?.Trim() ?? string.Empty;

		return TowerTypes.TryGetValue(trimmed, out string? mapped) ? mapped : trimmed;
	}

	/// <summary>Translates <c>APT_BASE.SITE_TYPE_CODE</c> to its display form.</summary>
	/// <param name="siteTypeCode">The raw NASR code.</param>
	/// <returns>The display form, or the trimmed raw code when it is not a code we translate.</returns>
	public static string MapFacilityType(string? siteTypeCode)
	{
		string trimmed = siteTypeCode?.Trim() ?? string.Empty;

		return FacilityTypes.TryGetValue(trimmed, out string? mapped) ? mapped : trimmed;
	}

	/// <summary>
	/// Builds the class-airspace display string from a <c>CLS_ARSP</c> row, which can flag more
	/// than one class at the same airport.
	/// </summary>
	/// <param name="row">The airport's <c>CLS_ARSP</c> row, or <see langword="null"/> when it has none.</param>
	/// <returns>
	/// <c>Delta</c>, <c>Delta &amp; Echo</c>, <c>Bravo, Delta, &amp; Echo</c>, or
	/// <see langword="null"/> when the row is absent or flags nothing.
	/// </returns>
	public static string? BuildClassAirspace(ClsArspCsvDataModel.ClsArsp? row)
	{
		if (row is null)
		{
			return null;
		}

		List<string> classes = [];

		if (IsYes(row.ClassBAirspace)) classes.Add("Bravo");
		if (IsYes(row.ClassCAirspace)) classes.Add("Charlie");
		if (IsYes(row.ClassDAirspace)) classes.Add("Delta");
		if (IsYes(row.ClassEAirspace)) classes.Add("Echo");

		return classes.Count switch
		{
			0 => null,
			1 => classes[0],
			2 => $"{classes[0]} & {classes[1]}",
			_ => JoinWithSerialAmpersand(classes),
		};
	}

	private static string JoinWithSerialAmpersand(IReadOnlyList<string> values)
	{
		StringBuilder builder = new();

		for (int i = 0; i < values.Count - 1; i++)
		{
			builder.Append(values[i]).Append(", ");
		}

		builder.Append("& ").Append(values[^1]);

		return builder.ToString();
	}

	private static bool IsYes(string? value) =>
		value is not null && value.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase);
}
