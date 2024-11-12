using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents an element that can be serialized or deserialized using XML. 
/// This class provides properties to define various geographic and visual attributes of the element, 
/// and implements IXmlSerializable to facilitate custom XML serialization/deserialization.
/// </summary>
public class Element : IXmlSerializable
{
	/// <summary>
	/// Gets the xsi:type attribute, which represents the XML schema instance type for the element.
	/// </summary>
	public string XsiType { get; private set; }

	/// <summary>
	/// Gets the filters associated with the element, used for additional configuration or metadata.
	/// </summary>
	public string Filters { get; private set; } = null;

	/// <summary>
	/// Gets the starting latitude of the element.
	/// </summary>
	public double StartLat { get; private set; }

	/// <summary>
	/// Gets the starting longitude of the element.
	/// </summary>
	public double StartLon { get; private set; }

	/// <summary>
	/// Gets the ending latitude of the element.
	/// </summary>
	public double EndLat { get; private set; }

	/// <summary>
	/// Gets the ending longitude of the element.
	/// </summary>
	public double EndLon { get; private set; }

	/// <summary>
	/// Gets the latitude for the central point of the element.
	/// </summary>
	public double Lat { get; private set; }

	/// <summary>
	/// Gets the longitude for the central point of the element.
	/// </summary>
	public double Lon { get; private set; }

	/// <summary>
	/// Gets the lines associated with the element, possibly representing visual or other linked elements.
	/// </summary>
	public string Lines { get; private set; }

	/// <summary>
	/// Gets the background color identifier for the element.
	/// </summary>
	public int? Bcg { get; private set; } = null;

	/// <summary>
	/// Gets the size attribute for the element, typically representing a visual size.
	/// </summary>
	public int? Size { get; private set; } = null;

	/// <summary>
	/// Gets a value indicating whether the element should be underlined.
	/// </summary>
	public bool? Underline { get; private set; } = null;

	/// <summary>
	/// Gets a value indicating whether the element should be opaque.
	/// </summary>
	public bool? Opaque { get; private set; } = null;

	/// <summary>
	/// Gets the X-axis offset for the element.
	/// </summary>
	public int? XOffset { get; private set; } = null;

	/// <summary>
	/// Gets the Y-axis offset for the element.
	/// </summary>
	public int? YOffset { get; private set; } = null;

	/// <summary>
	/// Gets the style associated with the element, which might represent visual or descriptive style information.
	/// </summary>
	public string Style { get; private set; } = null;

	/// <summary>
	/// Gets the thickness attribute of the element, used for line elements or similar.
	/// </summary>
	public int? Thickness { get; private set; } = null;

	/// <summary>
	/// Gets the z-index of the element, determining the drawing order in a graphical context.
	/// </summary>
	public int? ZIndex { get; private set; } = null;

	/// <summary>
	/// Reads the XML representation of the element from the given XmlReader.
	/// </summary>
	/// <param name="reader">The XmlReader from which to read the element data.</param>
	public void ReadXml(XmlReader reader)
	{
		// Read the attributes from the XML and assign them to the properties.
		string attr1 = reader.GetAttribute("xsi-type");
		string attr2 = reader.GetAttribute("Filters");
		string attr3 = reader.GetAttribute("StartLat");
		string attr4 = reader.GetAttribute("StartLon");
		string attr5 = reader.GetAttribute("EndLat");
		string attr6 = reader.GetAttribute("EndLon");
		string attr7 = reader.GetAttribute("Lat");
		string attr8 = reader.GetAttribute("Lon");
		string attr9 = reader.GetAttribute("Lines");
		string attr10 = reader.GetAttribute("Bcg");
		string attr11 = reader.GetAttribute("Size");
		string attr12 = reader.GetAttribute("Underline");
		string attr13 = reader.GetAttribute("Opaque");
		string attr14 = reader.GetAttribute("XOffset");
		string attr15 = reader.GetAttribute("YOffset");
		string attr16 = reader.GetAttribute("Style");
		string attr17 = reader.GetAttribute("Thickness");
		string attr18 = reader.GetAttribute("zIndex");
		reader.Read();

		// Assign parsed values to properties
		XsiType = attr1;
		Filters = attr2;
		if (attr3 != null) { StartLat = double.Parse(attr3); }
		if (attr4 != null) { StartLon = double.Parse(attr4); }
		if (attr5 != null) { EndLat = double.Parse(attr5); }
		if (attr6 != null) { EndLon = double.Parse(attr6); }
		if (attr7 != null) { Lat = double.Parse(attr7); }
		if (attr8 != null) { Lon = double.Parse(attr8); }
		Lines = attr9;
		Bcg = ConvertToNullable<int>(attr10);
		Size = ConvertToNullable<int>(attr11);
		Underline = ConvertToNullable<bool>(attr12);
		Opaque = ConvertToNullable<bool>(attr13);
		XOffset = ConvertToNullable<int>(attr14);
		YOffset = ConvertToNullable<int>(attr15);
		Style = attr16;
		Thickness = ConvertToNullable<int>(attr17);
		ZIndex = ConvertToNullable<int>(attr18);
	}

	/// <summary>
	/// Converts the input string value to a nullable type, handling empty strings as null.
	/// </summary>
	/// <typeparam name="T">The value type to be converted to.</typeparam>
	/// <param name="inputValue">The string value to be converted.</param>
	/// <returns>A nullable type of the specified type, or null if conversion is not possible.</returns>
	private static T? ConvertToNullable<T>(string inputValue) where T : struct
	{
		if (string.IsNullOrEmpty(inputValue) || inputValue.Trim().Length == 0)
		{
			// If the input value is null or empty, return null.
			return null;
		}
		try
		{
			// Attempt to convert the input value to the specified type using TypeConverter.
			TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
			return (T)conv.ConvertFrom(inputValue);
		}
		catch (NotSupportedException)
		{
			// If the conversion fails, return null.
			return null;
		}
	}

	/// <summary>
	/// Returns the XML schema (not implemented).
	/// </summary>
	/// <returns>Always returns null as schema is not provided.</returns>
	public XmlSchema GetSchema() { return null; }

	/// <summary>
	/// Writes the XML representation of the element (not implemented).
	/// </summary>
	/// <param name="writer">The XmlWriter to which the element data should be written.</param>
	public void WriteXml(XmlWriter writer) { throw new NotImplementedException(); }
}