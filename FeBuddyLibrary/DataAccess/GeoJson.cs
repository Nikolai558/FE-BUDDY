using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FeBuddyLibrary.Models;
using Newtonsoft.Json;

namespace FeBuddyLibrary.DataAccess
{
    public class GeoJson
    {
        public GeoMapSet ReadGeoMap(string filepath)
        {
            // TODO - xsi:type will mess up the reading of XML. Need to either work around it or figure out the proper way to handle it.

            XmlSerializer serializer = new XmlSerializer(typeof(GeoMapSet));
            
            GeoMapSet geo;

            using (Stream reader = new FileStream(filepath, FileMode.Open))
            {
                geo = (GeoMapSet)serializer.Deserialize(reader);
            }

            return geo;
        }

        public void WriteGeoJson(string dirPath, GeoMapSet geo)
        {
            //string jsonString = JsonSerializer.Serialize(geo, new JsonSerializerOptions()
            //{
            //    WriteIndented = true,
            //});

            //File.WriteAllText(dirPath + "test.json", jsonString);

            var types = new List<string>();


            foreach (GeoMap geoMap in geo.GeoMaps.GeoMap)
            {
                string geoMapDir = Path.Combine(dirPath, geoMap.Name);

                foreach (GeoMapObject geoMapObject in geoMap.Objects.GeoMapObject)
                {
                    string fileName = Path.Combine(geoMapDir, geoMapObject.Description + ".geojson");
                    FileInfo file = new FileInfo(fileName);
                    file.Directory.Create(); // If the directory already exists, this method does nothing.

                    var geojson = new FeatureCollection();

                    foreach (Element element in geoMapObject.Elements.Element)
                    {
                        switch (element.XsiType)
                        {
                            case "Symbol": { geojson.features.Add(CreateSymbolFeature(element, geoMapObject.SymbolDefaults)); break; }
                            case "Text": { geojson.features.Add(CreateTextFeature(element, geoMapObject.TextDefaults)); break; }
                            case "Line": { break; }
                            default: { break; }
                        }
                    }
                    
                    string jsonString = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
                    File.WriteAllText(file.FullName, jsonString);
                }
            }
        }

        private Feature CreateTextFeature(Element element, TextDefaults textDefaults)
        {
            var output = new Feature()
            {
                geometry = new Geometry()
                {
                    type = "Point",
                    coordinates = new List<dynamic>() { element.Lon, element.Lat }
                },
                properties = new Properties()
                {
                    text = new string[] { element.Lines },
                    bcg = textDefaults.Bcg,
                    size = textDefaults.Size,
                    underline = textDefaults.Underline,
                    opaque = textDefaults.Opaque,
                    xOffset = textDefaults.XOffset,
                    yOffset = textDefaults.YOffset,
                    color = null,
                    zIndex = null
                }
            };

            try
            {
                output.properties.filters = Array.ConvertAll(textDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s));

            }
            catch (Exception)
            {
                output.properties.filters = new int[] { 0 };
            }
            return output;
        }

        private Feature CreateSymbolFeature(Element element, SymbolDefaults symbolDefaults)
        {
            var output = new Feature() {
                    geometry = new Geometry()
                    {
                        type = "Point",
                        coordinates = new List<dynamic>() { element.Lon, element.Lat }
                    },
                    properties = new Properties()
                    {
                        style = symbolDefaults.Style,
                        size = symbolDefaults.Size,
                        bcg = symbolDefaults.Bcg,
                        zIndex = null,
                        color = null,
                    }};
            try
            {
                output.properties.filters = Array.ConvertAll(symbolDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s));

            }
            catch (Exception)
            {
                output.properties.filters = new int[] { 0 };
            }
            return output;
        }




















    }
}
