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
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using FeBuddyLibrary.Dxf.Data;
using FeBuddyLibrary.Dxf.Models;
using System.Drawing.Design;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;

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

        public SctFileModel ConvertString(string toConvertString)
        {
            DataFunctions _df = new DataFunctions();
            SctFileModel _sctFileModel = _df.ReadString(toConvertString);
            return _sctFileModel;
        }

        public SctFileModel ReadSctFile(string fullSourceFilePath)
        {
            DataFunctions _df = new DataFunctions();
            SctFileModel _sctFileModel = _df.ReadSctFile(fullSourceFilePath);
            return _sctFileModel;
        }

        public void WriteSctGeoJson(string dirPath, SctFileModel sctFileModel, string sctFileName)
        {
            var sctInfoSectionName = sctFileModel.SctInfoSection.SctFileName?.ToUpper().Trim();

            if (sctInfoSectionName != null)
            {
                dirPath = Path.Combine(dirPath, MakeValidFileName(sctInfoSectionName));
            }
            else
            {
                dirPath = Path.Combine(dirPath, MakeValidFileName(sctFileName));
            }

            // FE-Buddy GeoJson (Folder)
            //  SCT File Name (Folder)
            //      LOW-AIRWAY.geojson
            WriteArtccGeo(dirPath, sctFileModel.SctLowAirwaySection, "LOW-AIRWAY.geojson");
            //      HIGH-AIRWAY.geojson
            WriteArtccGeo(dirPath, sctFileModel.SctHighAirwaySection, "HIGH-AIRWAY.geojson");
            //      ARTCC.geojson
            WriteArtccGeo(dirPath, sctFileModel.SctArtccSection, "ARTCC.geojson");
            //      ARTCC-HIGH.geojson
            WriteArtccGeo(dirPath, sctFileModel.SctArtccHighSection, "ARTCC-HIGH.geojson");
            //      ARTCC-LOW.geojson
            WriteArtccGeo(dirPath, sctFileModel.SctArtccLowSection, "ARTCC-LOW.geojson");
            //      GEO.geojson
            WriteArtccGeo(dirPath, sctFileModel.SctGeoSection, "GEO.geojson");
            //      LABELS.geojson
            WriteLabelsGeo(dirPath, sctFileModel.SctLabelSection, "LABELS.geojson");
            //      REGIONS.geojson
            WriteRegionGeo(dirPath, sctFileModel.SctRegionsSection, "REGIONS.geojson");
            //      SID (Folder) //Started 5:34PM ENDED 6:22PM
            WriteSidStarGeo(dirPath, sctFileModel.SctSidSection, "SID");
            //      STAR (Folder)
            WriteSidStarGeo(dirPath, sctFileModel.SctStarSection, "STAR");
        }

        private void WriteSidStarGeo(string dirPath, List<SctSidStarModel> sctSidSection, string folderName)
        {
            int count = 0;
            foreach (SctSidStarModel model in sctSidSection)
            {
                var outputFile = Path.Combine(dirPath, folderName, MakeValidFileName(count.ToString().PadLeft(3, '0') + "__" + model.DiagramName.Trim() + ".geojson"));
                FileInfo fileInfo = new FileInfo(outputFile);
                Directory.CreateDirectory(fileInfo.DirectoryName);
                var geojson = new FeatureCollection();
                List<Feature> allFeatures = new List<Feature>();

                var startLat = double.Parse(LatLonHelpers.CreateDecFormat(model.StartLat, false));
                var startLon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(model.StartLon, false)));
                var endLat = double.Parse(LatLonHelpers.CreateDecFormat(model.EndLat, false));
                var endLon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(model.EndLon, false)));

                Feature currentFeature = new Feature()
                {
                    type = "Feature",
                    geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() }
                };

                currentFeature.geometry.coordinates.Add(new List<double>() { startLon, startLat });
                currentFeature.geometry.coordinates.Add(new List<double>() { endLon, endLat });

                double prevEndLat = endLat;
                double prevEndLon = endLon;

                foreach (SctAditionalDiagramLineSegments lineSeg in model.AdditionalLines)
                {
                    startLat = double.Parse(LatLonHelpers.CreateDecFormat(lineSeg.StartLat, false));
                    startLon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(lineSeg.StartLon, false)));
                    endLat = double.Parse(LatLonHelpers.CreateDecFormat(lineSeg.EndLat, false));
                    endLon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(lineSeg.EndLon, false)));

                    bool crossesAM = false;
                    var coords = LatLonHelpers.CheckAMCrossing(startLat, startLon, endLat, endLon);
                    if (coords.Count() == 4)
                    {
                        crossesAM = true;
                    }


                    if (startLat != prevEndLat || startLon != prevEndLon)
                    {
                        if (currentFeature != null && currentFeature.geometry.coordinates.Count() >= 1)
                        {
                            allFeatures.Add(currentFeature);
                            currentFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() }, properties = { } };
                        }

                        if (crossesAM)
                        {
                            currentFeature.geometry.coordinates.Add(coords[0]);
                            currentFeature.geometry.coordinates.Add(coords[1]);
                            allFeatures.Add(currentFeature);
                            currentFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() }, properties = { } };
                            currentFeature.geometry.coordinates.Add(coords[2]);
                            currentFeature.geometry.coordinates.Add(coords[3]);
                        }
                        else
                        {
                            currentFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() }, properties = { } };
                            currentFeature.geometry.coordinates.Add(new List<double>() { startLon, startLat });
                            currentFeature.geometry.coordinates.Add(new List<double>() { endLon, endLat });
                        }
                    }
                    else
                    {
                        if (crossesAM)
                        {
                            currentFeature.geometry.coordinates.Add(coords[1]);
                            allFeatures.Add(currentFeature);
                            currentFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() }, properties = { } };
                            currentFeature.geometry.coordinates.Add(coords[2]);
                            currentFeature.geometry.coordinates.Add(coords[3]);
                        }
                        else
                        {
                            currentFeature.geometry.coordinates.Add(new List<double> { endLon, endLat });
                        }
                    }

                    prevEndLat = endLat;
                    prevEndLon = endLon;
                }

                if (currentFeature != null && currentFeature.geometry.coordinates.Count() >= 1)
                {
                    allFeatures.Add(currentFeature);
                }

                if (allFeatures.Count() >= 1)
                {
                    geojson.features = allFeatures;
                }
                if (geojson.features.Count() >= 1)
                {
                    string json = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
                    File.WriteAllText(outputFile, json);
                }

                count += 1;
            }
        }

        private void WriteRegionGeo(string dirPath, List<SctRegionModel> sctRegionsSection, string fileName)
        {
            var outputFile = Path.Combine(dirPath, MakeValidFileName(fileName));
            FileInfo fileInfo = new FileInfo(outputFile);
            Directory.CreateDirectory(fileInfo.DirectoryName);
            var geojson = new FeatureCollection();
            List<Feature> allFeatures = new List<Feature>();

            foreach (SctRegionModel model in sctRegionsSection)
            {
                Feature feature = new Feature()
                {
                    type = "Feature",
                    geometry = new Geometry() { type = "Polygon", coordinates = new List<dynamic>() { new List<dynamic>() } }
                };

                double lat = double.Parse(LatLonHelpers.CreateDecFormat(model.Lat, false));
                double lon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(model.Lon, false)));

                feature.geometry.coordinates[0].Add(new List<double>() { lon, lat });

                foreach (RegionPolygonPoints additionalPoints in model.AdditionalRegionInfo)
                {
                    lat = double.Parse(LatLonHelpers.CreateDecFormat(additionalPoints.Lat, false));
                    lon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(additionalPoints.Lon, false)));

                    feature.geometry.coordinates[0].Add(new List<double>() { lon, lat });
                }

                var firstLon = feature.geometry.coordinates[0][0][0];
                var firstlat = feature.geometry.coordinates[0][0][1];

                var lastLon = feature.geometry.coordinates[0][feature.geometry.coordinates[0].Count -1][0];
                var LastLat = feature.geometry.coordinates[0][feature.geometry.coordinates[0].Count - 1][1];

                if (firstlat != LastLat || firstLon != lastLon)
                {
                    feature.geometry.coordinates[0].Add(feature.geometry.coordinates[0][0]);
                }


                if (feature.geometry.coordinates.Count() >= 1)
                {
                    allFeatures.Add(feature);
                }
            }

            if (allFeatures.Count() >= 1)
            {
                geojson.features = allFeatures;
            }

            if (geojson.features.Count() >= 1)
            {
                string json = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(outputFile, json);
            }
        }

        private void WriteLabelsGeo(string dirPath, List<SctLabelModel> labelModels, string fileName)
        {
            var outputFile = Path.Combine(dirPath, MakeValidFileName(fileName));
            FileInfo fileInfo = new FileInfo(outputFile);
            Directory.CreateDirectory(fileInfo.DirectoryName);
            var geojson = new FeatureCollection();
            List<Feature> allFeatures = new List<Feature>();

            foreach (SctLabelModel model in labelModels)
            {
                var lat = double.Parse(LatLonHelpers.CreateDecFormat(model.Lat, false));
                var lon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(model.Lon, false)));


                Feature feature = new Feature() { 
                    type = "Feature", 
                    properties = new Properties() { text = new string[] { model.LabelText } }, 
                    geometry = new Geometry() { coordinates = new List<dynamic>() { lon, lat }, type = "Point"}
                };
                allFeatures.Add(feature);
            }

            if (allFeatures.Count() >= 1)
            {
                geojson.features = allFeatures;
            }

            if (geojson.features.Count() >= 1)
            {
                string json = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(outputFile, json);
            }
        }

        private void WriteArtccGeo<T>(string dirPath, List<T> artccModel, string fileName) where T: class, IStartEndLat
        {
            var outputFile = Path.Combine(dirPath, MakeValidFileName(fileName));
            //outputFile = MakeValidFileName(outputFile);

            FileInfo fileInfo = new FileInfo(outputFile);
            Directory.CreateDirectory(fileInfo.DirectoryName);

            var geojson = new FeatureCollection();
            List<Feature> allFeatures = new List<Feature>();

            double prevEndLat = 0;
            double prevEndLon = 0;
            Feature currentFeature = null;
            foreach (var model in artccModel)
            {
                double startLat = double.Parse(LatLonHelpers.CreateDecFormat(model.StartLat, false));
                double startLon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(model.StartLon, false)));
                double EndLat = double.Parse(LatLonHelpers.CreateDecFormat(model.EndLat, false));
                double EndLon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(model.EndLon, false)));

                bool crossesAM = false;
                var coords = LatLonHelpers.CheckAMCrossing(startLat, startLon, EndLat, EndLon);
                if (coords.Count() == 4)
                {
                    crossesAM = true;
                }


                if (startLat != prevEndLat || startLon != prevEndLon)
                {
                    if (currentFeature != null && currentFeature.geometry.coordinates.Count() >= 1)
                    {
                        allFeatures.Add(currentFeature);
                        currentFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() }, properties = { } };
                    }

                    if (crossesAM)
                    {
                        currentFeature.geometry.coordinates.Add(coords[0]);
                        currentFeature.geometry.coordinates.Add(coords[1]);
                        allFeatures.Add(currentFeature);
                        currentFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() }, properties = { } };
                        currentFeature.geometry.coordinates.Add(coords[2]);
                        currentFeature.geometry.coordinates.Add(coords[3]);
                    }
                    else
                    {
                        currentFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() }, properties = { } };
                        currentFeature.geometry.coordinates.Add(new List<double>() { startLon, startLat });
                        currentFeature.geometry.coordinates.Add(new List<double>() { EndLon, EndLat });
                    }
                }
                else
                {
                    if (crossesAM)
                    {
                        currentFeature.geometry.coordinates.Add(coords[1]);
                        allFeatures.Add(currentFeature);
                        currentFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() }, properties = { } };
                        currentFeature.geometry.coordinates.Add(coords[2]);
                        currentFeature.geometry.coordinates.Add(coords[3]);
                    }
                    else
                    {
                        currentFeature.geometry.coordinates.Add(new List<double> { EndLon, EndLat });
                    }
                }

                prevEndLat = EndLat;
                prevEndLon = EndLon;
            }

            if (currentFeature != null && currentFeature.geometry.coordinates.Count() >= 1)
            {
                allFeatures.Add(currentFeature);
            }

            geojson.features = allFeatures;

            if (geojson.features.Count() >= 1)
            {
                string json = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(outputFile, json);
            }
        }

        //private static string MakeValidFileName(string name)
        //{
        //    string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        //    string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        //    return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "-");
        //}


        private static Regex _invalidFileNameCharsRegex = new Regex(string.Concat("[", Regex.Escape(new string(Path.GetInvalidFileNameChars())), "]+"), RegexOptions.Compiled);

        private static string MakeValidFileName(string name)
        {
            return _invalidFileNameCharsRegex.Replace(name, "-");
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

                geojson.features.Add(new Feature()
                {
                    type = "Feature",
                    geometry = new Geometry()
                    {
                        type = "Point",
                        coordinates = new List<dynamic>() { 90.0, 180.0 }
                    },
                    properties = new Properties()
                    {
                        isLineDefaults = true,
                        style = "solid",
                        thickness = 1,
                    }
                });

                geojson.features.AddRange(allFeatures);

                string jsonString = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
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
            //                    Feature currentFeature = new Feature()
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
            //                    otherFeatures.Add(currentFeature);
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
            //    string jsonString = JsonConvert.SerializeObject(asdexOtherFeatureCollection, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
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
                var coords = LatLonHelpers.CheckAMCrossing(item.StartLat, item.StartLon, item.EndLat, item.EndLon);
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
                        //var coords = new List<double>() { item.endLon, item.endLat };

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

        

        private int[] getFilters(GeoMapObject geoMapObject, string xsiType)
        {
            List<int> filters = new List<int>();
            string filtersString = "";

            switch (xsiType)
            {
                case "Line": { if (geoMapObject.LineDefaults != null) filtersString += "," + geoMapObject.LineDefaults.Filters; break; }
                case "Symbol": { if (geoMapObject.SymbolDefaults != null) filtersString += "," + geoMapObject.SymbolDefaults.Filters; break;}
                case "Text": { if (geoMapObject.TextDefaults != null) filtersString += "," + geoMapObject.TextDefaults.Filters; break;}
                default:
                    break;
            }

            foreach (string filter in filtersString.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','))
            {
                if (!string.IsNullOrWhiteSpace(filter) && !filters.Contains(int.Parse(filter)))
                {
                    filters.Add(int.Parse(filter));
                }
            }

            return filters.ToArray();
        }

        private List<string> createDefaultFileNames(GeoMapObject geoMapObject, string dirPath)
        {
            string filterDir;
            string fileName;
            string fullFilePath;
            List<string> output = new List<string>();

            if (geoMapObject.LineDefaults != null)
            {
                if (geoMapObject.LineDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty) == "") return output;
                foreach (var filter in Array.ConvertAll(geoMapObject.LineDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s)))
                {
                    filterDir = "FILTER " + filter.ToString().PadLeft(2, '0');
                    fileName = $"{filterDir}__TDM {geoMapObject.TdmOnly.ToString()[0]}__{geoMapObject.LineDefaults.ToString().Replace(" Defaults: ", "__")}";
                    fullFilePath = Path.Combine(dirPath, filterDir, fileName + ".geojson");

                    if (!output.Contains(fullFilePath)) { output.Add(fullFilePath); }
                }
            }
            if (geoMapObject.SymbolDefaults != null)
            {
                if (geoMapObject.SymbolDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty) == "") return output;
                foreach (var filter in Array.ConvertAll(geoMapObject.SymbolDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s)))
                {
                    filterDir = "FILTER " + filter.ToString().PadLeft(2, '0');
                    fileName = $"{filterDir}__TDM {geoMapObject.TdmOnly.ToString()[0]}__{geoMapObject.SymbolDefaults.ToString().Replace(" Defaults: ", "__")}";
                    fullFilePath = Path.Combine(dirPath, filterDir, fileName + ".geojson");
                    if (!output.Contains(fullFilePath)) { output.Add(fullFilePath); }
                }
            }
            if (geoMapObject.TextDefaults != null)
            {
                if (geoMapObject.TextDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty) == "") return output;
                foreach (var filter in Array.ConvertAll(geoMapObject.TextDefaults.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s)))
                {
                    filterDir = "FILTER " + filter.ToString().PadLeft(2, '0');
                    fileName = $"{filterDir}__TDM {geoMapObject.TdmOnly.ToString()[0]}__{geoMapObject.TextDefaults.ToString().Replace(" Defaults: ", "__")}";
                    fullFilePath = Path.Combine(dirPath, filterDir, fileName + ".geojson");
                    if (!output.Contains(fullFilePath)) { output.Add(fullFilePath); }
                }
            }
            return output;
        }

        private string CombineProperties(string defaultProperties, string overridenProperties) 
        {
            defaultProperties += "__";

            string output = defaultProperties;

            if (overridenProperties.Contains("__"))
            {
                List<string> ovPropertiesList = overridenProperties.Split("__").ToList();
                foreach (var prop in ovPropertiesList)
                {
                    if (prop == "") continue;
                    Regex regex= new Regex($"_({prop.Split(' ')[0]}[\\s\\S]*?.)[_|.]");
                    var matches = regex.Match(output).Groups[0].ToString();
                    output = regex.Replace(output, "_" + prop + "_");
                }
            }

            if (output == "__")
            {
                output += "MISSING DEFAULTS";
            }

            return output;
        }

        private List<string> CreateElementFileNames(GeoMapObject geoMapObject, string dirPath, Element element)
        {
            List<string> output = new List<string>();
            string overrideProperties = CreateProperties(element).ToString();
            string defaultProperties = "";

            string filterDir;
            string fileName;
            string fullFilePath;


            switch (element.XsiType)
            {
                case "Line": { defaultProperties = geoMapObject.LineDefaults?.ToString().Replace("Line Defaults: ", "__"); break; }
                case "Symbol": { defaultProperties = geoMapObject.SymbolDefaults?.ToString().Replace("Symbol Defaults: ", "__"); break; }
                case "Text": { defaultProperties = geoMapObject.TextDefaults?.ToString().Replace("Text Defaults: ", "__"); break; }
            }

            string combinedProperties = CombineProperties(defaultProperties, overrideProperties);


            if (string.IsNullOrEmpty(element.Filters) || string.IsNullOrWhiteSpace(element.Filters))
            {
                int[] defaultFiltersList = getFilters(geoMapObject, element.XsiType);

                if (defaultFiltersList.Length == 0) // Filter Defaults are missing, No custom overide filters either. 
                {
                    filterDir = "MISSING DEFAULTS";
                    fileName = $"FILTER MM__TDM {geoMapObject.TdmOnly.ToString()[0]}__{element.XsiType}{combinedProperties}";
                    fullFilePath = Path.Combine(dirPath, filterDir, fileName + ".geojson");
                    if (!output.Contains(fullFilePath)) { output.Add(fullFilePath); }
                }
                else if (defaultFiltersList.Length > 1) // Filter Defaults have more than one Filter
                {
                    string multiFilterFolder = "MULTI FILTERS";
                    filterDir = "FILTER";
                    fileName = "FILTER";

                    foreach (var filter in defaultFiltersList)
                    {
                        filterDir += " " + filter.ToString().PadLeft(2, '0');
                        fileName += " " + $"{filter.ToString().PadLeft(2, '0')}";
                    }
                    fileName += $"__TDM {geoMapObject.TdmOnly.ToString()[0]}__{element.XsiType}{combinedProperties}";
                    fullFilePath = Path.Combine(dirPath, multiFilterFolder, filterDir, fileName + ".geojson");
                    if (!output.Contains(fullFilePath)) { output.Add(fullFilePath); }
                }
                else // Filter defaults has exactly one filter
                {
                    filterDir = "FILTER " + defaultFiltersList[0].ToString().PadLeft(2, '0');
                    fileName = $"FILTER {defaultFiltersList[0].ToString().PadLeft(2, '0')}__TDM {geoMapObject.TdmOnly.ToString()[0]}__{element.XsiType}{combinedProperties}";
                    fullFilePath = Path.Combine(dirPath, filterDir, fileName + ".geojson");
                    if (!output.Contains(fullFilePath)) { output.Add(fullFilePath); }
                }
            }
            else
            {
                // Custom Override filters
                int[] AllFilterGroups = Array.ConvertAll(element.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s));

                if (AllFilterGroups.Length > 1) // Custom Override filters have more than one Filter
                {
                    string multiFilterFolder = "MULTI FILTERS";
                    filterDir = "FILTER";
                    fileName = "FILTER";

                    foreach (var filter in AllFilterGroups)
                    {
                        filterDir += " " + filter.ToString().PadLeft(2, '0');
                        fileName += " " + $"{filter.ToString().PadLeft(2, '0')}";
                    }
                    fileName += $"__TDM {geoMapObject.TdmOnly.ToString()[0]}__{element.XsiType}{combinedProperties}";
                    fullFilePath = Path.Combine(dirPath, multiFilterFolder, filterDir, fileName + ".geojson");
                    if (!output.Contains(fullFilePath)) { output.Add(fullFilePath); }
                }
                else // Custom Override filters has exactly one filter
                {
                    filterDir = "FILTER " + AllFilterGroups[0].ToString().PadLeft(2, '0');
                    fileName = $"FILTER {AllFilterGroups[0].ToString().PadLeft(2, '0')}__TDM {geoMapObject.TdmOnly.ToString()[0]}__{element.XsiType}{combinedProperties}";
                    fullFilePath = Path.Combine(dirPath, filterDir, fileName + ".geojson");
                    if (!output.Contains(fullFilePath)) { output.Add(fullFilePath); }
                }
                


                // THIS FUNCTION WILL CREATED DUPLICATE FILES FOR EACH FILTER IN THE LIST (ex. FILTER=1,2,3 will create 3 files each containing the same data)
                //foreach (var customFilters in Array.ConvertAll(element.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s)))
                //{
                //    filterDir = "FILTER " + customFilters.ToString().PadLeft(2, '0');
                //    fileName = $"FILTER {customFilters.ToString().PadLeft(2, '0')}__TDM {geoMapObject.TdmOnly.ToString()[0]}__{element.XsiType}{combinedProperties}";
                //    fullFilePath = Path.Combine(dirPath, filterDir, fileName + ".geojson");
                //    if (!output.Contains(fullFilePath)) { output.Add(fullFilePath); }
                //}
            }

            return output;
        }

        public void WriteCombinedGeoMapGeoJson(string dirPath, GeoMapSet geo)
        {
            //throw new NotImplementedException();

            StringBuilder geoMapObjectLog = new StringBuilder();

            Dictionary<string, FeatureCollection> FileFeatures = new Dictionary<string, FeatureCollection>();

            Dictionary<string, Dictionary<string, List<string>>> LogDictionary = new Dictionary<string, Dictionary<string, List<string>>>(); 

            foreach (GeoMap geoMap in geo.GeoMaps.GeoMap)
            {
                LogDictionary.TryAdd(geoMap.Name, new Dictionary<string, List<string>>());

                string geoMapDir = Path.Combine(dirPath, MakeValidFileName(geoMap.Name));

                foreach (GeoMapObject geoMapObject in geoMap.Objects.GeoMapObject)
                {
                    Dictionary<string, List<Element>> AllLines = new Dictionary<string, List<Element>>();
                    Dictionary<string, Feature> AllLinesDefaults = new Dictionary<string, Feature>();
                    foreach (Element element in geoMapObject.Elements.Element)
                    {
                        // figure out how to get new Full file path added to list above
                        // if individual elements have different properties.
                        var elementFilePaths = CreateElementFileNames(geoMapObject, geoMapDir, element);
                        foreach (var elementFilePath in elementFilePaths)
                        {
                            LogDictionary[geoMap.Name].TryAdd(elementFilePath, new List<string>());
                            if (!LogDictionary[geoMap.Name][elementFilePath].Contains(geoMapObject.Description))
                            {
                                LogDictionary[geoMap.Name][elementFilePath].Add(geoMapObject.Description);
                            }

                            FileFeatures.TryAdd(elementFilePath, new FeatureCollection());
                            switch (element.XsiType)
                            {
                                case "Symbol":
                                    {
                                        var featureToBeAdded = CreateSymbolFeature(element);

                                        if (geoMapObject.SymbolDefaults != null && (FileFeatures[elementFilePath].features.Count() == 0 || (!FileFeatures[elementFilePath].features[0].properties.isSymbolDefaults ?? true)))
                                        {
                                            // Add defaults to feature if they exist
                                            Feature defaultFeature = new Feature()
                                            {
                                                type = "Feature",
                                                geometry = new Geometry()
                                                {
                                                    type = "Point",
                                                    coordinates = new List<dynamic>() { 90.0, 180.0 }
                                                },
                                                properties = new Properties()
                                                {
                                                    isSymbolDefaults = true,
                                                    bcg = geoMapObject.SymbolDefaults?.Bcg ?? null,
                                                    filters = geoMapObject.SymbolDefaults?.Filters?.Replace(" ", string.Empty)?.Replace("\t", string.Empty)?.Split(',')?.Where(s => !string.IsNullOrEmpty(s)).Select(s => int.Parse(s)).ToArray() ?? Array.Empty<int>(),
                                                    style = geoMapObject.SymbolDefaults?.Style ?? null,
                                                    size = geoMapObject.SymbolDefaults?.Size ?? null,
                                                }
                                            };
                                            FileFeatures[elementFilePath].features.Add(defaultFeature);
                                        }

                                        if (featureToBeAdded != null) { FileFeatures[elementFilePath].features.Add(featureToBeAdded); }
                                        else { _errorLog.AppendLine($"{geoMap.Name}  {geoMapObject.Description}  SYMBOL  {element.Lat} {element.Lon}: \n\t- Was not added to geojson because <SymbolDefaults> was not found for this map.\n\n\n"); }
                                        break;
                                    }
                                case "Text":
                                    {
                                        var featureToBeAdded = CreateTextFeature(element);

                                        if (geoMapObject.TextDefaults != null && (FileFeatures[elementFilePath].features.Count() == 0 || (!FileFeatures[elementFilePath].features[0].properties.isTextDefaults ?? true)))
                                        {
                                            // Add defaults to feature if they exist
                                            Feature defaultFeature = new Feature()
                                            {
                                                type = "Feature",
                                                geometry = new Geometry()
                                                {
                                                    type = "Point",
                                                    coordinates = new List<dynamic>() { 90.0, 180.0 }
                                                },
                                                properties = new Properties()
                                                {
                                                    isTextDefaults = true,
                                                    bcg = geoMapObject.TextDefaults?.Bcg ?? null,
                                                    filters = geoMapObject.TextDefaults?.Filters?.Replace(" ", string.Empty)?.Replace("\t", string.Empty)?.Split(',')?.Where(s => !string.IsNullOrEmpty(s)).Select(s => int.Parse(s)).ToArray() ?? Array.Empty<int>(),
                                                    size = geoMapObject.TextDefaults?.Size ?? null,
                                                    underline = geoMapObject.TextDefaults?.Underline ?? null,
                                                    opaque = geoMapObject.TextDefaults?.Opaque ?? null,
                                                    xOffset = geoMapObject.TextDefaults?.XOffset ?? null,
                                                    yOffset = geoMapObject.TextDefaults?.YOffset ?? null,
                                                }
                                            };
                                            FileFeatures[elementFilePath].features.Add(defaultFeature);
                                        }

                                        if (featureToBeAdded != null) { FileFeatures[elementFilePath].features.Add(featureToBeAdded); }
                                        else { _errorLog.AppendLine($"{geoMap.Name}  {geoMapObject.Description}  TEXT  {element.Lat} {element.Lon}: \n\t- Was not added to geojson because <TextDefaults> was not found for this map.\n\n\n"); }
                                        break;
                                    }
                                case "Line":
                                    {
                                        // TODO - Double check why we do this code below......
                                        if (geoMapObject.LineDefaults == null) { _errorLog.AppendLine($"{geoMap.Name}  {geoMapObject.Description}  LINE  {element.StartLat} {element.StartLon}  {element.EndLat} {element.EndLon}: \n\t- Was not added to geojson because <LineDefaults> was not found for this map.\n\n\n"); }
                                        AllLines.TryAdd(elementFilePath, new List<Element>());
                                        AllLinesDefaults.TryAdd(elementFilePath, new Feature()
                                        {
                                            type = "Feature",
                                            geometry = new Geometry()
                                            {
                                                type = "Point",
                                                coordinates = new List<dynamic>() { 90.0, 180.0 }
                                            },
                                            properties = new Properties()
                                            {
                                                isLineDefaults = true,
                                                bcg = geoMapObject.LineDefaults?.Bcg ?? null,
                                                filters = geoMapObject.LineDefaults?.Filters?.Replace(" ", string.Empty)?.Replace("\t", string.Empty)?.Split(',')?.Where(s => !string.IsNullOrEmpty(s)).Select(s => int.Parse(s)).ToArray() ?? Array.Empty<int>(),
                                                style = geoMapObject.LineDefaults?.Style ?? null,
                                                thickness = geoMapObject.LineDefaults?.Thickness ?? null,
                                            }
                                        });
                                        AllLines[elementFilePath].Add(element);
                                        break;
                                    }
                                default: { break; }
                            }
                        }
                    }

                    foreach (string filePathKey in AllLines.Keys)
                    {
                        if (AllLines[filePathKey].Count() > 0)
                        {
                            foreach (var item in CreateLineFeature(AllLines[filePathKey]))
                            {
                                if (item != null)
                                {
                                    if (FileFeatures[filePathKey].features.Count() == 0 || (!FileFeatures[filePathKey].features[0].properties.isLineDefaults ?? true))
                                    {
                                        FileFeatures[filePathKey].features.Add(AllLinesDefaults[filePathKey]);
                                    }

                                    FileFeatures[filePathKey].features.Add(item);
                                }
                            }
                        }
                    }
                }
            }

            foreach (string fullFilePath in FileFeatures.Keys)
            {
                FileInfo file = new FileInfo(fullFilePath);
                file.Directory.Create(); // If the directory already exists, this method does nothing.

                string jsonString = JsonConvert.SerializeObject(FileFeatures[fullFilePath], new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });

                File.WriteAllText(fullFilePath, jsonString);
            }

            foreach (var Map in LogDictionary.Keys)
            {
                geoMapObjectLog.AppendLine("\n\n-------------------------------------------------------------------------------------------------------------------------------------------------\n\n");
                geoMapObjectLog.AppendLine(Map);

                foreach (var filePath in LogDictionary[Map].Keys)
                {
                    geoMapObjectLog.AppendLine("\n\t" + filePath.Replace(dirPath, string.Empty));
                    foreach (var mapObject in LogDictionary[Map][filePath])
                    {
                        geoMapObjectLog.AppendLine("\t\t" + mapObject);
                    }
                }
            }


            File.WriteAllText(Path.Combine(dirPath, "GeoMapObject Per File Log.txt"), geoMapObjectLog.ToString());
        }

        public void WriteGeoMapGeoJson(string dirPath, GeoMapSet geo)
        {
            StringBuilder geoMapObjectLog = new StringBuilder();
            foreach (GeoMap geoMap in geo.GeoMaps.GeoMap)
            {
                geoMapObjectLog.AppendLine($"\n\n-------------------------------------------------------------------------------------------------------------------------------------------------\n\n");
                geoMapObjectLog.AppendLine($"{geoMap.Name}");

                string geoMapDir = Path.Combine(dirPath, geoMap.Name);

                int textdefaults = 0;
                int symboldefaults = 0;
                int linedefaults = 0;
                foreach (GeoMapObject geoMapObject in geoMap.Objects.GeoMapObject)
                {

                    // TODO: The log file is duplicating lines inside if they have duplicate description names/info.
                    geoMapObjectLog.AppendLine($"\n\n\tDescription: {geoMapObject.Description}");
                    geoMapObjectLog.AppendLine($"\t\tTDM: {geoMapObject.TdmOnly}");

                    if (geoMapObject.LineDefaults?.ToString() != null) { geoMapObjectLog.AppendLine("\t\t" + geoMapObject.LineDefaults?.ToString());}
                    if (geoMapObject.TextDefaults?.ToString() != null) { geoMapObjectLog.AppendLine("\t\t" + geoMapObject.TextDefaults?.ToString());}
                    if (geoMapObject.SymbolDefaults?.ToString() != null) { geoMapObjectLog.AppendLine("\t\t" + geoMapObject.SymbolDefaults?.ToString());}

                    string fileName = geoMapObject.Description + ".geojson";
                    fileName = MakeValidFileName(fileName);

                    string fullFilePath = Path.Combine(geoMapDir, fileName);
                    FileInfo file = new FileInfo(fullFilePath);
                    file.Directory.Create(); // If the directory already exists, this method does nothing.

                    var geojson = new FeatureCollection();

                    List<Element> AllLines = new List<Element>();
                    Feature AllLinesDefaults = new Feature()
                    {
                        type = "Feature",
                        geometry = new Geometry()
                        {
                            type = "Point",
                            coordinates = new List<dynamic>() { 90.0, 180.0 }
                        },
                        properties = new Properties()
                        {
                            isLineDefaults = true,
                            bcg = geoMapObject.LineDefaults?.Bcg ?? null,
                            filters = Array.ConvertAll(geoMapObject.LineDefaults?.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(',') ?? new string[] { }, s => int.Parse(s)),
                            style = geoMapObject.LineDefaults?.Style ?? null,
                            thickness = geoMapObject.LineDefaults?.Thickness ?? null,
                        }
                    };

                    foreach (Element element in geoMapObject.Elements.Element)
                    {
                        switch (element.XsiType)
                        {
                            case "Symbol": 
                                {
                                    var featureToBeAdded = CreateSymbolFeature(element);

                                    if (geoMapObject.SymbolDefaults != null && (geojson.features.Count() == 0 || (!geojson.features[0].properties.isSymbolDefaults ?? true)))
                                    {
                                        // Add defaults to feature if they exist
                                        Feature defaultFeature = new Feature()
                                        {
                                            type = "Feature",
                                            geometry = new Geometry()
                                            {
                                                type = "Point",
                                                coordinates = new List<dynamic>() { 90.0, 180.0 }
                                            },
                                            properties = new Properties()
                                            {
                                                isSymbolDefaults = true,
                                                bcg = geoMapObject.SymbolDefaults?.Bcg ?? null,
                                                filters = Array.ConvertAll(geoMapObject.SymbolDefaults?.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(',') ?? new string[] { }, s => int.Parse(s)),
                                                style = geoMapObject.SymbolDefaults?.Style ?? null,
                                                size = geoMapObject.SymbolDefaults?.Size ?? null,
                                            }
                                        };
                                        geojson.features.Add(defaultFeature);
                                    }

                                    if (featureToBeAdded!= null) { geojson.features.Add(featureToBeAdded); }
                                    else { _errorLog.AppendLine($"{geoMap.Name}  {geoMapObject.Description}  SYMBOL  {element.Lat} {element.Lon}: \n\t- Was not added to geojson because <SymbolDefaults> was not found for this map.\n\n\n"); }
                                    break; 
                                }
                            case "Text": 
                                {
                                    var featureToBeAdded = CreateTextFeature(element);

                                    if (geoMapObject.TextDefaults != null && (geojson.features.Count() == 0 || (!geojson.features[0].properties.isTextDefaults ?? true)))
                                    {
                                        // Add defaults to feature if they exist
                                        Feature defaultFeature = new Feature()
                                        {
                                            type = "Feature",
                                            geometry = new Geometry()
                                            {
                                                type = "Point",
                                                coordinates = new List<dynamic>() { 90.0, 180.0 }
                                            },
                                            properties = new Properties()
                                            {
                                                isTextDefaults = true,
                                                bcg = geoMapObject.TextDefaults?.Bcg ?? null,
                                                filters = Array.ConvertAll(geoMapObject.TextDefaults?.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(',') ?? new string[] { }, s => int.Parse(s)),
                                                size = geoMapObject.TextDefaults?.Size ?? null,
                                                underline = geoMapObject.TextDefaults?.Underline ?? null,
                                                opaque = geoMapObject.TextDefaults?.Opaque ?? null,
                                                xOffset = geoMapObject.TextDefaults?.XOffset ?? null,
                                                yOffset = geoMapObject.TextDefaults?.YOffset ?? null,
                                            }
                                        };
                                        geojson.features.Add(defaultFeature);
                                    }

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
                                int totalFeatureCount = geojson.features.Count();
                                bool shouldAddDefaults = true;

                                if (totalFeatureCount <= 3)
                                {
                                    for (int i = 0; i < totalFeatureCount; i++)
                                    {
                                        if (!(geojson.features[i].properties.isLineDefaults ?? true))
                                        {
                                            shouldAddDefaults = false;
                                            break;
                                        }
                                    }
                                    if (shouldAddDefaults)
                                    {
                                        geojson.features.Add(AllLinesDefaults);
                                    }
                                }

                                geojson.features.Add(item);
                            }
                        }
                    }

                    // This fixes the duplicate description issue #148
                    if (File.Exists(file.FullName))
                    {
                        FeatureCollection inFile = JsonConvert.DeserializeObject<FeatureCollection>(File.ReadAllText(file.FullName));

                        if (HasSameDefaultValues(geojson, inFile))
                        {
                            // Combine the features with only ONE set of defaults
                            // TODO - This will place the defaults whereever in the file.... Not right at the top.  
                            geojson = CombineFeatureCollections(geojson, inFile);
                        }
                        else
                        {
                            // write the file with a '(#)' at the end of the name
                            string newFileName = GetUniqueFileName(file.FullName);
                            file = new FileInfo(newFileName);
                        }


                        //geojson.features.AddRange(inFile.features);
                    }

                    string jsonString = JsonConvert.SerializeObject(geojson, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });

                    //jsonString = PostProcess(jsonString);

                    File.WriteAllText(file.FullName, jsonString);
                }
            }
            if (!string.IsNullOrWhiteSpace(_errorLog.ToString()) && !string.IsNullOrEmpty(_errorLog.ToString()))
            {
                FileInfo errorFile = new FileInfo(Path.Combine(dirPath, "ErrorLog.txt"));
                //errorFile.Create();
                File.WriteAllText(errorFile.FullName, _errorLog.ToString());
            }
            File.WriteAllText(Path.Combine(dirPath, "GeoMapObject Properties.txt"), geoMapObjectLog.ToString());
        }

        private string GetUniqueFileName(string originalPath)
        {
            string directory = Path.GetDirectoryName(originalPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            int count = 1;
            string newPath = originalPath;
            while (File.Exists(newPath))
            {
                string tempFileName = string.Format("{0} ({1})", fileNameWithoutExtension, count++);
                newPath = Path.Combine(directory, tempFileName + extension);
            }
            return newPath;
        }

        private FeatureCollection CombineFeatureCollections(FeatureCollection obj1, FeatureCollection obj2)
        {
            FeatureCollection combined = new FeatureCollection();
            FeatureCollection output = new FeatureCollection();
            Dictionary<string, bool> addedDefaults = new Dictionary<string, bool>() { { "LineDefaults", false }, { "TextDefaults", false }, { "SymbolDefaults", false } };

            foreach (var featureCollection in new[] {obj1, obj2})
            {
                foreach (var feature in featureCollection.features)
                {
                    bool addFeature = true;
                    bool isDefaults = false;
                    if (feature.properties.isLineDefaults ?? false)
                    {
                        addFeature = !addedDefaults["LineDefaults"];
                        addedDefaults["LineDefaults"] = true;
                        isDefaults = true;
                    }

                    if (feature.properties.isTextDefaults ?? false)
                    {
                        addFeature = !addedDefaults["TextDefaults"];
                        addedDefaults["TextDefaults"] = true;
                        isDefaults = true;
                    }
                    
                    if (feature.properties.isSymbolDefaults ?? false)
                    {
                        addFeature = !addedDefaults["SymbolDefaults"];
                        addedDefaults["SymbolDefaults"] = true;
                        isDefaults = true;
                    }

                    if (addFeature && !isDefaults)
                    {
                        combined.features.Add(feature);
                    }

                    if (addFeature && isDefaults)
                    {
                        output.features.Add(feature);
                    }
                }
            }

            output.features.AddRange(combined.features);

            return output;

        }

        private bool HasSameDefaultValues(FeatureCollection obj1, FeatureCollection obj2)
        {
            Dictionary<string, Feature> objOneDefaults = new Dictionary<string, Feature>() { { "LineDefaults", null }, {"SymbolDefaults", null }, {"TextDefaults" , null }};
            Dictionary<string, Feature> objTwoDefaults = new Dictionary<string, Feature>() { { "LineDefaults", null }, {"SymbolDefaults", null }, {"TextDefaults" , null }};

            foreach (var item in obj1.features)
            {
                if (item.properties.isLineDefaults ?? false) { objOneDefaults["LineDefaults"] = item; }
                if (item.properties.isSymbolDefaults ?? false) { objOneDefaults["SymbolDefaults"] = item; }
                if (item.properties.isTextDefaults ?? false) { objOneDefaults["TextDefaults"] = item; }
            }

            foreach (var item in obj2.features)
            {
                if (item.properties.isLineDefaults ?? false) { objTwoDefaults["LineDefaults"] = item; }
                if (item.properties.isSymbolDefaults ?? false) { objTwoDefaults["SymbolDefaults"] = item; }
                if (item.properties.isTextDefaults ?? false) { objTwoDefaults["TextDefaults"] = item; }
            }

            foreach (var key in objOneDefaults.Keys)
            {
                if (objOneDefaults[key] != null && objTwoDefaults[key] != null)
                {
                    if (!objOneDefaults[key].Equals(objTwoDefaults[key])) return false;
                }
            }
            
            return true;
        }

        private Properties CreateProperties(Element element)
        {
            Properties elem_properties = new Properties();

            if (element.Filters != null && !string.IsNullOrWhiteSpace(element.Filters)) { elem_properties.filters = Array.ConvertAll(element.Filters.Replace(" ", string.Empty).Replace("\t", string.Empty).Split(','), s => int.Parse(s)); }
            if (element.Style != null) { elem_properties.style = element.Style; }
            if (element.Bcg != null) { elem_properties.bcg = element.Bcg; }
            if (element.Thickness != null) { elem_properties.thickness = element.Thickness; }
            if (element.Size != null) { elem_properties.size = element.Size; }
            if (element.Underline != null) { elem_properties.underline = element.Underline; }
            if (element.Opaque != null) { elem_properties.opaque = element.Opaque; }
            if (element.XOffset != null) { elem_properties.xOffset = element.XOffset; }
            if (element.YOffset != null) { elem_properties.yOffset = element.YOffset; }
            if (element.ZIndex != null) { elem_properties.zIndex = element.ZIndex; }

            return elem_properties;
        }

        private void CrossesAM(Element element, ref Feature currentFeature, ref List<Feature> featuresOutput, List<dynamic> coords, bool continutionOfFeature) 
        {
            if (!continutionOfFeature)
            {
                // Curent Feature Start coords Match Previous End Coords. Do not add Start coords.
                currentFeature.geometry.coordinates.Add(coords[0]);
            }
            currentFeature.geometry.coordinates.Add(coords[1]);
            currentFeature.properties = CreateProperties(element);

            featuresOutput.Add(currentFeature);

            currentFeature = new Feature()
            {
                properties = CreateProperties(element),
                geometry = new Geometry() { type = "LineString" }
            };
            currentFeature.geometry.coordinates.Add(coords[2]);
            currentFeature.geometry.coordinates.Add(coords[3]);
        }

        private void CheckFeatureProperties(Element element, List<Feature> featuresOutput, Element prevElement, List<dynamic> coords, ref Feature currentFeature)
        {
            // This function takes in the current currentFeature and the previous currentFeature.
            // if the Start Coords match the End Coords of the previous currentFeature BUT
            // the properties are DIFFERENT we need to Split the features instead of
            // combining them into one currentFeature group...

            // Properties for Previous and Current DO match (i.e. They are the same!)
            if (CreateProperties(prevElement).Equals(CreateProperties(element)))
            {
                currentFeature.geometry.coordinates.Add(coords[1]);
                return;
            }

            // Properties are different - Filters, Bcg, Size, and/or Thickness, etc.
            featuresOutput.Add(currentFeature);

            Feature newCurrentFeature = new Feature()
            {
                properties = CreateProperties(element),
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
                var coords = LatLonHelpers.CheckAMCrossing(element.StartLat, element.StartLon, element.EndLat, element.EndLon);
                if (coords.Count() == 4) { crossesAM = true; }

                if (prevElement == null)
                {
                    if (crossesAM)
                    {
                        CrossesAM(element, ref currentFeature, ref featuresOutput, coords, false);
                    }
                    else
                    {
                        currentFeature.properties = CreateProperties(element);
                        currentFeature.geometry.coordinates = coords;
                    }
                    prevElement = element;
                    continue;
                }
                else
                {
                    //if ((LatLonHelpers.CorrectIlleagleLon(element.StartLon).ToString() + " " + element.StartLat.ToString() != LatLonHelpers.CorrectIlleagleLon(prevElement.EndLon).ToString() + " " + prevElement.EndLat.ToString())
                    //    || (LatLonHelpers.CorrectIlleagleLon(element.EndLon).ToString() + " " + element.EndLat.ToString() != LatLonHelpers.CorrectIlleagleLon(prevElement.StartLon).ToString() + " " + prevElement.StartLat.ToString()))

                    var correctedStartLon = LatLonHelpers.CorrectIlleagleLon(element.StartLon).ToString();
                    var correctedEndLon = LatLonHelpers.CorrectIlleagleLon(element.EndLon).ToString();
                    var prevCorrectedEndLon = LatLonHelpers.CorrectIlleagleLon(prevElement.EndLon).ToString();
                    var prevCorrectedStartLon = LatLonHelpers.CorrectIlleagleLon(prevElement.StartLon).ToString();

                    var startEndMismatch = $"{correctedStartLon} {element.StartLat}" != $"{prevCorrectedEndLon} {prevElement.EndLat}";
                    var endStartMismatch = $"{correctedEndLon} {element.EndLat}" != $"{prevCorrectedStartLon} {prevElement.StartLat}";

                    // If start coords do NOT match end coords of previous element
                    if (startEndMismatch || endStartMismatch)
                    {
                        if (crossesAM)
                        {
                            if (currentFeature.geometry.coordinates.Count() > 0)
                            {
                                featuresOutput.Add(currentFeature);
                                currentFeature = new Feature()
                                {
                                    properties = CreateProperties(element),
                                    geometry = new Geometry() { type = "LineString" }
                                };
                            }

                            CrossesAM(element, ref currentFeature, ref featuresOutput, coords, false);
                        }
                        else
                        {
                            if (currentFeature.geometry.coordinates.Count() > 0)
                            {
                                featuresOutput.Add(currentFeature);
                            }

                            currentFeature = new Feature()
                            {
                                properties = CreateProperties(element),
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
                            CrossesAM(element, ref currentFeature, ref featuresOutput, coords, true);
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

        private Feature CreateTextFeature(Element element)
        {
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

        private Feature CreateSymbolFeature(Element element)
        {
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

    public interface IStartEndLat
    {
        string StartLat { get; set; }
        string StartLon { get; set; }
        string EndLat { get; set; }
        string EndLon { get; set; }
    }
}
