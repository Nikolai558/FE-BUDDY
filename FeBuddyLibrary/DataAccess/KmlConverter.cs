using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeBuddyLibrary.Dxf.Data;
using FeBuddyLibrary.Dxf.Models;
using FeBuddyLibrary.Helpers;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;

namespace FeBuddyLibrary.DataAccess
{
    public class KmlConverter
    {
        // TODO - TO USE THIS USE THE FOLLOWING: 
        //      KmlConverter test = new KmlConverter(@"C:\Users\Desktop\KML\ZLC SECTOR.kml", @"C:\Users\Desktop\KML\ZLC SECTOR.SCT2");
        //      test.ConvertSctToKml();

        private KmlFile _kml;
        private string _kmlFilePath;

        private Document _kmlDocument;

        private SctFileModel _sctFileModel;
        private string _sctFileText;
        private string _sctFilePath;

        private List<string> _currentStyleIds;

        private DataFunctions _df;

        public KmlConverter(string kmlFilePath, string sectorFilePath)
        {
            _kmlFilePath = kmlFilePath;
            _sctFilePath = sectorFilePath;
            _df = new DataFunctions();
            _kmlDocument = new Document();
            _currentStyleIds = new List<string>();
        }

        public void ConvertSctToKml(string section)
        {
            _sctFileModel = _df.ReadSctFile(_sctFilePath);

            switch (section)
            {
                case "ALL" :
                    {
                        AddDefineSection();
                        AddInfoSection();
                        AddVorSection();
                        AddNdbSection();
                        AddAirportSection();
                        AddRunwaySection();
                        AddFixesSection();
                        AddArtccModelsSection();
                        AddDiagramSections();
                        AddLabelSection();
                        AddRegionSection();
                        AddGeoSection();
                        break;
                    }
                case "Colors": { AddDefineSection(); break; }
                case "[INFO]": { AddInfoSection(); break; }
                case "[VOR]": { AddVorSection(); break; }
                case "[NDB]": { AddNdbSection(); break; }
                case "[AIRPORT]": { AddAirportSection(); break; }
                case "[RUNWAY]": { AddRunwaySection(); break; }
                case "[FIXES]": { AddFixesSection(); break; }
                case "[ARTCC]": { AddArtccModelsSection("ARTCC"); break; }
                case "[ARTCC LOW]": { AddArtccModelsSection("ARTCC LOW"); break; }
                case "[ARTCC HIGH]": { AddArtccModelsSection("ARTCC HIGH"); break; }
                case "[LOW AIRWAY]": { AddArtccModelsSection("LOW AIRWAY"); break; }
                case "[HIGH AIRWAY]": { AddArtccModelsSection("HIGH AIRWAY"); break; }
                case "[SID]": { AddDiagramSections("SID"); break; }
                case "[STAR]": { AddDiagramSections("STAR"); break; }
                case "[LABELS]": { AddLabelSection(); break; }
                case "[REGIONS]": { AddRegionSection(); break; }
                case "[GEO]": { AddGeoSection(); break; }
                default:
                    break;
            }

            SaveKML();
        }

