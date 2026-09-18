using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents an individual background menu item that can be serialized or deserialized using XML.
/// Contains a label attribute to describe the item.
/// </summary>
[XmlRoot(ElementName = "BcgMenuItem")]
public class BcgMenuItem
{
	/// <summary>
	/// Gets or sets the label for the background menu item.
	/// </summary>
	[XmlAttribute(AttributeName = "Label")]
	public string Label { get; set; }
}

/// <summary>
/// Represents a collection of items, which include both background menu items and filter menu items.
/// </summary>
[XmlRoot(ElementName = "Items")]
public class Items
{
	/// <summary>
	/// Gets or sets the list of background menu items.
	/// </summary>
	[XmlElement(ElementName = "BcgMenuItem")]
	public List<BcgMenuItem> BcgMenuItem { get; set; }

	/// <summary>
	/// Gets or sets the list of filter menu items.
	/// </summary>
	[XmlElement(ElementName = "FilterMenuItem")]
	public List<FilterMenuItem> FilterMenuItem { get; set; }
}

/// <summary>
/// Represents a background menu that can be serialized or deserialized using XML.
/// Contains a collection of items and a name attribute.
/// </summary>
[XmlRoot(ElementName = "BcgMenu")]
public class BcgMenu
{
	/// <summary>
	/// Gets or sets the collection of items within the background menu.
	/// </summary>
	[XmlElement(ElementName = "Items")]
	public Items Items { get; set; }

	/// <summary>
	/// Gets or sets the name of the background menu.
	/// </summary>
	[XmlAttribute(AttributeName = "Name")]
	public string Name { get; set; }
}

/// <summary>
/// Represents a collection of background menus that can be serialized or deserialized using XML.
/// </summary>
[XmlRoot(ElementName = "BcgMenus")]
public class BcgMenus
{
	/// <summary>
	/// Gets or sets the list of background menus.
	/// </summary>
	[XmlElement(ElementName = "BcgMenu")]
	public List<BcgMenu> BcgMenu { get; set; }
}

/// <summary>
/// Represents an individual filter menu item that can be serialized or deserialized using XML.
/// Contains two label attributes to describe the item.
/// </summary>
[XmlRoot(ElementName = "FilterMenuItem")]
public class FilterMenuItem
{
	/// <summary>
	/// Gets or sets the first label line for the filter menu item.
	/// </summary>
	[XmlAttribute(AttributeName = "LabelLine1")]
	public string LabelLine1 { get; set; }

	/// <summary>
	/// Gets or sets the second label line for the filter menu item.
	/// </summary>
	[XmlAttribute(AttributeName = "LabelLine2")]
	public string LabelLine2 { get; set; }
}

/// <summary>
/// Represents a filter menu that can be serialized or deserialized using XML.
/// Contains a collection of items and a name attribute.
/// </summary>
[XmlRoot(ElementName = "FilterMenu")]
public class FilterMenu
{
	/// <summary>
	/// Gets or sets the collection of items within the filter menu.
	/// </summary>
	[XmlElement(ElementName = "Items")]
	public Items Items { get; set; }

	/// <summary>
	/// Gets or sets the name of the filter menu.
	/// </summary>
	[XmlAttribute(AttributeName = "Name")]
	public string Name { get; set; }
}

/// <summary>
/// Represents a collection of filter menus that can be serialized or deserialized using XML.
/// </summary>
[XmlRoot(ElementName = "FilterMenus")]
public class FilterMenus
{
	/// <summary>
	/// Gets or sets the list of filter menus.
	/// </summary>
	[XmlElement(ElementName = "FilterMenu")]
	public List<FilterMenu> FilterMenu { get; set; }
}
