using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Moves one CRC ERAM defaults row (<see cref="EramClassDefault"/>) in and out of a tab's saved
/// config and its run settings block - the one way every tab with a CRC ERAM Defaults card does it.
/// </summary>
/// <remarks>
/// Only the fields the row's kind shows are read, saved or sent: a Line row never writes a
/// <c>size</c>, a Text row never a <c>style</c>.
/// </remarks>
public static class CrcDefaultsRowIo
{
	/// <summary>Restores a row from the tab's saved config; a field with nothing saved keeps its value.</summary>
	/// <param name="row">The row to fill.</param>
	/// <param name="prefix">Where the row is saved under the tab's node, e.g. <c>CrcEramPropertyDefaults.Airports_Symbol</c>.</param>
	/// <param name="get">Reads one of the tab's saved values by its key under the tab's node.</param>
	public static void Load(EramClassDefault row, string prefix, Func<string, string?> get)
	{
		ArgumentNullException.ThrowIfNull(row);
		ArgumentNullException.ThrowIfNull(get);

		row.Bcg = get($"{prefix}.bcg") ?? row.Bcg;
		row.Filters = get($"{prefix}.filters") ?? row.Filters;

		if (row.ShowStyle)
		{
			row.Style = get($"{prefix}.style") ?? row.Style;
		}

		if (row.ShowThickness)
		{
			row.Thickness = get($"{prefix}.thickness") ?? row.Thickness;
		}

		if (row.ShowSize)
		{
			row.Size = get($"{prefix}.size") ?? row.Size;
		}

		if (row.ShowTextOptions)
		{
			row.Underline = get($"{prefix}.underline") ?? row.Underline;
			row.Opaque = get($"{prefix}.opaque") ?? row.Opaque;
			row.XOffset = get($"{prefix}.xOffset") ?? row.XOffset;
			row.YOffset = get($"{prefix}.yOffset") ?? row.YOffset;
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

		if (row.ShowStyle)
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
