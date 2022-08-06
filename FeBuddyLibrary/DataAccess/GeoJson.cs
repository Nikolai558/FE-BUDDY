using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FeBuddyLibrary.Models;
using Newtonsoft.Json.Linq;

namespace FeBuddyLibrary.DataAccess
{
    public class GeoJson
    {
        public GeoMapSet ReadGeoMap(string filepath)
        {
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
                    System.IO.FileInfo file = new System.IO.FileInfo(fileName);
                    file.Directory.Create(); // If the directory already exists, this method does nothing.
                    System.IO.File.WriteAllText(file.FullName, "TEST");
                }


            }







        }




















    }
}
