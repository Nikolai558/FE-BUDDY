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
/// against what CRC can draw: a defaults set with a missing or invalid value is not used at all
/// (CRC defaults are never guessed), and an invalid override value is left out so the feature
/// takes its file's default instead.
/// </remarks>
internal static class EramCrcProperties
{
	/// <summary>Complete, valid Line defaults from ERAM properties.</summary>
	/// <param name="properties">An object's <c>LineDefaults</c>, or <see langword="null"/> when it has none.</param>
	/// <param name="problem">Why they cannot be used, when they cannot.</param>
	/// <returns>The defaults, or <see langword="null"/> when they are missing, incomplete or invalid.</returns>
	public static CrcLineDefaults? LineDefaults(EramProperties? properties, out string? problem)
	{
		if (!TryRequire(properties, "LineDefaults", out problem, ("Bcg", properties?.Bcg), ("Filters", properties?.Filters),
			("Style", properties?.Style), ("Thickness", properties?.Thickness)))
		{
			return null;
		}

		CrcLineDefaults defaults = new()
		{
			Bcg = properties!.Bcg!.Value,
			Filters = properties.Filters!,
			Style = Style(properties.Style, CrcPropertyValidator.ValidLineStyles)!,
			Thickness = properties.Thickness!.Value,
		};

		return Valid(CrcPropertyValidator.ValidateLineDefaults(defaults), "LineDefaults", ref problem) ? defaults : null;
	}

	/// <summary>Complete, valid Symbol defaults from ERAM properties.</summary>
	/// <param name="properties">An object's <c>SymbolDefaults</c>, or <see langword="null"/> when it has none.</param>
	/// <param name="problem">Why they cannot be used, when they cannot.</param>
	/// <returns>The defaults, or <see langword="null"/> when they are missing, incomplete or invalid.</returns>
	public static CrcSymbolDefaults? SymbolDefaults(EramProperties? properties, out string? problem)
	{
		if (!TryRequire(properties, "SymbolDefaults", out problem, ("Bcg", properties?.Bcg), ("Filters", properties?.Filters),
			("Style", properties?.Style), ("Size", properties?.Size)))
		{
			return null;
		}

		CrcSymbolDefaults defaults = new()
		{
			Bcg = properties!.Bcg!.Value,
			Filters = properties.Filters!,
			Style = Style(properties.Style, CrcPropertyValidator.ValidSymbolStyles)!,
			Size = properties.Size!.Value,
		};

		return Valid(CrcPropertyValidator.ValidateSymbolDefaults(defaults), "SymbolDefaults", ref problem) ? defaults : null;
	}

	/// <summary>Complete, valid Text defaults from ERAM properties.</summary>
	/// <param name="properties">An object's <c>TextDefaults</c>, or <see langword="null"/> when it has none.</param>
	/// <param name="problem">Why they cannot be used, when they cannot.</param>
	/// <returns>The defaults, or <see langword="null"/> when they are missing, incomplete or invalid.</returns>
	public static CrcTextDefaults? TextDefaults(EramProperties? properties, out string? problem)
	{
		if (!TryRequire(properties, "TextDefaults", out problem, ("Bcg", properties?.Bcg), ("Filters", properties?.Filters),
			("Size", properties?.Size), ("Underline", properties?.Underline),
			("XOffset", properties?.XOffset), ("YOffset", properties?.YOffset)))
		{
			return null;
		}

		CrcTextDefaults defaults = new()
		{
			Bcg = properties!.Bcg!.Value,
			Filters = properties.Filters!,
			Size = properties.Size!.Value,
			Underline = properties.Underline!.Value,
			Opaque = false,
			XOffset = properties.XOffset!.Value,
			YOffset = properties.YOffset!.Value,
		};

		return Valid(CrcPropertyValidator.ValidateTextDefaults(defaults), "TextDefaults", ref problem) ? defaults : null;
	}

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
	/// <param name="dropped">Told of each value left out, as <c>name="value"</c>.</param>
	/// <returns>The attributes, in CRC's order; empty when the element sets nothing usable.</returns>
	public static AttributesTable Overrides(EramElementKind kind, EramProperties overrides, Action<string> dropped)
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
				dropped($"Filters=\"{string.Join(',', filters)}\"");
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

	private static void AddStyle(AttributesTable table, string? style, IReadOnlyList<string> valid, Action<string> dropped)
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
			dropped($"Style=\"{style}\"");
		}
	}

	private static void Add<T>(AttributesTable table, string name, T? value, Func<T, bool> isValid, Action<string> dropped)
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
			dropped($"{char.ToUpperInvariant(name[0])}{name[1..]}=\"{set}\"");
		}
	}

	private static bool TryRequire(EramProperties? properties, string element, out string? problem, params (string Name, object? Value)[] values)
	{
		if (properties is null)
		{
			problem = $"it has no {element}";
			return false;
		}

		string[] missing = [.. values.Where(value => value.Value is null).Select(value => value.Name)];
		problem = missing.Length > 0 ? $"its {element} have no {string.Join(", ", missing)}" : null;
		return missing.Length == 0;
	}

	private static bool Valid(CrcPropertyValidationResult result, string element, ref string? problem)
	{
		if (result.IsValid)
		{
			return true;
		}

		problem = $"its {element} are not values CRC can draw ({string.Join(" ", result.Errors)})";
		return false;
	}
}
