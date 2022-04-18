using FeBuddyLibrary.Dxf.Models;
using FeBuddyLibrary.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace FeBuddyLibrary.Dxf.Data
{
    public class SctDxf
    {
        private readonly SctFileModel _sctFileModel;
        private StringBuilder OneFileSB = new StringBuilder();

        public SctDxf(SctFileModel SctFileModel, string dxfFilePath)
        {
            _sctFileModel = SctFileModel;
            CreateColorsDxf(); // Colors (#define ---.)
            CreateInfoSectionDxf(); // [INFO]
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

            File.WriteAllText(dxfFilePath, OneFileSB.ToString());
        }

        private void CreateColorsDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb;

            sb = new StringBuilder();
            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");

            var model = _sctFileModel.SctFileColors;

            foreach (SctColorModel colorLine in model)
            {
                sb.AppendLine("  0\nINSERT\n  8\nCOLOR__\n  2\nCOLOR");
                sb.AppendLine(" 10");
                sb.AppendLine(" -30.0");
                sb.AppendLine(" 20");
                sb.AppendLine("  30.0");
                sb.AppendLine("  0\nTEXT\n  8\nCOLOR__");
                sb.AppendLine(" 10");
                sb.AppendLine(" -30.0");
                sb.AppendLine(" 20");
                sb.AppendLine("  30.0");
                sb.AppendLine(" 40\n0.006\n  1");
                sb.AppendLine(colorLine.AllInfo.Trim());
            }

            sb.AppendLine("  0\nINSERT\n  8\nCOLOR__\n  2\nCOLOR");
            sb.AppendLine(" 10");
            sb.AppendLine(" -30.0");
            sb.AppendLine(" 20");
            sb.AppendLine("  30.0");
            sb.AppendLine("  0\nTEXT\n  8\nCOLOR__");
            sb.AppendLine(" 10");
            sb.AppendLine(" -30.0");
            sb.AppendLine(" 20");
            sb.AppendLine("  30.0");
            sb.AppendLine(" 40\n0.006\n  1");
            sb.AppendLine("#define NOCOLOR 255");

            sb.AppendLine("  0\nENDSEC");
            //sb.AppendLine("  0\nEOF");
            //File.WriteAllText(_outputFilePath + @"\FIX.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());
        }

        private void CreateInfoSectionDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb;

            sb = new StringBuilder();
            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");

            var model = _sctFileModel.SctInfoSection;

            PropertyInfo[] properties = typeof(SctInfoModel).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (property.Name == "AllInfo")
                {
                    continue;
                }

                if (property.Name == "AdditionalLines")
                {
                    int currentIndex = 0;
                    foreach (string additionalLine in model.AdditionalLines)
                    {
                        sb.AppendLine("  0\nINSERT\n  8\nINFO__\n  2\nINFO");
                        sb.AppendLine(" 10");
                        sb.AppendLine(" -30.0");
                        sb.AppendLine(" 20");
                        sb.AppendLine("  30.0");
                        sb.AppendLine("  0\nTEXT\n  8\nINFO__");
                        sb.AppendLine(" 10");
                        sb.AppendLine(" -30.0");
                        sb.AppendLine(" 20");
                        sb.AppendLine("  30.0");
                        sb.AppendLine(" 40\n0.006\n  1");
                        sb.AppendLine(model.AdditionalLines[currentIndex].Trim());
                        currentIndex += 1;
                    }
                    continue;
                }

                sb.AppendLine("  0\nINSERT\n  8\nINFO__\n  2\nINFO");
                sb.AppendLine(" 10");
                sb.AppendLine(" -30.0");
                sb.AppendLine(" 20");
                sb.AppendLine("  30.0");
                sb.AppendLine("  0\nTEXT\n  8\nINFO__");
                sb.AppendLine(" 10");
                sb.AppendLine(" -30.0");
                sb.AppendLine(" 20");
                sb.AppendLine("  30.0");
                sb.AppendLine(" 40\n0.006\n  1");
                sb.AppendLine(property.GetValue(model).ToString().Trim());
            }

            sb.AppendLine("  0\nENDSEC");
            //sb.AppendLine("  0\nEOF");
            //File.WriteAllText(_outputFilePath + @"\FIX.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());
        }

        private void CreaeteLabelsDxf()
        {
            // TODO Fix File path! 
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");


            foreach (SctLabelModel model in _sctFileModel.SctLabelSection)
            {
                string _color = model.Color;
                if (_color is not null)
                {
                    _color = _color.Trim();
                }
                sb.AppendLine($"  0\nINSERT\n  8\nLABELS__---{_color ?? string.Empty}---\n  2\nLABELS");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, true), false));
                sb.AppendLine($"  0\nTEXT\n  8\nLABELS__---{_color ?? string.Empty}---");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, true), false));
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
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, true), false));

                foreach (RegionPolygonPoints lineSegments in model.AdditionalRegionInfo)
                {
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.Lon, false, true), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.Lat, true, true), false));
                }

                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, true), false));

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


            foreach (SctGeoModel model in _sctFileModel.SctGeoSection)
            {
                string _color = model.Color;
                if (_color is not null)
                {
                    _color = _color.Trim();
                }
                sb.AppendLine("  0\nLINE\n 62\n56\n  8");
                sb.AppendLine($"GEO__---{_color ?? string.Empty}---");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.StartLon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.StartLat, true, true), false));
                sb.AppendLine(" 11");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.EndLon, false, true), false));
                sb.AppendLine(" 21");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.EndLat, true, true), false));
            }

            sb.AppendLine("  0\nENDSEC");
            //sb.AppendLine("  0\nEOF");
            //File.WriteAllText(_outputFilePath + $"\\GEO.dxf", sb.ToString());
            OneFileSB.Append(sb.ToString());

        }

        private void CreateRunwayDxf()
        {
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb;

            sb = new StringBuilder();
            sb.AppendLine("  0\nSECTION\n  2\nENTITIES");


            foreach (SctRunwayModel model in _sctFileModel.SctRunwaySection)
            {
                sb.AppendLine("  0\nLINE\n 8");
                sb.AppendLine($"RUNWAY__{model.RunwayNumber} {model.OppositeRunwayNumber} {model.MagRunwayHeading} {model.OppositeMagRunwayHeading}");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.StartLon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.StartLat, true, true), false));
                sb.AppendLine(" 11");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.EndLon, false, true), false));
                sb.AppendLine(" 21");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.EndLat, true, true), false));
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


            foreach (SctFixesModel model in _sctFileModel.SctFixesSection)
            {
                sb.AppendLine("  0\nINSERT\n  8\nFIX__\n  2\nFIX");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, true), false));
                sb.AppendLine("  0\nTEXT\n  8\nFIX__");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, true), false));
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
                string _color = sctSid.Color;
                if (_color is not null)
                {
                    _color = _color.Trim();
                }
                diagramName = sctSid.DiagramName;
                diagramName = diagramName.Replace('/', ' ');
                // TODO - FIGURE OUT IF CONVERTING E CORDINATES IS NEEDED! 
                sb.AppendLine("  0\nSECTION\n  2\nENTITIES");
                sb.AppendLine("  0\nLINE\n 62\n7\n 8");
                sb.AppendLine("SID__" + diagramName+$"---{_color ?? string.Empty}---");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.StartLon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.StartLat, true, true), false));
                sb.AppendLine(" 11");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.EndLon, false, true), false));
                sb.AppendLine(" 21");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctSid.EndLat, true, true), false));

                foreach (SctAditionalDiagramLineSegments lineSegments in sctSid.AdditionalLines)
                {
                    sb.AppendLine("  0\nLINE\n 62\n7\n  8");
                    sb.AppendLine("SID__" + diagramName + $"---{_color ?? string.Empty}---");
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.StartLon, false, true), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.StartLat, true, true), false));
                    sb.AppendLine(" 11");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.EndLon, false, true), false)); 
                    sb.AppendLine(" 21");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.EndLat, true, true), false));
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
            foreach (SctSidStarModel sctStar in _sctFileModel.SctStarSection)
            {
                string _color = sctStar.Color;
                if (_color is not null)
                {
                    _color = _color.Trim();
                }
                diagramName = sctStar.DiagramName;
                diagramName = diagramName.Replace('/', ' ');
                // TODO - FIGURE OUT IF CONVERTING E CORDINATES IS NEEDED! 
                sb.AppendLine("  0\nSECTION\n  2\nENTITIES");
                sb.AppendLine("  0\nLINE\n 62\n7\n 8");
                sb.AppendLine("STAR__" + diagramName + $"---{_color ?? string.Empty}---");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctStar.StartLon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctStar.StartLat, true, true), false));
                sb.AppendLine(" 11");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctStar.EndLon, false, true), false));
                sb.AppendLine(" 21");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(sctStar.EndLat, true, true), false));

                foreach (SctAditionalDiagramLineSegments lineSegments in sctStar.AdditionalLines)
                {
                    sb.AppendLine("  0\nLINE\n 62\n7\n  8");
                    sb.AppendLine("STAR__" + diagramName + $"---{_color ?? string.Empty}---");
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.StartLon, false, true), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.StartLat, true, true), false));
                    sb.AppendLine(" 11");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.EndLon, false, true), false));
                    sb.AppendLine(" 21");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(lineSegments.EndLat, true, true), false));
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
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lat, true, true), false));
                sb.AppendLine("  0\nTEXT\n  8\nAIRPORT__");
                sb.AppendLine(" 10");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lon, false, true), false));
                sb.AppendLine(" 20");
                sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(airportModel.Lat, true, true), false));
                sb.AppendLine(" 40\n0.006\n  1");
                sb.AppendLine(airportModel.Id + " " +airportModel.Frequency);
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
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(artccModel.StartLon, false, true), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(artccModel.StartLat, true, true), false));
                    sb.AppendLine(" 11");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(artccModel.EndLon, false, true), false));
                    sb.AppendLine(" 21");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(artccModel.EndLat, true, true), false));
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
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, true), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, true), false));
                    sb.AppendLine($"  0\nTEXT\n  8\n{diagramType}__");
                    sb.AppendLine(" 10");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lon, false, true), false));
                    sb.AppendLine(" 20");
                    sb.AppendLine("  " + LatLonHelpers.CreateDecFormat(LatLonHelpers.CorrectLatLon(model.Lat, true, true), false));
                    sb.AppendLine(" 40\n0.006\n  1");
                    sb.AppendLine(model.Id + " " + model.Frequency);
                }
                sb.AppendLine("  0\nENDSEC");
                //sb.AppendLine("  0\nEOF");
                //File.WriteAllText(_outputFilePath + $"\\{diagramType}.dxf", sb.ToString());
                OneFileSB.Append(sb.ToString());

            }
        }
    }
}
