using System.Globalization;
using System.Text;

using FeBuddy.Core.Infrastructure.Eram.Models;

namespace FeBuddy.Core.Application.Conversions.EramToGeojson;

/// <summary>
/// Writes <c>ConsoleCommandControl.txt</c>: a rundown of the map menus in an ERAM
/// <c>ConsoleCommandControl.xml</c>, laid out as the original ERAM_2_GEOJSON tool laid it out.
/// </summary>
/// <remarks>
/// <para>
/// Two sections, Brightness Control Groups then Filter Groups. Each menu is listed with the maps in
/// <c>Geomaps.xml</c> that use it (their <c>GeomapId</c>s, or <c>None</c>), then each of its
/// buttons' label, menu position and groups, in file order:
/// </para>
/// <code>
/// BCG Menu: ZOB
///
///     Used with:  CENTER, OCP
///
///     Label:      AAV
///     Position:   1
///     Group:      1, 2
/// </code>
/// <para>
/// The gaps are tabs, as the original wrote them. A filter button's second label line sits under
/// its first. Lines end in CRLF; the original mixed LF and CRLF.
/// </para>
/// </remarks>
public static class EramConsoleCommandControlRundown
{
	/// <summary>The rundown's file name.</summary>
	public const string FileName = "ConsoleCommandControl.txt";

	private const string Rule = ":::::::::::::::::::::::::::::::::::";

	/// <summary>Writes the rundown into <paramref name="outputDirectory"/>.</summary>
	/// <param name="menus">The ConsoleCommandControl file's map menus.</param>
	/// <param name="maps">The Geomaps file's maps, for which of them use each menu.</param>
	/// <param name="outputDirectory">The folder it goes in; created if need be.</param>
	/// <returns>The rundown's path.</returns>
	/// <exception cref="IOException">Thrown when the file cannot be written.</exception>
	public static string Write(EramConsoleCommandControl menus, IReadOnlyList<EramGeoMap> maps, string outputDirectory)
	{
		ArgumentNullException.ThrowIfNull(menus);
		ArgumentNullException.ThrowIfNull(maps);
		ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

		Directory.CreateDirectory(outputDirectory);
		string path = Path.Combine(outputDirectory, FileName);
		File.WriteAllText(path, Format(menus, maps), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		return path;
	}

	/// <summary>Builds the rundown's text.</summary>
	internal static string Format(EramConsoleCommandControl menus, IReadOnlyList<EramGeoMap> maps)
	{
		StringBuilder text = new();

		Banner(text, "::   Brightness Control Groups   ::");

		foreach (EramMapMenu menu in menus.BrightnessMenus)
		{
			text.AppendLine(CultureInfo.InvariantCulture, $"BCG Menu: {menu.Name}");
			AppendMenu(text, menu, UsedWith(maps, map => map.BcgMenuName, menu.Name));
		}

		Banner(text, "::        Filter Groups          ::");

		foreach (EramMapMenu menu in menus.FilterMenus)
		{
			text.AppendLine(CultureInfo.InvariantCulture, $"FilterMenu: {menu.Name}");
			AppendMenu(text, menu, UsedWith(maps, map => map.FilterMenuName, menu.Name));
		}

		return text.ToString();
	}

	private static void Banner(StringBuilder text, string title) =>
		text.AppendLine(Rule).AppendLine(title).AppendLine(Rule).AppendLine();

	private static void AppendMenu(StringBuilder text, EramMapMenu menu, string usedWith)
	{
		text.AppendLine();
		text.AppendLine(CultureInfo.InvariantCulture, $"\tUsed with:\t{usedWith}");
		text.AppendLine();

		foreach (EramMapMenuButton button in menu.Buttons)
		{
			text.AppendLine(CultureInfo.InvariantCulture, $"\tLabel:\t\t{button.LabelLine1}");

			if (button.LabelLine2 is not null)
			{
				text.AppendLine(CultureInfo.InvariantCulture, $"\t\t\t\t{button.LabelLine2}");
			}

			text.AppendLine(CultureInfo.InvariantCulture, $"\tPosition:\t{button.Position}");
			text.AppendLine(CultureInfo.InvariantCulture, $"\tGroup:\t\t{string.Join(", ", button.Groups)}");
			text.AppendLine();
		}

		text.AppendLine();
	}

	/// <summary>The <c>GeomapId</c>s of the maps that name the menu, once each in file order, or <c>None</c>.</summary>
	private static string UsedWith(IReadOnlyList<EramGeoMap> maps, Func<EramGeoMap, string?> menuOf, string menu)
	{
		string[] ids = [.. maps.Where(map => map.Name.Length > 0 && menuOf(map) == menu).Select(map => map.Name).Distinct(StringComparer.Ordinal)];
		return ids.Length > 0 ? string.Join(", ", ids) : "None";
	}
}
