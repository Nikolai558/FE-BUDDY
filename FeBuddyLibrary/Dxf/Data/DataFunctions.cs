using FeBuddyLibrary.Dxf.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FeBuddyLibrary.Dxf.Data
{
    public class DataFunctions
    {
        private string _sectorFileText;
        private string _sectorFilePath;
        private SctFileModel _sctFileModel;
        
        public void CreateSctModel(string FullFilePath)
        {
            _sectorFilePath = FullFilePath;

            _sectorFileText = File.ReadAllText(_sectorFilePath) + "\n[END]";

            _sctFileModel = new SctFileModel()
            {
                SctFileColors = GetSctColors(GetSctFileSection("Colors")),
                SctInfoSection = GetSctInfo(GetSctFileSection("[INFO]")),
                SctVORSection = GetSctVors(GetSctFileSection("[VOR]")),
                SctNDBSection = GetSctNnbs(GetSctFileSection("[VOR]")),
                SctAirportSection = GetSctAirports(GetSctFileSection("[VOR]")),
                SctRunwaySection = GetSctRunways(GetSctFileSection("[VOR]")),
                SctFixesSection = GetSctFixes(GetSctFileSection("[VOR]")),
                SctArtccSection = GetSctArtcc(GetSctFileSection("[VOR]")),
                SctArtccHighSection = GetSctHighArtcc(GetSctFileSection("[VOR]")),
                SctArtccLowSection = GetSctLowArtcc(GetSctFileSection("[VOR]")),
                SctSidSection = GetSctSids(GetSctFileSection("[VOR]")),
                SctStarSection = GetSctStars(GetSctFileSection("[VOR]")),
                SctLowAirwaySection = GetSctLowAirway(GetSctFileSection("[VOR]")),
                SctHighAirwaySection = GetSctHighAirway(GetSctFileSection("[VOR]")),
                SctGeoSection = GetSctGeo(GetSctFileSection("[VOR]")),
                SctRegionsSection = GetSctRegions(GetSctFileSection("[VOR]")),
                SctLabelSection = GetSctLabels(GetSctFileSection("[VOR]")),
            };
        }

        private List<SctLabelModel> GetSctLabels(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctRegionModel> GetSctRegions(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctGeoModel> GetSctGeo(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctArtccModel> GetSctHighAirway(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctArtccModel> GetSctLowAirway(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctSidStarModel> GetSctStars(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctSidStarModel> GetSctSids(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctArtccModel> GetSctLowArtcc(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctArtccModel> GetSctHighArtcc(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctArtccModel> GetSctArtcc(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctFixesModel> GetSctFixes(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctRunwayModel> GetSctRunways(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<SctAirportModel> GetSctAirports(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<VORNDBModel> GetSctNnbs(string[] vs)
        {
            throw new NotImplementedException();
        }

        private List<VORNDBModel> GetSctVors(string[] vs)
        {
            throw new NotImplementedException();
        }

        private SctInfoModel GetSctInfo(string[] vs)
        {
            SctInfoModel result = new SctInfoModel()
            {
                Header = vs[0],
                SctFileName = vs[1],
                DefaultCallsign = vs[2],
                DefaultAirport = vs[3],
                CenterLat = vs[4],
                CenterLon = vs[5],
                NMPerLat = vs[6],
                NMPerLon = vs[7],
                MagneticVariation = vs[8],
                SctScale = vs[9],
                AdditionalLines = vs[10..]
            };

            return result;
        }

        private List<SctColorModel> GetSctColors(string[] SctColorSection)
        {
            List<SctColorModel> result = new List<SctColorModel>();

            foreach (string line in SctColorSection)
            {
                result.Add(new SctColorModel()
                {
                    Name = line.Split(' ')[1],
                    ColorCode = line.Split(' ')[2]
                });
            }
            return result;
        }

        private string[] GetSctFileSection(string Category)
        {
            return Category switch
            {
                "Colors" => new Regex(@"#define.+").Matches(_sectorFileText).Cast<Match>().Select(match => match.Value.Trim()).ToArray(),
                "[INFO]" => new Regex(@"(\[INFO\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[VOR]" => new Regex(@"(\[VOR\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[NDB]" => new Regex(@"(\[NDB\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[AIRPORT]" => new Regex(@"(\[AIRPORT\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[RUNWAY]" => new Regex(@"(\[RUNWAY\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[FIXES]" => new Regex(@"(\[FIXES\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[ARTCC]" => new Regex(@"(\[ARTCC\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[ARTCC HIGH]" => new Regex(@"(\[ARTCC HIGH\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[ARTCC LOW]" => new Regex(@"(\[ARTCC LOW\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[SID]" => new Regex(@"(\[SID\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[STAR]" => new Regex(@"(\[STAR\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[LOW AIRWAY]" => new Regex(@"(\[LOW AIRWAY\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[HIGH AIRWAY]" => new Regex(@"(\[HIGH AIRWAY\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[GEO]" => new Regex(@"(\[GEO\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[REGIONS]" => new Regex(@"(\[REGIONS\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[LABELS]" => new Regex(@"(\[LABELS\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
