using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeBuddyLibrary.Dxf.Models;
using FeBuddyLibrary.Helpers;

namespace FeBuddyLibrary.Dxf.Data
{
    public class DxfSct
    {
        List<string> _dxfFilelines;
        SctFileModel _sctFileModel;

        public DxfSct(string dxfFilePath, string sctFilePath)
        {
            ////var test = DxfDocument.Load(@"C:\Users\nikol\Desktop\DXF Conversions\All.dxf");
            //DxfDocument doc = new DxfDocument();
            //Line entity = new Line();
            //doc.AddEntity(entity);
            //doc.Save(@"C:\Users\nikol\Desktop\DXF Conversions\testfromlib.dxf");
            _dxfFilelines = readDxfFile(dxfFilePath);
            _sctFileModel = new SctFileModel();

            CreateSctFileModle();

            WriteSectorFile(sctFilePath);
        }

        private void WriteSectorFile(string sctFilePath)
        {

            StringBuilder sectorFileData = new StringBuilder();

            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctFileColors));

            sectorFileData.AppendLine(_sctFileModel.GetInfoSection);

            sectorFileData.AppendLine("[VOR]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctVORSection));

            sectorFileData.AppendLine("[NDB]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctNDBSection));

            sectorFileData.AppendLine("[AIRPORT]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctAirportSection));

            sectorFileData.AppendLine("[RUNWAY]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctRunwaySection));

            sectorFileData.AppendLine("[FIXES]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctFixesSection));

            sectorFileData.AppendLine("[ARTCC]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctArtccSection));

            sectorFileData.AppendLine("[ARTCC HIGH]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctArtccHighSection));

            sectorFileData.AppendLine("[ARTCC LOW]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctArtccLowSection));

            sectorFileData.AppendLine("[SID]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctSidSection));

            sectorFileData.AppendLine("[STAR]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctStarSection));

            sectorFileData.AppendLine("[LOW AIRWAY]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctLowAirwaySection));

            sectorFileData.AppendLine("[HIGH AIRWAY]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctHighAirwaySection));

            sectorFileData.AppendLine("[GEO]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctGeoSection));

            sectorFileData.AppendLine("[REGIONS]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctRegionsSection));

            sectorFileData.AppendLine("[LABELS]");
            sectorFileData.AppendLine(_sctFileModel.GetSection(_sctFileModel.SctLabelSection));


            File.WriteAllText(sctFilePath, sectorFileData.ToString());
        }

        private void CreateSctFileModle()
        {
            _sctFileModel.SctFileColors = colorParser(); // Done
            _sctFileModel.SctInfoSection = infoParser(); // Done
            _sctFileModel.SctVORSection = vorAndNDBParser("VOR__"); // Done
            _sctFileModel.SctNDBSection = vorAndNDBParser("NDB__"); // Done
            _sctFileModel.SctAirportSection = airportParser(); // Done
            _sctFileModel.SctRunwaySection = runwayParser(); // Done
            _sctFileModel.SctFixesSection = fixesParser(); // Done
            _sctFileModel.SctArtccSection = artccParser("ARTCC__"); // DONE
            _sctFileModel.SctArtccHighSection = artccParser("ARTCC_HIGH__"); // DONE
            _sctFileModel.SctArtccLowSection = artccParser("ARTCC_LOW__"); // DONE
            _sctFileModel.SctSidSection = sidStarParser("SID__"); // Done
            _sctFileModel.SctStarSection = sidStarParser("STAR__"); // Done
            _sctFileModel.SctLowAirwaySection = artccParser("LOW_AIRWAY__"); // DONE
            _sctFileModel.SctHighAirwaySection = artccParser("HIGH_AIRWAY__"); // DONE
            _sctFileModel.SctGeoSection = geoParser(); // Done
            _sctFileModel.SctRegionsSection = regionsParser(); // Done
            _sctFileModel.SctLabelSection = labelParser(); // Done
        }

        private List<SctLabelModel> labelParser()
        {
            List<SctLabelModel> results = new List<SctLabelModel>();
            bool isInText = false;
            bool isInSectionNeeded = false;
            int currentLine = -1;
            SctLabelModel model = null;

            foreach (string line in _dxfFilelines)
            {
                currentLine++;
                if (line.Contains("TEXT"))
                {
                    isInText = true;
                }

                if (isInText && line.Contains("LABELS__"))
                {
                    isInSectionNeeded = true;
                    if (model != null && !string.IsNullOrEmpty(model.LabelText))
                    {
                        results.Add(model);
                    }
                    model = new SctLabelModel()
                    {
                        Color = line[(line.IndexOf("---")+3)..line.LastIndexOf("---")].Trim()
                    };
                }

                if (line.Contains("__") && !line.Contains("LABELS__"))
                {
                    isInSectionNeeded = false;
                }

                if (isInText && isInSectionNeeded)
                {
                    switch (line.Trim())
                    {
                        case "10": model.Lon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), false); break;
                        case "20": model.Lat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), true); break;
                        case "40":
                            {
                                model.LabelText = "\"" + _dxfFilelines[currentLine + 3].Trim() + "\"";
                                isInText = false;
                                isInSectionNeeded = false;
                                break;
                            }
                    }
                }
            }

            if (model != null)
            {
                results.Add(model);
            }
            return results;
        }

        private List<SctRegionModel> regionsParser()
        {
            List<SctRegionModel> results = new List<SctRegionModel>();
            bool isInPolyLine = false;
            bool isInSectionNeeded = false;
            bool firstCoordinate = true;

            RegionPolygonPoints point = new RegionPolygonPoints();

            int currentLine = -1;

            SctRegionModel model = null;

            foreach (string line in _dxfFilelines)
            {
                currentLine++;
                if (line.Contains("LWPOLYLINE"))
                {
                    isInPolyLine = true;
                }
                if (isInPolyLine && line.Contains("REGION__"))
                {
                    isInSectionNeeded = true;
                    if (model != null)
                    {
                        model.AdditionalRegionInfo.RemoveAt(model.AdditionalRegionInfo.Count -1);
                        results.Add(model);
                    }
                    model = new SctRegionModel()
                    {
                        RegionColorName = line[(line.LastIndexOf("__")+2)..],
                        AdditionalRegionInfo = new List<RegionPolygonPoints>()
                    };
                    firstCoordinate = true;
                }

                if (isInPolyLine && isInSectionNeeded)
                {
                    if (firstCoordinate)
                    {
                        switch (line.Trim())
                        {
                            case "10": model.Lon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), false); break;
                            case "20": model.Lat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine +1].Trim()), true); firstCoordinate = false; break;
                        }
                    }
                    else
                    {
                        switch (line.Trim())
                        {
                            case "10": point.Lon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), false); break;
                            case "20":
                                {
                                    point.Lat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), true);
                                    model.AdditionalRegionInfo.Add(point);
                                    point = new RegionPolygonPoints();
                                    break;
                                }
                            case "0": isInPolyLine = false; isInSectionNeeded = false; break;
                        }
                    }
                }
            }

            if (model != null)
            {
                results.Add(model);
            }
            return results;
        }