        private void AddGeoSection()
        {
            Folder geoFolder = new Folder();
            geoFolder.Name = "[GEO]";

            int count = 0;
            foreach (SctGeoModel item in _sctFileModel.SctGeoSection)
            {
                LineString line = new LineString();
                line.Coordinates = new CoordinateCollection();
                line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLon, false, false, _df._navaidPositions), false))));
                line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLon, false, false, _df._navaidPositions), false))));
                Placemark geoPlacemark = new Placemark();
                geoPlacemark.Geometry = line;
                geoPlacemark.Name = "GeoLine_" + count;
                geoPlacemark.Visibility = false;

                geoFolder.AddFeature(geoPlacemark);
                count += 1;
            }

            geoFolder.Visibility = false;
            _kmlDocument.AddFeature(geoFolder);
        }

        private void AddRegionSection()
        {
            Folder regionsFolder = new Folder();
            regionsFolder.Name = "[REGIONS]";

            foreach (var item in _sctFileModel.SctRegionsSection)
            {

                bool undefinedColor = false;
                if (!_currentStyleIds.Contains(item.RegionColorName) && !_currentStyleIds.Contains("Undefined_" + item.RegionColorName))
                {
                    AddColorStyle("Undefined_" + item.RegionColorName, item.RegionColorName);
                    undefinedColor = true;
                }

                if (_currentStyleIds.Contains("Undefined_" + item.RegionColorName))
                {
                    undefinedColor = true;
                }

                CoordinateCollection coordinates = new CoordinateCollection();

                // Starting Point

                coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lon, false, false, _df._navaidPositions), false))));

                foreach (var addlCoords in item.AdditionalRegionInfo)
                {
                    coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(addlCoords.Lat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(addlCoords.Lon, false, false, _df._navaidPositions), false))));
                }

                // Starting point but at the end of the list to complete the region. 
                coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lon, false, false, _df._navaidPositions), false))));

                OuterBoundary outerBoundary = new OuterBoundary();
                outerBoundary.LinearRing = new LinearRing();
                outerBoundary.LinearRing.Coordinates = coordinates;

                Polygon polygon = new Polygon();
                polygon.Tessellate = true;
                polygon.OuterBoundary = outerBoundary;

                Placemark regionPlacemark = new Placemark();
                regionPlacemark.Name = item.RegionColorName;
                regionPlacemark.Geometry = polygon;
                regionPlacemark.Visibility = false;

                if (undefinedColor)
                {
                    regionPlacemark.StyleUrl = new Uri("#Undefined_" + item.RegionColorName, UriKind.Relative);
                }
                else
                {
                    regionPlacemark.StyleUrl = new Uri("#" + item.RegionColorName, UriKind.Relative);
                }
                //regionPlacemark.StyleUrl = new Uri($"#{item.RegionColorName}", UriKind.Relative); 

                regionsFolder.AddFeature(regionPlacemark);
            }

            regionsFolder.Visibility = false;
            _kmlDocument.AddFeature(regionsFolder);
        }

        private void AddLabelSection()
        {
            Folder leablesFolder = new Folder();
            leablesFolder.Name = "[LABELS]";

            foreach (SctLabelModel item in _sctFileModel.SctLabelSection)
            {
                bool undefinedColor = false;
                if (!_currentStyleIds.Contains(item.Color) && !_currentStyleIds.Contains("Undefined_" + item.Color))
                {
                    AddColorStyle("Undefined_" + item.Color, item.Color);
                    undefinedColor = true;
                }
                if (_currentStyleIds.Contains("Undefined_" + item.Color))
                {
                    undefinedColor = true;
                }

                var point = new Point();
                Placemark labelPlaceMark = new Placemark();
                labelPlaceMark.Name = item.LabelText ;
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lon, false, false, _df._navaidPositions), false)));
                labelPlaceMark.Geometry = point;
                labelPlaceMark.Visibility = false;

                if (undefinedColor)
                {
                    labelPlaceMark.StyleUrl = new Uri("#Undefined_" + item.Color, UriKind.Relative);
                }
                else
                {
                    labelPlaceMark.StyleUrl = new Uri("#" + item.Color, UriKind.Relative);
                }

                leablesFolder.AddFeature(labelPlaceMark);
            }
            leablesFolder.Visibility = false;
            _kmlDocument.AddFeature(leablesFolder);
        }

        private void AddDiagramSections(string onlyThisSection = null)
        {
            Dictionary<string, List<SctSidStarModel>> collections = new Dictionary<string, List<SctSidStarModel>>
            {
                {"SID", _sctFileModel.SctSidSection},
                {"STAR", _sctFileModel.SctStarSection}
            };
            foreach (var collectionName in collections.Keys)
            {
                if (onlyThisSection != null && onlyThisSection != collectionName)
                {
                    continue;
                }
                Folder collectionFolder = new Folder();
                collectionFolder.Name = $"[{collectionName}]";
                
                foreach (SctSidStarModel item in collections[collectionName])
                {
                    bool undefinedColor = false;
                    if (!_currentStyleIds.Contains(item.Color) && !_currentStyleIds.Contains("Undefined_" + item.Color))
                    {
                        AddColorStyle("Undefined_" + item.Color, item.Color);
                        undefinedColor = true;
                    }
                    if (_currentStyleIds.Contains("Undefined_" + item.Color))
                    {
                        undefinedColor = true;
                    }

                    Folder diagramFolder = new Folder();
                    diagramFolder.Name = item.DiagramName;
                    int collectionCount = 0;
                    
                    LineString line = new LineString();
                    line.Coordinates = new CoordinateCollection();
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLon, false, false, _df._navaidPositions), false))));
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLon, false, false, _df._navaidPositions), false))));
                    Placemark itemPlacemark = new Placemark();
                    itemPlacemark.Geometry = line;
                    itemPlacemark.Name = "LineCollection_" + collectionCount;
                    itemPlacemark.Visibility = false;

                    if (undefinedColor)
                    {
                        itemPlacemark.StyleUrl = new Uri("#Undefined_" + item.Color, UriKind.Relative);
                    }
                    else
                    {
                        itemPlacemark.StyleUrl = new Uri("#" + item.Color, UriKind.Relative);
                    }

                    diagramFolder.AddFeature(itemPlacemark);

                    LineString additionalLines = new LineString();
                    additionalLines.Coordinates = new CoordinateCollection();
                    Placemark additionalLinesPlacemark = new Placemark();

                    string tempcolor = "";
                    string previousEndingCoord = null;
                    foreach (var lineSeg in item.AdditionalLines)
                    {
                        undefinedColor = false;

                        if (previousEndingCoord != null && previousEndingCoord != $"{lineSeg.StartLat} {lineSeg.StartLon}")
                        {
                            if (!_currentStyleIds.Contains(lineSeg.Color) && !_currentStyleIds.Contains("Undefined_" + lineSeg.Color))
                            {
                                AddColorStyle("Undefined_" + lineSeg.Color, lineSeg.Color);
                                undefinedColor = true;
                            }
                            if (_currentStyleIds.Contains("Undefined_" + lineSeg.Color))
                            {
                                undefinedColor = true;
                            }

                            collectionCount += 1;
                            // starting line segment does not match last ending point.
                            additionalLinesPlacemark.Geometry = additionalLines;
                            additionalLinesPlacemark.Name = "LineCollection_" + collectionCount;
                            additionalLinesPlacemark.Visibility = false;
                            if (undefinedColor)
                            {
                                additionalLinesPlacemark.StyleUrl = new Uri("#Undefined_" + lineSeg.Color, UriKind.Relative);
                            }
                            else
                            {
                                additionalLinesPlacemark.StyleUrl = new Uri("#" + lineSeg.Color, UriKind.Relative);
                            }

                            diagramFolder.AddFeature(additionalLinesPlacemark);

                            additionalLines = new LineString();
                            additionalLines.Coordinates = new CoordinateCollection();
                            additionalLinesPlacemark = new Placemark();
                        }

                        additionalLines.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSeg.StartLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSeg.StartLon, false, false, _df._navaidPositions), false))));
                        additionalLines.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSeg.EndLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSeg.EndLon, false, false, _df._navaidPositions), false))));
                        previousEndingCoord = lineSeg.EndLat + " " + lineSeg.EndLon;
                        tempcolor = lineSeg.Color;
                    }
                    collectionCount += 1;
                    // starting line segment does not match last ending point.
                    additionalLinesPlacemark.Geometry = additionalLines;
                    additionalLinesPlacemark.Name = "LineCollection_" + collectionCount;
                    additionalLinesPlacemark.Visibility = false;

                    if (undefinedColor)
                    {
                        additionalLinesPlacemark.StyleUrl = new Uri("#Undefined_" + tempcolor, UriKind.Relative);
                    }
                    else
                    {
                        additionalLinesPlacemark.StyleUrl = new Uri("#" + tempcolor, UriKind.Relative);
                    }

                    diagramFolder.AddFeature(additionalLinesPlacemark);

                    collectionFolder.AddFeature(diagramFolder);
                }
                collectionFolder.Visibility = false;
                _kmlDocument.AddFeature(collectionFolder);
            }
        }

        private void AddArtccModelsSection(string onlyThisSection = null)
        {
            Dictionary<string, List<SctArtccModel>> collections = new Dictionary<string, List<SctArtccModel>>
            {
                {"ARTCC", _sctFileModel.SctArtccSection },
                {"ARTCC LOW", _sctFileModel.SctArtccLowSection},
                {"ARTCC HIGH", _sctFileModel.SctArtccHighSection},
                {"LOW AIRWAY", _sctFileModel.SctLowAirwaySection},
                {"HIGH AIRWAY", _sctFileModel.SctHighAirwaySection}
            };

            foreach (var collectionName in collections.Keys)
            {
                if (onlyThisSection != null && onlyThisSection != collectionName)
                {
                    continue;
                }

                Folder collectionFolder = new Folder();
                collectionFolder.Name = $"[{collectionName}]";
                foreach (var item in collections[collectionName])
                {
                    LineString line = new LineString();
                    line.Coordinates = new CoordinateCollection();
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLon, false, false, _df._navaidPositions), false))));
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLon, false, false, _df._navaidPositions), false))));
                    Placemark itemPlacemark = new Placemark();
                    itemPlacemark.Geometry = line;
                    itemPlacemark.Name = item.Name;
                    itemPlacemark.Visibility = false;

                    collectionFolder.AddFeature(itemPlacemark);
                }
                collectionFolder.Visibility = false;
                _kmlDocument.AddFeature(collectionFolder);
            }
        }

        private void AddFixesSection()
        {
            Folder fixFolder = new Folder();
            fixFolder.Name = "[FIXES]";

            foreach (SctFixesModel item in _sctFileModel.SctFixesSection)
            {
                var point = new Point();
                Placemark fixPlaceMark = new Placemark();
                fixPlaceMark.Name = item.FixName;
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lon, false, false, _df._navaidPositions), false)));
                fixPlaceMark.Geometry = point;
                fixPlaceMark.Visibility = false;
                fixFolder.AddFeature(fixPlaceMark);
            }
            fixFolder.Visibility = false;
            _kmlDocument.AddFeature(fixFolder);
        }

        private void AddRunwaySection()
        {
            Folder runwayFolder = new Folder();
            runwayFolder.Name = "[RUNWAY]";

            foreach (SctRunwayModel item in _sctFileModel.SctRunwaySection)
            {
                var description = new Description();

                LineString line = new LineString();
                line.Coordinates = new CoordinateCollection();
                line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLon, false, false, _df._navaidPositions), false))));
                line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLon, false, false, _df._navaidPositions), false))));
                Placemark runwayPlacemark = new Placemark();
                runwayPlacemark.Geometry = line;
                runwayPlacemark.Name = item.RunwayNumber + "-" + item.OppositeRunwayNumber;
                description.Text = item.AllInfo.Split(" ")[0] + " " + item.AllInfo.Split(" ")[1] + " " + item.AllInfo.Split(" ")[2] + " " + item.AllInfo.Split(" ")[3] + " ";
                runwayPlacemark.Description = description;
                runwayPlacemark.Visibility = false;

                runwayFolder.AddFeature(runwayPlacemark);
            }

            runwayFolder.Visibility = false;
            _kmlDocument.AddFeature(runwayFolder);
        }

        private void AddAirportSection()
        {
            Folder airportFolder = new Folder();
            airportFolder.Name = "[AIRPORT]";

            foreach (SctAirportModel item in _sctFileModel.SctAirportSection)
            {
                var description = new Description();
                var point = new Point();
                Placemark airportPlaceMark = new Placemark();
                airportPlaceMark.Name = item.Id;
                description.Text = item.AllInfo.Split(" ")[0] + " " + item.AllInfo.Split(" ")[1];
                airportPlaceMark.Description = description;
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lon, false, false, _df._navaidPositions), false)));
                airportPlaceMark.Geometry = point;
                airportPlaceMark.Visibility = false;
                airportFolder.AddFeature(airportPlaceMark);
            }
            airportFolder.Visibility = false;
            _kmlDocument.AddFeature(airportFolder);
        }

        private void AddVorSection()
        {
            Folder vorFolder = new Folder();
            vorFolder.Name = "[VOR]";
            foreach (VORNDBModel item in _sctFileModel.SctVORSection)
            {
                var description = new Description();
                var point = new Point();
                Placemark vorPlaceMark = new Placemark();
                vorPlaceMark.Name = item.Id + " - " + item.Frequency;
                description.Text = item.Comments ?? "";
                vorPlaceMark.Description = description;
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lon, false, false, _df._navaidPositions), false)));
                vorPlaceMark.Geometry = point;
                vorPlaceMark.Visibility = false;
                vorFolder.AddFeature(vorPlaceMark);
            }
            vorFolder.Visibility = false;
            _kmlDocument.AddFeature(vorFolder);
        }

        private void AddNdbSection()
        {
            Folder ndbFolder = new Folder();
            ndbFolder.Name = "[NDB]";

            foreach (VORNDBModel item in _sctFileModel.SctNDBSection)
            {
                var description = new Description();
                var point = new Point();
                Placemark NDBPlaceMark = new Placemark();
                NDBPlaceMark.Name = item.Id + " - " + item.Frequency;
                description.Text = item.Comments ?? "";
                NDBPlaceMark.Description = description;
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lon, false, false, _df._navaidPositions), false)));
                NDBPlaceMark.Geometry = point;
                NDBPlaceMark.Visibility = false;
                ndbFolder.AddFeature(NDBPlaceMark);
            }
            ndbFolder.Visibility = false;
            _kmlDocument.AddFeature(ndbFolder);
        }

        private void SaveKML()
        {
            _kml = KmlFile.Create(_kmlDocument, true);
            using (var stream = File.Create(_kmlFilePath))
            {
                _kml.Save(stream);
            }

            //var kmz = KmzFile.Create(_kml);
            //using (var stream = File.Create(_kmlFilePath.Replace(".kml", ".kmz")))
            //{
            //    kmz.Save(stream);
            //}

        }

        private void AddInfoSection()
        {
            var point = new Point();
            point.Coordinate = new Vector(90, 0);

            Placemark infoPlacemark = new Placemark();
            infoPlacemark.Name = "Info";

            Description description = new Description();
            description.Text = _sctFileModel.SctInfoSection.AllInfo;
            infoPlacemark.Description= description;
            infoPlacemark.Geometry = point;
            infoPlacemark.Visibility = false;

            Folder infoFolder = new Folder();
            infoFolder.Name = "[INFO]";
            infoFolder.AddFeature(infoPlacemark);
            infoFolder.Visibility = false;

            _kmlDocument.AddFeature(infoFolder);
        }

        private void AddColorStyle(string name, string colorInt)
        {
            if (_currentStyleIds.Contains(name) || _currentStyleIds.Contains("Undefined_" + name))
            {
                return;
            }

            PolygonStyle pstyle = new PolygonStyle();
            pstyle.Color = ConvertColor(colorInt);
            pstyle.Id = name;

            Style style = new Style();
            style.Id = name;
            style.Line = new LineStyle() { Id = name, Color = ConvertColor(colorInt) };
            style.Polygon = pstyle;
            _kmlDocument.AddStyle(style);
            _currentStyleIds.Add(name);
        }

        private Color32 ConvertColor(string colorInt, int alpha = 255)
        {
            // Color Math found by https://webtools.kusternet.ch/color; https://vrc.rosscarlson.dev/docs/doc.php?page=appendix_g

            int r;
            int g;
            int b;
            int a;

            int color;
            bool isColor = int.TryParse(colorInt, out color);

            if (isColor)
            {
                r = color % 256;
                g = (color / 256) % 256;
                b = (color / 65536) % 256;
                a = alpha;
            }
            else
            {
                r = 255;
                g = 255;
                b = 255;
                a = alpha;
            }

            return new Color32((byte)a, (byte)b, (byte)g, (byte)r);
        }

        private void AddDefineSection()
        {
            var point = new Point();
            point.Coordinate = new Vector(90, 0);


            StringBuilder sb = new StringBuilder();
            foreach (var item in _sctFileModel.SctFileColors)
            {
                AddColorStyle(item.Name, item.ColorCode);
                sb.AppendLine(item.AllInfo);
            }

            Placemark ColorsPlacemark = new Placemark();
            ColorsPlacemark.Name = "Colors";
            Description description = new Description();
            description.Text = sb.ToString();

            ColorsPlacemark.Description = description;
            ColorsPlacemark.Geometry = point;
            ColorsPlacemark.Visibility = false;

            Folder colorFolder = new Folder();
            colorFolder.Name = "[Colors]";
            colorFolder.AddFeature(ColorsPlacemark);
            colorFolder.Visibility = false;


            _kmlDocument.AddFeature(colorFolder);
        }
    }
}
