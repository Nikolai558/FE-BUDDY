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

            // TODO - What happens if a sct file does not have one or more of the following categories? 

            _sctFileModel = new SctFileModel()
            {
                SctFileColors = GetSctColors(GetSctFileSection("Colors")),
                SctInfoSection = GetSctInfo(GetSctFileSection("[INFO]")),
                SctVORSection = GetSctVors(GetSctFileSection("[VOR]")),
                SctNDBSection = GetSctNnbs(GetSctFileSection("[NDB]")),
                SctAirportSection = GetSctAirports(GetSctFileSection("[AIRPORT]")),
                SctRunwaySection = GetSctRunways(GetSctFileSection("[RUNWAY]")),
                SctFixesSection = GetSctFixes(GetSctFileSection("[FIXES]")),
                SctArtccSection = ParseArtccModels(GetSctFileSection("[ARTCC]")),
                SctArtccHighSection = ParseArtccModels(GetSctFileSection("[ARTCC HIGH]")),
                SctArtccLowSection = ParseArtccModels(GetSctFileSection("[ARTCC LOW]")),
                SctSidSection = GetSctSidsAndStars(GetSctFileSection("[SID]")),
                SctStarSection = GetSctSidsAndStars(GetSctFileSection("[STAR]")),
                SctLowAirwaySection = ParseArtccModels(GetSctFileSection("[LOW AIRWAY]")),
                SctHighAirwaySection = ParseArtccModels(GetSctFileSection("[HIGH AIRWAY]")),
                SctGeoSection = GetSctGeo(GetSctFileSection("[GEO]")),
                SctRegionsSection = GetSctRegions(GetSctFileSection("[REGIONS]")),
                SctLabelSection = GetSctLabels(GetSctFileSection("[LABELS]")),
            };

            SctDxf sctDxf = new SctDxf(_sctFileModel);
        }

        private List<SctLabelModel> GetSctLabels(string[] vs)
        {
            var result = new List<SctLabelModel>();
            return result;
            throw new NotImplementedException();
        }

        private List<SctRegionModel> GetSctRegions(string[] vs)
        {
            var result = new List<SctRegionModel>();
            return result;
            throw new NotImplementedException();
        }

        private List<SctGeoModel> GetSctGeo(string[] vs)
        {
            var result = new List<SctGeoModel>();
            return result;
            throw new NotImplementedException();
        }
        private List<SctFixesModel> GetSctFixes(string[] vs)
        {
            var result = new List<SctFixesModel>();
            return result;
            throw new NotImplementedException();
        }

        private List<SctRunwayModel> GetSctRunways(string[] vs)
        {
            var result = new List<SctRunwayModel>();
            return result;
            throw new NotImplementedException();
        }
        private List<VORNDBModel> GetSctNnbs(string[] vs)
        {
            var result = new List<VORNDBModel>();
            return result;
            throw new NotImplementedException();
        }

        private List<VORNDBModel> GetSctVors(string[] vs)
        {
            var result = new List<VORNDBModel>();
            return result;
            throw new NotImplementedException();
        }


        private List<SctArtccModel> ParseArtccModels(string[] vs)
        {
            // Incharge of doing the [ARTCC], [ARTCC HIGH], [ARTCC LOW], [AIRWAY LOW], [AIRWAY HIGH]
            List<SctArtccModel> results = new List<SctArtccModel>();

            foreach (string line in vs)
            {
                Regex trimmer = new Regex(@"\s\s+");
                string _line = trimmer.Replace(line, " ");

                if (_line.Split(' ').Length >= 5)
                {
                    string[] splitLine;
                    string comments = "";
                    if (_line.Contains(';'))
                    {
                        comments = _line[_line.IndexOf(';')..];
                        splitLine = _line[.._line.IndexOf(';')].Trim().Split(' ');
                    }
                    else
                    {
                        splitLine = _line.Split(' ');
                    }

                    SctArtccModel airportModel = new SctArtccModel()
                    {
                        Name = splitLine[0],
                        StartLat = splitLine[1],
                        StartLon = splitLine[2],
                        EndLat = splitLine[3],
                        EndLon = splitLine[4],
                    };

                    if (!string.IsNullOrEmpty(comments))
                    {
                        airportModel.Comments = comments;
                    }
                    results.Add(airportModel);
                }
            }
            return results;
        }

        private List<SctSidStarModel> GetSctSidsAndStars(string[] vs)
        {
            List<SctSidStarModel> result = new List<SctSidStarModel>();

            bool inDiagram = false;
            string diagramName = "";
            SctSidStarModel diagramModel = null;

            foreach (string line in vs)
            {
                if (line.Length < 26)
                {
                    // Line does not have enough characters to do the required parsing so we will just ignore it.
                    inDiagram = false;
                    continue;
                }
                if (!inDiagram && line[..26].Trim() == "")
                {
                    // Line is blank or We have not read the first part of the diagram yet
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(line[..26].Trim()) && !line[..26].Contains('\t') && line[..26].Trim() != diagramName)
                {
                    // Line starts with diagram name so we are either a new diagram or the first diagram
                    if (inDiagram && diagramModel is not null)
                    {
                        // We have completed the previous diagram so we need to add it to our list
                        result.Add(diagramModel);
                    }
                    inDiagram = true;
                    diagramName = line[..26].Trim();
                    diagramModel = new SctSidStarModel()
                    {
                        DiagramName = diagramName,
                        StartLat = line[26..].Split(' ')[0],
                        StartLon = line[26..].Split(' ')[1],
                        EndLat = line[26..].Split(' ')[2],
                        EndLon = line[26..].Split(' ')[3],
                        AdditionalLines = new List<SctAditionalDiagramLineSegments>()
                    };
                    if (line[26..].Split(' ').Length > 4)
                    {
                        // Some diagrams name lines do not have colors assigned to them. If it is there put it in else ignore.
                        diagramModel.Color = line[26..].Split(' ')[4];
                    }
                    continue;
                }

                // Some lines do not have colors associated with it. we have to check that. 
                if (line[26..].Split(' ').Length > 4)
                {
                    // We are in a diagram this additional line segment goes with it
                    diagramModel.AdditionalLines.Add(new SctAditionalDiagramLineSegments()
                    {
                        StartLat = line[26..].Split(' ')[0],
                        StartLon = line[26..].Split(' ')[1],
                        EndLat = line[26..].Split(' ')[2],
                        EndLon = line[26..].Split(' ')[3],
                        Color = line[26..].Split(' ')[4],
                    });
                }
                else
                {
                    // We are in a diagram this additional line segment goes with it
                    diagramModel.AdditionalLines.Add(new SctAditionalDiagramLineSegments()
                    {
                        StartLat = line[26..].Split(' ')[0],
                        StartLon = line[26..].Split(' ')[1],
                        EndLat = line[26..].Split(' ')[2],
                        EndLon = line[26..].Split(' ')[3],
                    });
                }
            }
            // This is the last diagram we need to be sure to add it to our list.
            if (inDiagram && diagramModel is not null)
            {
                result.Add(diagramModel);
            }
            return result;
        }

        private List<SctAirportModel> GetSctAirports(string[] vs)
        {
            List<SctAirportModel> results = new List<SctAirportModel>();

            foreach (string line in vs)
            {
                Regex trimmer = new Regex(@"\s\s+");
                string _line = trimmer.Replace(line, " ");

                if (_line.Split(' ').Length > 3)
                {
                    string[] splitLine;
                    string comments = "";
                    if (_line.Contains(';'))
                    {
                        comments = _line[_line.IndexOf(';')..];
                        splitLine = _line[.._line.IndexOf(';')].Trim().Split(' ');
                    }
                    else
                    {
                        splitLine = _line.Split(' ');
                    }

                    SctAirportModel airportModel = new SctAirportModel()
                    {
                        Id = splitLine[0],
                        Frequency = splitLine[1],
                        Lat = splitLine[2],
                        Lon = splitLine[3],
                    };

                    if (splitLine.Length > 4)
                    {
                        airportModel.Airspace = splitLine[4];
                    }

                    if (!string.IsNullOrEmpty(comments))
                    {
                        airportModel.Comments = comments;
                    }
                    results.Add(airportModel);
                }
            }
            return results;
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
            // http://regexstorm.net/tester (Regex testing website.)
            // https://regexr.com/ (Another Regex Website)

            // TODO - Alot of these are the same, could probably simplifiy this function.
            //string _category = $"\\{Category[..^1]}\\]";
            //string _regexPattern = "(" + _category + @"(.|\n)*?(?=\[))";
            //Regex _regex = new Regex(_regexPattern);

            //return Category switch
            //{
            //    "Colors" => new Regex(@"#define.+").Matches(_sectorFileText).Cast<Match>().Select(match => match.Value.Trim()).ToArray(),
            //    _ => _regex.Match(_sectorFileText).Value.Trim().Split("\n"),
            //};

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
