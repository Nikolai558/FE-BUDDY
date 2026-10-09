namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Each AIRAC sub-service tab's saved <c>Area</c> - which one of its area filters a run uses - and
/// the area a tab starts on when it has none saved: what its older settings meant. Layout 2's step
/// (<see cref="UserConfigMigrations"/>) saves that area for every tab that has settings, and a tab
/// that never saved any works it out the same way when it loads.
/// </summary>
/// <remarks>
/// <para>
/// Before layout 2 a tab could use its ARTCCs and a Region of Interest at once. Where it did, it
/// starts on its ARTCCs (Procedures: its facilities), the ROI kept but unused. Where it used one or
/// neither, it gets exactly what it got before. A tab's ARTCC list falls back to the facility in
/// Settings ▸ Facility Profile while it has never saved one, as the tabs do.
/// </para>
/// <para>
/// The values are the names of <c>FeBuddy.Core.Application.Settings.SubServiceArea</c>, which the
/// settings blocks carry.
/// </para>
/// </remarks>
public static class UserConfigAreas
{
	/// <summary>The saved value for the ARTCCs (Procedures: the facilities).</summary>
	public const string Artccs = "Artccs";

	/// <summary>The saved value for the Region of Interest.</summary>
	public const string Roi = "Roi";

	/// <summary>The saved value for everything in the cycle.</summary>
	public const string Everything = "Everything";

	/// <summary>The saved value for Procedures' "only what you list".</summary>
	public const string None = "None";

	/// <summary>The key under a tab's node its area is saved in.</summary>
	public const string AreaKey = "Area";

	/// <summary>Where every AIRAC sub-service tab's settings are.</summary>
	public const string AiracNode = "Services.AiracService";

	/// <summary>The facility from Settings ▸ Facility Profile.</summary>
	public const string FacilityKey = $"{AiracNode}.UserArtccId";

	/// <summary>Whether the default ROI is on.</summary>
	public const string DefaultRoiKey = $"{AiracNode}.DefaultRoi.FilterByRoi";

	/// <summary>The Procedures tab's node name.</summary>
	public const string ProceduresTab = "Procedures";

	/// <summary>
	/// The tabs with an area, by node name, each with the key of its ARTCC list (Procedures: its
	/// facilities), or <see langword="null"/> for a tab whose only area is the ROI.
	/// </summary>
	public static IReadOnlyDictionary<string, string?> Tabs { get; } = new Dictionary<string, string?>(StringComparer.Ordinal)
	{
		["ArtccBoundaries"] = "LocationFilter",
		["Arrivals"] = "ArtccFilter",
		["Departures"] = "ArtccFilter",
		["Airports"] = null,
		["Airways"] = null,
		["Navaids"] = null,
		["Fixes"] = null,
		["WxStations"] = null,
		[ProceduresTab] = "Facilities",
	};

	/// <summary>The area a tab starts on when it has none saved, from its other settings.</summary>
	/// <param name="tab">The tab's node name, one of <see cref="Tabs"/>, e.g. <c>Arrivals</c>.</param>
	/// <param name="get">Reads a saved setting by dotted path; <see langword="null"/> when it isn't set.</param>
	/// <returns>
	/// <see cref="Artccs"/> when its ARTCC list (or, never saved, the facility) names any; otherwise for
	/// Procedures <see cref="Roi"/> when it brought in the ROI's airports, else <see cref="None"/>; for
	/// any other tab <see cref="Roi"/> when it has its own ROI or the default ROI is on, else
	/// <see cref="Everything"/>.
	/// </returns>
	/// <exception cref="ArgumentException"><paramref name="tab"/> is not one of <see cref="Tabs"/>.</exception>
	public static string DefaultFor(string tab, Func<string, string?> get)
	{
		ArgumentNullException.ThrowIfNull(get);

		if (!Tabs.TryGetValue(tab, out string? artccListKey))
		{
			throw new ArgumentException($"'{tab}' has no area.", nameof(tab));
		}

		string node = $"{AiracNode}.{tab}";

		if (artccListKey is not null && HasEntries(get($"{node}.{artccListKey}") ?? get(FacilityKey)))
		{
			return Artccs;
		}

		if (tab == ProceduresTab)
		{
			return IsYes(get($"{node}.IncludeRoiAirports")) ? Roi : None;
		}

		return IsYes(get($"{node}.Roi.OverrideDefaultRoi")) || IsYes(get(DefaultRoiKey)) ? Roi : Everything;
	}

	/// <summary>
	/// Layout 2's step: saves the area for every tab that has settings, drops Procedures'
	/// <c>IncludeRoiAirports</c> (its area says it now), and drops Telephony's ROI, which never limited it.
	/// </summary>
	/// <param name="settings">The settings being brought forward.</param>
	internal static void SaveForEveryTab(UserConfigMigrationContext settings)
	{
		foreach (string tab in Tabs.Keys)
		{
			string node = $"{AiracNode}.{tab}";

			if (settings.KeysAtOrBelow(node).Count > 0 && settings.Get($"{node}.{AreaKey}") is null)
			{
				settings.Set($"{node}.{AreaKey}", DefaultFor(tab, settings.Get));
			}
		}

		settings.Remove($"{AiracNode}.{ProceduresTab}.IncludeRoiAirports");
		settings.Remove($"{AiracNode}.Telephony.Roi");
	}

	private static bool HasEntries(string? list) =>
		(list ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 0;

	private static bool IsYes(string? value) =>
		value?.Trim() is { } text && (text.Equals("Y", StringComparison.OrdinalIgnoreCase) || text.Equals("true", StringComparison.OrdinalIgnoreCase));
}
