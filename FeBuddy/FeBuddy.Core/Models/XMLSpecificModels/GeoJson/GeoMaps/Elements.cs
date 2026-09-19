using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents a collection of <see cref="Element"/> objects that can be serialized or deserialized using XML.
/// This class is marked as the root element in XML serialization and contains a list of elements.
/// </summary>
[XmlRoot(ElementName = "Elements")]
public class Elements
{
	/// <summary>
	/// Gets or sets the list of <see cref="Element"/> objects that belong to this collection.
	/// </summary>
	[XmlElement(ElementName = "Element")]
	public List<Element> Element { get; set; }
}