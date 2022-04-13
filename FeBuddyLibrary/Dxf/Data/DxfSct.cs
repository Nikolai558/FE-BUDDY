using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeBuddyLibrary.Dxf.Models;
using FeBuddyLibrary.Helpers;
using netDxf;
using netDxf.Entities;

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
            _sctFileModel.SctFileColors = colorParser();
            _sctFileModel.SctInfoSection = infoParser();
            _sctFileModel.SctVORSection = vorParser();
            _sctFileModel.SctNDBSection = ndbParser();
            _sctFileModel.SctAirportSection = airportParser();
            _sctFileModel.SctRunwaySection = runwayParser();
            _sctFileModel.SctFixesSection = fixesParser();
            _sctFileModel.SctArtccSection = artccParser();
            _sctFileModel.SctArtccHighSection = artccHighParser();
            _sctFileModel.SctArtccLowSection = artccLowParser();
            _sctFileModel.SctSidSection = sidParser();
            _sctFileModel.SctStarSection = starParser();
            _sctFileModel.SctLowAirwaySection = lowAirwayParser();
            _sctFileModel.SctHighAirwaySection = highAirwayParser();
            _sctFileModel.SctGeoSection = geoParser();
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

        private List<SctArtccModel> highAirwayParser()
        {
            throw new NotImplementedException();
        }

        private List<SctArtccModel> lowAirwayParser()
        {
            throw new NotImplementedException();
        }

        private List<SctSidStarModel> starParser()
        {
            throw new NotImplementedException();
        }

        private List<SctSidStarModel> sidParser()
        {
            throw new NotImplementedException();
        }

        private List<SctArtccModel> artccLowParser()
        {
            throw new NotImplementedException();
        }

        private List<SctArtccModel> artccHighParser()
        {
            throw new NotImplementedException();
        }

        private List<SctArtccModel> artccParser()
        {
            throw new NotImplementedException();
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

        private List<VORNDBModel> ndbParser()
        {
            throw new NotImplementedException();
        }

        private List<VORNDBModel> vorParser()
        {
            throw new NotImplementedException();
        }

        private SctInfoModel infoParser()
        {
            throw new NotImplementedException();
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
