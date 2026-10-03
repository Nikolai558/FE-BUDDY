using System.Globalization;

using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Eram.Models;

using NetTopologySuite.Features;

namespace FeBuddy.Core.Application.Conversions.EramToGeojson;

/// <summary>
/// Turns ERAM display properties into CRC's: complete defaults for a file's
/// <c>isLineDefaults</c> / <c>isSymbolDefaults</c> / <c>isTextDefaults</c> Feature, and the
/// per-feature overrides an element sets.
/// </summary>
/// <remarks>
/// ERAM spells styles its own way (<c>Solid</c>, <c>ShortDashed</c>, <c>RNAVOnlyWaypoint</c>);
/// CRC's names are the same words in camelCase, so they are matched ignoring case. ERAM text has
/// no opaque background, so its Text defaults always have <c>opaque</c> off. Every value is checked
/// against what CRC can draw: a defaults set with a missing or invalid value is not used as CRC
/// defaults (they are never guessed) - <see cref="Gaps"/> says what keeps it from being - and an
/// invalid value is taken out (<see cref="Usable"/>) before it is laid over anything, so what it
/// would have covered shows through.
/// </remarks>
internal static class EramCrcProperties
{
	/// <summary>Complete, valid Line defaults from ERAM properties.</summary>
	/// <param name="properties">An object's <c>LineDefaults</c>, or <see langword="null"/> when it has none.</param>
	/// <returns>The defaults, or <see langword="null"/> when they are missing, incomplete or invalid (see <see cref="Gaps"/>).</returns>
	public static CrcLineDefaults? LineDefaults(EramProperties? properties) =>
		Gaps(EramElementKind.Line, properties).Count > 0 ? null : new CrcLineDefaults
		{
			Bcg = properties!.Bcg!.Value,
			Filters = properties.Filters!,
			Style = Style(properties.Style, CrcPropertyValidator.ValidLineStyles)!,
			Thickness = properties.Thickness!.Value,
		};

	/// <summary>Complete, valid Symbol defaults from ERAM properties.</summary>
	/// <param name="properties">An object's <c>SymbolDefaults</c>, or <see langword="null"/> when it has none.</param>
	/// <returns>The defaults, or <see langword="null"/> when they are missing, incomplete or invalid (see <see cref="Gaps"/>).</returns>
	public static CrcSymbolDefaults? SymbolDefaults(EramProperties? properties) =>
		Gaps(EramElementKind.Symbol, properties).Count > 0 ? null : new CrcSymbolDefaults
		{
			Bcg = properties!.Bcg!.Value,
			Filters = properties.Filters!,
			Style = Style(properties.Style, CrcPropertyValidator.ValidSymbolStyles)!,
			Size = properties.Size!.Value,
		};

	/// <summary>Complete, valid Text defaults from ERAM properties.</summary>
	/// <param name="properties">An object's <c>TextDefaults</c>, or <see langword="null"/> when it has none.</param>
	/// <returns>The defaults, or <see langword="null"/> when they are missing, incomplete or invalid (see <see cref="Gaps"/>).</returns>
	public static CrcTextDefaults? TextDefaults(EramProperties? properties) =>
		Gaps(EramElementKind.Text, properties).Count > 0 ? null : new CrcTextDefaults
		{
			Bcg = properties!.Bcg!.Value,
			Filters = properties.Filters!,
			Size = properties.Size!.Value,
			Underline = properties.Underline!.Value,
			Opaque = false,
			XOffset = properties.XOffset!.Value,
			YOffset = properties.YOffset!.Value,
		};

	/// <summary>
	/// What keeps ERAM properties from being complete CRC defaults of a kind: each property CRC's
	/// defaults of that kind need that they lack, or hold with a value CRC can't draw.
	/// </summary>
	/// <param name="kind">Which defaults: Line, Symbol or Text.</param>
	/// <param name="properties">An object's defaults, or <see langword="null"/> when it has none.</param>
	/// <returns>Each such property, in CRC's order; empty when the defaults are complete and valid.</returns>
	public static IReadOnlyList<EramDefaultsGap> Gaps(EramElementKind kind, EramProperties? properties)
	{
		EramProperties values = properties ?? EramProperties.None;
		Dictionary<string, string> invalid = new(StringComparer.Ordinal);
		Overrides(kind, values, (property, value) => invalid[property] = value);

		return [.. Required(kind, values)
			.Where(required => required.Value is null || invalid.ContainsKey(required.Property))
			.Select(required => new EramDefaultsGap(required.Property, required.Value is null ? null : invalid[required.Property]))];
	}

