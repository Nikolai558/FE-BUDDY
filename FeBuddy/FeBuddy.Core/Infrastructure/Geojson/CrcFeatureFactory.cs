using System.Linq;

using FeBuddy.Core.Domain.Crc;
using FeBuddy.Core.Domain.Crc.Models;

using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Geojson;

/// <summary>
/// Builds CRC ERAM GeoJSON property Features: the non-rendered "isDefaults" Feature that
/// establishes a file's baseline properties (from <see cref="CrcLineDefaults"/>,
/// <see cref="CrcSymbolDefaults"/> or <see cref="CrcTextDefaults"/>), and the "Overriding
/// Property" attribute sets written on individual rendered Features.
/// </summary>
/// <remarks>
/// See the three-tier property model in
/// <see href="https://github.com/KCSanders7070/CRC_GeoJson_Concepts/blob/main/CRC_Geojsons.md">
/// CRC_Geojsons.md</see>: a per-feature Default Override beats an isDefaults Feature, which
/// in turn beats CRC's own auto-assigned fallback. Both methods on this class validate the
/// supplied properties before building anything, via <see cref="CrcPropertyValidator"/>.
/// </remarks>
public static class CrcFeatureFactory
{
	/// <summary>
	/// Shared geometry factory (SRID 4326 / WGS84), matching the one used elsewhere in the
	/// airway generator so all emitted geometry is consistent.
	/// </summary>
	private static readonly GeometryFactory GeometryFactory =
		NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

	/// <summary>
	/// The coordinate CRC's isDefaults Features use: [longitude=90, latitude=180]. This is
	/// intentionally an out-of-range point (valid latitude is -90..90) so that any GeoJSON
	/// viewer other than CRC simply omits it from the map. See old FE-Buddy's
	/// <c>AddIsDefaults.cs</c> and the CRC_Geojsons.md example, both of which use this exact
	/// coordinate.
	/// </summary>
	private static readonly Point IsDefaultsPoint =
		GeometryFactory.CreatePoint(new Coordinate(x: 90.0, y: 180.0));

	/// <summary>
	/// Builds the non-rendered <c>isLineDefaults</c> Feature for a GeoJSON file: first in the
	/// file, it gives every Line Feature that does not override them their properties.
	/// </summary>
	/// <param name="defaults">The defaults. Every value is written.</param>
	/// <returns>The Feature, ready to insert at index 0 of the FeatureCollection.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="defaults"/> fails CRC validation.</exception>
	/// <remarks>
	/// A defaults Feature only ever carries the properties CRC reads as defaults, in a fixed
	/// order: the <c>is...Defaults</c> flag, <c>bcg</c>, <c>filters</c>, then the kind's own.
	/// </remarks>
	public static Feature CreateDefault(CrcLineDefaults defaults)
	{
		ThrowIfInvalid(CrcPropertyValidator.ValidateLineDefaults(defaults), "Line");

		AttributesTable attributes = StartDefaults("isLineDefaults", defaults.Bcg, defaults.Filters);
		attributes.Add("style", defaults.Style);
		attributes.Add("thickness", defaults.Thickness);

		return new Feature(IsDefaultsPoint, attributes);
	}

	/// <summary>Builds the non-rendered <c>isSymbolDefaults</c> Feature for a GeoJSON file.</summary>
	/// <param name="defaults">The defaults. Every value is written.</param>
	/// <returns>The Feature, ready to insert at index 0 of the FeatureCollection.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="defaults"/> fails CRC validation.</exception>
	public static Feature CreateDefault(CrcSymbolDefaults defaults)
	{
		ThrowIfInvalid(CrcPropertyValidator.ValidateSymbolDefaults(defaults), "Symbol");

		AttributesTable attributes = StartDefaults("isSymbolDefaults", defaults.Bcg, defaults.Filters);
		attributes.Add("style", defaults.Style);
		attributes.Add("size", defaults.Size);

		return new Feature(IsDefaultsPoint, attributes);
	}

	/// <summary>
	/// Builds the non-rendered <c>isTextDefaults</c> Feature for a GeoJSON file. It never
	/// carries <c>text</c>: CRC does not read a label from a defaults Feature.
	/// </summary>
	/// <param name="defaults">The defaults. Every value is written.</param>
	/// <returns>The Feature, ready to insert at index 0 of the FeatureCollection.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="defaults"/> fails CRC validation.</exception>
	public static Feature CreateDefault(CrcTextDefaults defaults)
	{
		ThrowIfInvalid(CrcPropertyValidator.ValidateTextDefaults(defaults), "Text");

		AttributesTable attributes = StartDefaults("isTextDefaults", defaults.Bcg, defaults.Filters);
		attributes.Add("size", defaults.Size);
		attributes.Add("underline", defaults.Underline);
		attributes.Add("opaque", defaults.Opaque);
		attributes.Add("xOffset", defaults.XOffset);
		attributes.Add("yOffset", defaults.YOffset);

		return new Feature(IsDefaultsPoint, attributes);
	}

