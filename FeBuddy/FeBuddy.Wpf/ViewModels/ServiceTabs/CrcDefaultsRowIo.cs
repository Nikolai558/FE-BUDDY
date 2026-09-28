using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Moves one CRC ERAM defaults row (<see cref="EramClassDefault"/>) in and out of a tab's saved
/// config and its run settings block - the one way every tab with a CRC ERAM Defaults card does it.
/// </summary>
/// <remarks>
/// Only the fields the row's kind shows are read, saved or sent: a Line row never writes a
/// <c>size</c>, a Text row never a <c>style</c>. A row whose Features bring their own style
/// (<see cref="EramClassDefault.StyleFromFeatures"/>) still saves its style, so switching back
/// restores it, but does not send one.
/// </remarks>
public static class CrcDefaultsRowIo
{
	/// <summary>
	/// Restores a row from the tab's saved config. A field with nothing saved is blank, as a new
	/// row starts - never what the row held before, or a reload (discard, undo, a settings import)
	/// would keep a value the config no longer has and the next save would write it back.
	/// </summary>
	/// <param name="row">The row to fill.</param>
	/// <param name="prefix">Where the row is saved under the tab's node, e.g. <c>CrcEramPropertyDefaults.Airports_Symbol</c>.</param>
	/// <param name="get">Reads one of the tab's saved values by its key under the tab's node.</param>
	public static void Load(EramClassDefault row, string prefix, Func<string, string?> get)
	{
		ArgumentNullException.ThrowIfNull(row);
		ArgumentNullException.ThrowIfNull(get);

		row.Bcg = get($"{prefix}.bcg") ?? string.Empty;
		row.Filters = get($"{prefix}.filters") ?? string.Empty;

		if (row.ShowStyle)
		{
			row.Style = get($"{prefix}.style") ?? string.Empty;
		}

		if (row.ShowThickness)
		{
			row.Thickness = get($"{prefix}.thickness") ?? string.Empty;
		}

		if (row.ShowSize)
		{
			row.Size = get($"{prefix}.size") ?? string.Empty;
		}

		if (row.ShowTextOptions)
		{
			row.Underline = get($"{prefix}.underline") ?? string.Empty;
			row.Opaque = get($"{prefix}.opaque") ?? string.Empty;
			row.XOffset = get($"{prefix}.xOffset") ?? string.Empty;
			row.YOffset = get($"{prefix}.yOffset") ?? string.Empty;
		}
	}

	/// <summary>Writes a row into the tab's config.</summary>
	/// <param name="row">The row to save.</param>
	/// <param name="prefix">Where the row is saved under the tab's node.</param>
	/// <param name="set">Writes one value by its key under the tab's node.</param>
	public static void Save(EramClassDefault row, string prefix, Action<string, string> set)
	{
		ArgumentNullException.ThrowIfNull(row);
		ArgumentNullException.ThrowIfNull(set);

		set($"{prefix}.bcg", row.Bcg);
		set($"{prefix}.filters", row.Filters);

		if (row.ShowStyle)
		{
			set($"{prefix}.style", row.Style);
		}

		if (row.ShowThickness)
		{
			set($"{prefix}.thickness", row.Thickness);
		}

		if (row.ShowSize)
		{
			set($"{prefix}.size", row.Size);
		}

		if (row.ShowTextOptions)
		{
			set($"{prefix}.underline", row.Underline);
			set($"{prefix}.opaque", row.Opaque);
			set($"{prefix}.xOffset", row.XOffset);
			set($"{prefix}.yOffset", row.YOffset);
		}
	}

	/// <summary>
	/// Adds a row to a run's settings block under <c>Crc.&lt;Class&gt;.&lt;Kind&gt;.*</c>, the keys
	/// the library's <c>CrcDefaultsReader</c> reads.
	/// </summary>
	/// <param name="row">The row to send.</param>
	/// <param name="settings">The settings block being built.</param>
	public static void AddToSettingsBlock(EramClassDefault row, IDictionary<string, string> settings)
	{
		ArgumentNullException.ThrowIfNull(row);
		ArgumentNullException.ThrowIfNull(settings);

		string prefix = $"Crc.{row.ClassName}.{row.Kind}";
		settings[$"{prefix}.bcg"] = row.Bcg;
		settings[$"{prefix}.filters"] = row.Filters;

		if (row.AsksForStyle)
		{
			settings[$"{prefix}.style"] = row.Style;
		}

		if (row.ShowThickness)
		{
			settings[$"{prefix}.thickness"] = row.Thickness;
		}

		if (row.ShowSize)
		{
			settings[$"{prefix}.size"] = row.Size;
		}

		if (row.ShowTextOptions)
		{
			settings[$"{prefix}.underline"] = row.Underline;
			settings[$"{prefix}.opaque"] = row.Opaque;
			settings[$"{prefix}.xOffset"] = row.XOffset.Trim();
			settings[$"{prefix}.yOffset"] = row.YOffset.Trim();
		}
	}
}