	/// <summary>
	/// The properties with every value CRC can't draw for <paramref name="kind"/> taken out, so that
	/// when they are laid over other values (<see cref="Over"/>), those show through instead.
	/// </summary>
	/// <param name="kind">What the properties are for; decides which properties apply.</param>
	/// <param name="properties">An element's own values, or an object's defaults.</param>
	/// <returns>The same values less the invalid ones; <paramref name="properties"/> itself when all are valid.</returns>
	public static EramProperties Usable(EramElementKind kind, EramProperties properties)
	{
		HashSet<string> invalid = new(StringComparer.Ordinal);
		Overrides(kind, properties, (property, _) => invalid.Add(property));

		return invalid.Count == 0 ? properties : properties with
		{
			Bcg = invalid.Contains("bcg") ? null : properties.Bcg,
			Filters = invalid.Contains("filters") ? null : properties.Filters,
			Style = invalid.Contains("style") ? null : properties.Style,
			Thickness = invalid.Contains("thickness") ? null : properties.Thickness,
			Size = invalid.Contains("size") ? null : properties.Size,
		};
	}

	/// <summary>
	/// What CRC draws a property of a kind with when nothing sets it (see
	/// <see cref="CrcPropertyValidator.LineAutoAssigned"/>), or <see langword="null"/> for
	/// <c>filters</c>, which CRC never assigns.
	/// </summary>
	/// <param name="kind">Line, Symbol or Text.</param>
	/// <param name="property">The CRC property, e.g. <c>style</c>.</param>
	/// <returns>The value, as CRC writes it (<c>vor</c>, <c>1</c>, <c>false</c>).</returns>
	public static string? AutoAssigned(EramElementKind kind, string property) => (kind switch
	{
		EramElementKind.Line => CrcPropertyValidator.LineAutoAssigned,
		EramElementKind.Symbol => CrcPropertyValidator.SymbolAutoAssigned,
		_ => CrcPropertyValidator.TextAutoAssigned,
	}).GetValueOrDefault(property);

	/// <summary>CRC defaults as ERAM properties, so an element's overrides can be laid over them.</summary>
	/// <param name="defaults">The Line, Symbol or Text defaults, or <see langword="null"/>.</param>
	/// <returns>The same values as ERAM properties; <see cref="EramProperties.None"/> for <see langword="null"/>.</returns>
	public static EramProperties AsEram(object? defaults) => defaults switch
	{
		CrcLineDefaults line => new() { Bcg = line.Bcg, Filters = line.Filters, Style = line.Style, Thickness = line.Thickness },
		CrcSymbolDefaults symbol => new() { Bcg = symbol.Bcg, Filters = symbol.Filters, Style = symbol.Style, Size = symbol.Size },
		CrcTextDefaults text => new()
		{
			Bcg = text.Bcg,
			Filters = text.Filters,
			Size = text.Size,
			Underline = text.Underline,
			XOffset = text.XOffset,
			YOffset = text.YOffset,
		},
		_ => EramProperties.None,
	};

	/// <summary>An element's overrides laid over its defaults: each value the element sets wins.</summary>
	/// <param name="defaults">The defaults.</param>
	/// <param name="overrides">The element's own values.</param>
	/// <returns>The values the element is drawn with.</returns>
	public static EramProperties Over(EramProperties defaults, EramProperties overrides) => new()
	{
		Bcg = overrides.Bcg ?? defaults.Bcg,
		Filters = overrides.Filters ?? defaults.Filters,
		Style = overrides.Style ?? defaults.Style,
		Thickness = overrides.Thickness ?? defaults.Thickness,
		Size = overrides.Size ?? defaults.Size,
		Underline = overrides.Underline ?? defaults.Underline,
		XOffset = overrides.XOffset ?? defaults.XOffset,
		YOffset = overrides.YOffset ?? defaults.YOffset,
	};

