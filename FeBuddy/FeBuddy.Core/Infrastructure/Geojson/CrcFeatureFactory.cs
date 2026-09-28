using FeBuddy.Core.Domain.Crc;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Geojson;

/// <summary>
/// Builds the CRC ERAM properties FE-Buddy writes into GeoJSON: the non-rendered "isDefaults"
/// Feature that sets a file's baseline, and the per-Feature overrides that differ from it.
/// </summary>
/// <remarks>
/// CRC decides a Feature's look in three tiers (see
/// <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">CRC_Geojsons.md</see>):
/// properties on the Feature itself beat the file's isDefaults Feature, which beats CRC's own
/// fallback. Everything is validated with <see cref="CrcPropertyValidator"/> before it is built,
/// so FE-Buddy never writes a value CRC cannot draw.
/// </remarks>
public static class CrcFeatureFactory
{
	/// <summary>
	/// Where CRC's isDefaults Features sit: longitude 90, latitude 180. The latitude is out of
	/// range on purpose - any GeoJSON viewer other than CRC leaves the Feature off the map.
	/// </summary>
	private static readonly Point IsDefaultsPoint =
		Wgs84.Factory.CreatePoint(new Coordinate(x: 90.0, y: 180.0));

	/// <summary>
	/// Builds a file's non-rendered <c>isLineDefaults</c> Feature: every Line Feature in the file
	/// that does not override them takes these properties.
	/// </summary>
	/// <param name="defaults">The defaults. Every value is written.</param>
	/// <returns>The Feature, to insert first in the FeatureCollection.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="defaults"/> fails CRC validation.</exception>
	/// <remarks>
	/// A defaults Feature carries only the properties CRC reads as defaults, in a fixed order:
	/// the <c>is...Defaults</c> flag, <c>bcg</c>, <c>filters</c>, then the kind's own.
	/// </remarks>
	public static Feature CreateDefaultsFeature(CrcLineDefaults defaults)
	{
		CrcPropertyValidator.ThrowIfInvalid(CrcPropertyValidator.ValidateLineDefaults(defaults), "Invalid CRC Line defaults");

		AttributesTable attributes = StartDefaults("isLineDefaults", defaults.Bcg, defaults.Filters);
		attributes.Add("style", defaults.Style);
		attributes.Add("thickness", defaults.Thickness);

		return new Feature(IsDefaultsPoint, attributes);
	}

	/// <summary>
	/// Builds a file's non-rendered <c>isSymbolDefaults</c> Feature. <c>style</c> is left out
	/// when <see cref="CrcSymbolDefaults.Style"/> is <see langword="null"/>: every Symbol
	/// Feature in the file then carries its own <c>style</c> instead.
	/// </summary>
	/// <param name="defaults">The defaults. Every value but <c>style</c> is always written.</param>
	/// <returns>The Feature, to insert first in the FeatureCollection.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="defaults"/> fails CRC validation.</exception>
	public static Feature CreateDefaultsFeature(CrcSymbolDefaults defaults)
	{
		CrcPropertyValidator.ThrowIfInvalid(CrcPropertyValidator.ValidateSymbolDefaults(defaults), "Invalid CRC Symbol defaults");

		AttributesTable attributes = StartDefaults("isSymbolDefaults", defaults.Bcg, defaults.Filters);
		AddIfSet(attributes, "style", defaults.Style);
		attributes.Add("size", defaults.Size);

		return new Feature(IsDefaultsPoint, attributes);
	}

	/// <summary>
	/// Builds a file's non-rendered <c>isTextDefaults</c> Feature. It never carries <c>text</c>:
	/// CRC does not read a label from a defaults Feature.
	/// </summary>
	/// <param name="defaults">The defaults. Every value is written.</param>
	/// <returns>The Feature, to insert first in the FeatureCollection.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="defaults"/> fails CRC validation.</exception>
	public static Feature CreateDefaultsFeature(CrcTextDefaults defaults)
	{
		CrcPropertyValidator.ThrowIfInvalid(CrcPropertyValidator.ValidateTextDefaults(defaults), "Invalid CRC Text defaults");

		AttributesTable attributes = StartDefaults("isTextDefaults", defaults.Bcg, defaults.Filters);
		attributes.Add("size", defaults.Size);
		attributes.Add("underline", defaults.Underline);
		attributes.Add("opaque", defaults.Opaque);
		attributes.Add("xOffset", defaults.XOffset);
		attributes.Add("yOffset", defaults.YOffset);

		return new Feature(IsDefaultsPoint, attributes);
	}