        private List<SctGeoModel> geoParser()
        {
            bool isInLineSection = false;
            bool isInGeoSection = false;

            List<SctGeoModel> models = new List<SctGeoModel>();
            SctGeoModel currentModel = null;


            int currentIndex = -1;
            string previous_line = "";
            foreach (string current_line in _dxfFilelines)
            {
                string _currentLine = current_line.Trim();
                currentIndex += 1;
                if (previous_line == "0" && _currentLine == "LINE")
                {
                    isInLineSection = true;
                }
                if (isInLineSection && _currentLine.Contains("GEO__"))
                {
                    if (currentModel is not null)
                    {
                        models.Add(currentModel);
                    }
                    currentModel = new SctGeoModel();
                    currentModel.Color = _currentLine[(_currentLine.IndexOf("---")+3).._currentLine.LastIndexOf("---")];
                    isInGeoSection = true;
                }

                if (isInLineSection && isInGeoSection)
                {
                    switch (_currentLine)
                    {
                        case "10": { currentModel.StartLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1].Trim()), false); break; }
                        case "20": { currentModel.StartLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1].Trim()), true); break; }
                        case "11": { currentModel.EndLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1].Trim()), false); break; }
                        case "21": { currentModel.EndLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1].Trim()), true); isInGeoSection = false; isInLineSection = false; break; }
                        default: { break; }
                    }
                }
                previous_line = _currentLine;
            }
            if (currentModel is not null)
            {
                models.Add(currentModel);
            }

            return models;
        }

        private List<SctSidStarModel> sidStarParser(string containsString)
        {
            List<SctSidStarModel> results = new List<SctSidStarModel>();

            bool isInSectionNeeded = false;
            bool isInLineSection = false;
            string currentDiagramName = "";
            SctAditionalDiagramLineSegments additionalLineSegments = new SctAditionalDiagramLineSegments();

            SctSidStarModel model = null;
            int currentLine = -1;
            bool firstDiagramOccurance = true;
            foreach (string line in _dxfFilelines)
            {
                currentLine++;
                if (line.Contains("LINE"))
                {
                    isInLineSection = true;
                }

                if (isInLineSection && line.Contains(containsString))
                {
                    isInSectionNeeded = true;
                    if (currentDiagramName != line[containsString.Length..(line[..line.LastIndexOf("---")].LastIndexOf("---"))])
                    {
                        if (model != null && !string.IsNullOrEmpty(model.EndLat))
                        {
                            results.Add(model);
                        }
                        firstDiagramOccurance = true;
                        currentDiagramName = line[containsString.Length..(line[..line.LastIndexOf("---")].LastIndexOf("---"))];
                        model = new SctSidStarModel()
                        {
                            Color = line[(line[..line.LastIndexOf("---")].LastIndexOf("---") + 3)..line.LastIndexOf("---")],
                            DiagramName = line[containsString.Length..(line[..line.LastIndexOf("---")].LastIndexOf("---"))],
                            AdditionalLines = new List<SctAditionalDiagramLineSegments>()
                        };
                    }
                }

                if (line.Contains("__") && !line.Contains(containsString))
                {
                    isInSectionNeeded = false;
                }


                if (isInLineSection && isInSectionNeeded)
                {
                    switch (firstDiagramOccurance)
                    {
                        case true:
                            {
                                switch (line.Trim())
                                {
                                    case "10": model.StartLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), false); break;
                                    case "20": model.StartLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), true); break;
                                    case "11": model.EndLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), false); break;
                                    case "21": model.EndLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), true); isInLineSection = false; isInSectionNeeded = false; firstDiagramOccurance = false; break;
                                }
                                break;
                            }
                        case false:
                            {
                                switch (line.Trim())
                                {
                                    case "10": additionalLineSegments.StartLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), false); break;
                                    case "20": additionalLineSegments.StartLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), true); break;
                                    case "11": additionalLineSegments.EndLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()), false); break;
                                    case "21":
                                        {
                                            additionalLineSegments.EndLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentLine + 1].Trim()),true);
                                            additionalLineSegments.Color = model.Color;
                                            model.AdditionalLines.Add(additionalLineSegments);
                                            additionalLineSegments = new SctAditionalDiagramLineSegments();
                                            isInLineSection = false; 
                                            isInSectionNeeded = false;
                                            break;
                                        }
                                }
                                break;
                            }
                    }
                }
            }

            if (model != null)
            {
                results.Add(model);
            }
            return results;
        }

        private List<SctArtccModel> artccParser(string containsString)
        {
            string[] validContainsString = new string[] 
            { 
                "ARTCC__", "ARTCC_HIGH__", "ARTCC_LOW__", "LOW_AIRWAY__", "HIGH_AIRWAY__"
            };
            if (!validContainsString.Contains(containsString)) throw new Exception("VALUE NOT ALLOWED");

            bool isInLineSection = false;
            bool isInSectionNeeded = false;

            List<SctArtccModel> models = new List<SctArtccModel>();
            SctArtccModel currentModel = null;

            int currentIndex = -1;
            string previous_line = "";
            foreach (string current_line in _dxfFilelines)
            {
                string _currentLine = current_line.Trim();
                currentIndex += 1;
                if (previous_line == "0" && _currentLine == "LINE")
                {
                    isInLineSection = true;
                }
                if (isInLineSection && _currentLine.Contains(containsString))
                {
                    if (currentModel is not null)
                    {
                        models.Add(currentModel);
                    }
                    currentModel = new SctArtccModel();
                    currentModel.Name = _currentLine[containsString.Length..]; // TODO DOUBLE CHECK THIS, MIGHT NEED TO ADD 1 TO IT.
                    isInSectionNeeded = true;
                }

                if (isInLineSection && isInSectionNeeded)
                {
                    switch (_currentLine)
                    {
                        case "10": { currentModel.StartLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1].Trim()), false); break; }
                        case "20": { currentModel.StartLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1].Trim()), true); break; }
                        case "11": { currentModel.EndLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1].Trim()), false); break; }
                        case "21": { currentModel.EndLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1].Trim()), true); isInSectionNeeded = false; isInLineSection = false; break; }
                        default: { break; }
                    }
                }
                previous_line = _currentLine;
            }
            if (currentModel is not null)
            {
                models.Add(currentModel);
            }

            return models;
        }

        private List<SctFixesModel> fixesParser()
        {
            List<SctFixesModel> results = new List<SctFixesModel>();

            bool isInFixSection = false;
            bool isInTextSection = false;

            SctFixesModel model = null;

            int currentLine = -1;
            foreach (string line in _dxfFilelines)
            {
                currentLine++;

                if (line.Contains("TEXT"))
                {
                    isInTextSection = true;
                }

                if (isInTextSection && line.Contains("FIX__"))
                {
                    isInFixSection = true;
                    if (model != null && !string.IsNullOrEmpty(model.FixName))
                    {
                        results.Add(model);
                    }
                    model = new SctFixesModel();
                }

                if (line.Contains("__") && !line.Contains("FIX__"))
                {
                    isInFixSection = false;
                }

                if (isInTextSection && isInFixSection)
                {
                    switch (line.Trim())
                    {
                        case "10": model.Lon = LatLonHelpers.CreateDMS(Double.Parse(_dxfFilelines[currentLine + 1].Trim()), false); break;
                        case "20": model.Lat = LatLonHelpers.CreateDMS(Double.Parse(_dxfFilelines[currentLine + 1].Trim()), true); break;
                        case "40": model.FixName = _dxfFilelines[currentLine + 3].Trim(); isInFixSection = false; isInTextSection = false; break;
                    }
                }
            }
            if (model != null)
            {
                results.Add(model);
            }

            return results;
        }

        private List<SctRunwayModel> runwayParser()
        {
            List<SctRunwayModel> results = new List<SctRunwayModel>();

            bool isInLineSection = false;
            bool isInRunwaySection = false;
            SctRunwayModel model = null;

            int currentLine = -1;
            foreach (string line in _dxfFilelines)
            {
                currentLine++;
                if (line.Contains("LINE"))
                {
                    isInLineSection = true;
                }
                if (isInLineSection && line.Contains("RUNWAY__"))
                {
                    isInRunwaySection = true;
                    if (model != null && !string.IsNullOrEmpty(model.EndLat))
                    {
                        results.Add(model);
                    }
                    string _line = line.Trim()[(line.LastIndexOf("_") + 1)..];
                    string[] split = _line.Split(' ');
                    model = new SctRunwayModel()
                    {
                        RunwayNumber = split[0],
                        OppositeRunwayNumber = split[1],
                        MagRunwayHeading = split[2],
                        OppositeMagRunwayHeading = split[3],
                    };
                }

                if (line.Contains("__") && !line.Contains("RUNWAY__"))
                {
                    isInRunwaySection = false;
                }

                if (isInLineSection && isInRunwaySection)
                {
                    switch (line.Trim())
                    {
                        case "10": model.StartLon = LatLonHelpers.CreateDMS(Double.Parse(_dxfFilelines[currentLine +1].Trim()), false); break;
                        case "20": model.StartLat = LatLonHelpers.CreateDMS(Double.Parse(_dxfFilelines[currentLine + 1].Trim()), true); break;
                        case "11": model.EndLon = LatLonHelpers.CreateDMS(Double.Parse(_dxfFilelines[currentLine + 1].Trim()), false); break;
                        case "21": model.EndLat = LatLonHelpers.CreateDMS(Double.Parse(_dxfFilelines[currentLine + 1].Trim()), true); isInLineSection = false; isInRunwaySection = false; break;
                    }
                }
            }

            if (model != null)
            {
                results.Add(model);
            }
            return results;
        }

        private List<SctAirportModel> airportParser()
        {
            List<SctAirportModel> results = new List<SctAirportModel>();
            bool isInAirportSection = false;
            bool isInTextSection = false;
            SctAirportModel model = null;

            int currentLine = -1;
            foreach (string line in _dxfFilelines)
            {
                currentLine += 1;
                if (line.Contains("AIRPORT__"))
                {
                    isInAirportSection = true;
                    if (model != null && !string.IsNullOrEmpty(model.Id))
                    {
                        results.Add(model);
                    }
                    model = new SctAirportModel();
                }

                if (isInAirportSection && line.Contains("TEXT"))
                {
                    isInTextSection = true;
                }

                if (line.Contains("__") && !line.Contains("AIRPORT__"))
                {
                    isInAirportSection = false;
                }

                if (isInTextSection && isInAirportSection)
                {
                    switch (line.Trim())
                    {
                        case "10": model.Lon = LatLonHelpers.CreateDMS(Double.Parse(_dxfFilelines[currentLine + 1]), false); break;
                        case "20": model.Lat = LatLonHelpers.CreateDMS(Double.Parse(_dxfFilelines[currentLine + 1]), true); break;
                        case "40":
                            {
                                model.Id = _dxfFilelines[currentLine + 3].Split(' ')[0];
                                model.Frequency = _dxfFilelines[currentLine + 3].Split(' ')[1];
                                isInAirportSection = false;
                                isInTextSection = false;
                                break;
                            }
                    }
                }
            }
            if (model != null && !string.IsNullOrEmpty(model.Id))
            {
                results.Add(model);
            }
            return results;
        }

        private List<VORNDBModel> vorAndNDBParser(string containsString)
        {
            if (!new string[] { "VOR__", "NDB__" }.Contains(containsString)) throw new Exception("INCORRECT VALUE");

            List<VORNDBModel> results = new List<VORNDBModel>();

            VORNDBModel model = null;

            bool isInVorSection = false;

            int currentIndex = -1;
            foreach (string line in _dxfFilelines)
            {
                currentIndex += 1;
                if (line.Contains(containsString))
                {
                    isInVorSection = true;
                    if (model is not null && !string.IsNullOrEmpty(model.Id))
                    {
                        results.Add(model);
                    }
                    model = new VORNDBModel();
                }

                if (line.Contains("__") && !line.Contains(containsString))
                {
                    isInVorSection = false;
                }

                if (isInVorSection)
                {
                    switch (line.Trim())
                    {
                        case "10": model.Lon = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1]), false); break;
                        case "20": model.Lat = LatLonHelpers.CreateDMS(double.Parse(_dxfFilelines[currentIndex + 1]), true); break;
                        case "0.006":
                            {
                                model.Id = _dxfFilelines[currentIndex + 2].Split(' ')[0];
                                model.Frequency = _dxfFilelines[currentIndex + 2].Split(' ')[1];
                                isInVorSection = false; 
                                break;
                            }
                    }
                }
            }
            if (model is not null)
            {
                results.Add(model);
            }
            return results;
        }

        private SctInfoModel infoParser()
        {
            SctInfoModel model = new SctInfoModel();
            List<string> additionalLines = new List<string>();

            bool isInInfoSection = false;
            bool isInInfoTextSection = false;

            int currentIndex = -1;
            int propertyCount = 0;
            foreach (string line in _dxfFilelines)
            {
                currentIndex += 1;
                if (line.Contains("INFO__")) isInInfoSection = true;

                if (isInInfoSection)
                {
                    string text = "";

                    switch (line.Trim())
                    {
                        case "40": isInInfoTextSection = true; continue;
                        case "0.006": if(isInInfoTextSection) text = _dxfFilelines[currentIndex + 2]; break;
                    }
                    if (isInInfoTextSection)
                    {
                        isInInfoSection = false;
                        isInInfoTextSection = false;

                        if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                        {
                            switch (propertyCount)
                            {
                                case 0: model.Header = text; break;
                                case 1: model.SctFileName = text; break;
                                case 2: model.DefaultCallsign = text; break;
                                case 3: model.DefaultAirport = text; break;
                                case 4: model.CenterLat = text; break;
                                case 5: model.CenterLon = text; break;
                                case 6: model.NMPerLat = text; break;
                                case 7: model.NMPerLon = text; break;
                                case 8: model.MagneticVariation = text; break;
                                case 9: model.SctScale = text; break;
                                default: additionalLines.Add(text); break;
                            }
                            propertyCount += 1;

                        }
                    }
                }
            }
            model.AdditionalLines = additionalLines.ToArray();

            return model;
        }

        private List<SctColorModel> colorParser()
        {
            List<SctColorModel> results = new List<SctColorModel>();

            bool isInColorSection = false;
            SctColorModel colorModel = null;

            foreach (string line in _dxfFilelines)
            {
                if (line.Contains("COLOR__"))
                {
                    isInColorSection = true;
                }

                if (isInColorSection && line.Contains("#define"))
                {
                    if (colorModel is not null)
                    {
                        results.Add(colorModel);
                    }

                    colorModel = new SctColorModel()
                    {
                        Name = line.Split(' ')[1],
                        ColorCode = line.Split(' ')[2]
                    };
                    isInColorSection = false;
                }
            }
            if (colorModel is not null)
            {
                results.Add(colorModel);
            }
            return results;
        }

        public List<string> readDxfFile(string filePath)
        {
            // Might be able to trim down the string builder. but for now just turn the entire dxf file into a string builder...
            List<string> output = new List<string>();

            foreach (string line in File.ReadLines(filePath))
            {
                output.Add(line);
            }
            return output;
        }
    }
}
