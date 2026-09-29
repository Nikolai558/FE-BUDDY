namespace FeBuddy.Core.Infrastructure.Eram.Models;

/// <summary>
/// One map menu in <c>ConsoleCommandControl.xml</c>: a <c>MapBrightnessMenu</c> (its brightness
/// control group buttons) or a <c>MapFilterMenu</c> (its filter buttons).
/// </summary>
/// <param name="Name">The menu's <c>BCGMenuName</c> or <c>FilterMenuName</c>, which a map in <c>Geomaps.xml</c> names to use it; empty when the file gives none.</param>
/// <param name="Buttons">Its buttons, in file order.</param>
public sealed record EramMapMenu(string Name, IReadOnlyList<EramMapMenuButton> Buttons);
