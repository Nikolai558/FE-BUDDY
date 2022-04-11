using FeBuddyLibrary.Dxf.Models;
using FeBuddyLibrary.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyLibrary.Dxf.Data
{
    public class SctDxf
    {
        private readonly SctFileModel _sctFileModel;

        public SctDxf(SctFileModel SctFileModel)
        {
            _sctFileModel = SctFileModel;
            CreateSidDxf();
            CreateStarDxf();
            CreateAirportsDxf();
            CreateSctArtccModelStuff();
            CreateNdbAndVorDxf();
            CreateFixesDxf();
        }

        private void CreateFixesDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb;

            sb = new StringBuilder();
            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");


            foreach (var model in _sctFileModel.SctFixesSection)
            {
                sb.AppendLine("  0\nINSERT\n  8\nFIX\n  2\nFIX");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                sb.AppendLine("  0\nTEXT\n  8\nFIX");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                sb.AppendLine(" 40\n0.006\n  1");
                sb.AppendLine(model.FixName);
            }
            sb.AppendLine("  0\nENDSEC");
            sb.AppendLine("  0\nEOF");
            File.WriteAllText(_outputFilePath + @"\FIX.dxf", sb.ToString());
        }

        private void CreateSidDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb = new StringBuilder();

            string diagramName = "";
            foreach (SctSidStarModel sctSid in _sctFileModel.SctSidSection)
            {
                diagramName = sctSid.DiagramName;
                diagramName = diagramName.Replace('/', ' ');
                // TODO - FIGURE OUT IF CONVERTING E CORDINATES IS NEEDED! 
                sb.AppendLine("  0\nSECTION\n  2\nENTITIES");
                sb.AppendLine("  0\nLINE\n 62\n7\n 8");
                sb.AppendLine("SID_" + diagramName);
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.StartLon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.StartLat, true, false), false));
                sb.AppendLine(" 11");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.EndLon, false, false), false));
                sb.AppendLine(" 21");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.EndLat, true, false), false));

                foreach (SctAditionalDiagramLineSegments lineSegments in sctSid.AdditionalLines)
                {
                    sb.AppendLine("  0\nLINE\n 62\n7\n  8");
                    sb.AppendLine("SID_" + diagramName);
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.StartLon, false, false), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.StartLat, true, false), false));
                    sb.AppendLine(" 11");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.EndLon, false, false), false)); 
                    sb.AppendLine(" 21");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.EndLat, true, false), false));
                }
                sb.AppendLine("  0\nENDSEC");
            }
            sb.AppendLine("  0\nEOF");

            File.WriteAllText(_outputFilePath + @"\SID.dxf", sb.ToString());
        }

        private void CreateStarDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb = new StringBuilder();

            string diagramName = "";
            foreach (SctSidStarModel sctSid in _sctFileModel.SctStarSection)
            {
                diagramName = sctSid.DiagramName;
                diagramName = diagramName.Replace('/', ' ');
                // TODO - FIGURE OUT IF CONVERTING E CORDINATES IS NEEDED! 
                sb.AppendLine("  0\nSECTION\n  2\nENTITIES");
                sb.AppendLine("  0\nLINE\n 62\n7\n 8");
                sb.AppendLine("STAR_" + diagramName);
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.StartLon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.StartLat, true, false), false));
                sb.AppendLine(" 11");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.EndLon, false, false), false));
                sb.AppendLine(" 21");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.EndLat, true, false), false));

                foreach (SctAditionalDiagramLineSegments lineSegments in sctSid.AdditionalLines)
                {
                    sb.AppendLine("  0\nLINE\n 62\n7\n  8");
                    sb.AppendLine("STAR_" + diagramName);
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.StartLon, false, false), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.StartLat, true, false), false));
                    sb.AppendLine(" 11");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.EndLon, false, false), false));
                    sb.AppendLine(" 21");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.EndLat, true, false), false));
                }
                sb.AppendLine("  0\nENDSEC");
            }
            sb.AppendLine("  0\nEOF");

            File.WriteAllText(_outputFilePath + @"\STAR.dxf", sb.ToString());
        }

        private void CreateAirportsDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");


            foreach (SctAirportModel airportModel in _sctFileModel.SctAirportSection)
            {
                sb.AppendLine("  0\nINSERT\n  8\nAIRPORT\n  2\nAIRPORT");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lat, true, false), false));
                sb.AppendLine("  0\nTEXT\n  8\nAIRPORT");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lat, true, false), false));
                sb.AppendLine(" 40\n0.006\n  1");
                sb.AppendLine(airportModel.Id);
            }
            sb.AppendLine("  0\nENDSEC");
            sb.AppendLine("  0\nEOF");
            File.WriteAllText(_outputFilePath + @"\AIRPORT.dxf", sb.ToString());
        }

        private void CreateSctArtccModelStuff()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";

            StringBuilder sb;

            var sctArtccModelList = new Dictionary<string, List<SctArtccModel>>()
            {

                { "ARTCC",  _sctFileModel.SctArtccSection},
                { "ARTCC_HIGH",  _sctFileModel.SctArtccHighSection},
                { "ARTCC_LOW",  _sctFileModel.SctArtccLowSection},
                { "LOW_AIRWAY",  _sctFileModel.SctLowAirwaySection},
                { "HIGH_AIRWAY",  _sctFileModel.SctHighAirwaySection}
            };

            foreach (string diagramType in sctArtccModelList.Keys)
            {
                sb = new StringBuilder();

                sb.AppendLine("  0\nSECTION\n  2\nENTITIES");

                foreach (SctArtccModel artccModel in sctArtccModelList[diagramType])
                {
                    sb.AppendLine("  0\nLINE\n 8");
                    sb.AppendLine(diagramType + "_" + artccModel.Name);
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(artccModel.StartLon, false, false), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(artccModel.StartLat, true, false), false));
                    sb.AppendLine(" 11");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(artccModel.EndLon, false, false), false));
                    sb.AppendLine(" 21");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(artccModel.EndLat, true, false), false));
                }

                sb.AppendLine("  0\nENDSEC");
                sb.AppendLine("  0\nEOF");
                File.WriteAllText(_outputFilePath + $"\\{diagramType}.dxf", sb.ToString());
            }
        }

        private void CreateNdbAndVorDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb;

            var sctArtccModelList = new Dictionary<string, List<VORNDBModel>>()
            {
                { "NDB",  _sctFileModel.SctNDBSection},
                { "VOR",  _sctFileModel.SctVORSection},
            };

            foreach (string diagramType in sctArtccModelList.Keys)
            {
                sb = new StringBuilder();
                sb.AppendLine("  0\nSECTION\n  2\nENTITIES");


                foreach (var model in sctArtccModelList[diagramType])
                {
                    sb.AppendLine($"  0\nINSERT\n  8\n{diagramType}\n  2\n{diagramType}");
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                    sb.AppendLine($"  0\nTEXT\n  8\n{diagramType}");
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                    sb.AppendLine(" 40\n0.006\n  1");
                    sb.AppendLine(model.Id);
                }
                sb.AppendLine("  0\nENDSEC");
                sb.AppendLine("  0\nEOF");
                File.WriteAllText(_outputFilePath + $"\\{diagramType}.dxf", sb.ToString());
            }
        }
    }
}
