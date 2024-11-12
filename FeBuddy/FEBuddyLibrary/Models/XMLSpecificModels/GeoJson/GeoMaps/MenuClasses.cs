using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;


[XmlRoot(ElementName = "BcgMenuItem")]
public class BcgMenuItem
{
	[XmlAttribute(AttributeName = "Label")]
	public string Label { get; set; }
}

[XmlRoot(ElementName = "Items")]
public class Items
{
	[XmlElement(ElementName = "BcgMenuItem")]
	public List<BcgMenuItem> BcgMenuItem { get; set; }

	[XmlElement(ElementName = "FilterMenuItem")]
	public List<FilterMenuItem> FilterMenuItem { get; set; }
}

[XmlRoot(ElementName = "BcgMenu")]
public class BcgMenu
{
	[XmlElement(ElementName = "Items")]
	public Items Items { get; set; }

	[XmlAttribute(AttributeName = "Name")]
	public string Name { get; set; }
}

[XmlRoot(ElementName = "BcgMenus")]
public class BcgMenus
{
	[XmlElement(ElementName = "BcgMenu")]
	public List<BcgMenu> BcgMenu { get; set; }
}

[XmlRoot(ElementName = "FilterMenuItem")]
public class FilterMenuItem
{
	[XmlAttribute(AttributeName = "LabelLine1")]
	public string LabelLine1 { get; set; }

	[XmlAttribute(AttributeName = "LabelLine2")]
	public string LabelLine2 { get; set; }
}

[XmlRoot(ElementName = "FilterMenu")]
public class FilterMenu
{
	[XmlElement(ElementName = "Items")]
	public Items Items { get; set; }

	[XmlAttribute(AttributeName = "Name")]
	public string Name { get; set; }
}

[XmlRoot(ElementName = "FilterMenus")]
public class FilterMenus
{
	[XmlElement(ElementName = "FilterMenu")]
	public List<FilterMenu> FilterMenu { get; set; }
}
