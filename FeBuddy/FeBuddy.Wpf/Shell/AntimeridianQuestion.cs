using System.Windows;

using FeBuddy.Wpf.Views;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// Asks whether an ROI whose Southwest Longitude is east of its Northeast Longitude crosses the
/// antimeridian (AM): the one ROI that looks wrong but may be meant. Core works out the ROI a yes
/// means (<c>RoiFilter.AsCrossingAntimeridian</c>); this only asks. The Map page's ROI editor, the
/// ROI picker window and a tab's own ROI all ask it.
/// </summary>
public static class AntimeridianQuestion
{
	/// <summary>The question's heading.</summary>
	internal const string Title = "Does this ROI cross the antimeridian (AM)?";

	/// <summary>The antimeridian link's tooltip.</summary>
	internal const string LinkToolTip =
		"The antimeridian (AM) is the 180° meridian, where 180°E meets 180°W, in the Pacific west of Alaska. " +
		"An ROI over the western Aleutians crosses it.";

	/// <summary>
	/// The question's text, with the longitudes as typed and a link to what the antimeridian is
	/// (<see cref="Links.Antimeridian"/>, with <see cref="LinkToolTip"/> as its tooltip).
	/// </summary>
	/// <param name="swLon">The Southwest Longitude, as typed.</param>
	/// <param name="neLon">The Northeast Longitude, as typed.</param>
	/// <returns>The text, in <c>bhv:InlineCode</c>'s Markdown.</returns>
	internal static string Message(string swLon, string neLon) =>
		$"The Southwest Longitude ({swLon}) is east of the Northeast Longitude ({neLon}). That's only right if the ROI " +
		$"crosses the [antimeridian]({Links.Antimeridian} \"{LinkToolTip}\") (AM), the 180° meridian.";

	/// <summary>Asks, over the window in front.</summary>
	/// <param name="swLon">The Southwest Longitude, as typed.</param>
	/// <param name="neLon">The Northeast Longitude, as typed.</param>
	/// <returns><see langword="true"/> when the user says the ROI crosses the AM.</returns>
	public static bool Ask(string swLon, string neLon)
	{
		Window? owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
			?? Application.Current?.MainWindow;

		return ConfirmWindow.Show(owner, Title, Message(swLon, neLon), "Yes, it crosses the AM", "No, let me fix it");
	}
}
