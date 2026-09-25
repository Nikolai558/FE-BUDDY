using System.Globalization;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.VeramToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Veram.Models;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Conversions.VeramToGeojson;

/// <summary>
/// Writes one converted vERAM GeoMaps file into <c>…\vERAM to GeoJSON\&lt;source name&gt;\&lt;GeoMap&gt;\</c>,
/// in either layout (<see cref="VeramOutputLayout"/>).
/// </summary>
/// <remarks>
/// <para>
/// <b>GeoMapObject Description</b> writes a file per object, named after its description, holding
/// a defaults Feature for each kind it draws and its elements with their own overrides. Objects
/// sharing a description share a file when their defaults agree; otherwise the later one gets a
/// numbered file of its own.
/// </para>
/// <para>
/// <b>Filter Index and Similar Attributes</b> works out how each element is actually drawn - its
/// defaults with its overrides laid over them - and files it by filter, TDM setting, kind and
/// look: <c>FILTER 05\FILTER 05__TDM F__Line__BCG 3__Style solid__Thickness 1.geojson</c>, with
/// several filters under <c>MULTI FILTERS\</c>. Everything in such a file draws the same way, so
/// its defaults Feature describes every Feature and none carries overrides. An element that cannot
/// be fully described goes under <c>MISSING DEFAULTS\</c> with whatever it sets itself.
/// </para>
/// <para>
/// Where each file's defaults come from is <see cref="VeramDefaultsSource"/>. Line elements are
/// joined back into lines (<see cref="SegmentJoiner"/>); symbols and text are a Point each.
/// </para>
/// </remarks>
public static class VeramGeojsonWriter
{
	/// <summary>The conversion's folder name inside the output directory.</summary>
	public const string RootFolder = "vERAM to GeoJSON";

	private const string LogSource = "VeramGeojsonWriter";

	/// <summary>The folder every converted GeoMaps file's own folder goes in.</summary>
	/// <param name="settings">The parsed settings.</param>
	/// <returns>The directory.</returns>
	public static string OutputDirectory(ConversionSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		return ServiceOutputPaths.Resolve(settings.OutputDirectory, settings.AddFeBuddyOutputFolder, RootFolder);
	}

