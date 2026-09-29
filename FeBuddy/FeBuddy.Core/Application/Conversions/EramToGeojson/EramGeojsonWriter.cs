using System.Globalization;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Eram.Models;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Conversions.EramToGeojson;

/// <summary>
/// Writes one converted ERAM <c>Geomaps.xml</c> into <c>…\ERAM_TO_GEOJSON\</c>, in the layouts
/// and with the names of the original ERAM_2_GEOJSON tool (<see cref="EramOutputLayout"/>).
/// </summary>
/// <remarks>
/// <para>
/// Every map is named <c>&lt;GeomapId&gt;_&lt;LabelLine1&gt;-&lt;LabelLine2&gt;</c>
/// (<see cref="MapName"/>). What an element looks like is worked out first: its object's
/// defaults - from the XML, the tab's CRC defaults, or both (<see cref="EramDefaultsSource"/>) -
/// with its own values laid over them, and filter <c>0</c> (always shown) when neither gives any.
/// That look decides where it goes and what it carries:
/// </para>
/// <list type="bullet">
/// <item><b>By Filters</b>: <c>&lt;map&gt;\Filter_01\Filter_01_Lines.geojson</c> (and <c>_Symbols</c>,
/// <c>_Text</c>), or <c>Multi-Filter_02_03_08\</c> for several filters, sorted. Everything with
/// those filters goes in, whatever its look, so the file's isDefaults Feature is the look most of
/// its Features share, and each Feature that looks different carries what differs.</item>
/// <item><b>By Attributes</b>: <c>&lt;map&gt;\BCG 01_Filters 01_Type AAV_Group 64_Object ZOB3NM_Style Solid_Thick 1_Lines.geojson</c>
/// - symbols <c>…_Style Vor_Font 1_Symbols</c>, text <c>…_Font 1_Underline F_X 0_Y 0_Text</c>. Everything
/// in a file looks the same, so its isDefaults Feature describes it all and no Feature overrides it.</item>
/// <item><b>Raw</b>: <c>&lt;map&gt;.geojson</c> beside the map folders, one Feature per element
/// (lines are not joined), each carrying its whole look and no isDefaults Feature.</item>
/// </list>
/// <para>
/// A look missing a value CRC needs (a BCG, say, from neither the XML nor the tab) cannot be an
/// isDefaults Feature: its Features carry what they have, and CRC falls back to its own defaults
/// for the rest (By Attributes names the missing values <c>none</c>). Text ERAM keeps hidden
/// (<c>DisplaySetting</c> false) is left out. The chosen <c>feb.*</c> properties go on every
/// Feature; lines are joined (<see cref="SegmentJoiner"/>) only where those match too.
/// </para>
/// </remarks>
public static class EramGeojsonWriter
{
	/// <summary>The conversion's folder inside the output directory: the original tool's <c>E2G_OUTPUT</c>.</summary>
	public const string RootFolder = "ERAM_TO_GEOJSON";

	private const string LogSource = "EramGeojsonWriter";

	/// <summary>Filters for an element that neither it nor its defaults give: always shown, as ERAM does.</summary>
	private static readonly IReadOnlyList<int> AlwaysShown = [0];

	/// <summary>The folder everything is written into.</summary>
	/// <param name="settings">The parsed settings.</param>
	/// <returns>The directory.</returns>
	public static string OutputDirectory(ConversionSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		return OutputDirectory(settings.OutputDirectory, settings.AddFeBuddyOutputFolder);
	}

	/// <summary>The folder everything is written into, from the output preferences alone - for a check before a run.</summary>
	/// <param name="outputDirectory">The output directory.</param>
	/// <param name="addFeBuddyOutputFolder">Whether output goes in a <c>FE-Buddy_Output</c> folder.</param>
	/// <returns>The directory.</returns>
	public static string OutputDirectory(string outputDirectory, bool addFeBuddyOutputFolder)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

