using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.GeoMaps;

/// <summary>
/// Represents the default settings for a symbol element that can be serialized or deserialized using XML.
/// This class is marked as the root element in XML serialization and contains attributes that define the visual properties of a symbol.
/// </summary>
[XmlRoot(ElementName = "SymbolDefaults", IsNullable = true)]
public class SymbolDefaults
{
	/// <summary>
	/// Gets or sets the background color identifier for the symbol.
	/// </summary>
	[XmlAttribute(AttributeName = "Bcg")]
	public int Bcg { get; set; }

	/// <summary>
	/// Gets or sets the filter settings for the symbol.
	/// </summary>
	[XmlAttribute(AttributeName = "Filters")]
	public string Filters { get; set; }

	/// <summary>
	/// Gets or sets the style of the symbol, which could define its visual appearance.
	/// </summary>
	[XmlAttribute(AttributeName = "Style")]
	public string Style { get; set; }

	/// <summary>
	/// Gets or sets the size of the symbol.
	/// </summary>
	[XmlAttribute(AttributeName = "Size")]
	public int Size { get; set; }

	/// <summary>
	/// Returns a string representation of the symbol defaults, including the background color, style, and size.
	/// </summary>
	/// <returns>A formatted string describing the symbol default settings.</returns>
	public override string ToString()
	{
		return $"Symbol Defaults: BCG {Bcg}__Style {Style}__Size {Size}";
	}
}
