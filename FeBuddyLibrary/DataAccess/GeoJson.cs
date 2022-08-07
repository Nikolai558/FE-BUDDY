﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FeBuddyLibrary.Models;
using Newtonsoft.Json;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Dat;

namespace FeBuddyLibrary.DataAccess
{
    public enum VideoMapFileFormat
    {
        shortName,
        longName,
        both
    }

    public class GeoJson
    {
        public VideoMaps ReadVideoMap(string filepath)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), "FE-BUDDY", "tempGeoMap.xml");
            File.WriteAllText(tempFile, File.ReadAllText(filepath).Replace("xsi:type", "xsi-type"));
            
            XmlSerializer serializer = new XmlSerializer(typeof(VideoMaps));

            VideoMaps videoMaps;

            using (Stream reader = new FileStream(tempFile, FileMode.Open))
            {
                videoMaps = (VideoMaps)serializer.Deserialize(reader);
            }

            return videoMaps;
        }

        public GeoMapSet ReadGeoMap(string filepath)
        {
            // TODO - xsi:type will mess up the reading of XML. Need to either work around it or figure out the proper way to handle it.
            var tempFile = Path.Combine(Path.GetTempPath(), "FE-BUDDY", "tempGeoMap.xml");
            File.WriteAllText(tempFile, File.ReadAllText(filepath).Replace("xsi:type", "xsi-type"));

            XmlSerializer serializer = new XmlSerializer(typeof(GeoMapSet));
            
            GeoMapSet geo;

            using (Stream reader = new FileStream(tempFile, FileMode.Open))
            {
                

                geo = (GeoMapSet)serializer.Deserialize(reader);
            }

            return geo;
        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "-");
        }

        

        public void WriteVideoMapGeoJson(string dirPath, VideoMaps videoMaps, string videoMapName, VideoMapFileFormat videoMapFileFormat)
        {
            int count = 0;
            string videoMapDir = Path.Combine(dirPath, videoMapName);
            foreach (VideoMap videoMapObject in videoMaps.VideoMap)
            {
                Dictionary<string, string> colors = null;
                string fileName = "";
                string fullFilePath = "";
                count += 1;
                if (videoMapFileFormat == VideoMapFileFormat.both)
                {
                    fileName = videoMapObject.ShortName + "__" + videoMapObject.LongName + ".geojson";
                    fileName = MakeValidFileName(fileName);
                    //fileName = count.ToString().PadLeft(3, '0') + fileName;
                    fullFilePath = Path.Combine(videoMapDir, fileName);
                }
                if (videoMapFileFormat == VideoMapFileFormat.shortName) 
                {
                    fileName = videoMapObject.ShortName + ".geojson";
                    fileName = MakeValidFileName(fileName);
                    //fileName = count.ToString().PadLeft(3, '0') + fileName;
                    fullFilePath = Path.Combine(videoMapDir, fileName); 
                }
                if (videoMapFileFormat == VideoMapFileFormat.longName) 
                {
                    fileName = videoMapObject.LongName + ".geojson";
                    fileName = MakeValidFileName(fileName);
                    //fileName = count.ToString().PadLeft(3, '0') + fileName;
                    fullFilePath = Path.Combine(videoMapDir, fileName); 
                }

                FileInfo file = new FileInfo(fullFilePath);
                file.Directory.Create();
                if (count == 1)
                {
                    foreach (FileInfo fileInfo in file.Directory.EnumerateFiles())
                    {
                        fileInfo.Delete();
                    }
                }

                if (file.Exists)
                {
                    file = new FileInfo(Path.Combine(videoMapDir, fileName.Split('.')[0] + $"Map_{count}_Duplicate.geojson"));
                }
                var geojson = new FeatureCollection();

                if (videoMapObject.Colors?.NamedColor != null)
                {
                    colors = new Dictionary<string, string>();
                    colors.Add("", null);
                    foreach (var item in videoMapObject.Colors.NamedColor)
                    {
                        Color itemColor = Color.FromArgb(item.Red, item.Green, item.Blue);
                        colors.Add(item.Name, itemColor.R.ToString("X2") + itemColor.G.ToString("X2") + itemColor.B.ToString("X2"));
                    }
                }

                List<Feature> allFeatures = new List<Feature>();
                vmElement prevElement = null;
                Feature currentFeature = new Feature();
                foreach (var item in videoMapObject.Elements.Element)
                {
                    if (prevElement == null)
                    {
                        currentFeature.geometry = new Geometry()
                        {
                            type = "LineString",
                            coordinates = CheckAMCrossing(item.StartLat, item.StartLon, item.EndLat, item.EndLon)
                        };
                        currentFeature.properties = new Properties()
                        {
                            color = colors?[item.Color] ?? null,
                            style = item.Style,
                            thickness = item.Thickness,
                        };
                        prevElement = item;
                        continue;
                    }
                    else
                    {
                        if (LatLonHelpers.CorrectIlleagleLon(item.StartLon).ToString() + " " + item.StartLat.ToString() != LatLonHelpers.CorrectIlleagleLon(prevElement.EndLon) + " " + prevElement.EndLat)
                        {
                            if (currentFeature.geometry.coordinates.Count() > 0)
                            {
                                allFeatures.Add(currentFeature);
                            }

                            currentFeature = new Feature()
                            {
                                properties = new Properties()
                                {
                                    style = item.Style,
                                    thickness = item.Thickness,
                                    color = colors?[item.Color] ?? null
                                },
                                geometry = new Geometry()
                                {
                                    type = "LineString",
                                    coordinates = CheckAMCrossing(item.StartLat, item.StartLon, item.EndLat, item.EndLon)
                                }
                            };
                        }
                        else
                        {
                            //var coords = new List<double>() { item.EndLon, item.EndLat };

                            var coords = CheckAMCrossing(item.StartLat, item.StartLon, item.EndLat, item.EndLon);

                            if (coords.Count() > 2)
                            {
                                currentFeature.geometry.coordinates.AddRange(coords.GetRange(1, coords.Count() - 1));
                            }
                            else
                            {
                                currentFeature.geometry.coordinates.Add(coords.GetRange(1, 1));
                            }

                        }
                    }
                    prevElement = item;
                }
                allFeatures.Add(currentFeature);

                geojson.features.AddRange(allFeatures);

                string jsonString = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(file.FullName, jsonString);


            }
        }

        private List<dynamic> CheckAMCrossing(double startLat, double startLon, double endLat, double endLon)
        {
            startLon = LatLonHelpers.CorrectIlleagleLon(startLon);
            endLon = LatLonHelpers.CorrectIlleagleLon(endLon);

            List<dynamic> dynamicList = new List<dynamic>();

            if (!((startLon < 0 && endLon > 0) || (startLon > 0 && endLon < 0)))
            {
                // Lon does not cross the AM in any way. Just return our Coordinates. 
                dynamicList.Add(new List<double>() { startLon, startLat });
                dynamicList.Add(new List<double>() { endLon, endLat });
                return dynamicList;
            }
            
            // Lon DOES cross the AM. 
            Loc startLoc = new Loc() { Latitude = startLat, Longitude = startLon };
            Loc endLoc = new Loc() { Latitude = endLat, Longitude = endLon };
            Loc midPointStartLoc = new Loc();
            Loc midPointEndLoc = new Loc();
            var bearing = LatLonHelpers.HaversineGreatCircleBearing(startLoc, endLoc);

            if ((startLoc.Longitude < 0 && (bearing > 180 && bearing < 360)) || (startLoc.Longitude > 0 && (bearing > 0 && bearing < 180)))
            {
                // Dont need to split
                dynamicList.Add(new List<double>() { startLon, startLat });
                dynamicList.Add(new List<double>() { endLon, endLat });
                return dynamicList;
            }

            if (startLoc.Longitude < 0)
            {
                startLoc.Longitude = startLoc.Longitude + 180;
                // See if this works, If needed change to the limit -179.99999999999
                midPointStartLoc.Longitude = -180;
            }
            else
            {
                startLoc.Longitude = startLoc.Longitude - 180;
                midPointStartLoc.Longitude = 180;
            }

            if (endLoc.Longitude < 0)
            {
                endLoc.Longitude = endLoc.Longitude + 180;
                midPointEndLoc.Longitude = -180;
            }
            else
            {
                endLoc.Longitude = endLoc.Longitude - 180;
                midPointEndLoc.Longitude = 180;
            }

            var slope = (startLat - endLat) / (startLoc.Longitude - endLoc.Longitude);
            var midPointLat = startLat - (slope * startLoc.Longitude);

            dynamicList.Add(new List<double>() { startLon, startLat });
            dynamicList.Add(new List<double>() { midPointStartLoc.Longitude, midPointLat });
            dynamicList.Add(new List<double>() { midPointEndLoc.Longitude, midPointLat });
            dynamicList.Add(new List<double>() { endLon, endLat });
            return dynamicList;

        }


        public void WriteGeoMapGeoJson(string dirPath, GeoMapSet geo)
        {
            foreach (GeoMap geoMap in geo.GeoMaps.GeoMap)
            {
                string geoMapDir = Path.Combine(dirPath, geoMap.Name);

                foreach (GeoMapObject geoMapObject in geoMap.Objects.GeoMapObject)
                {
                    string fileName = geoMapObject.Description + ".geojson";
                    fileName = MakeValidFileName(fileName);

                    string fullFilePath = Path.Combine(geoMapDir, fileName);
                    FileInfo file = new FileInfo(fullFilePath);
                    file.Directory.Create(); // If the directory already exists, this method does nothing.

                    var geojson = new FeatureCollection();

                    List<Element> AllLines = new List<Element>();
                    foreach (Element element in geoMapObject.Elements.Element)
                    {
                        switch (element.XsiType)
                        {
                            case "Symbol": { geojson.features.Add(CreateSymbolFeature(element, geoMapObject.SymbolDefaults)); break; }
                            case "Text": { geojson.features.Add(CreateTextFeature(element, geoMapObject.TextDefaults)); break; }
                            case "Line": { AllLines.Add(element); break; }
                            default: { break; }
                        }
                    }
                    if (AllLines.Count() > 0)
                    {
                        geojson.features.AddRange(CreateLineFeature(AllLines, geoMapObject.LineDefaults));
                    }
                    
                    string jsonString = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
                    File.WriteAllText(file.FullName, jsonString);
                }
            }
        }

        private List<Feature> CreateLineFeature(List<Element> elements, LineDefaults lineDefaults)
        {
            List<Feature> featuresOutput = new List<Feature>();
            
            Element prevElement = null;
            Feature currentFeature = new Feature()
            {
                properties = new Properties()
                {
                    bcg = lineDefaults.Bcg,
                    style = lineDefaults.Style,
                    thickness = lineDefaults.Thickness,

                    color = null,
                    zIndex = null,
                },
            };


            foreach (Element element in elements)
            {
                if (prevElement == null)
                {
                    currentFeature.geometry = new Geometry()
                    {
                        type = "LineString",
                        coordinates = CheckAMCrossing(element.StartLat, element.StartLon, element.EndLat, element.EndLon)
                    };
                    prevElement = element;
                    continue;
                }
                else
                {
                    if (LatLonHelpers.CorrectIlleagleLon(element.StartLon).ToString() + " " + element.StartLat.ToString() != LatLonHelpers.CorrectIlleagleLon(prevElement.EndLon).ToString() + " " + prevElement.EndLat.ToString())
                    {
                        if (currentFeature.geometry.coordinates.Count() > 0)
                        {
                            featuresOutput.Add(currentFeature);
                        }

                        // Start Lat/Lon is different from Previous Element End Lat/Lon
                        currentFeature = new Feature()
                        {
                            properties = new Properties()
                            {
                                bcg = lineDefaults.Bcg,
                                style = lineDefaults.Style,
                                thickness = lineDefaults.Thickness,

                                color = null,
                                zIndex = null,
                            },
                            geometry = new Geometry()
                            {
                                type = "LineString",
                                coordinates = CheckAMCrossing(element.StartLat, element.StartLon, element.EndLat, element.EndLon)
                            }
                        };
                    }
                    else
                    {
                        //var coords = new List<double>() { item.EndLon, item.EndLat };

                        var coords = CheckAMCrossing(element.StartLat, element.StartLon, element.EndLat, element.EndLon);

                        if (coords.Count() > 2)
                        {
                            currentFeature.geometry.coordinates.AddRange(coords.GetRange(1, coords.Count() - 1));
                        }
                        else
                        {
                            currentFeature.geometry.coordinates.Add(coords.GetRange(1, 1));
                        }

                        //var coords = new List<double>() { LatLonHelpers.CorrectIlleagleLon(element.EndLon), element.EndLat };
                        //currentFeature.geometry.coordinates.Add(coords);
                    }
                }
                prevElement = element;
            }
            featuresOutput.Add(currentFeature);
            return featuresOutput;
        }

        private Feature CreateTextFeature(Element element, TextDefaults textDefaults)
        {
            var output = new Feature()
            {
                geometry = new Geometry()
                {
                    type = "Point",
                    coordinates = new List<dynamic>() { LatLonHelpers.CorrectIlleagleLon(element.Lon), element.Lat }
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
                        coordinates = new List<dynamic>() { LatLonHelpers.CorrectIlleagleLon(element.Lon), element.Lat }
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
