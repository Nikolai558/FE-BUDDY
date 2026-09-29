using System.Xml;
using System.Xml.Linq;

using FeBuddy.Core.Infrastructure.Eram.Models;

namespace FeBuddy.Core.Infrastructure.Eram;

/// <summary>
/// Reads the map menus in an ERAM <c>ConsoleCommandControl.xml</c> (the
/// <c>ConsoleCommandControl_Records</c> file of an ERAM adaptation export): each brightness menu
/// and filter menu, with its buttons' positions, labels and groups.
/// </summary>
/// <remarks>
/// <para>
/// The layout is <c>ConsoleCommandControl_Records</c> holding <c>MapBrightnessMenu</c>s and
/// <c>MapFilterMenu</c>s, then keypad and function keys, which are not read. Values are child
/// elements:
/// </para>
/// <code>
/// &lt;MapBrightnessMenu&gt;
///   &lt;BCGMenuName&gt;ZOB&lt;/BCGMenuName&gt;
///   &lt;MapBCGButton&gt;
///     &lt;MenuPosition&gt;1&lt;/MenuPosition&gt; &lt;Label&gt;AAV&lt;/Label&gt;
///     &lt;MapBCGGroups&gt;&lt;MapBCGGroup&gt;1&lt;/MapBCGGroup&gt;&lt;/MapBCGGroups&gt;
///   &lt;/MapBCGButton&gt;
/// &lt;/MapBrightnessMenu&gt;
/// &lt;MapFilterMenu&gt;
///   &lt;FilterMenuName&gt;ZOB&lt;/FilterMenuName&gt;
///   &lt;MapFilterButton&gt;
///     &lt;MenuPosition&gt;1&lt;/MenuPosition&gt; &lt;LabelLine1&gt;HI&lt;/LabelLine1&gt; &lt;LabelLine2&gt;ALT&lt;/LabelLine2&gt;
///     &lt;MapFilterGroups&gt;&lt;MapFilterGroup&gt;1&lt;/MapFilterGroup&gt;&lt;/MapFilterGroups&gt; ...
/// </code>
/// <para>
/// Values are kept as text, as the file writes them: they are listed, never drawn, so nothing in
/// them can be wrong. The file is small and read whole. A file that is not well-formed XML, or not
/// a <c>ConsoleCommandControl_Records</c> file at all, throws <see cref="InvalidDataException"/>.
/// </para>
/// </remarks>
public static class EramConsoleCommandControlReader
{
	/// <summary>The root element of an ERAM <c>ConsoleCommandControl.xml</c>.</summary>
	public const string RootElement = "ConsoleCommandControl_Records";

	/// <summary>What an adaptation export calls the file.</summary>
	public const string FileName = "ConsoleCommandControl.xml";

	/// <summary>Reads a ConsoleCommandControl file from disk.</summary>
	/// <param name="path">The file to read.</param>
	/// <returns>The file's map menus.</returns>
	/// <exception cref="IOException">Thrown when the file cannot be read.</exception>
	/// <exception cref="InvalidDataException">Thrown when the file is not well-formed XML or not a ConsoleCommandControl file.</exception>
	public static EramConsoleCommandControl Read(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		using FileStream stream = File.OpenRead(path);
		return Parse(stream, path);
	}

	/// <summary>
	/// Whether a file is an ERAM ConsoleCommandControl file, from its first element alone. Used to
	/// find it beside <c>Geomaps.xml</c>, among the rest of an adaptation export.
	/// </summary>
	/// <param name="path">The file to look at.</param>
	/// <returns><see langword="true"/> when it starts with <c>&lt;ConsoleCommandControl_Records&gt;</c>; <see langword="false"/> otherwise, or when it cannot be read.</returns>
	public static bool IsConsoleCommandControlFile(string path) => EramXmlFile.HasRoot(path, RootElement);

	/// <summary>Reads ConsoleCommandControl XML from a stream.</summary>
	/// <param name="stream">The XML.</param>
	/// <param name="sourcePath">What to call the source in an error, usually its path.</param>
	/// <returns>The content's map menus.</returns>
	/// <exception cref="InvalidDataException">Thrown when the content is not well-formed XML or not a ConsoleCommandControl file.</exception>
	public static EramConsoleCommandControl Parse(Stream stream, string sourcePath)
	{
		ArgumentNullException.ThrowIfNull(stream);

		XElement root;

		try
		{
			using XmlReader reader = XmlReader.Create(stream, EramXmlFile.Settings);
			root = XElement.Load(reader);
		}
		catch (XmlException ex)
		{
			throw new InvalidDataException($"{Path.GetFileName(sourcePath)} is not well-formed XML: {ex.Message}", ex);
		}

		if (root.Name.LocalName != RootElement)
		{
			throw new InvalidDataException(
				$"{Path.GetFileName(sourcePath)} is not an ERAM ConsoleCommandControl file: it starts with <{root.Name.LocalName}>, not <{RootElement}>.");
		}

		return new EramConsoleCommandControl(
			[.. root.Elements("MapBrightnessMenu").Select(menu => Menu(menu, "BCGMenuName", "MapBCGButton", BcgButton))],
			[.. root.Elements("MapFilterMenu").Select(menu => Menu(menu, "FilterMenuName", "MapFilterButton", FilterButton))]);
	}

	private static EramMapMenu Menu(XElement menu, string nameElement, string buttonElement, Func<XElement, EramMapMenuButton> button) =>
		new(Value(menu, nameElement) ?? string.Empty, [.. menu.Elements(buttonElement).Select(button)]);

	private static EramMapMenuButton BcgButton(XElement button) => new(
		Value(button, "MenuPosition"),
		Value(button, "Label"),
		null,
		Groups(button, "MapBCGGroups", "MapBCGGroup"));

	private static EramMapMenuButton FilterButton(XElement button) => new(
		Value(button, "MenuPosition"),
		Value(button, "LabelLine1"),
		Value(button, "LabelLine2"),
		Groups(button, "MapFilterGroups", "MapFilterGroup"));

	private static IReadOnlyList<string> Groups(XElement button, string container, string entry) =>
		[.. button.Elements(container).Elements(entry).Select(group => group.Value.Trim()).Where(group => group.Length > 0)];

	/// <summary>A child's trimmed text, or <see langword="null"/> when it is missing or blank.</summary>
	private static string? Value(XElement parent, string name) =>
		parent.Element(name)?.Value.Trim() is { Length: > 0 } value ? value : null;
}
