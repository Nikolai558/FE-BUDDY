using FeBuddyLibrary.Dxf.Models;
using FeBuddyLibrary.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netDxf;

namespace FeBuddyLibrary.Dxf.Data
{
    public class SctDxf
    {
        private readonly SctFileModel _sctFileModel;
        private StringBuilder OneFileSB = new StringBuilder();

        public SctDxf(SctFileModel SctFileModel)
        {
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";

            _sctFileModel = SctFileModel;
            CreateNdbAndVorDxf(); // [NDB] [VOR]
            CreateAirportsDxf(); // [AIRPORT]
            CreateRunwayDxf(); // [RUNWAY]
            CreateFixesDxf(); // [FIXES]
            CreateSctArtccModelStuff(); // [ARTCC] [ARTCC_HIGH] [ARTCC_LOW] [LOW_AIRWAY] [HIGH_AIRWAY]
            CreateSidDxf(); // [SID]
            CreateStarDxf(); // [STAR]
            CreateGeoDxf(); // [GEO]
            CreateRegionDxf(); // [REGIONS]
            CreaeteLabelsDxf(); // [LABELS]

            //File.WriteAllText(_outputFilePath + "\\All.dxf", OneFileSB.ToString());
        }

        private void CreaeteLabelsDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");


            foreach (var model in _sctFileModel.SctLabelSection)
            {
                sb.AppendLine($"  0\nINSERT\n  8\nLABELS__\n  2\nLABELS");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                sb.AppendLine($"  0\nTEXT\n  8\nLABELS__");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                sb.AppendLine(" 40\n0.006\n  1");
                sb.AppendLine(model.LabelText);
            }
            sb.AppendLine("  0\nENDSEC");
            sb.AppendLine("  0\nEOF");
            //File.WriteAllText(_outputFilePath + $"\\LABELS.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());
        }

        private void CreateRegionDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb = new StringBuilder();

            string diagramName = "";
            foreach (SctRegionModel model in _sctFileModel.SctRegionsSection)
            {
                int verticieCount = model.AdditionalRegionInfo.Count() + 2;
                diagramName = model.RegionColorName;
                diagramName = diagramName.Replace('/', ' ');
                // TODO - FIGURE OUT IF CONVERTING E CORDINATES IS NEEDED! 
                sb.AppendLine("  0\nSECTION\n  2\nENTITIES");
                sb.AppendLine("  0\nLWPOLYLINE\n 5\n100\n  8");
                sb.AppendLine("REGION__" + diagramName);
                sb.AppendLine("90\n    " + verticieCount.ToString());
                sb.AppendLine("70\n    1\n43\n0.0");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));

                foreach (RegionPolygonPoints lineSegments in model.AdditionalRegionInfo)
                {
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.Lon, false, false), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.Lat, true, false), false));
                }

                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));

                sb.AppendLine("  0\nENDSEC");
            }
            //sb.AppendLine("  0\nEOF");

            //File.WriteAllText(_outputFilePath + @"\REGIONS.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());
        }

        private void CreateGeoDxf()
        {
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb;

            sb = new StringBuilder();
            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");


            foreach (var model in _sctFileModel.SctGeoSection)
            {
                sb.AppendLine("  0\nLINE\n 62\n56\n  8");
                sb.AppendLine($"GEO__");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.StartLon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.StartLat, true, false), false));
                sb.AppendLine(" 11");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.EndLon, false, false), false));
                sb.AppendLine(" 21");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.EndLat, true, false), false));
            }

            sb.AppendLine("  0\nENDSEC");
            sb.AppendLine("  0\nEOF");
            File.WriteAllText(_outputFilePath + $"\\GEO.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());

        }

        private void CreateRunwayDxf()
        {
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb;

            sb = new StringBuilder();
            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");


            foreach (var model in _sctFileModel.SctRunwaySection)
            {
                sb.AppendLine("  0\nLINE\n 8");
                sb.AppendLine($"RUNWAY__{model.RunwayNumber} {model.OppositeRunwayNumber} {model.MagRunwayHeading} {model.OppositeMagRunwayHeading}");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.StartLon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.StartLat, true, false), false));
                sb.AppendLine(" 11");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.EndLon, false, false), false));
                sb.AppendLine(" 21");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.EndLat, true, false), false));
            }

            sb.AppendLine("  0\nENDSEC");
            //sb.AppendLine("  0\nEOF");
            //File.WriteAllText(_outputFilePath + $"\\RUNWAYS.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());

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
                sb.AppendLine("  0\nINSERT\n  8\nFIX__\n  2\nFIX");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                sb.AppendLine("  0\nTEXT\n  8\nFIX__");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                sb.AppendLine(" 40\n0.006\n  1");
                sb.AppendLine(model.FixName);
            }
            sb.AppendLine("  0\nENDSEC");
            //sb.AppendLine("  0\nEOF");
            //File.WriteAllText(_outputFilePath + @"\FIX.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());

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
                sb.AppendLine("SID__" + diagramName);
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
                    sb.AppendLine("SID__" + diagramName);
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
            //sb.AppendLine("  0\nEOF");

            //File.WriteAllText(_outputFilePath + @"\SID.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());

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
                sb.AppendLine("STAR__" + diagramName);
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
                    sb.AppendLine("STAR__" + diagramName);
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
            //sb.AppendLine("  0\nEOF");

            //File.WriteAllText(_outputFilePath + @"\STAR.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());

        }

        private void CreateAirportsDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");


            foreach (SctAirportModel airportModel in _sctFileModel.SctAirportSection)
            {
                sb.AppendLine("  0\nINSERT\n  8\nAIRPORT__\n  2\nAIRPORT");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lat, true, false), false));
                sb.AppendLine("  0\nTEXT\n  8\nAIRPORT__");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lon, false, false), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lat, true, false), false));
                sb.AppendLine(" 40\n0.006\n  1");
                sb.AppendLine(airportModel.Id);
            }
            sb.AppendLine("  0\nENDSEC");
            //sb.AppendLine("  0\nEOF");
            //File.WriteAllText(_outputFilePath + @"\AIRPORT.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());

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
                    sb.AppendLine(diagramType + "__" + artccModel.Name);
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
                //sb.AppendLine("  0\nEOF");
                //File.WriteAllText(_outputFilePath + $"\\{diagramType}.dxf", sb.ToString());
                OneFileSB.Append(sb.ToString());

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
                    sb.AppendLine($"  0\nINSERT\n  8\n{diagramType}__\n  2\n{diagramType}");
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                    sb.AppendLine($"  0\nTEXT\n  8\n{diagramType}__");
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, false), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, false), false));
                    sb.AppendLine(" 40\n0.006\n  1");
                    sb.AppendLine(model.Id);
                }
                sb.AppendLine("  0\nENDSEC");
                //sb.AppendLine("  0\nEOF");
                //File.WriteAllText(_outputFilePath + $"\\{diagramType}.dxf", sb.ToString());
                OneFileSB.Append(sb.ToString());

            }
        }
    }
}