	/// <summary>Writes every file one GeoMaps file converts to.</summary>
	/// <param name="geoMaps">The GeoMaps file's content.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="files">The run's file set, which does the writing and remembers what was written.</param>
	/// <param name="messages">Where notices about missing defaults and unusable values go.</param>
	/// <returns>The files written for this GeoMaps file, and how many rendered Features they hold.</returns>
	public static (IReadOnlyList<string> Paths, int FeatureCount) Write(
		VeramGeoMapFile geoMaps,
		VeramToGeojsonSettings settings,
		GeojsonFileSet files,
		List<ServiceMessage> messages)
	{
		ArgumentNullException.ThrowIfNull(geoMaps);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(files);
		ArgumentNullException.ThrowIfNull(messages);

		int before = files.FilesWritten.Count;
		string sourceName = Path.GetFileName(geoMaps.SourcePath);
		string sourceDirectory = Path.Combine(
			OutputDirectory(settings), ConversionFiles.SafeFileName(Path.GetFileNameWithoutExtension(geoMaps.SourcePath)));

		HashSet<string> mapFolders = new(StringComparer.OrdinalIgnoreCase);
		HashSet<VeramElementKind> kindsWithoutCard = [];

		foreach (VeramGeoMap map in geoMaps.Maps)
		{
			MapWriter writer = new(Path.Combine(sourceDirectory, ConversionFiles.UniqueFileName(map.Name, mapFolders)), messages);

			foreach (VeramGeoMapObject mapObject in map.Objects)
			{
				string context = $"{sourceName}: {map.Name} / {mapObject.Description}";
				ResolvedDefaults defaults = Resolve(mapObject, settings, context, messages, kindsWithoutCard);

				if (settings.OutputLayout == VeramOutputLayout.ByObject)
				{
					writer.AddByObject(mapObject, defaults, context);
				}
				else
				{
					writer.AddByFilter(mapObject, defaults, context);
				}
			}

			writer.WriteTo(files);
		}

		// With the tab's defaults as the only source, a kind left without them is one choice made
		// once, so it is said once rather than for every object.
		foreach (VeramElementKind kind in kindsWithoutCard.Order())
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{sourceName}: no CRC {kind} defaults are set on the tab, so its {Plural(kind)} have no defaults; " +
				"CRC draws only those that carry their own filters."));
		}

		string[] written = [.. files.FilesWritten.Skip(before)];
		return (written, written.Sum(path => files.RenderedFeatureCountsByFile[path]));
	}

	// ================= defaults =================

	/// <summary>An object's CRC defaults per kind, after <see cref="VeramDefaultsSource"/> has had its say.</summary>
	private sealed record ResolvedDefaults(CrcLineDefaults? Line, CrcSymbolDefaults? Symbol, CrcTextDefaults? Text, bool UseOverrides)
	{
		public object? For(VeramElementKind kind) => kind switch
		{
			VeramElementKind.Line => Line,
			VeramElementKind.Symbol => Symbol,
			_ => Text,
		};
	}

	private static ResolvedDefaults Resolve(
		VeramGeoMapObject mapObject,
		VeramToGeojsonSettings settings,
		string context,
		List<ServiceMessage> messages,
		HashSet<VeramElementKind> kindsWithoutCard)
	{
		HashSet<VeramElementKind> kinds = [.. mapObject.Elements.Select(element => element.Kind)];

		T? Pick<T>(VeramElementKind kind, T? fromXml, string? problem, T? fromCard) where T : class
		{
			if (!kinds.Contains(kind))
			{
				return null;
			}

			if (settings.DefaultsSource == VeramDefaultsSource.Card)
			{
				if (fromCard is null)
				{
					kindsWithoutCard.Add(kind);
				}

				return fromCard;
			}

			if (fromXml is not null)
			{
				return fromXml;
			}

			if (settings.DefaultsSource == VeramDefaultsSource.XmlThenCard && fromCard is not null)
			{
				messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
					$"{context}: {problem}, so its {Plural(kind)} use the CRC {kind} defaults set on the tab."));
				return fromCard;
			}

			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{context}: {problem}, so its {Plural(kind)} have no defaults; CRC draws only those that carry their own filters."));
			return null;
		}

		return new ResolvedDefaults(
			Pick(VeramElementKind.Line, VeramCrcProperties.LineDefaults(mapObject.LineDefaults, out string? lineProblem), lineProblem, settings.LineDefaults),
			Pick(VeramElementKind.Symbol, VeramCrcProperties.SymbolDefaults(mapObject.SymbolDefaults, out string? symbolProblem), symbolProblem, settings.SymbolDefaults),
			Pick(VeramElementKind.Text, VeramCrcProperties.TextDefaults(mapObject.TextDefaults, out string? textProblem), textProblem, settings.TextDefaults),
			UseOverrides: settings.DefaultsSource != VeramDefaultsSource.Card);
	}

	// ================= naming =================

	/// <summary>How a set of defaults reads in a file name, e.g. <c>BCG 3__Style solid__Thickness 1</c>.</summary>
	private static string Describe(object defaults) => defaults switch
	{
		CrcLineDefaults line => $"BCG {line.Bcg}__Style {line.Style}__Thickness {line.Thickness}",
		CrcSymbolDefaults symbol => $"BCG {symbol.Bcg}__Style {symbol.Style}__Size {symbol.Size}",
		CrcTextDefaults text => $"BCG {text.Bcg}__Size {text.Size}__Underline {Flag(text.Underline)}__Opaque {Flag(text.Opaque)}" +
			$"__XOffset {text.XOffset}__YOffset {text.YOffset}",
		_ => "none",
	};

	private static IReadOnlyList<int> FiltersOf(object defaults) => defaults switch
	{
		CrcLineDefaults line => line.Filters,
		CrcSymbolDefaults symbol => symbol.Filters,
		_ => ((CrcTextDefaults)defaults).Filters,
	};

	private static string Flag(bool value) => value ? "T" : "F";

	private static string Plural(VeramElementKind kind) => kind switch
	{
		VeramElementKind.Line => "lines",
		VeramElementKind.Symbol => "symbols",
		_ => "text",
	};

	private static string FilterLabel(IEnumerable<int> filters) =>
		"FILTER " + string.Join(' ', filters.Select(filter => filter.ToString("00", CultureInfo.InvariantCulture)));

	/// <summary>The same values always give the same key, so equal overrides share a line Feature.</summary>
	private static string Signature(AttributesTable attributes) =>
		string.Join(';', attributes.GetNames().Select(name => attributes[name] is int[] values
			? $"{name}={string.Join(',', values)}"
			: $"{name}={attributes[name]}"));

	// ================= one GeoMap =================

	/// <summary>Collects one GeoMap's files as its objects are added, then writes them.</summary>
	private sealed class MapWriter(string directory, List<ServiceMessage> messages)
	{
		private readonly List<FileContent> _files = [];
		private readonly Dictionary<string, List<FileContent>> _byDescription = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, FileContent> _byPath = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> _objectFileNames = new(StringComparer.OrdinalIgnoreCase);

		public void AddByObject(VeramGeoMapObject mapObject, ResolvedDefaults defaults, string context)
		{
			if (mapObject.Elements.Count == 0)
			{
				return;
			}

			string description = ConversionFiles.SafeFileName(mapObject.Description);

			if (!_byDescription.TryGetValue(description, out List<FileContent>? sameDescription))
			{
				sameDescription = _byDescription[description] = [];
			}

			// Objects sharing a description share a file when their defaults agree; otherwise the
			// later one gets a numbered file of its own.
			FileContent? file = sameDescription.FirstOrDefault(candidate => candidate.Accepts(mapObject, defaults));

			if (file is null)
			{
				file = new FileContent(directory, ConversionFiles.UniqueFileName(mapObject.Description, _objectFileNames));
				sameDescription.Add(file);
				_files.Add(file);
			}

			file.Absorb(mapObject, defaults);

			foreach (VeramElement element in mapObject.Elements)
			{
				AttributesTable overrides = defaults.UseOverrides
					? VeramCrcProperties.Overrides(element.Kind, element.Overrides, value => Dropped(context, element, value))
					: [];

				file.Add(element, overrides, context, messages);
			}
		}

		public void AddByFilter(VeramGeoMapObject mapObject, ResolvedDefaults defaults, string context)
		{
			string tdm = $"TDM {Flag(mapObject.TdmOnly)}";

			foreach (VeramElement element in mapObject.Elements)
			{
				// How this element is actually drawn: its object's defaults with its own values on top.
				VeramProperties drawn = VeramCrcProperties.Over(
					VeramCrcProperties.AsVeram(defaults.For(element.Kind)),
					defaults.UseOverrides ? element.Overrides : VeramProperties.None);

				object? complete = element.Kind switch
				{
					VeramElementKind.Line => VeramCrcProperties.LineDefaults(drawn, out _),
					VeramElementKind.Symbol => VeramCrcProperties.SymbolDefaults(drawn, out _),
					_ => VeramCrcProperties.TextDefaults(drawn, out _),
				};

				FileContent file;
				AttributesTable overrides;

				if (complete is not null)
				{
					IReadOnlyList<int> filters = FiltersOf(complete);
					string label = FilterLabel(filters);
					string folder = filters.Count > 1 ? Path.Combine(directory, "MULTI FILTERS", label) : Path.Combine(directory, label);

					file = FileAt(folder, $"{label}__{tdm}__{element.Kind}__{Describe(complete)}");
					file.SetDefaults(element.Kind, complete);
					overrides = [];
				}
				else
				{
					// Not enough to describe the look fully: kept apart, with whatever it sets itself.
					file = FileAt(Path.Combine(directory, "MISSING DEFAULTS"), $"{tdm}__{element.Kind}");
					overrides = defaults.UseOverrides
						? VeramCrcProperties.Overrides(element.Kind, element.Overrides, value => Dropped(context, element, value))
						: [];
				}

				file.Add(element, overrides, context, messages);
			}
		}

		public void WriteTo(GeojsonFileSet files)
		{
			foreach (FileContent file in _files)
			{
				file.WriteTo(files);
			}
		}

		private FileContent FileAt(string folder, string name)
		{
			string path = Path.Combine(folder, ConversionFiles.SafeFileName(name));

			if (!_byPath.TryGetValue(path, out FileContent? file))
			{
				file = _byPath[path] = new FileContent(folder, ConversionFiles.SafeFileName(name));
				_files.Add(file);
			}

			return file;
		}

		private void Dropped(string context, VeramElement element, string value) =>
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{context}: a {element.Kind} element's {value} is not a value CRC can draw and was left out, so it takes its file's default."));
	}

	// ================= one output file =================

	/// <summary>One output file being built: its defaults per kind, its lines grouped by look, and its points.</summary>
	private sealed class FileContent(string directory, string name)
	{
		private readonly Dictionary<VeramElementKind, object?> _defaults = [];
		private readonly List<(string Key, AttributesTable Attributes, List<(Coordinate Start, Coordinate End)> Segments)> _lines = [];
		private readonly List<Feature> _points = [];

		/// <summary>Whether an object's defaults agree with this file's for every kind both draw.</summary>
		public bool Accepts(VeramGeoMapObject mapObject, ResolvedDefaults defaults) =>
			mapObject.Elements.Select(element => element.Kind).Distinct().All(kind =>
				!_defaults.TryGetValue(kind, out object? existing)
				|| Describe(existing ?? "none") == Describe(defaults.For(kind) ?? "none"));

		/// <summary>Takes an object's defaults for every kind it draws that the file does not have yet.</summary>
		public void Absorb(VeramGeoMapObject mapObject, ResolvedDefaults defaults)
		{
			foreach (VeramElementKind kind in mapObject.Elements.Select(element => element.Kind).Distinct())
			{
				_defaults.TryAdd(kind, defaults.For(kind));
			}
		}

		public void SetDefaults(VeramElementKind kind, object defaults) => _defaults.TryAdd(kind, defaults);

		public void Add(VeramElement element, AttributesTable overrides, string context, List<ServiceMessage> messages)
		{
			switch (element.Kind)
			{
				case VeramElementKind.Line:
					string key = Signature(overrides);
					int index = _lines.FindIndex(group => group.Key == key);

					if (index < 0)
					{
						_lines.Add((key, overrides, []));
						index = _lines.Count - 1;
					}

					_lines[index].Segments.Add((element.Start, element.End!));
					break;

				case VeramElementKind.Symbol:
					_points.Add(new Feature(Wgs84.Point(element.Start.Y, element.Start.X), overrides));
					break;

				default:
					if (string.IsNullOrWhiteSpace(element.Text))
					{
						messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
							$"{context}: a Text element at {element.Start.Y:0.######}, {element.Start.X:0.######} has no text and was skipped."));
						return;
					}

					AttributesTable attributes = new() { { "text", new[] { element.Text } } };

					foreach (string attribute in overrides.GetNames())
					{
						attributes.Add(attribute, overrides[attribute]);
					}

					_points.Add(new Feature(Wgs84.Point(element.Start.Y, element.Start.X), attributes));
					break;
			}
		}

		public void WriteTo(GeojsonFileSet files)
		{
			FeatureCollection collection = [];

			foreach (object? defaults in _defaults.OrderBy(pair => pair.Key).Select(pair => pair.Value))
			{
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

			files.Write(collection, rendered, directory, $"{name}.geojson");
		}
	}
}