		return ServiceOutputPaths.Resolve(outputDirectory, addFeBuddyOutputFolder, RootFolder);
	}

	/// <summary>
	/// A map's name, as the original tool gave it: <c>&lt;GeomapId&gt;_&lt;LabelLine1&gt;-&lt;LabelLine2&gt;</c>,
	/// e.g. <c>CENTER_CENTER-MAP</c>, with the characters Windows forbids in a name taken out and
	/// <c>LL1</c> / <c>LL2</c> for a label line the map has none of.
	/// </summary>
	/// <param name="map">The map.</param>
	/// <returns>The name.</returns>
	public static string MapName(EramGeoMap map)
	{
		ArgumentNullException.ThrowIfNull(map);

		return $"{FileSafe(map.Name)}_{FileSafe(map.LabelLine1 ?? "LL1")}-{FileSafe(map.LabelLine2 ?? "LL2")}";
	}

	/// <summary>Writes every file one Geomaps file converts to.</summary>
	/// <param name="geoMaps">The Geomaps file's content.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="files">The run's file set, which does the writing and remembers what was written.</param>
	/// <param name="messages">Where notices about missing defaults and unusable values go.</param>
	/// <returns>The files written for this Geomaps file, and how many rendered Features they hold.</returns>
	public static (IReadOnlyList<string> Paths, int FeatureCount) Write(
		EramGeoMapFile geoMaps,
		EramToGeojsonSettings settings,
		GeojsonFileSet files,
		List<ServiceMessage> messages)
	{
		ArgumentNullException.ThrowIfNull(geoMaps);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(files);
		ArgumentNullException.ThrowIfNull(messages);

		int before = files.FilesWritten.Count;
		string sourceName = Path.GetFileName(geoMaps.SourcePath);
		string root = OutputDirectory(settings);

		HashSet<string> mapNames = new(StringComparer.OrdinalIgnoreCase);
		HashSet<EramElementKind> kindsWithoutCard = [];
		int hidden = 0;

		foreach (EramGeoMap map in geoMaps.Maps)
		{
			string mapName = Unique(MapName(map), mapNames);
			List<Drawn> drawn = [];

			foreach (EramGeoMapObject mapObject in map.Objects)
			{
				string context = $"{sourceName}: {map.Name} / {mapObject.Name}";
				Bases bases = Resolve(mapObject, settings, context, messages, kindsWithoutCard);

				foreach (EramElement element in mapObject.Elements)
				{
					if (element.Kind == EramElementKind.Text)
					{
						if ((element.Overrides.Display ?? mapObject.TextDefaults?.Display) == false)
						{
							hidden++;
							continue;
						}

						if (element.TextLines is not { } lines || lines.All(string.IsNullOrWhiteSpace))
						{
							messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
								$"{context}: a Text element at {element.Start.Y:0.######}, {element.Start.X:0.######} has no text and was skipped."));
							continue;
						}
					}

					EramProperties own = bases.UseOverrides ? element.Overrides : EramProperties.None;
					EramProperties look = EramCrcProperties.Over(bases.For(element.Kind), own);
					look = look with { Filters = look.Filters ?? AlwaysShown };

					// An element's own unusable value is said once per element; an unusable default
					// has already been said once for its object, so it is left out quietly here.
					EramCrcProperties.Overrides(element.Kind, own, value => Dropped(context, element, value, messages));
					AttributesTable attributes = EramCrcProperties.Overrides(element.Kind, look, _ => { });

					drawn.Add(new Drawn(element, mapObject, look, Complete(element.Kind, look), attributes, Feb(element, mapObject, settings)));
				}
			}

			switch (settings.OutputLayout)
			{
				case EramOutputLayout.ByFilters:
					WriteByFilters(drawn, Path.Combine(root, mapName), files);
					break;

				case EramOutputLayout.ByAttributes:
					WriteByAttributes(drawn, Path.Combine(root, mapName), files);
					break;

				default:
					WriteRaw(drawn, root, mapName, files);
					break;
			}
		}

		// With the tab's defaults as the only source, a kind left without them is one choice made
		// once, so it is said once rather than for every object.
		foreach (EramElementKind kind in kindsWithoutCard.Order())
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{sourceName}: no CRC {kind} defaults are set on the tab, so its {Plural(kind)} carry only their filters " +
				"and CRC draws them with its own defaults."));
		}

		if (hidden > 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				$"{sourceName}: {hidden:N0} label(s) ERAM keeps hidden (DisplaySetting false) were left out."));
		}

		string[] written = [.. files.FilesWritten.Skip(before)];
		return (written, written.Sum(path => files.RenderedFeatureCountsByFile[path]));
	}

	// ================= looks =================

	/// <summary>One element to write: where it is, its object, how it looks, and the properties it carries.</summary>
	/// <param name="Element">The element.</param>
	/// <param name="Object">Its object.</param>
	/// <param name="Look">How it looks, as ERAM properties: its defaults with its own values laid over them.</param>
	/// <param name="Complete">The look as complete CRC defaults, or <see langword="null"/> when a value CRC needs is missing or invalid.</param>
	/// <param name="Attributes">The look's CRC properties, less any CRC cannot draw.</param>
	/// <param name="Feb">The chosen <c>feb.*</c> properties.</param>
	private sealed record Drawn(
		EramElement Element,
		EramGeoMapObject Object,
		EramProperties Look,
		object? Complete,
		AttributesTable Attributes,
		AttributesTable Feb)
	{
		public EramElementKind Kind => Element.Kind;
	}

	/// <summary>An object's base look per kind - what its elements' own values are laid over.</summary>
	private sealed record Bases(EramProperties Line, EramProperties Symbol, EramProperties Text, bool UseOverrides)
	{
		public EramProperties For(EramElementKind kind) => kind switch
		{
			EramElementKind.Line => Line,
			EramElementKind.Symbol => Symbol,
			_ => Text,
		};
	}

	private static Bases Resolve(
		EramGeoMapObject mapObject,
		EramToGeojsonSettings settings,
		string context,
		List<ServiceMessage> messages,
		HashSet<EramElementKind> kindsWithoutCard)
	{
		HashSet<EramElementKind> kinds = [.. mapObject.Elements.Select(element => element.Kind)];

		EramProperties Pick(EramElementKind kind, EramProperties? fromXml, object? fromCard)
		{
			if (!kinds.Contains(kind))
			{
				return EramProperties.None;
			}

			if (settings.DefaultsSource == EramDefaultsSource.Card)
			{
				if (fromCard is null)
				{
					kindsWithoutCard.Add(kind);
				}

				return EramCrcProperties.AsEram(fromCard);
			}

			// The card fills whatever the XML leaves out, filters included.
			if (settings.DefaultsSource == EramDefaultsSource.XmlThenCard && fromCard is not null)
			{
				if (Complete(kind, fromXml, out string? gap) is null)
				{
					messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
						$"{context}: {gap}, so the CRC {kind} defaults set on the tab fill in what it leaves out."));
				}

				return EramCrcProperties.Over(EramCrcProperties.AsEram(fromCard), fromXml ?? EramProperties.None);
			}

			// Defaults with no filters show at every filter setting - filter 0 - as ERAM shows them.
			if (Complete(kind, fromXml is null ? null : fromXml with { Filters = fromXml.Filters ?? AlwaysShown }, out string? problem) is null)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"{context}: {problem}, so its {Plural(kind)} have only what each sets itself; CRC draws the rest with its own defaults."));
			}

			return fromXml ?? EramProperties.None;
		}

		return new Bases(
			Pick(EramElementKind.Line, mapObject.LineDefaults, settings.LineDefaults),
			Pick(EramElementKind.Symbol, mapObject.SymbolDefaults, settings.SymbolDefaults),
			Pick(EramElementKind.Text, mapObject.TextDefaults, settings.TextDefaults),
			UseOverrides: settings.DefaultsSource != EramDefaultsSource.Card);
	}

	/// <summary>A look as complete CRC defaults of its kind, or <see langword="null"/> when it cannot be one.</summary>
	private static object? Complete(EramElementKind kind, EramProperties look) => Complete(kind, look, out _);

	/// <summary>A look as complete CRC defaults of its kind, or <see langword="null"/> and why when it cannot be one (or there is none).</summary>
	private static object? Complete(EramElementKind kind, EramProperties? look, out string? problem) => kind switch
	{
		EramElementKind.Line => EramCrcProperties.LineDefaults(look, out problem),
		EramElementKind.Symbol => EramCrcProperties.SymbolDefaults(look, out problem),
		_ => EramCrcProperties.TextDefaults(look, out problem),
	};

	private static AttributesTable Feb(EramElement element, EramGeoMapObject mapObject, EramToGeojsonSettings settings)
	{
		AttributesTable feb = [];

		FebProperties.Add(feb, settings.IncludeFebProperties, settings.FebProperties, property => property switch
		{
			EramFebProperty.MapObjectType => mapObject.ObjectType,
			EramFebProperty.MapGroupId => mapObject.MapGroupId,
			EramFebProperty.LineObjectId => element.Kind == EramElementKind.Line && !element.IsSaa ? element.ObjectId : null,
			EramFebProperty.SymbolId => element.Kind != EramElementKind.Line && !element.IsSaa ? element.ObjectId : null,
			_ => element.IsSaa ? element.ObjectId : null,
		});

		return feb;
	}

	// ================= By Filters =================

	private static void WriteByFilters(List<Drawn> drawn, string mapDirectory, GeojsonFileSet files)
	{
		foreach (IGrouping<string, Drawn> filterSet in drawn.GroupBy(item => FilterKey(item.Look.Filters!)))
		{
			foreach (IGrouping<EramElementKind, Drawn> kind in filterSet.GroupBy(item => item.Kind).OrderBy(group => group.Key))
			{
				Drawn[] items = [.. kind];

				// The look most of the file's Features share is its isDefaults Feature.
				object? defaults = items
					.Where(item => item.Complete is not null)
					.GroupBy(item => Signature(DefaultsAttributes(item.Complete!)))
					.OrderByDescending(group => group.Count())
					.Select(group => group.First().Complete)
					.FirstOrDefault();

				AttributesTable? defaultsAttributes = defaults is null ? null : DefaultsAttributes(defaults);

				FileBuilder file = new(defaults);

				foreach (Drawn item in items)
				{
					file.Add(item, Without(item.Attributes, defaultsAttributes));
				}

				file.WriteTo(files, Path.Combine(mapDirectory, filterSet.Key), $"{filterSet.Key}_{KindSuffix(kind.Key)}.geojson");
			}
		}
	}

	/// <summary><c>Filter_01</c> for one filter, <c>Multi-Filter_02_03_08</c> for several, sorted; two digits at least.</summary>
	private static string FilterKey(IReadOnlyList<int> filters) => filters.Count == 1
		? $"Filter_{TwoDigits(filters[0])}"
		: $"Multi-Filter_{string.Join('_', filters.Order().Select(TwoDigits))}";

	// ================= By Attributes =================

	private static void WriteByAttributes(List<Drawn> drawn, string mapDirectory, GeojsonFileSet files)
	{
		foreach (IGrouping<string, Drawn> group in drawn.GroupBy(AttributesName, StringComparer.OrdinalIgnoreCase))
		{
			Drawn first = group.First();

			// Everything here looks the same: its isDefaults Feature describes it all, so no
			// Feature needs to say more. A look missing a value has no isDefaults Feature, and its
			// Features carry what they have.
			FileBuilder file = new(first.Complete);

			foreach (Drawn item in group)
			{
				file.Add(item, first.Complete is null ? item.Attributes : []);
			}

			file.WriteTo(files, mapDirectory, $"{group.Key}.geojson");
		}
	}

	/// <summary>The original tool's name for a look, without <c>.geojson</c>; a value the look lacks is <c>none</c>.</summary>
	private static string AttributesName(Drawn item)
	{
		EramProperties look = item.Look;
		string start = $"BCG {(look.Bcg is { } bcg ? TwoDigits(bcg) : "none")}" +
			$"_Filters {string.Join(' ', look.Filters!.Order().Select(TwoDigits))}" +
			$"_Type {item.Object.ObjectType}_Group {Value(item.Object.MapGroupId)}";

		string name = item.Kind switch
		{
			EramElementKind.Line =>
				$"{start}_Object {item.Element.ObjectId ?? "none"}_Style {StyleName(look.Style)}_Thick {Value(look.Thickness)}_Lines",
			EramElementKind.Symbol =>
				$"{start}_Style {StyleName(look.Style)}_Font {Value(look.Size)}_Symbols",
			_ =>
				$"{start}_Font {Value(look.Size)}_Underline {(look.Underline is { } underline ? (underline ? "T" : "F") : "none")}" +
				$"_X {Value(look.XOffset)}_Y {Value(look.YOffset)}_Text",
		};

		return FileSafe(name);
	}

	/// <summary>
	/// A style as the original tool named it: as the XML spells it (<c>Solid</c>, <c>VOR</c>). A
	/// style from the tab's CRC defaults (<c>shortDashed</c>) gets a capital first letter, to read
	/// like ERAM's.
	/// </summary>
	private static string StyleName(string? style) =>
		style is { Length: > 0 } name ? char.ToUpperInvariant(name[0]) + name[1..] : "none";

	// ================= Raw =================

	private static void WriteRaw(List<Drawn> drawn, string root, string mapName, GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		foreach (Drawn item in drawn)
		{
			Geometry geometry = item.Kind == EramElementKind.Line
				? Wgs84.Factory.CreateLineString([item.Element.Start, item.Element.End!])
				: Wgs84.Point(item.Element.Start.Y, item.Element.Start.X);

			collection.Add(new Feature(geometry, Properties(item, item.Attributes)));
		}

		files.Write(collection, collection.Count, root, $"{mapName}.geojson");
	}

	// ================= one output file =================

	/// <summary>One file being built: its isDefaults Feature, its lines grouped by what they carry, and its points.</summary>
	private sealed class FileBuilder(object? defaults)
	{
		private readonly List<(string Key, AttributesTable Attributes, List<(Coordinate Start, Coordinate End)> Segments)> _lines = [];
		private readonly List<Feature> _points = [];

		/// <summary>Adds an element carrying <paramref name="own"/> - what it says beyond the file's isDefaults Feature.</summary>
		public void Add(Drawn item, AttributesTable own)
		{
			AttributesTable attributes = Properties(item, own);

			if (item.Kind == EramElementKind.Line)
			{
				string key = Signature(attributes);
				int index = _lines.FindIndex(group => group.Key == key);

				if (index < 0)
				{
					_lines.Add((key, attributes, []));
					index = _lines.Count - 1;
				}

				_lines[index].Segments.Add((item.Element.Start, item.Element.End!));
				return;
			}

			_points.Add(new Feature(Wgs84.Point(item.Element.Start.Y, item.Element.Start.X), attributes));
		}

		public void WriteTo(GeojsonFileSet files, string directory, string fileName)
		{
			FeatureCollection collection = [];

			switch (defaults)
			{
				case CrcLineDefaults line:
					collection.Add(CrcFeatureFactory.CreateDefaultsFeature(line));
					break;
				case CrcSymbolDefaults symbol:
					collection.Add(CrcFeatureFactory.CreateDefaultsFeature(symbol));
					break;
				case CrcTextDefaults text:
					collection.Add(CrcFeatureFactory.CreateDefaultsFeature(text));
					break;
			}

			int rendered = 0;

			foreach ((string _, AttributesTable attributes, List<(Coordinate Start, Coordinate End)> segments) in _lines)
			{
				IReadOnlyList<LineString> lines = SegmentJoiner.Join(segments);

				if (lines.Count > 0)
				{
					collection.Add(new Feature(Wgs84.Factory.CreateMultiLineString([.. lines]), attributes));
					rendered++;
				}
			}

			foreach (Feature point in _points)
			{
				collection.Add(point);
				rendered++;
			}

			files.Write(collection, rendered, directory, fileName);
		}
	}

	// ================= properties =================

	/// <summary>A Feature's properties: its text first (one CRC line per ERAM TextLine), then <paramref name="own"/>, then its <c>feb.*</c>.</summary>
	private static AttributesTable Properties(Drawn item, AttributesTable own)
	{
		AttributesTable attributes = [];

		if (item.Kind == EramElementKind.Text)
		{
			attributes.Add("text", item.Element.TextLines!.ToArray());
		}

		foreach (string name in own.GetNames())
		{
			attributes.Add(name, own[name]);
		}

		foreach (string name in item.Feb.GetNames())
		{
			attributes.Add(name, item.Feb[name]);
		}

		return attributes;
	}

	/// <summary>The properties an isDefaults Feature of <paramref name="defaults"/> carries.</summary>
	private static AttributesTable DefaultsAttributes(object defaults) => (AttributesTable)(defaults switch
	{
		CrcLineDefaults line => CrcFeatureFactory.CreateDefaultsFeature(line),
		CrcSymbolDefaults symbol => CrcFeatureFactory.CreateDefaultsFeature(symbol),
		_ => CrcFeatureFactory.CreateDefaultsFeature((CrcTextDefaults)defaults),
	}).Attributes;

	/// <summary>The properties in <paramref name="attributes"/> that say something <paramref name="defaults"/> does not.</summary>
	private static AttributesTable Without(AttributesTable attributes, AttributesTable? defaults)
	{
		AttributesTable own = [];

		foreach (string name in attributes.GetNames())
		{
			if (defaults is null || !defaults.Exists(name) || !SameValue(attributes[name], defaults[name]))
			{
				own.Add(name, attributes[name]);
			}
		}

		return own;
	}

	private static bool SameValue(object? a, object? b) =>
		a is int[] left && b is int[] right ? left.SequenceEqual(right) : Equals(a, b);

	/// <summary>The same values always give the same key, so Features carrying the same things can be told apart from the rest.</summary>
	private static string Signature(AttributesTable attributes) =>
		string.Join(';', attributes.GetNames().Select(name => attributes[name] is int[] values
			? $"{name}={string.Join(',', values)}"
			: $"{name}={attributes[name]}"));

	// ================= names =================

	private static string KindSuffix(EramElementKind kind) => kind switch
	{
		EramElementKind.Line => "Lines",
		EramElementKind.Symbol => "Symbols",
		_ => "Text",
	};

	private static string Plural(EramElementKind kind) => kind switch
	{
		EramElementKind.Line => "lines",
		EramElementKind.Symbol => "symbols",
		_ => "text",
	};

	private static string TwoDigits(int value) => value.ToString("00", CultureInfo.InvariantCulture);

	private static string Value(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "none";

	/// <summary>A name with the characters Windows forbids in a file name taken out, as the original tool did.</summary>
	private static string FileSafe(string name) => string.Concat(name.Split(Path.GetInvalidFileNameChars()));

	/// <summary>The name itself, or with <c> (2)</c>, <c> (3)</c>… when another map already has it, so no map's files overwrite another's.</summary>
	private static string Unique(string name, HashSet<string> used)
	{
		string unique = name;

		for (int copy = 2; !used.Add(unique); copy++)
		{
			unique = $"{name} ({copy})";
		}

		return unique;
	}

	private static void Dropped(string context, EramElement element, string value, List<ServiceMessage> messages) =>
		messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
			$"{context}: a {element.Kind} element's {value} is not a value CRC can draw and was left out."));
}
