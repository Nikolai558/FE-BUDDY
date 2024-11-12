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
public class Element: IXmlSerializable
{
	public string XsiType { get; private set; }
	public string Filters { get; private set; } = null;
	public double StartLat { get; private set; }
	public double StartLon { get; private set; }
	public double EndLat { get; private set; }
	public double EndLon { get; private set; }
	public double Lat { get; private set; }
	public double Lon { get; private set; }
	public string Lines { get; private set; }
	public int? Bcg { get; private set; } = null;
	public int? Size { get; private set; } = null;
	public bool? Underline { get; private set; } = null;
	public bool? Opaque { get; private set; } = null;
	public int? XOffset { get; private set; } = null;
	public int? YOffset { get; private set; } = null;
	public string Style { get; private set; } = null;
	public int? Thickness { get; private set; } = null;
	public int? ZIndex { get; private set; } = null;


	public void ReadXml(XmlReader reader)
	{
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


	// Here be dragons....
	private static T? ConvertToNullable<T>(string inputValue) where T : struct
	{
		if (string.IsNullOrEmpty(inputValue) || inputValue.Trim().Length == 0)
		{
			// Magic Here 
			return null;
		}
		try
		{
			// Magic There.....
			TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
			return (T)conv.ConvertFrom(inputValue);
		}
		catch (NotSupportedException)
		{
			// MAGIC EVERYWHERE! 
			return null;
		}
	}
	public XmlSchema GetSchema() { return null; }
	public void WriteXml(XmlWriter writer) { throw new NotImplementedException(); }
}
