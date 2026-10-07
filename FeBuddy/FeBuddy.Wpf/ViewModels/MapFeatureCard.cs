using System.Globalization;
using System.Windows.Media;

using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// One shape in the map's properties panel (Ctrl + click, issue #327): the layer it is in, what it
/// is, and its properties - a GeoJSON feature's as the file has them, or what the AIRAC data says
/// about a live layer's airport, runway, NAVAID or boundary.
/// </summary>
public sealed class MapFeatureCard
{
	/// <summary>Creates the card for one shape clicked.</summary>
	/// <param name="hit">The shape.</param>
	public MapFeatureCard(MapHit hit)
	{
		ArgumentNullException.ThrowIfNull(hit);

		Hit = hit;
		Properties = hit.Geometry.Feature?.Properties() ?? [];
		CopyText = hit.Geometry.Feature?.CopyText() ?? string.Empty;
	}

	/// <summary>The shape.</summary>
	public MapHit Hit { get; }

	/// <summary>The layer's name: the file it came from, or the live layer.</summary>
	public string Title => Hit.Layer.Name;

	/// <summary>The layer's colour, for the swatch beside its name.</summary>
	public Brush Swatch => Hit.Layer.Stroke;

	/// <summary>
	/// What the shape is, on one line: e.g. <c>LineString · 12 points · feature 152 in the file</c>, or
	/// <c>Airport · 41.40917, -81.85500</c>.
	/// </summary>
	public string Summary
	{
		get
		{
			MapGeometry geometry = Hit.Geometry;
			List<string> parts = [geometry.Feature?.Kind ?? geometry.Kind.ToString()];

			if (Hit.Point is { } point)
			{
				parts.Add(Format(point));
			}
			else
			{
				int points = geometry.Parts.Sum(part => part.Count);
				parts.Add(geometry.Parts.Count > 1
					? $"{geometry.Parts.Count} parts, {points} points"
					: $"{points} points");
			}

			if (geometry.Feature?.Number is { } number)
			{
				parts.Add($"feature {number.ToString(CultureInfo.InvariantCulture)} in the file");
			}

			return string.Join(" · ", parts);
		}
	}

	/// <summary>Its properties, in order.</summary>
	public IReadOnlyList<MapProperty> Properties { get; }

	/// <summary>Whether it has any; without, the card says so.</summary>
	public bool HasProperties => Properties.Count > 0;

	/// <summary>What the copy button copies: a file's <c>properties</c> as JSON, or the data as <c>Name: value</c> lines.</summary>
	public string CopyText { get; }

	/// <summary>Whether there is anything to copy.</summary>
	public bool CanCopy => CopyText.Length > 0;

	/// <summary>The copy button's tooltip.</summary>
	public string CopyToolTip => Hit.Geometry.Feature?.IsFromFile == true
		? "Copy the properties as JSON"
		: "Copy the properties";

	/// <summary>A point as the map's cursor read-out shows one.</summary>
	/// <param name="point">The point.</param>
	/// <returns>e.g. <c>41.40917, -81.85500</c>.</returns>
	internal static string Format(GeoPoint point) =>
		$"{point.Lat.ToString("0.00000", CultureInfo.InvariantCulture)}, {point.Lon.ToString("0.00000", CultureInfo.InvariantCulture)}";
}
