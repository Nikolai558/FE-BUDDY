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

        public DxfSct()
        {
            ////var test = DxfDocument.Load(@"C:\Users\nikol\Desktop\DXF Conversions\All.dxf");
            //DxfDocument doc = new DxfDocument();
            //Line entity = new Line();
            //doc.AddEntity(entity);
            //doc.Save(@"C:\Users\nikol\Desktop\DXF Conversions\testfromlib.dxf");
            _dxfFilelines = readDxfFile(@"C:\Users\nikol\Desktop\DXF Conversions\All.dxf");
            _sctFileModel = new SctFileModel();

            CreateSctFileModle();
        }

        private void CreateSctFileModle()
        {
            _sctFileModel.SctFileColors = colorParser(); // Done
            _sctFileModel.SctInfoSection = infoParser(); // Done
            _sctFileModel.SctVORSection = vorAndNDBParser("VOR__"); // Done
            _sctFileModel.SctNDBSection = vorAndNDBParser("NDB__"); // Done
            _sctFileModel.SctAirportSection = airportParser();
            _sctFileModel.SctRunwaySection = runwayParser();
            _sctFileModel.SctFixesSection = fixesParser();
            _sctFileModel.SctArtccSection = artccParser("ARTCC__"); // DONE
            _sctFileModel.SctArtccHighSection = artccParser("ARTCC_HIGH__"); // DONE
            _sctFileModel.SctArtccLowSection = artccParser("ARTCC_LOW__"); // DONE
            _sctFileModel.SctSidSection = sidParser();
            _sctFileModel.SctStarSection = starParser();
            _sctFileModel.SctLowAirwaySection = artccParser("LOW_AIRWAY__"); // DONE
            _sctFileModel.SctHighAirwaySection = artccParser("HIGH_AIRWAY__"); // DONE
            _sctFileModel.SctGeoSection = geoParser(); // Done
            _sctFileModel.SctRegionsSection = regionsParser();
            _sctFileModel.SctLabelSection = labelParser();
        }

        private List<SctLabelModel> labelParser()
        {
            throw new NotImplementedException();
        }

        private List<SctRegionModel> regionsParser()
        {
            throw new NotImplementedException();
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
                    currentModel.Color = _currentLine[_currentLine.IndexOf("---").._currentLine.LastIndexOf("---")];
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

        private List<SctSidStarModel> starParser()
        {
            throw new NotImplementedException();
        }

        private List<SctSidStarModel> sidParser()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        private List<SctRunwayModel> runwayParser()
        {
            throw new NotImplementedException();
        }

        private List<SctAirportModel> airportParser()
        {
            throw new NotImplementedException();
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

                if (isInVorSection)
                {
                    switch (line.Trim())
                    {
                        case "10": model.Lon = _dxfFilelines[currentIndex + 1]; break;
                        case "20": model.Lat = _dxfFilelines[currentIndex + 1]; break;
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
