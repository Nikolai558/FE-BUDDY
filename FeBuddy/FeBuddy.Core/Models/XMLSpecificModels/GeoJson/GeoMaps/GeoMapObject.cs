using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents an object within a geographic map that can be serialized or deserialized using XML.
/// This class is marked as the root element in XML serialization and contains various visual defaults,
/// as well as a collection of elements that describe its detailed composition.
/// </summary>
[XmlRoot(ElementName = "GeoMapObject")]
public class GeoMapObject
{
	/// <summary>
	/// Gets or sets the default line properties for the geographic object.
	/// </summary>
	[XmlElement(ElementName = "LineDefaults", IsNullable = true)]
	public LineDefaults LineDefaults { get; set; }

	/// <summary>
	/// Gets or sets the default symbol properties for the geographic object.
	/// </summary>
	[XmlElement(ElementName = "SymbolDefaults", IsNullable = true)]
	public SymbolDefaults SymbolDefaults { get; set; }

	/// <summary>
	/// Gets or sets the default text properties for the geographic object.
	/// </summary>
	[XmlElement(ElementName = "TextDefaults", IsNullable = true)]
	public TextDefaults TextDefaults { get; set; }

	/// <summary>
	/// Gets or sets the collection of elements that define this geographic object.
	/// </summary>
	[XmlElement(ElementName = "Elements")]
	public Elements Elements { get; set; }

	/// <summary>
	/// Gets or sets the description of the geographic object.
	/// </summary>
	[XmlAttribute(AttributeName = "Description")]
	public string Description { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this object is TDM-only (Traffic Data Management).
	/// </summary>
	[XmlAttribute(AttributeName = "TdmOnly")]
	public bool TdmOnly { get; set; }
}
