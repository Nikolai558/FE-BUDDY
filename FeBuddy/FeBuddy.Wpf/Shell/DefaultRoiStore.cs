using System.Globalization;

using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// The one saved default ROI (<c>Services.AiracService.DefaultRoi</c> in <c>UserConfig.json</c>),
/// read and written by both Settings and the Map page's inline editor.
/// </summary>
/// <remarks>
/// Each section's view-model is built once and kept alive for the rest of the session (see
/// <see cref="ViewModels.NavItem"/>), so a value read once in a constructor goes stale as soon as
/// another section changes it. Every read and write goes through here, and every write raises
/// <see cref="Changed"/>, so the view-models that didn't make the change still hear about it.
/// The misspelled <c>DefaultCoordindates</c> key is what existing config files contain.
/// </remarks>
public static class DefaultRoiStore
{
	private const string Node = "Services.AiracService.DefaultRoi";
	private const string FilterKey = $"{Node}.FilterByRoi";
	private const string SwLatKey = $"{Node}.DefaultCoordindates.SwLat";
	private const string SwLonKey = $"{Node}.DefaultCoordindates.SwLon";
	private const string NeLatKey = $"{Node}.DefaultCoordindates.NeLat";
	private const string NeLonKey = $"{Node}.DefaultCoordindates.NeLon";

	/// <summary>Raised after <see cref="Set"/> or <see cref="Clear"/> writes to disk.</summary>
	public static event EventHandler? Changed;

	/// <summary>Reads the current default ROI.</summary>
	/// <returns>The saved ROI, or <see langword="null"/> when none is set or its corners don't parse.</returns>
	public static RegionOfInterest? Load()
	{
		// Clear() only flips this flag - it leaves the last-drawn coordinates in place (in case
		// the user re-enables the ROI and wants them back), so they must not be trusted alone.
		if (!string.Equals(UserConfigFile.GetValue(FilterKey), "true", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		if (double.TryParse(UserConfigFile.GetValue(SwLatKey), NumberStyles.Float, CultureInfo.InvariantCulture, out double swLat)
			&& double.TryParse(UserConfigFile.GetValue(SwLonKey), NumberStyles.Float, CultureInfo.InvariantCulture, out double swLon)
			&& double.TryParse(UserConfigFile.GetValue(NeLatKey), NumberStyles.Float, CultureInfo.InvariantCulture, out double neLat)
			&& double.TryParse(UserConfigFile.GetValue(NeLonKey), NumberStyles.Float, CultureInfo.InvariantCulture, out double neLon))
		{
			return new RegionOfInterest(swLat, swLon, neLat, neLon);
		}

		return null;
	}

	/// <summary>Saves <paramref name="roi"/> as the default ROI and turns ROI filtering on.</summary>
	/// <param name="roi">The new default ROI.</param>
	public static void Set(RegionOfInterest roi)
	{
		UserConfigFile.TrySetValue(FilterKey, "true");
		UserConfigFile.TrySetValue(SwLatKey, roi.SwLat.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.TrySetValue(SwLonKey, roi.SwLon.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.TrySetValue(NeLatKey, roi.NeLat.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.TrySetValue(NeLonKey, roi.NeLon.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.Save(Node);
		Changed?.Invoke(null, EventArgs.Empty);
	}

	/// <summary>Turns the default ROI off. Its coordinates stay in the file (see <see cref="Load"/>).</summary>
	public static void Clear()
	{
		UserConfigFile.TrySetValue(FilterKey, "false");
		UserConfigFile.Save(Node);
		Changed?.Invoke(null, EventArgs.Empty);
	}
}