	/// <summary>
	/// The CRC properties an element sets for its kind, leaving out any CRC cannot draw.
	/// </summary>
	/// <param name="kind">What the element draws; decides which properties apply.</param>
	/// <param name="overrides">The element's own values.</param>
	/// <param name="dropped">Told of each value left out: the CRC property (<c>style</c>) and the value (<c>DME</c>).</param>
	/// <returns>The attributes, in CRC's order; empty when the element sets nothing usable.</returns>
	public static AttributesTable Overrides(EramElementKind kind, EramProperties overrides, Action<string, string> dropped)
	{
		AttributesTable table = [];

		Add(table, "bcg", overrides.Bcg, value => value is >= CrcPropertyValidator.MinBcg and <= CrcPropertyValidator.MaxBcg, dropped);

		if (overrides.Filters is { } filters)
		{
			if (filters.All(filter => filter is >= CrcPropertyValidator.MinFilter and <= CrcPropertyValidator.MaxFilter))
			{
				table.Add("filters", filters.ToArray());
			}
			else
			{
				dropped("filters", string.Join(',', filters));
			}
		}

		switch (kind)
		{
			case EramElementKind.Line:
				AddStyle(table, overrides.Style, CrcPropertyValidator.ValidLineStyles, dropped);
				Add(table, "thickness", overrides.Thickness, value => value is >= CrcPropertyValidator.MinThickness and <= CrcPropertyValidator.MaxThickness, dropped);
				break;

			case EramElementKind.Symbol:
				AddStyle(table, overrides.Style, CrcPropertyValidator.ValidSymbolStyles, dropped);
				Add(table, "size", overrides.Size, value => value is >= CrcPropertyValidator.MinSymbolSize and <= CrcPropertyValidator.MaxSymbolSize, dropped);
				break;

			default:
				Add(table, "size", overrides.Size, value => value is >= CrcPropertyValidator.MinTextSize and <= CrcPropertyValidator.MaxTextSize, dropped);
				Add(table, "underline", overrides.Underline, _ => true, dropped);
				Add(table, "xOffset", overrides.XOffset, _ => true, dropped);
				Add(table, "yOffset", overrides.YOffset, _ => true, dropped);
				break;
		}

		return table;
	}

	/// <summary>An ERAM style in CRC's spelling, or the value unchanged when CRC has no such style.</summary>
	private static string? Style(string? style, IReadOnlyList<string> valid) => SettingsValueReader.NormalizeStyle(style, valid);

	private static void AddStyle(AttributesTable table, string? style, IReadOnlyList<string> valid, Action<string, string> dropped)
	{
		if (style is null)
		{
			return;
		}

		string crc = Style(style, valid)!;

		if (valid.Contains(crc, StringComparer.Ordinal))
		{
			table.Add("style", crc);
		}
		else
		{
			dropped("style", style);
		}
	}

	private static void Add<T>(AttributesTable table, string name, T? value, Func<T, bool> isValid, Action<string, string> dropped)
		where T : struct
	{
		if (value is not { } set)
		{
			return;
		}

		if (isValid(set))
		{
			table.Add(name, set);
		}
		else
		{
			dropped(name, Convert.ToString(set, CultureInfo.InvariantCulture)!);
		}
	}

	/// <summary>The properties CRC defaults of a kind need, in CRC's order, with the values <paramref name="properties"/> hold.</summary>
	private static (string Property, object? Value)[] Required(EramElementKind kind, EramProperties properties) => kind switch
	{
		EramElementKind.Line =>
			[("bcg", properties.Bcg), ("filters", properties.Filters), ("style", properties.Style), ("thickness", properties.Thickness)],
		EramElementKind.Symbol =>
			[("bcg", properties.Bcg), ("filters", properties.Filters), ("style", properties.Style), ("size", properties.Size)],
		_ =>
			[
				("bcg", properties.Bcg), ("filters", properties.Filters), ("size", properties.Size), ("underline", properties.Underline),
				("xOffset", properties.XOffset), ("yOffset", properties.YOffset),
			],
	};
}
