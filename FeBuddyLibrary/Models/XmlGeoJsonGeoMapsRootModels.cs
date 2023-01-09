using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
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

    [XmlRoot(ElementName = "LineDefaults", IsNullable = true)]
    public class LineDefaults
    {
        [XmlAttribute(AttributeName = "Bcg")]
        public int Bcg { get; set; }

        [XmlAttribute(AttributeName = "Filters")]
        public string Filters { get; set; }

        [XmlAttribute(AttributeName = "Style")]
        public string Style { get; set; }

        [XmlAttribute(AttributeName = "Thickness")]
        public int Thickness { get; set; }

        public override string ToString()
        {
            return $"Line Defaults: BCG {Bcg}__Filters {Filters}__Style {Style}__Thickness {Thickness}";
        }
    }

    [XmlRoot(ElementName = "SymbolDefaults", IsNullable = true)]
    public class SymbolDefaults
    {
        [XmlAttribute(AttributeName = "Bcg")]
        public int Bcg { get; set; }

        [XmlAttribute(AttributeName = "Filters")]
        public string Filters { get; set; }

        [XmlAttribute(AttributeName = "Style")]
        public string Style { get; set; }

        [XmlAttribute(AttributeName = "Size")]
        public int Size { get; set; }

        public override string ToString()
        {
            return $"Symbol Defaults: BCG {Bcg}__Filters {Filters}__Style {Style}__Size {Size}";
        }

    }

    [XmlRoot(ElementName = "TextDefaults", IsNullable = true)]
    public class TextDefaults
    {
        [XmlAttribute(AttributeName = "Bcg")]
        public int Bcg { get; set; }

        [XmlAttribute(AttributeName = "Filters")]
        public string Filters { get; set; }

        [XmlAttribute(AttributeName = "Size")]
        public int Size { get; set; }

        [XmlAttribute(AttributeName = "Underline")]
        public bool Underline { get; set; }

        [XmlAttribute(AttributeName = "Opaque")]
        public bool Opaque { get; set; }

        [XmlAttribute(AttributeName = "XOffset")]
        public int XOffset { get; set; }
        [XmlAttribute(AttributeName = "YOffset")]
        public int YOffset { get; set; }
        public override string ToString()
        {
            return $"Text Defaults: BCG {Bcg}__Filters {Filters}__Size {Size}__Underline {Underline}__Opaque {Opaque}__XOffset {XOffset}__YOffset {YOffset}";
        }
    }

    public class Element : IXmlSerializable
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
        private static T? ConvertToNullable<T>(string inputValue) where T: struct
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
        public XmlSchema GetSchema() { return null;  }
        public void WriteXml(XmlWriter writer) { throw new NotImplementedException(); }
    }


    //[XmlRoot(ElementName = "Element")]
    //public class Element
    //{
    //    [XmlAttribute(AttributeName = "xsi-type")]
    //    public string XsiType { get; set; }

    //    [XmlAttribute(AttributeName = "Filters")]
    //    public string Filters { get; set; } = null;

    //    [XmlAttribute(AttributeName = "StartLat")]
    //    public double StartLat { get; set; }

    //    [XmlAttribute(AttributeName = "StartLon")]
    //    public double StartLon { get; set; }

    //    [XmlAttribute(AttributeName = "EndLat")]
    //    public double EndLat { get; set; }

    //    [XmlAttribute(AttributeName = "EndLon")]
    //    public double EndLon { get; set; }

    //    [XmlAttribute(AttributeName = "Lat")]
    //    public double Lat { get; set; }

    //    [XmlAttribute(AttributeName = "Lon")]
    //    public double Lon { get; set; }

    //    [XmlAttribute(AttributeName = "Lines")]
    //    public string Lines { get; set; }


    //    [XmlAttribute(AttributeName = "Bcg")]
    //    public int? Bcg { get; set; } = null;

    //    [XmlAttribute(AttributeName = "Size")]
    //    public int? Size { get; set; } = null;

    //    [XmlAttribute(AttributeName = "Underline")]
    //    public bool? Underline { get; set; } = null;

    //    [XmlAttribute(AttributeName = "Opaque")]
    //    public bool? Opaque { get; set; } = null;

    //    [XmlAttribute(AttributeName = "XOffset")]
    //    public int? XOffset { get; set; } = null;
    //    [XmlAttribute(AttributeName = "YOffset")]
    //    public int? YOffset { get; set; } = null;

    //    [XmlAttribute(AttributeName = "Style")]
    //    public string Style { get; set; } = null;
    //}

    [XmlRoot(ElementName = "Elements")]
    public class Elements
    {
        [XmlElement(ElementName = "Element")]
        public List<Element> Element { get; set; }
    }

    [XmlRoot(ElementName = "GeoMapObject")]
    public class GeoMapObject
    {
        [XmlElement(ElementName = "LineDefaults", IsNullable = true)]
        public LineDefaults LineDefaults { get; set; }

        [XmlElement(ElementName = "SymbolDefaults", IsNullable = true)]
        public SymbolDefaults SymbolDefaults { get; set; }
        
        [XmlElement(ElementName = "TextDefaults", IsNullable = true)]
        public TextDefaults TextDefaults { get; set; }

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
