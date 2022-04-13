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
        List<string> _dxfFileSB;

        public DxfSct()
        {
            ////var test = DxfDocument.Load(@"C:\Users\nikol\Desktop\DXF Conversions\All.dxf");
            //DxfDocument doc = new DxfDocument();
            //Line entity = new Line();
            //doc.AddEntity(entity);
            //doc.Save(@"C:\Users\nikol\Desktop\DXF Conversions\testfromlib.dxf");
            _dxfFileSB = readDxfFile(@"C:\Users\nikol\Desktop\DXF Conversions\All.dxf");
            List<SctGeoModel> geoModels = geoParser();
        }

        private List<SctGeoModel> geoParser()
        {
            bool isInLineSection = false;
            bool isInGeoSection = false;

            List<SctGeoModel> models = new List<SctGeoModel>();
            SctGeoModel currentModel = null;


            int currentIndex = -1;
            string previous_line = "";
            foreach (string current_line in _dxfFileSB)
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
                    isInGeoSection = true;
                }

                if (isInLineSection && isInGeoSection)
                {
                    switch (_currentLine)
                    {
                        case "10": { currentModel.StartLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFileSB[currentIndex + 1].Trim()), false); break; }
                        case "20": { currentModel.StartLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFileSB[currentIndex + 1].Trim()), true); break; }
                        case "11": { currentModel.EndLon = LatLonHelpers.CreateDMS(double.Parse(_dxfFileSB[currentIndex + 1].Trim()),false); break; }
                        case "21": { currentModel.EndLat = LatLonHelpers.CreateDMS(double.Parse(_dxfFileSB[currentIndex + 1].Trim()), true); isInGeoSection = false; isInLineSection = false; break; }
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
