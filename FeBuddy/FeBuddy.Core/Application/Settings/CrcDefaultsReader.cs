using FeBuddy.Core.Domain.Crc;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Settings;

/// <summary>
/// Reads CRC defaults out of a raw settings block, the one way every service does it.
/// </summary>
/// <remarks>
/// <para>
/// Keys are <c>&lt;prefix&gt;.&lt;property&gt;</c>, where the prefix is the service's
/// <c>Crc.&lt;Class&gt;.&lt;Kind&gt;</c> (e.g. <c>Crc.Departures.Text</c>, <c>Crc.High.Line</c>).
/// Every property is required: FE-Buddy asks the user for each value rather than leaving any
/// to CRC's own fallback, so a missing one is an error that names the key.
/// </para>
/// <para>
/// A service calls these only for the defaults a file it is writing actually needs: one that
/// is uploaded to vNAS and chosen for CRC-ERAM defaults (<c>CrcDefaultsFor</c>).
/// </para>
/// </remarks>
public static class CrcDefaultsReader
{
	private static readonly HashSet<string> LinePropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "style", "thickness" };

	private static readonly HashSet<string> SymbolPropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "style", "size" };

	// No "text": a defaults Feature never carries a label, every Text Feature brings its own.
	private static readonly HashSet<string> TextPropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "size", "underline", "opaque", "xOffset", "yOffset" };

	/// <summary>The property names a kind's defaults are read from, as in <c>&lt;prefix&gt;.&lt;name&gt;</c>.</summary>
	/// <param name="kind">The feature kind.</param>
	/// <returns>The names, matched ignoring case.</returns>
	public static IReadOnlySet<string> PropertyNames(CrcFeatureKind kind) => kind switch
	{
		CrcFeatureKind.Line => LinePropertyNames,
		CrcFeatureKind.Symbol => SymbolPropertyNames,
		CrcFeatureKind.Text => TextPropertyNames,
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown CRC feature kind.")
	};

	/// <summary>Reads and validates the Line defaults under <paramref name="keyPrefix"/>.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="keyPrefix">e.g. <c>Crc.Runways.Line</c>.</param>
	/// <returns>The validated defaults.</returns>
	/// <exception cref="ArgumentException">Thrown when a value is missing or invalid.</exception>
	public static CrcLineDefaults ReadLine(IReadOnlyDictionary<string, string> settings, string keyPrefix)
	{
		CrcLineDefaults defaults = new()
		{
			Bcg = SettingsValueReader.RequiredInt(settings, $"{keyPrefix}.bcg"),
			Filters = SettingsValueReader.RequiredIntList(settings, $"{keyPrefix}.filters"),
			Style = SettingsValueReader.NormalizeStyle(
				SettingsValueReader.RequiredString(settings, $"{keyPrefix}.style"),
				CrcPropertyValidator.ValidLineStyles)!,
			Thickness = SettingsValueReader.RequiredInt(settings, $"{keyPrefix}.thickness"),
		};

		CrcPropertyValidator.ThrowIfInvalid(CrcPropertyValidator.ValidateLineDefaults(defaults), $"Invalid CRC defaults under '{keyPrefix}'");
		return defaults;
	}

	/// <summary>Reads and validates the Symbol defaults under <paramref name="keyPrefix"/>.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="keyPrefix">e.g. <c>Crc.Airports.Symbol</c>.</param>
	/// <param name="readStyle">
	/// When <see langword="false"/>, <c>&lt;keyPrefix&gt;.style</c> is not read and
	/// <see cref="CrcSymbolDefaults.Style"/> is left <see langword="null"/> - for a class whose
	/// Features each carry their own style rather than one style for the whole file (e.g. NAVAIDs
	/// styled by type). Default <see langword="true"/>.
	/// </param>
	/// <returns>The validated defaults.</returns>
	/// <exception cref="ArgumentException">Thrown when a value is missing or invalid.</exception>
	public static CrcSymbolDefaults ReadSymbol(IReadOnlyDictionary<string, string> settings, string keyPrefix, bool readStyle = true)
	{
		string? style = readStyle
			? SettingsValueReader.NormalizeStyle(
				SettingsValueReader.RequiredString(settings, $"{keyPrefix}.style"),
				CrcPropertyValidator.ValidSymbolStyles)
			: null;

		CrcSymbolDefaults defaults = new()
		{
			Bcg = SettingsValueReader.RequiredInt(settings, $"{keyPrefix}.bcg"),
			Filters = SettingsValueReader.RequiredIntList(settings, $"{keyPrefix}.filters"),
			Style = style,
			Size = SettingsValueReader.RequiredInt(settings, $"{keyPrefix}.size"),
		};

		CrcPropertyValidator.ThrowIfInvalid(CrcPropertyValidator.ValidateSymbolDefaults(defaults), $"Invalid CRC defaults under '{keyPrefix}'");
		return defaults;
	}

	/// <summary>Reads and validates the Text defaults under <paramref name="keyPrefix"/>.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="keyPrefix">e.g. <c>Crc.Departures.Text</c>.</param>
	/// <returns>The validated defaults.</returns>
	/// <exception cref="ArgumentException">Thrown when a value is missing or invalid.</exception>
	public static CrcTextDefaults ReadText(IReadOnlyDictionary<string, string> settings, string keyPrefix)
	{
		CrcTextDefaults defaults = new()
		{
			Bcg = SettingsValueReader.RequiredInt(settings, $"{keyPrefix}.bcg"),
			Filters = SettingsValueReader.RequiredIntList(settings, $"{keyPrefix}.filters"),
			Size = SettingsValueReader.RequiredInt(settings, $"{keyPrefix}.size"),
			Underline = SettingsValueReader.RequiredYesNo(settings, $"{keyPrefix}.underline"),
			Opaque = SettingsValueReader.RequiredYesNo(settings, $"{keyPrefix}.opaque"),
			XOffset = SettingsValueReader.RequiredInt(settings, $"{keyPrefix}.xOffset"),
			YOffset = SettingsValueReader.RequiredInt(settings, $"{keyPrefix}.yOffset"),
		};

		CrcPropertyValidator.ThrowIfInvalid(CrcPropertyValidator.ValidateTextDefaults(defaults), $"Invalid CRC defaults under '{keyPrefix}'");
		return defaults;
	}
}
