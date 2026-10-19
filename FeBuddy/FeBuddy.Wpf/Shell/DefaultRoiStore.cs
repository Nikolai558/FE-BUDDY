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
/// </remarks>
public static class DefaultRoiStore
{
	// Internal so the unit tests can check every settings layout's keys are still the ones read here,
	// and that no other page's node sits inside this one.
	internal const string Node = "General.DefaultRoi";
	internal const string FilterKey = UserConfigKeys.DefaultRoiFilter;
	internal const string SwLatKey = $"{Node}.Corners.SwLat";
	internal const string SwLonKey = $"{Node}.Corners.SwLon";
	internal const string NeLatKey = $"{Node}.Corners.NeLat";
	internal const string NeLonKey = $"{Node}.Corners.NeLon";

	/// <summary>Raised after <see cref="Set"/> or <see cref="Clear"/> has written to disk; not when the write failed.</summary>
	public static event EventHandler? Changed;

	/// <summary>Reads the current default ROI.</summary>
	/// <returns>The saved ROI, or <see langword="null"/> when none is set or its corners don't parse.</returns>
	public static RegionOfInterest? Load()
	{
		// Clear() only flips this flag - it leaves the last-drawn coordinates in place (in case
		// the user re-enables the ROI and wants them back), so they must not be trusted alone.
		if (!string.Equals(UserConfigFile.GetValue(FilterKey), "Y", StringComparison.OrdinalIgnoreCase))
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
	/// <returns><see langword="false"/> when the settings file couldn't be written; the user has been told.</returns>
	public static bool Set(RegionOfInterest roi)
	{
		UserConfigFile.TrySetValue(FilterKey, "Y");
		UserConfigFile.TrySetValue(SwLatKey, roi.SwLat.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.TrySetValue(SwLonKey, roi.SwLon.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.TrySetValue(NeLatKey, roi.NeLat.ToString(CultureInfo.InvariantCulture));
		UserConfigFile.TrySetValue(NeLonKey, roi.NeLon.ToString(CultureInfo.InvariantCulture));
		return SaveAndAnnounce();
	}

	/// <summary>
	/// Raises <see cref="Changed"/> without writing anything: for when the whole config was replaced
	/// (a settings import), so every page showing the default ROI re-reads it.
	/// </summary>
	public static void NotifyReloaded() => Changed?.Invoke(null, EventArgs.Empty);

	/// <summary>Turns the default ROI off. Its coordinates stay in the file (see <see cref="Load"/>).</summary>
	/// <returns><see langword="false"/> when the settings file couldn't be written; the user has been told.</returns>
	public static bool Clear()
	{
		UserConfigFile.TrySetValue(FilterKey, "N");
		return SaveAndAnnounce();
	}

	/// <summary>Saves the node, and raises <see cref="Changed"/> only once it is on disk.</summary>
	private static bool SaveAndAnnounce()
	{
		if (!SettingsWrites.TrySave(Node, "default ROI"))
		{
			return false;
		}

		Changed?.Invoke(null, EventArgs.Empty);
		return true;
	}
}