	/// <summary>The flag, <c>bcg</c> and <c>filters</c> every defaults Feature starts with, in that order.</summary>
	private static AttributesTable StartDefaults(string flagName, int bcg, IReadOnlyList<int> filters)
	{
		AttributesTable attributes = new();
		attributes.Add(flagName, true);
		attributes.Add("bcg", bcg);
		attributes.Add("filters", filters.ToArray());
		return attributes;
	}

	private static void ThrowIfInvalid(CrcPropertyValidationResult validation, string kind)
	{
		if (!validation.IsValid)
		{
			throw new ArgumentException(
				$"Invalid CRC {kind} defaults:" + Environment.NewLine + string.Join(Environment.NewLine, validation.Errors));
		}
	}

	/// <summary>
	/// Builds the property table for a single rendered Feature's "Overriding Property" set
	/// (a per-feature Default Override, the highest-priority tier of CRC's property model).
	/// </summary>
	/// <param name="kind">Which feature family <paramref name="properties"/> applies to.</param>
	/// <param name="properties">
	/// A <see cref="CrcLineProperties"/>, <see cref="CrcSymbolProperties"/>, or
	/// <see cref="CrcTextProperties"/> instance matching <paramref name="kind"/>.
	/// </param>
	/// <returns>An <see cref="AttributesTable"/> ready to attach to a rendered Feature.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="properties"/> fails CRC property validation, or is not the
	/// type expected for <paramref name="kind"/>.
	/// </exception>
	public static AttributesTable CreateFeatureProperty(CrcFeatureKind kind, object properties)
	{
		ValidateOrThrow(kind, properties, "feature override");

		return BuildAttributesTable(kind, properties);
	}

	/// <summary>
	/// Runs <see cref="CrcPropertyValidator"/> and throws an
	/// <see cref="ArgumentException"/> listing every violation when validation fails.
	/// </summary>
	private static void ValidateOrThrow(CrcFeatureKind kind, object properties, string context)
	{
		CrcPropertyValidationResult validation =
			CrcPropertyValidator.Validate(kind, properties);

		if (!validation.IsValid)
		{
			throw new ArgumentException(
				$"Invalid CRC {kind} properties for {context}:" +
				Environment.NewLine +
				string.Join(Environment.NewLine, validation.Errors),
				nameof(properties));
		}
	}

	/// <summary>
	/// Converts a typed CRC property record into an <see cref="AttributesTable"/>, omitting
	/// any optional property left null so CRC's own auto-assigned fallback applies to it.
	/// </summary>
	private static AttributesTable BuildAttributesTable(CrcFeatureKind kind, object properties)
	{
		AttributesTable table = new();

		switch (kind)
		{
			case CrcFeatureKind.Line:
				AddLineAttributes(table, (CrcLineProperties)properties);
				break;

			case CrcFeatureKind.Symbol:
				AddSymbolAttributes(table, (CrcSymbolProperties)properties);
				break;

			case CrcFeatureKind.Text:
				AddTextAttributes(table, (CrcTextProperties)properties);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown CRC feature kind.");
		}

		return table;
	}

	private static void AddLineAttributes(AttributesTable table, CrcLineProperties properties)
	{
		if (properties.Bcg is int bcg)
		{
			table.Add("bcg", bcg);
		}

		table.Add("filters", properties.Filters.ToArray());

		if (properties.Style is not null)
		{
			table.Add("style", properties.Style);
		}

		if (properties.Thickness is int thickness)
		{
			table.Add("thickness", thickness);
		}
	}

	private static void AddSymbolAttributes(AttributesTable table, CrcSymbolProperties properties)
	{
		if (properties.Bcg is int bcg)
		{
			table.Add("bcg", bcg);
		}

		table.Add("filters", properties.Filters.ToArray());

		if (properties.Style is not null)
		{
			table.Add("style", properties.Style);
		}

		if (properties.Size is int size)
		{
			table.Add("size", size);
		}
	}

	private static void AddTextAttributes(AttributesTable table, CrcTextProperties properties)
	{
		if (properties.Bcg is int bcg)
		{
			table.Add("bcg", bcg);
		}

		table.Add("filters", properties.Filters.ToArray());
		table.Add("text", properties.Text.ToArray());

		if (properties.Size is int size)
		{
			table.Add("size", size);
		}

		// Same order as a Text defaults Feature: size, underline, opaque, xOffset, yOffset.
		if (properties.Underline is bool underline)
		{
			table.Add("underline", underline);
		}

		if (properties.Opaque is bool opaque)
		{
			table.Add("opaque", opaque);
		}

		if (properties.XOffset is int xOffset)
		{
			table.Add("xOffset", xOffset);
		}

		if (properties.YOffset is int yOffset)
		{
			table.Add("yOffset", yOffset);
		}
	}
}
