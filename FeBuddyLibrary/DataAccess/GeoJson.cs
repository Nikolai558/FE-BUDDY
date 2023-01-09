using System;
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
using System.Windows.Forms;
using System.Reflection;

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
        public Dictionary<string, List<string>> asdexColorDef { get; set; } = new Dictionary<string, List<string>>()
            {
                { "runway", new List<string> { "runway", "rway", "rwy" } },
                { "taxiway", new List<string> { "taxiway", "tway", "twy", "taxi" } },
                { "apron", new List<string> { "ramp", "apron" } },
                { "structure", new List<string> { "bldg", "terminal", "building", "bldng", "struct", "structure" } },
                { "UNKNOWN", new List<string> { } }
            };

        private StringBuilder _errorLog = new StringBuilder();

        private bool isDefaultObject()
        {
            return false;
        }

        public string AddNullValues(string text)
        {
            StringBuilder output = new StringBuilder();

            var allLines = text.Split('\n');

            foreach (string line in allLines)
            {
                string tmpLine = line;

                if (!line.Contains("<Element xsi-type=\""))
                {
                    output.Append(line);
                    continue;
                }

                bool needsBcg = !line.Contains("Bcg=\"");
                bool needsSize = !line.Contains("Size=\"");
                bool needsUnderline = !line.Contains("Underline=\"");
                bool needsOpaque = !line.Contains("Opaque=\"");
                bool needsXOffset = !line.Contains("XOffset=\"");
                bool needsYOffset = !line.Contains("YOffset=\"");
                bool needsStyle = !line.Contains("Style=\"");
                bool needsFilters = !line.Contains("Filters=\"");

                if (needsBcg) { tmpLine = tmpLine.Replace("/>", "Bcg=\"\"" + " />"); }
                if (needsSize) { tmpLine = tmpLine.Replace("/>", "Size=\"\"" + " />"); }
                if (needsUnderline) { tmpLine = tmpLine.Replace("/>", "Underline=\"\"" + " />"); }
                if (needsOpaque) { tmpLine = tmpLine.Replace("/>", "Opaque=\"\"" + " />"); }
                if (needsXOffset) { tmpLine = tmpLine.Replace("/>", "XOffset=\"\"" + " />"); }
                if (needsYOffset) { tmpLine = tmpLine.Replace("/>", "YOffset=\"\"" + " />"); }
                if (needsStyle) { tmpLine = tmpLine.Replace("/>", "Style=\"\"" + " />"); }
                if (needsFilters) { tmpLine = tmpLine.Replace("/>", "Filters=\"\"" + " />"); }
                output.Append(tmpLine);
            }
            return output.ToString();
        }

        public VideoMaps ReadVideoMap(string filepath)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), "FE-BUDDY", "tempGeoMap.xml");
            var all_text = File.ReadAllText(filepath).Replace("xsi:type", "xsi-type");
            //all_text = AddNullValues(all_text);
            File.WriteAllText(tempFile, all_text);
            
            XmlSerializer serializer = new XmlSerializer(typeof(VideoMaps));

            VideoMaps videoMaps;
            try
            {
                using (Stream reader = new FileStream(tempFile, FileMode.Open))
                {
                    videoMaps = (VideoMaps)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("There is an error in XML document"))
                {
                    throw new Exception($"It appears your selected source file is not formatted properly based on your selected file type.\n\n{ex.Message}");
                }
                throw;
            }
            

            return videoMaps;
        }

        public GeoMapSet ReadGeoMap(string filepath)
        {
            // xsi:type will mess up the reading of XML. Need to either work around it or figure out the proper way to handle it.
            var tempFile = Path.Combine(Path.GetTempPath(), "FE-BUDDY", "tempGeoMap.xml");
            var all_text = File.ReadAllText(filepath).Replace("xsi:type", "xsi-type");
            //all_text = AddNullValues(all_text);
            File.WriteAllText(tempFile, all_text);


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
                List<Feature> allFeatures;
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
                        if (!colors.ContainsKey(item.Name.ToLower()))
                        {
                            // todo - Warn user two color names have been used...
                            colors.Add(item.Name.ToLower(), "#" + itemColor.R.ToString("X2") + itemColor.G.ToString("X2") + itemColor.B.ToString("X2"));
                        }
                    }
                }

                if (videoMapObject.ShortName.Contains(" ASDEX"))
                {
                    // Create Asdex stuff.
                    allFeatures = CreateAsdexVideoMap(videoMapObject.Elements.Element, colors, file.FullName);
                }
                else
                {
                    allFeatures = CreateLineStringVideoMap(videoMapObject.Elements.Element, colors);
                }


                geojson.features.AddRange(allFeatures);

                string jsonString = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(file.FullName, jsonString);
            }
        }

        public Dictionary<string, List<string>> ValidateAsdexProperties(VideoMaps videoMaps)
        {
            Dictionary<string, List<string>> output = asdexColorDef;

            foreach (var vm in videoMaps.VideoMap)
            {
                if (vm.ShortName.Contains(" ASDEX"))
                {
                    foreach (var namedColor in vm.Colors.NamedColor)
                    {
                        bool isInUnknownKey = false;
                        foreach (var key in asdexColorDef.Keys)
                        {
                            if (key == "UNKNOWN")
                            {
                                isInUnknownKey = true;
                            }


                            if (asdexColorDef[key].Contains(namedColor.Name.ToLower()))
                            {
                                break;
                            }
                            else if (isInUnknownKey)
                            {
                                asdexColorDef["UNKNOWN"].Add(namedColor.Name.ToLower());
                            }
                        }
                    }
                }
            }

            return output;
        }

        private List<Feature> CreateAsdexVideoMap(List<vmElement> vmElements, Dictionary<string, string> colors, string fileName)
        {
            // TODO - Create GUI for unrecognized "colors"

            List<Feature> allFeatures = new List<Feature>();
            //List<vmElement> otherElementsNotPaths = new List<vmElement>();

            // use color def to figure out asdex type.
            // runway       = [runway, rway, rwy]
            // taxiway      = [taxiway, tway, twy]
            // apron        = [ramp]
            // structure    = [bldg, terminal]
            //currentFeature.properties.asdex 
            

            foreach (var elementItem in vmElements)
            {
                if (elementItem.XsiType != "Path")
                {
                    // Per Ross, Ignore XSI:Types inside an ASDEX File that does not = Path
                    //otherElementsNotPaths.Add(elementItem);
                    continue;
                }

                Feature currentFeature = new Feature()
                {
                    //properties = new Properties()
                    //{
                    //    style = Char.ToLowerInvariant(elementItem.Style[0]) + elementItem.Style[1..],
                    //    thickness = elementItem.Thickness,
                    //},
                    geometry = new Geometry()
                    {
                        type = "Polygon"
                    }
                };

                //try
                //{
                //    currentFeature.properties.color = colors?[elementItem.Color.ToLower()] ?? "";
                //}
                //catch (Exception ex)
                //{
                //    colors.Add(elementItem.Color.ToLower(), "#000000");
                //    asdexColorDef["structure"].Add(elementItem.Color.ToLower());
                //}


                //foreach (var asdexColorKey in asdexColorDef.Keys)
                //{
                //    if (asdexColorDef[asdexColorKey].Contains(elementItem.Color.ToLower()) && asdexColorKey != "UNKNOWN")
                //    {
                //        currentFeature.properties.asdex = asdexColorKey;
                //        currentFeature.properties.thickness = null;
                //        currentFeature.properties.style = null;
                //    }
                //}

                //if (currentFeature.properties.asdex == null)
                //{
                //    //currentFeature.properties.asdex = "structure";
                //    throw new Exception($"'{elementItem.Color}' Asdex color can not be assigned to an asdex geojson property...");
                //}

                currentFeature.geometry.coordinates.Add(new List<dynamic>());

                List<double> firstCoord = null;
                foreach (var coord in elementItem.Points.WorldPoint)
                {
                    if (firstCoord == null)
                    {
                        firstCoord = new List<double>() { coord.Lon, coord.Lat };
                    }
                    currentFeature.geometry.coordinates[0].Add(new List<double>() { coord.Lon, coord.Lat });
                }
                currentFeature.geometry.coordinates[0].Add(firstCoord);
                allFeatures.Add(currentFeature);
            }


            // ---------------------------------------------------------------------------------------------------------
            // PER ROSS, Ignore elements that are not Path's for ASDEX VM
            //
            // If we decide to change our mind about this the following code would create a seperate geojson file
            // next to the asdex in the allLines location. This is incomplete and would need work but it is started.
            // 
            // ---------------------------------------------------------------------------------------------------------
            //if (otherElementsNotPaths.Count() > 0)
            //{
            //    FeatureCollection asdexOtherFeatureCollection = new FeatureCollection();
            //    var otherFeatures = new List<Feature>();

            //    foreach (vmElement vmItem in otherElementsNotPaths)
            //    {
            //        switch (vmItem.XsiType.ToLower())
            //        {
            //            case "label":
            //                {
            //                    Feature feature = new Feature()
            //                    {
            //                        properties = new Properties()
            //                        {
            //                            text = new List<string>() { vmItem.Text}, Would need to create XML Attribute for .Text in the XmlGeoJsonVideoMapModel.cs File
            //                            color = colors[vmItem.Color],
            //                        },
            //                        geometry = new Geometry()
            //                        {
            //                            type = "Point",
            //                            coordinates = new List<dynamic>() { new List<double>() { vmItem.Lon, vmItem.Lat } } Would need to create XML Attribute for .Lat and .Lon in the XmlGeoJsonVideoMapModel.cs File
            //                        }
            //                    };
            //                    otherFeatures.Add(feature);
            //                    break;
            //                }
            //            case "line":
            //                {
            //                    break;
            //                }
            //            default:
            //                break;
            //        }
            //    }

            //    asdexOtherFeatureCollection.features.AddRange(otherFeatures);
            //    string jsonString = JsonConvert.SerializeObject(asdexOtherFeatureCollection, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
            //    string fullFilePath = fileName.Split('.')[0] + "-OtherFeatures.geojson"; 
            //    File.WriteAllText(fullFilePath, jsonString);
            //}

            return allFeatures;
        }

        private List<Feature> CreateLineStringVideoMap(List<vmElement> vmElements, Dictionary<string, string> colors)
        {
            List<Feature> allFeatures = new List<Feature>();
            vmElement prevElement = null;
            Feature currentFeature = new Feature();

            foreach (var item in vmElements)
            {
                if (item.XsiType != "Line")
                {
                    continue;
                }

                if (!colors.ContainsKey(item.Color.ToLower()))
                {
                    colors.Add(item.Color.ToLower(), null);
                }

                bool crossesAM = false;
                var coords = CheckAMCrossing(item.StartLat, item.StartLon, item.EndLat, item.EndLon);
                if (coords.Count() == 4)
                {
                    crossesAM = true;
                }

                if (prevElement == null)
                {
                    currentFeature.geometry = new Geometry()
                    {
                        type = "LineString",
                    };
                    //currentFeature.properties = new Properties()
                    //{
                    //    color = colors?[item.Color.ToLower()] ?? null,
                    //    //color = null,
                    //    style = Char.ToLowerInvariant(item.Style[0]) + item.Style[1..],
                    //    thickness = item.Thickness,
                    //};

                    if (crossesAM)
                    {
                        currentFeature.geometry.coordinates.Add(coords[0]);
                        currentFeature.geometry.coordinates.Add(coords[1]);
                        allFeatures.Add(currentFeature);
                        currentFeature = new Feature()
                        {
                            //properties = new Properties()
                            //{
                            //    color = colors?[item.Color.ToLower()] ?? null,
                            //    style = Char.ToLowerInvariant(item.Style[0]) + item.Style[1..],
                            //    thickness = item.Thickness,
                            //},
                            geometry = new Geometry() { type = "LineString" }
                        };
                        currentFeature.geometry.coordinates.Add(coords[2]);
                        currentFeature.geometry.coordinates.Add(coords[3]);
                    }
                    else
                    {
                        currentFeature.geometry.coordinates = coords;
                    }

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
                            //properties = new Properties()
                            //{
                            //    style = Char.ToLowerInvariant(item.Style[0]) + item.Style[1..],
                            //    thickness = item.Thickness,
                            //    color = colors?[item.Color.ToLower()] ?? null
                            //},
                            geometry = new Geometry()
                            {
                                type = "LineString",
                            }
                        };

                        if (crossesAM)
                        {
                            currentFeature.geometry.coordinates.Add(coords[0]);
                            currentFeature.geometry.coordinates.Add(coords[1]);
                            allFeatures.Add(currentFeature);
                            currentFeature = new Feature()
                            {
                                //properties = new Properties()
                                //{
                                //    color = colors?[item.Color.ToLower()] ?? null,
                                //    style = Char.ToLowerInvariant(item.Style[0]) + item.Style[1..],
                                //    thickness = item.Thickness,
                                //},
                                geometry = new Geometry() { type = "LineString" }
                            };
                            currentFeature.geometry.coordinates.Add(coords[2]);
                            currentFeature.geometry.coordinates.Add(coords[3]);
                        }
                        else
                        {
                            currentFeature.geometry.coordinates = coords;
                        }
                    }
                    else
                    {
                        //var coords = new List<double>() { item.EndLon, item.EndLat };

                        if (crossesAM)
                        {
                            //currentFeature.geometry.coordinates.Add(coords[0]);
                            currentFeature.geometry.coordinates.Add(coords[1]);
                            allFeatures.Add(currentFeature);
                            currentFeature = new Feature()
                            {
                                //properties = new Properties()
                                //{
                                //    color = colors?[item.Color.ToLower()] ?? null,
                                //    style = Char.ToLowerInvariant(item.Style[0]) + item.Style[1..],
                                //    thickness = item.Thickness,
                                //},
                                geometry = new Geometry() { type = "LineString" }
                            };
                            currentFeature.geometry.coordinates.Add(coords[2]);
                            currentFeature.geometry.coordinates.Add(coords[3]);
                        }
                        else
                        {
                            currentFeature.geometry.coordinates.Add(coords[1]);
                        }
                    }
                }
                prevElement = item;
            }
            allFeatures.Add(currentFeature);
            return allFeatures;
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

            //if ((startLoc.Longitude < 0 && (bearing > 180 && bearing < 360)) || (startLoc.Longitude > 0 && (bearing > 0 && bearing < 180)))
            if ((startLoc.Longitude < 0 && (bearing > 0 && bearing < 180)) || (startLoc.Longitude > 0 && (bearing > 180 && bearing < 360)))
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
                            case "Symbol": 
                                {
                                    var featureToBeAdded = CreateSymbolFeature(element, geoMapObject.SymbolDefaults);
                                    if (featureToBeAdded!= null) { geojson.features.Add(featureToBeAdded); }
                                    else { _errorLog.AppendLine($"{geoMap.Name}  {geoMapObject.Description}  SYMBOL  {element.Lat} {element.Lon}: \n\t- Was not added to geojson because <SymbolDefaults> was not found for this map.\n\n\n"); }
                                    break; 
                                }
                            case "Text": 
                                {
                                    var featureToBeAdded = CreateTextFeature(element, geoMapObject.TextDefaults);
                                    if (featureToBeAdded != null) { geojson.features.Add(featureToBeAdded); }
                                    else { _errorLog.AppendLine($"{geoMap.Name}  {geoMapObject.Description}  TEXT  {element.Lat} {element.Lon}: \n\t- Was not added to geojson because <TextDefaults> was not found for this map.\n\n\n"); }
                                    break; 
                                }
                            case "Line": 
                                {
                                    // TODO - Double check why we do this code below......
                                    if (geoMapObject.LineDefaults == null) { _errorLog.AppendLine($"{geoMap.Name}  {geoMapObject.Description}  LINE  {element.StartLat} {element.StartLon}  {element.EndLat} {element.EndLon}: \n\t- Was not added to geojson because <LineDefaults> was not found for this map.\n\n\n"); }
                                    AllLines.Add(element); 
                                    break; 
                                }
                            default: { break; }
                        }
                    }

                    if (AllLines.Count() > 0)
                    {
                        foreach (var item in CreateLineFeature(AllLines))
                        {
                            if (item != null)
                            {
                                geojson.features.Add(item);
                            }
                        }
                    }
                    
                    string jsonString = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
                    File.WriteAllText(file.FullName, jsonString);
                }
            }
            if (!string.IsNullOrWhiteSpace(_errorLog.ToString()) && !string.IsNullOrEmpty(_errorLog.ToString()))
            {
                FileInfo errorFile = new FileInfo(Path.Combine(dirPath, "ErrorLog.txt"));
                //errorFile.Create();
                File.WriteAllText(errorFile.FullName, _errorLog.ToString());
            }
        }

        private Properties CheckProperties(Element element)
        {
            Properties elem_properties = new Properties();

            if (element.Filters != null && !string.IsNullOrWhiteSpace(element.Filters)) { elem_properties.filters = Array.ConvertAll(element.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s)); }
            if (element.Style != null) { elem_properties.style = element.Style; }
            if (element.Bcg != null) { elem_properties.bcg = element.Bcg; }
            if (element.Thickness != null) { elem_properties.thickness = element.Thickness; }

            return elem_properties;
        }

        private void CrossesAM(Element element, Feature currentFeature, List<Feature> featuresOutput, List<dynamic> coords, bool continutionOfFeature) 
        {
            if (!continutionOfFeature)
            {
                // Curent Feature Start coords Match Previous End Coords. Do not add Start coords.
                currentFeature.geometry.coordinates.Add(coords[0]);
            }
            currentFeature.geometry.coordinates.Add(coords[1]);
            currentFeature.properties = CheckProperties(element);

            featuresOutput.Add(currentFeature);

            currentFeature = new Feature()
            {
                properties = CheckProperties(element),
                geometry = new Geometry() { type = "LineString" }
            };
            currentFeature.geometry.coordinates.Add(coords[2]);
            currentFeature.geometry.coordinates.Add(coords[3]);
        }

        private void CheckFeatureProperties(Element element, List<Feature> featuresOutput, Element prevElement, List<dynamic> coords, ref Feature currentFeature)
        {
            // This function takes in the current feature and the previous feature.
            // if the Start Coords match the End Coords of the previous feature BUT
            // the properties are DIFFERENT we need to Split the features instead of
            // combining them into one feature group...

            // Properties for Previous and Current DO match (i.e. They are the same!)
            if (CheckProperties(prevElement).Equals(CheckProperties(element)))
            {
                currentFeature.geometry.coordinates.Add(coords[1]);
                return;
            }

            // Properties are different - Filters, Bcg, Size, and/or Thickness, etc.
            featuresOutput.Add(currentFeature);

            Feature newCurrentFeature = new Feature()
            {
                properties = CheckProperties(element),
                geometry = new Geometry() { type = "LineString" }
            };
            newCurrentFeature.geometry.coordinates.Add(coords[0]);
            newCurrentFeature.geometry.coordinates.Add(coords[1]);
            currentFeature = newCurrentFeature;
        }

        private List<Feature> CreateLineFeature(List<Element> elements)
        {
            List<Feature> featuresOutput = new List<Feature>();
            Element prevElement = null;
            Feature currentFeature = new Feature() { geometry = new Geometry() { type = "LineString" }};

            foreach (Element element in elements)
            {
                bool crossesAM = false;
                var coords = CheckAMCrossing(element.StartLat, element.StartLon, element.EndLat, element.EndLon);
                if (coords.Count() == 4) { crossesAM = true; }

                if (prevElement == null)
                {
                    if (crossesAM)
                    {
                        CrossesAM(element, currentFeature, featuresOutput, coords, false);
                    }
                    else
                    {
                        currentFeature.properties = CheckProperties(element);
                        currentFeature.geometry.coordinates = coords;
                    }
                    prevElement = element;
                    continue;
                }
                else
                {
                    // If start coords do NOT match end coords of previous element
                    if (LatLonHelpers.CorrectIlleagleLon(element.StartLon).ToString() + " " + element.StartLat.ToString() != LatLonHelpers.CorrectIlleagleLon(prevElement.EndLon).ToString() + " " + prevElement.EndLat.ToString())
                    {
                        if (crossesAM)
                        {
                            if (currentFeature.geometry.coordinates.Count() > 0)
                            {
                                featuresOutput.Add(currentFeature);
                                currentFeature = new Feature()
                                {
                                    properties = CheckProperties(element),
                                    geometry = new Geometry() { type = "LineString" }
                                };
                            }

                            CrossesAM(element, currentFeature, featuresOutput, coords, false);
                        }
                        else
                        {
                            if (currentFeature.geometry.coordinates.Count() > 0)
                            {
                                featuresOutput.Add(currentFeature);
                            }

                            currentFeature = new Feature()
                            {
                                properties = CheckProperties(element),
                                geometry = new Geometry()
                                {
                                    type = "LineString",
                                }
                            };
                            currentFeature.geometry.coordinates = coords;
                        }
                    }
                    else
                    {
                        if (crossesAM)
                        {
                            CrossesAM(element, currentFeature, featuresOutput, coords, true);
                        }
                        else
                        {
                            CheckFeatureProperties(element, featuresOutput, prevElement, coords, ref currentFeature);
                            //currentFeature.geometry.coordinates.Add(coords[1]);
                        }
                    }
                }
                prevElement = element;
            }
            featuresOutput.Add(currentFeature);
            return featuresOutput;
        }

        private Feature CreateTextFeature(Element element, TextDefaults textDefaults)
        {
            if (textDefaults == null)
            {
                return null;
            }

            Properties elem_properties = new Properties();

            if (element.Filters != null && !string.IsNullOrWhiteSpace(element.Filters)) { elem_properties.filters = Array.ConvertAll(element.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s)); }
            if (element.Style != null) { elem_properties.style = element.Style; }
            if (element.Bcg != null) { elem_properties.bcg = element.Bcg; }
            if (element.Size != null) { elem_properties.size = element.Size; }
            if (element.Underline != null) { elem_properties.underline= element.Underline; }
            if (element.Opaque != null) { elem_properties.opaque = element.Opaque; }
            if (element.XOffset != null) { elem_properties.xOffset = element.XOffset; }
            if (element.YOffset != null) { elem_properties.yOffset = element.YOffset; }
            elem_properties.text = new string[] { element.Lines };

            var output = new Feature()
            {
                geometry = new Geometry()
                {
                    type = "Point",
                    coordinates = new List<dynamic>() { LatLonHelpers.CorrectIlleagleLon(element.Lon), element.Lat }
                },
                properties = elem_properties
            };

            // CRC Defaults. If any of these are true we don't need to include them in the geojson file.
            //if (allLines.properties.size == 0) { allLines.properties.size = null; }
            //if (allLines.properties.bcg == 1) { allLines.properties.bcg = null; }
            //if (allLines.properties.xOffset == 0) { allLines.properties.xOffset = null; }
            //if (allLines.properties.yOffset == 0) { allLines.properties.yOffset = null; }
            //if (allLines.properties.opaque == false) { allLines.properties.bcg = null; }
            //if (allLines.properties.underline == false) { allLines.properties.bcg = null; }

            //try
            //{
            //    allLines.properties.filters = Array.ConvertAll(textDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s));

            //}
            //catch (Exception)
            //{
            //    allLines.properties.filters = new int[] { 0 };
            //}
            return output;
        }

        private Feature CreateSymbolFeature(Element element, SymbolDefaults symbolDefaults)
        {
            if (symbolDefaults == null)
            {
                return null;
            }

            Properties elem_properties = new Properties();

            if (element.Bcg != null) { elem_properties.bcg = element.Bcg; }

            // Array.ConvertAll(symbolDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s));

            if (element.Filters != null && !string.IsNullOrWhiteSpace(element.Filters)) { elem_properties.filters = Array.ConvertAll(element.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s)); }
            if (element.Style != null && !string.IsNullOrWhiteSpace(element.Style)) { elem_properties.style = element.Style; }
            if (element.Size != null) { elem_properties.size = element.Size; }

            var output = new Feature() {
                    geometry = new Geometry()
                    {
                        type = "Point",
                        coordinates = new List<dynamic>() { LatLonHelpers.CorrectIlleagleLon(element.Lon), element.Lat }
                    },
                    properties = elem_properties
                    };

            // CRC Defaults. If any of these are true we don't need to include them in the geojson file.
            //if (allLines.properties.style == "VOR") { allLines.properties.style = null; }
            //if (allLines.properties.size == 0) { allLines.properties.size = null; }
            //if (allLines.properties.bcg == 1) { allLines.properties.bcg = null; }

            //try
            //{
            //    allLines.properties.filters = Array.ConvertAll(symbolDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s));

            //}
            //catch (Exception)
            //{
            //    allLines.properties.filters = new int[] { 0 };
            //}
            return output;
        }
    }
}
