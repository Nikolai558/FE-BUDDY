using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FeBuddyLibrary.Models;

namespace FeBuddyLibrary.DataAccess
{
    public class GeoJson
    {

        public void ReadGeoMap(string filepath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GeoMapSet));
            
            GeoMapSet geo;

            using (Stream reader = new FileStream(filepath, FileMode.Open))
            {
                geo = (GeoMapSet)serializer.Deserialize(reader);
            }

            string stop = "STOP";
        }






















    }
}
