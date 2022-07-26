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
        private StringBuilder sctFileStringBuilder;

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
            _kmlDocument = new Document();

            _df = new DataFunctions();
            _currentStyleIds = new List<string>();
        }

        public void ConvertKmltoSCT()
        {
            using (FileStream stream = File.OpenRead(_kmlFilePath))
            {
                _kml = KmlFile.Load(stream);
            }

            CreateSctFile();
        }

        private void CreateSctFile()
        {
            _sctFileModel = new SctFileModel();

            _kmlDocument = (Document)_kml.Root;

            sctFileStringBuilder = new StringBuilder();

            foreach (var item in _kmlDocument.Features)
            {
                ParseKml(item.Name);
            }
        }

        private void ParseKml(string section)
        {
            switch (section)
            {
                case "[Colors]":  { AddColorsSct(); break; }
                case "[INFO]": { AddInfoSct(); break; }
                case "[VOR]": { AddVorNdbSct("VOR"); break; }
                case "[NDB]": { AddVorNdbSct("NDB"); break; }
                case "[AIRPORT]": { AddAptSct(); break; }
                case "[RUNWAY]": { AddRwySct(); break; }
                case "[FIXES]": { AddFixesSct(); break; }
                case "[ARTCC]": { AddArtccModelSct("ARTCC"); break; }
                case "[ARTCC LOW]": { AddArtccModelSct("ARTCC LOW"); break; }
                case "[ARTCC HIGH]": { AddArtccModelSct("ARTCC HIGH"); break; }
                case "[LOW AIRWAY]": { AddArtccModelSct("LOW AIRWAY"); break; }
                case "[HIGH AIRWAY]": { AddArtccModelSct("HIGH AIRWAY"); break; }
                case "[SID]": { AddSidStarSct("SID"); break; }
                case "[STAR]": { AddSidStarSct("STAR"); break; }
                case "[LABELS]": { AddLabelsSct(); break; }
                case "[REGIONS]": { AddRegionSct(); break; }
                case "[GEO]": { AddGeoSct(); break; }
                default:
                    break;
            }
        }

        private void AddRegionSct()
        {
            sctFileStringBuilder.AppendLine($"\n[REGIONS]");

            var regionFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == "[REGIONS]").First(); 
            foreach (Feature item in regionFolder.Features)
            {
                SctRegionModel featureModl = new SctRegionModel();

                featureModl.RegionColorName = item.Name;
                featureModl.AdditionalRegionInfo = new List<RegionPolygonPoints>();

                var itemPlacemark = (Placemark)item;
                var itemPolygon = (Polygon)itemPlacemark.Geometry;
                LinearRing itemPoint = itemPolygon.OuterBoundary.LinearRing;

                int count = 0;
                foreach (Vector coord in itemPoint.Coordinates)
                {
                    if (count == 0)
                    {
                        featureModl.Lat = LatLonHelpers.CreateDMS(coord.Latitude, true);
                        featureModl.Lon = LatLonHelpers.CreateDMS(coord.Longitude, false);
                        count += 1;
                    }
                    else
                    {
                        RegionPolygonPoints addlPoint = new RegionPolygonPoints()
                        {
                            Lat = LatLonHelpers.CreateDMS(coord.Latitude, true),
                            Lon = LatLonHelpers.CreateDMS(coord.Longitude,false)
                        };
                        featureModl.AdditionalRegionInfo.Add(addlPoint);
                    }
                }
                sctFileStringBuilder.AppendLine(featureModl.AllInfo);
            }
            SaveSctFile();
        }

        private void AddSidStarSct(string section)
        {
            sctFileStringBuilder.AppendLine($"\n[{section}]");

            // [SID] (folder)
            var sectionFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == $"[{section}]").First();
            foreach (Folder diagramFolder in sectionFolder.Features)
            {
                // Diagrams (Folder)
                int count = 0;
                SctSidStarModel featureModel = new SctSidStarModel()
                {
                    DiagramName = diagramFolder.Name,
                    AdditionalLines = new List<SctAditionalDiagramLineSegments>()
                };
                foreach (Feature item in diagramFolder.Features)
                {
                    var itemPlacemark = (Placemark)item;
                    var itemPoint = (LineString)itemPlacemark.Geometry;
                    // Line collection (Path)
                    if (count == 0)
                    {
                        var slat = LatLonHelpers.CreateDMS(itemPoint.Coordinates.First().Latitude, true);
                        var slon = LatLonHelpers.CreateDMS(itemPoint.Coordinates.First().Longitude, false);

                        var elat = LatLonHelpers.CreateDMS(itemPoint.Coordinates.Last().Latitude, true);
                        var elon = LatLonHelpers.CreateDMS(itemPoint.Coordinates.Last().Longitude, false);

                        featureModel.StartLat = slat;
                        featureModel.StartLon = slon;

                        featureModel.EndLat = elat;
                        featureModel.EndLon = elon;
                        count += 1;
                    }
                    else
                    {
                        bool startingCoords = true;
                        SctAditionalDiagramLineSegments featAditionalLines = new SctAditionalDiagramLineSegments() ;
                        foreach (Vector coords in itemPoint.Coordinates)
                        {
                            if (startingCoords)
                            {
                                featAditionalLines.StartLat = LatLonHelpers.CreateDMS(coords.Latitude, true);
                                featAditionalLines.StartLon = LatLonHelpers.CreateDMS(coords.Longitude, false);
                                startingCoords = false;
                            }
                            else
                            {
                                featAditionalLines.EndLat = LatLonHelpers.CreateDMS(coords.Latitude, true);
                                featAditionalLines.EndLon = LatLonHelpers.CreateDMS(coords.Longitude, false);
                                startingCoords = true;

                                featureModel.AdditionalLines.Add(featAditionalLines);
                                featAditionalLines = new SctAditionalDiagramLineSegments();
                            }
                        }
                    }
                }
                
                sctFileStringBuilder.AppendLine(featureModel.AllInfo);
            }

            SaveSctFile();
        }

        private void AddGeoSct()
        {
            sctFileStringBuilder.AppendLine($"\n[GEO]");

            var labelsFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == $"[GEO]").First();
            foreach (Feature item in labelsFolder.Features)
            {
                SctGeoModel featureModel = new SctGeoModel();

                var itemPlacemark = (Placemark)item;
                var itemPoint = (LineString)itemPlacemark.Geometry;

                var slat = LatLonHelpers.CreateDMS(itemPoint.Coordinates.First().Latitude, true);
                var slon = LatLonHelpers.CreateDMS(itemPoint.Coordinates.First().Longitude, false);

                var elat = LatLonHelpers.CreateDMS(itemPoint.Coordinates.Last().Latitude, true);
                var elon = LatLonHelpers.CreateDMS(itemPoint.Coordinates.Last().Longitude, false);

                featureModel.StartLat = slat;
                featureModel.StartLon = slon;

                featureModel.EndLat = elat;
                featureModel.EndLon = elon;

                sctFileStringBuilder.AppendLine(featureModel.AllInfo);
            }

            SaveSctFile();
        }

        private void AddLabelsSct()
        {
            sctFileStringBuilder.AppendLine($"\n[LABELS]");

            var labelsFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == $"[LABELS]").First();
            foreach (Feature item in labelsFolder.Features)
            {
                SctLabelModel featureModel = new SctLabelModel() 
                {
                    LabelText = item.Name,
                };

                var itemPlacemark = (Placemark)item;
                var itemPoint = (Point)itemPlacemark.Geometry;
                var lat = LatLonHelpers.CreateDMS(itemPoint.Coordinate.Latitude, true);
                var lon = LatLonHelpers.CreateDMS(itemPoint.Coordinate.Longitude, false);

                featureModel.Lat = lat;
                featureModel.Lon = lon;

                sctFileStringBuilder.AppendLine(featureModel.AllInfo);
            }

            SaveSctFile();
        }

        private void AddArtccModelSct(string section)
        {
            sctFileStringBuilder.AppendLine($"\n[{section}]");

            var sectionFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == $"[{section}]").First();
            foreach (Feature item in sectionFolder.Features)
            {
                SctArtccModel featureModel = new SctArtccModel()
                {
                    Name = item.Name
                };

                var itemPlacemark = (Placemark)item;
                var itemPoint = (LineString)itemPlacemark.Geometry;

                var slat = LatLonHelpers.CreateDMS(itemPoint.Coordinates.First().Latitude, true);
                var slon = LatLonHelpers.CreateDMS(itemPoint.Coordinates.First().Longitude, false);

                var elat = LatLonHelpers.CreateDMS(itemPoint.Coordinates.Last().Latitude, true);
                var elon = LatLonHelpers.CreateDMS(itemPoint.Coordinates.Last().Longitude, false);

                featureModel.StartLat = slat;
                featureModel.StartLon = slon;

                featureModel.EndLat = elat;
                featureModel.EndLon = elon;

                sctFileStringBuilder.AppendLine(featureModel.AllInfo);
            }

            SaveSctFile();
        }

        private void AddFixesSct()
        {
            sctFileStringBuilder.AppendLine($"\n[FIXES]");

            var fixesFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == $"[FIXES]").First();
            foreach (Feature item in fixesFolder.Features)
            {
                SctFixesModel featureModel = new SctFixesModel()
                {
                    FixName = item.Name,
                };

                var itemPlacemark = (Placemark)item;
                var itemPoint = (Point)itemPlacemark.Geometry;
                var lat = LatLonHelpers.CreateDMS(itemPoint.Coordinate.Latitude, true);
                var lon = LatLonHelpers.CreateDMS(itemPoint.Coordinate.Longitude, false);

                featureModel.Lat = lat;
                featureModel.Lon = lon;

                sctFileStringBuilder.AppendLine(featureModel.AllInfo);
            }

            SaveSctFile();
        }

        private void AddRwySct()
        {
            sctFileStringBuilder.AppendLine($"\n[RUNWAY]");

            var runwayFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == $"[RUNWAY]").First();
            foreach (Feature item in runwayFolder.Features)
            {
                SctRunwayModel featureModel = new SctRunwayModel()
                {
                    RunwayNumber = item.Description.Text[(item.Description.Text.IndexOf($"[RWY]") + 5)..item.Description.Text.IndexOf($"[/RWY]")],
                    OppositeRunwayNumber = item.Description.Text[(item.Description.Text.IndexOf($"[OPPRWY]") + 8)..item.Description.Text.IndexOf($"[/OPPRWY]")],
                    MagRunwayHeading = item.Description.Text[(item.Description.Text.IndexOf($"[RWYHDG]") + 8)..item.Description.Text.IndexOf($"[/RWYHDG]")],
                    OppositeMagRunwayHeading = item.Description.Text[(item.Description.Text.IndexOf($"[OPPRWYHDG]") + 11)..item.Description.Text.IndexOf($"[/OPPRWYHDG]")],
                };


                var itemPlacemark = (Placemark)item;
                var itemPoint = (LineString)itemPlacemark.Geometry;

                var slat = LatLonHelpers.CreateDMS(itemPoint.Coordinates.First().Latitude, true);
                var slon = LatLonHelpers.CreateDMS(itemPoint.Coordinates.First().Longitude, false);

                var elat = LatLonHelpers.CreateDMS(itemPoint.Coordinates.Last().Latitude, true);
                var elon = LatLonHelpers.CreateDMS(itemPoint.Coordinates.Last().Longitude, false);

                featureModel.StartLat = slat;
                featureModel.StartLon = slon;

                featureModel.EndLat = elat;
                featureModel.EndLon = elon;

                sctFileStringBuilder.AppendLine(featureModel.AllInfo);
            }

            SaveSctFile();
        }

        private void AddAptSct()
        {
            sctFileStringBuilder.AppendLine($"\n[AIRPORT]");

            var aptFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == $"[AIRPORT]").First();
            foreach (Feature item in aptFolder.Features)
            {
                SctAirportModel featureModel = new SctAirportModel()
                {
                    Id = item.Name,
                    Frequency = item.Description.Text[(item.Description.Text.IndexOf($"[APTFREQ]")+ 9)..item.Description.Text.IndexOf($"[/APTFREQ]")],
                    // TODO - Fix this to show the actual airspace from [APTSPACE]...
                    Airspace = "",
                };

                var itemPlacemark = (Placemark)item;
                var itemPoint = (Point)itemPlacemark.Geometry;
                var lat = LatLonHelpers.CreateDMS(itemPoint.Coordinate.Latitude, true);
                var lon = LatLonHelpers.CreateDMS(itemPoint.Coordinate.Longitude, false);

                featureModel.Lat = lat;
                featureModel.Lon = lon;

                sctFileStringBuilder.AppendLine(featureModel.AllInfo);
            }

            SaveSctFile();
        }

        private void AddColorsSct()
        {
            var colorsFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == "[Colors]").First();
            var colorsPlacemark = (Feature)colorsFolder.Features.Where((x) => x.Name == "Colors").First();
            sctFileStringBuilder.Append(colorsPlacemark.Description.Text);
            SaveSctFile();
        }

        private void AddInfoSct()
        {
            var infoFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == "[INFO]").First();
            var infoPlacemark = (Feature)infoFolder.Features.Where((x) => x.Name == "Info").First();
            sctFileStringBuilder.AppendLine(infoPlacemark.Description.Text);
            SaveSctFile();
        }

        private void AddVorNdbSct(string section) 
        {
            sctFileStringBuilder.AppendLine($"\n[{section}]");

            var sectionFolder = (Folder)_kmlDocument.Features.Where((x) => x.Name == $"[{section}]").First();
            foreach (Feature item in sectionFolder.Features)
            {
                VORNDBModel featureModel = new VORNDBModel()
                {
                    Id = item.Name,
                    Frequency = item.Description.Text[(item.Description.Text.IndexOf($"[{section}FREQ]") + section.Length + 6)..item.Description.Text.IndexOf($"[/{section}FREQ]")],
                };

                var itemPlacemark = (Placemark)item;
                var itemPoint = (Point)itemPlacemark.Geometry;
                var lat = LatLonHelpers.CreateDMS(itemPoint.Coordinate.Latitude, true);
                var lon = LatLonHelpers.CreateDMS(itemPoint.Coordinate.Longitude, false);

                featureModel.Lat = lat;
                featureModel.Lon = lon;

                sctFileStringBuilder.AppendLine(featureModel.AllInfo);
            }

            SaveSctFile();
        }

        private void SaveSctFile()
        {
            File.WriteAllText(_sctFilePath.Split('.')[0] + "-converted.sct2", sctFileStringBuilder.ToString());
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

            foreach (SctRegionModel item in _sctFileModel.SctRegionsSection)
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
                Description description = new Description();
                description.Text = $"[FEB][COMMENTS]{item.Comments}[/COMMENTS][/FEB]";
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
                labelPlaceMark.Description = description;

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

                    if (additionalLines.Coordinates.Count > 0)
                    {
                        diagramFolder.AddFeature(additionalLinesPlacemark);
                    }

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
                    Description description = new Description();
                    description.Text = $"[FEB][COMMENTS]{item.Comments}[/COMMENTS][/FEB]";
                    LineString line = new LineString();
                    line.Coordinates = new CoordinateCollection();
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.StartLon, false, false, _df._navaidPositions), false))));
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.EndLon, false, false, _df._navaidPositions), false))));
                    Placemark itemPlacemark = new Placemark();
                    itemPlacemark.Geometry = line;
                    itemPlacemark.Name = item.Name;
                    itemPlacemark.Visibility = false;
                    itemPlacemark.Description = description;
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
                var description = new Description();
                description.Text = $"[FEB][COMMENTS]{item.Comments}[/COMMENTS][/FEB]";
                var point = new Point();
                Placemark fixPlaceMark = new Placemark();
                fixPlaceMark.Name = item.FixName;
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lat, true, false, _df._navaidPositions), false)), double.Parse(LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(item.Lon, false, false, _df._navaidPositions), false)));
                fixPlaceMark.Geometry = point;
                fixPlaceMark.Visibility = false;
                fixPlaceMark.Description = description;
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
                description.Text = $"[FEB][RWY]{item.RunwayNumber}[/RWY][OPPRWY]{item.OppositeRunwayNumber}[/OPPRWY][RWYHDG]{item.MagRunwayHeading}[/RWYHDG][OPPRWYHDG]{item.OppositeMagRunwayHeading}[/OPPRWYHDG][COMMENTS]{item.Comments}[/COMMENTS][/FEB]";
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
                description.Text = $"[FEB][APTFREQ]{item.Frequency}[/APTFREQ][COMMENTS]{item.Comments}[/COMMENTS][/FEB]";
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
                vorPlaceMark.Name = item.Id;
                description.Text = $"[FEB][VORFREQ]{item.Frequency}[/VORFREQ][COMMENTS]{item.Comments}[/COMMENTS][/FEB]";
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
                NDBPlaceMark.Name = item.Id;
                description.Text = $"[FEB][NDBFREQ]{item.Frequency}[/NDBFREQ][COMMENTS]{item.Comments}[/COMMENTS][/FEB]";
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
