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
        }

        public void CreateSidDxf()
        {
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb = new StringBuilder();

            string diagramName = "";
            foreach (SctSidStarModel sctSid in _sctFileModel.SctSidSection)
            {
                diagramName = sctSid.DiagramName;
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

            File.WriteAllText(_outputFilePath + @"\TestSidDXFCreation.dxf", sb.ToString());
        }

        public void CreateStarDxf()
        {
            string _outputFilePath = @"C:\Users\nikol\Desktop\DXF Conversions";
            StringBuilder sb = new StringBuilder();

            string diagramName = "";
            foreach (SctSidStarModel sctSid in _sctFileModel.SctStarSection)
            {
                diagramName = sctSid.DiagramName;
                diagramName = diagramName.Replace('/', ' ');

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

            File.WriteAllText(_outputFilePath + @"\TestSTARDXFCreation.dxf", sb.ToString());
        }
    }
}