	/// <summary>
	/// Builds the properties that make one Line Feature differ from its file's defaults. A
	/// property left <see langword="null"/> is not written, so the Feature inherits it.
	/// </summary>
	/// <param name="properties">The override values.</param>
	/// <returns>The attributes, ready to attach to the Feature.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="properties"/> fails CRC validation.</exception>
	public static AttributesTable CreateOverrideProperties(CrcLineProperties properties)
	{
		CrcPropertyValidator.ThrowIfInvalid(
			CrcPropertyValidator.ValidateLine(properties), "Invalid CRC Line properties for feature override", nameof(properties));

		AttributesTable table = StartOverride(properties.Bcg, properties.Filters);
		AddIfSet(table, "style", properties.Style);
		AddIfSet(table, "thickness", properties.Thickness);
		return table;
	}

	/// <summary>
	/// Builds the properties that make one Symbol Feature differ from its file's defaults. A
	/// property left <see langword="null"/> is not written, so the Feature inherits it.
	/// </summary>
	/// <param name="properties">The override values.</param>
	/// <returns>The attributes, ready to attach to the Feature.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="properties"/> fails CRC validation.</exception>
	public static AttributesTable CreateOverrideProperties(CrcSymbolProperties properties)
	{
		CrcPropertyValidator.ThrowIfInvalid(
			CrcPropertyValidator.ValidateSymbol(properties), "Invalid CRC Symbol properties for feature override", nameof(properties));

		AttributesTable table = StartOverride(properties.Bcg, properties.Filters);
		AddIfSet(table, "style", properties.Style);
		AddIfSet(table, "size", properties.Size);
		return table;
	}

	/// <summary>
	/// Builds the properties that make one Text Feature differ from its file's defaults,
	/// including its required <c>text</c>. A property left <see langword="null"/> is not written,
	/// so the Feature inherits it.
	/// </summary>
	/// <param name="properties">The override values.</param>
	/// <returns>The attributes, ready to attach to the Feature.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="properties"/> fails CRC validation.</exception>
	public static AttributesTable CreateOverrideProperties(CrcTextProperties properties)
	{
		CrcPropertyValidator.ThrowIfInvalid(
			CrcPropertyValidator.ValidateText(properties), "Invalid CRC Text properties for feature override", nameof(properties));

		AttributesTable table = StartOverride(properties.Bcg, properties.Filters);
		table.Add("text", properties.Text.ToArray());

		// Same order as a Text defaults Feature.
		AddIfSet(table, "size", properties.Size);
		AddIfSet(table, "underline", properties.Underline);
		AddIfSet(table, "opaque", properties.Opaque);
		AddIfSet(table, "xOffset", properties.XOffset);
		AddIfSet(table, "yOffset", properties.YOffset);
		return table;
	}

	/// <summary>The flag, <c>bcg</c> and <c>filters</c> every defaults Feature starts with, in that order.</summary>
	private static AttributesTable StartDefaults(string flagName, int bcg, IReadOnlyList<int> filters)
	{
		AttributesTable attributes = new()
		{
			{ flagName, true },
			{ "bcg", bcg },
			{ "filters", filters.ToArray() }
		};
		return attributes;
	}

	/// <summary>The <c>bcg</c> (when set) and <c>filters</c> every override starts with.</summary>
	private static AttributesTable StartOverride(int? bcg, IReadOnlyList<int> filters)
	{
		AttributesTable table = [];
		AddIfSet(table, "bcg", bcg);
		table.Add("filters", filters.ToArray());
		return table;
	}

	private static void AddIfSet(AttributesTable table, string name, object? value)
	{
		if (value is not null)
		{
			table.Add(name, value);
		}
	}
}
