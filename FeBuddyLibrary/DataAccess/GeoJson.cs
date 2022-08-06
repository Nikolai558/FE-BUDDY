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

            foreach (GeoMap geoMap in geo.GeoMaps.GeoMap)
            {
                string geoMapDir = Path.Combine(dirPath, geoMap.Name);

                foreach (GeoMapObject geoMapObject in geoMap.Objects.GeoMapObject)
                {
                    string fileName = Path.Combine(geoMapDir, geoMapObject.Description + ".geojson");
                    FileInfo file = new FileInfo(fileName);
                    file.Directory.Create(); // If the directory already exists, this method does nothing.

                    var geojson = new FeatureCollection();

                    foreach (var element in geoMapObject.Elements.Element)
                    {
                        if (element.XsiType == "Symbol")
                        {
                            geojson.features.Add(
                                new Feature() 
                                { 
                                    geometry = new Geometry() 
                                    { 
                                        type = element.XsiType,
                                        corrdinates = new List<dynamic>() { element.Lat, element.Lon } 
                                    },
                                    properties = new Properties()
                                    {
                                        //style = geoMapObject.SymbolDefaults.Style,
                                        //size = geoMapObject.SymbolDefaults.Size,
                                    }
                                });
                        }

                    }
                    
                    string jsonString = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
                    File.WriteAllText(file.FullName, jsonString);
                }
            }
        }




















    }
}
