namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>
/// The map menus in an ERAM <c>ConsoleCommandControl.xml</c> (<c>ConsoleCommandControl_Records</c>),
/// as read by <c>EramConsoleCommandControlReader</c>. The rest of the file (keypad and function
/// keys) is not read.
/// </summary>
/// <param name="BrightnessMenus">Every <c>MapBrightnessMenu</c>, in file order.</param>
/// <param name="FilterMenus">Every <c>MapFilterMenu</c>, in file order.</param>
public sealed record EramConsoleCommandControl(
	IReadOnlyList<EramMapMenu> BrightnessMenus,
	IReadOnlyList<EramMapMenu> FilterMenus);
