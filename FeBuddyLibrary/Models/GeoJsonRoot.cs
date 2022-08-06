using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddyLibrary.Models
{
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

    [XmlRoot(ElementName = "LineDefaults")]
    public class LineDefaults
    {
        [XmlAttribute(AttributeName = "Bcg")]
        public int Bcg { get; set; }

        [XmlAttribute(AttributeName = "Filters")]
        public int Filters { get; set; }

        [XmlAttribute(AttributeName = "Style")]
        public string Style { get; set; }

        [XmlAttribute(AttributeName = "Thickness")]
        public int Thickness { get; set; }
    }

    [XmlRoot(ElementName = "Element")]
    public class Element
    {
        [XmlAttribute(AttributeName = "xsi-type")]
        public string XsiType { get; set; }

        [XmlElement(ElementName = "Filters")]
        public object Filters { get; set; }

        [XmlAttribute(AttributeName = "StartLat")]
        public double StartLat { get; set; }

        [XmlAttribute(AttributeName = "StartLon")]
        public double StartLon { get; set; }

        [XmlAttribute(AttributeName = "EndLat")]
        public double EndLat { get; set; }

        [XmlAttribute(AttributeName = "EndLon")]
        public double EndLon { get; set; }
    }

    [XmlRoot(ElementName = "Elements")]
    public class Elements
    {
        [XmlElement(ElementName = "Element")]
        public List<Element> Element { get; set; }
    }

    [XmlRoot(ElementName = "GeoMapObject")]
    public class GeoMapObject
    {
        [XmlElement(ElementName = "LineDefaults")]
        public LineDefaults LineDefaults { get; set; }

        [XmlElement(ElementName = "Elements")]
        public Elements Elements { get; set; }

        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }

        [XmlAttribute(AttributeName = "TdmOnly")]
        public bool TdmOnly { get; set; }
    }

    [XmlRoot(ElementName = "Objects")]
    public class Objects
    {
        [XmlElement(ElementName = "GeoMapObject")]
        public List<GeoMapObject> GeoMapObject { get; set; }
    }

    [XmlRoot(ElementName = "GeoMap")]
    public class GeoMap
    {
        [XmlElement(ElementName = "Objects")]
        public Objects Objects { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "LabelLine1")]
        public string LabelLine1 { get; set; }

        [XmlAttribute(AttributeName = "LabelLine2")]
        public string LabelLine2 { get; set; }

        [XmlAttribute(AttributeName = "BcgMenuName")]
        public string BcgMenuName { get; set; }

        [XmlAttribute(AttributeName = "FilterMenuName")]
        public string FilterMenuName { get; set; }
    }

    [XmlRoot(ElementName = "GeoMaps")]
    public class GeoMaps
    {
        [XmlElement(ElementName = "GeoMap")]
        public List<GeoMap> GeoMap { get; set; }
    }

    [XmlRoot(ElementName = "GeoMapSet")]
    public class GeoMapSet
    {
        [XmlElement(ElementName = "BcgMenus")]
        public BcgMenus BcgMenus { get; set; }

        [XmlElement(ElementName = "FilterMenus")]
        public FilterMenus FilterMenus { get; set; }

        [XmlElement(ElementName = "GeoMaps")]
        public GeoMaps GeoMaps { get; set; }

        [XmlAttribute(AttributeName = "xsi")]
        public string Xsi { get; set; }

        [XmlAttribute(AttributeName = "xsd")]
        public string Xsd { get; set; }

        [XmlAttribute(AttributeName = "DefaultMap")]
        public string DefaultMap { get; set; }
    }
}
