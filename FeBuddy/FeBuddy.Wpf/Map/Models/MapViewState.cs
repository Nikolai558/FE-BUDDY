namespace FeBuddy.Wpf.Map.Models;

/// <summary>
/// Where a map is looking: the world-unit point at its centre and its scale. Saved when a map
/// closes so the next one (the Map page, or a map popup) opens where the user left off.
/// </summary>
/// <param name="CenterX">World x at the centre, 0..1.</param>
/// <param name="CenterY">World y at the centre, 0..1.</param>
/// <param name="Scale">Pixels per world unit.</param>
public readonly record struct MapViewState(double CenterX, double CenterY, double Scale);
