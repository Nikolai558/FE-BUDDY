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
        private KmlFile _kml;
        private string _kmlFilePath;

        private Folder _facilityRoot;

        private SctFileModel _sctFileModel;
        private string _sctFileText;
        private string _sctFilePath;

        public KmlConverter(string kmlFilePath, string sectorFilePath)
        {
            _kmlFilePath = kmlFilePath;
            _facilityRoot = new Folder();
            _sctFilePath = sectorFilePath;
        }

        public void ConvertSctToKml()
        {
            DataFunctions df = new DataFunctions();
            _sctFileModel = df.ReadSctFile(_sctFilePath);
            //AddDefineSection();
            //AddInfoSection();
            //AddVorAndNdb();
            //AddAirportSection();
            //AddRunwaySection();
            //AddFixesSection();
            //AddArtccModelsSection();
            //AddDiagramSections();
            AddLabelSection();
            SaveKML();
        }

        private void AddLabelSection()
        {
            Folder leablesFolder = new Folder();
            leablesFolder.Name = "[LABELS]";

            foreach (SctLabelModel item in _sctFileModel.SctLabelSection)
            {
                var point = new Point();
                Placemark labelPlaceMark = new Placemark();
                labelPlaceMark.Name = item.LabelText ;
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.Lat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.Lon, false)));
                labelPlaceMark.Geometry = point;
                labelPlaceMark.Visibility = false;
                leablesFolder.AddFeature(labelPlaceMark);
            }
            leablesFolder.Visibility = false;
            _facilityRoot.AddFeature(leablesFolder);
        }

        private void AddDiagramSections()
        {
            Dictionary<string, List<SctSidStarModel>> collections = new Dictionary<string, List<SctSidStarModel>>
            {
                {"SID", _sctFileModel.SctStarSection},
                {"STAR", _sctFileModel.SctSidSection}
            };
            foreach (var collectionName in collections.Keys)
            {
                Folder collectionFolder = new Folder();
                collectionFolder.Name = $"[{collectionName}]";
                
                foreach (SctSidStarModel item in collections[collectionName])
                {
                    Folder diagramFolder = new Folder();
                    diagramFolder.Name = item.DiagramName;
                    int collectionCount = 0;
                    
                    LineString line = new LineString();
                    line.Coordinates = new CoordinateCollection();
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.StartLat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.StartLon, false))));
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.EndLat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.EndLon, false))));
                    Placemark itemPlacemark = new Placemark();
                    itemPlacemark.Geometry = line;
                    itemPlacemark.Name = "LineCollection_" + collectionCount;
                    itemPlacemark.Visibility = false;
                    diagramFolder.AddFeature(itemPlacemark);

                    LineString additionalLines = new LineString();
                    additionalLines.Coordinates = new CoordinateCollection();
                    Placemark additionalLinesPlacemark = new Placemark();

                    string previousEndingCoord = null;
                    foreach (var lineSeg in item.AdditionalLines)
                    {
                        if (previousEndingCoord != null && previousEndingCoord != $"{lineSeg.StartLat} {lineSeg.StartLon}")
                        {
                            collectionCount += 1;
                            // starting line segment does not match last ending point.
                            additionalLinesPlacemark.Geometry = additionalLines;
                            additionalLinesPlacemark.Name = "LineCollection_" + collectionCount;
                            additionalLinesPlacemark.Visibility = false;
                            diagramFolder.AddFeature(additionalLinesPlacemark);

                            additionalLines = new LineString();
                            additionalLines.Coordinates = new CoordinateCollection();
                            additionalLinesPlacemark = new Placemark();
                        }

                        additionalLines.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(lineSeg.StartLat, false)), double.Parse(LatLonHelpers.CreateDecFormat(lineSeg.StartLon, false))));
                        additionalLines.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(lineSeg.EndLat, false)), double.Parse(LatLonHelpers.CreateDecFormat(lineSeg.EndLon, false))));
                        previousEndingCoord = lineSeg.EndLat + " " + lineSeg.EndLon;
                    }
                    collectionCount += 1;
                    // starting line segment does not match last ending point.
                    additionalLinesPlacemark.Geometry = additionalLines;
                    additionalLinesPlacemark.Name = "LineCollection_" + collectionCount;
                    additionalLinesPlacemark.Visibility = false;
                    diagramFolder.AddFeature(additionalLinesPlacemark);

                    collectionFolder.AddFeature(diagramFolder);
                }
                collectionFolder.Visibility = false;
                _facilityRoot.AddFeature(collectionFolder);
            }
        }

        private void AddArtccModelsSection()
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
                Folder collectionFolder = new Folder();
                collectionFolder.Name = $"[{collectionName}]";
                foreach (var item in collections[collectionName])
                {
                    LineString line = new LineString();
                    line.Coordinates = new CoordinateCollection();
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.StartLat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.StartLon, false))));
                    line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.EndLat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.EndLon, false))));
                    Placemark itemPlacemark = new Placemark();
                    itemPlacemark.Geometry = line;
                    itemPlacemark.Name = item.Name;
                    itemPlacemark.Visibility = false;

                    collectionFolder.AddFeature(itemPlacemark);
                }
                collectionFolder.Visibility = false;
                _facilityRoot.AddFeature(collectionFolder);
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
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.Lat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.Lon, false)));
                fixPlaceMark.Geometry = point;
                fixPlaceMark.Visibility = false;
                fixFolder.AddFeature(fixPlaceMark);
            }
            fixFolder.Visibility = false;
            _facilityRoot.AddFeature(fixFolder);
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
                line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.StartLat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.StartLon, false))));
                line.Coordinates.Add(new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.EndLat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.EndLon, false))));
                Placemark runwayPlacemark = new Placemark();
                runwayPlacemark.Geometry = line;
                runwayPlacemark.Name = item.RunwayNumber + "-" + item.OppositeRunwayNumber;
                description.Text = item.AllInfo.Split(" ")[0] + " " + item.AllInfo.Split(" ")[1] + " " + item.AllInfo.Split(" ")[2] + " " + item.AllInfo.Split(" ")[3] + " ";
                runwayPlacemark.Description = description;
                runwayPlacemark.Visibility = false;

                runwayFolder.AddFeature(runwayPlacemark);
            }

            runwayFolder.Visibility = false;
            _facilityRoot.AddFeature(runwayFolder);
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
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.Lat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.Lon, false)));
                airportPlaceMark.Geometry = point;
                airportPlaceMark.Visibility = false;
                airportFolder.AddFeature(airportPlaceMark);
            }
            airportFolder.Visibility = false;
            _facilityRoot.AddFeature(airportFolder);
        }

        private void AddVorAndNdb()
        {
            Folder vorFolder = new Folder();
            vorFolder.Name = "[VOR]";

            Folder ndbFolder = new Folder();
            ndbFolder.Name = "[NDB]";


            foreach (VORNDBModel item in _sctFileModel.SctVORSection)
            {
                var description = new Description();
                var point = new Point();
                Placemark vorPlaceMark = new Placemark();
                vorPlaceMark.Name = item.Id + " - " + item.Frequency;
                description.Text = item.Comments ?? "";
                vorPlaceMark.Description = description;
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.Lat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.Lon, false)));
                vorPlaceMark.Geometry = point;
                vorPlaceMark.Visibility = false;
                vorFolder.AddFeature(vorPlaceMark);
            }
            vorFolder.Visibility = false;
            _facilityRoot.AddFeature(vorFolder);

            foreach (VORNDBModel item in _sctFileModel.SctNDBSection)
            {
                var description = new Description();
                var point = new Point();
                Placemark NDBPlaceMark = new Placemark();
                NDBPlaceMark.Name = item.Id + " - " + item.Frequency;
                description.Text = item.Comments ?? "";
                NDBPlaceMark.Description = description;
                point.Coordinate = new Vector(double.Parse(LatLonHelpers.CreateDecFormat(item.Lat, false)), double.Parse(LatLonHelpers.CreateDecFormat(item.Lon, false)));
                NDBPlaceMark.Geometry = point;
                NDBPlaceMark.Visibility = false;
                ndbFolder.AddFeature(NDBPlaceMark);
            }
            ndbFolder.Visibility = false;
            _facilityRoot.AddFeature(ndbFolder);
        }

        private void SaveKML()
        {
            _kml = KmlFile.Create(_facilityRoot, true);
            using (var stream = System.IO.File.Create(_kmlFilePath))
            {
                _kml.Save(stream);
            }
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

            _facilityRoot.AddFeature(infoFolder);
        }

        private void AddDefineSection()
        {
            var point = new Point();
            point.Coordinate = new Vector(90, 0);


            StringBuilder sb = new StringBuilder();
            foreach (var item in _sctFileModel.SctFileColors)
            {
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


            _facilityRoot.AddFeature(colorFolder);
        }
    }
}
