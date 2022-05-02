using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FeBuddyLibrary.Dxf.Models;

namespace FeBuddyLibrary.Dxf.Data
{
    public class DataFunctions
    {
        private string _sectorFileText;
        private string _sectorFilePath;
        private SctFileModel _sctFileModel;

        // This is a dictionary for all Fixes, VOR, NDB, Airport lat an lon positions based on the "name"
        public Dictionary<string, Dictionary<string, string>> _navaidPositions = new Dictionary<string, Dictionary<string, string>>();

        public void CreateDxfFile(string FullFilePath, string outputFilePath)
        {
            _sectorFilePath = FullFilePath;

            _sectorFileText = File.ReadAllText(_sectorFilePath) + "\n[END]";

            // TODO - What happens if a sct file does not have one or more of the following categories? 

            _sctFileModel = new SctFileModel()
            {
                SctFileColors = GetSctColors(GetSctFileSection("Colors")),
                SctInfoSection = GetSctInfo(GetSctFileSection("[INFO]")),
                SctVORSection = GetSctNnbsAndVORS(GetSctFileSection("[VOR]")),
                SctNDBSection = GetSctNnbsAndVORS(GetSctFileSection("[NDB]")),
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

            SctDxf sctDxf = new SctDxf(_sctFileModel, outputFilePath, _navaidPositions);
        }

        private static bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\f';
        }

        private (bool, string) VerifyLine(string line)
        {
            bool outputBool = false;
            string outputString = line;

            // Blank or empty line
            if (string.IsNullOrEmpty(outputString)) return (outputBool, outputString);

            // Commented out line
            if (outputString.Substring(0, 1) == ";" || outputString.Substring(0, 1) == "//") return (outputBool, outputString);

            // Strip comments from Line
            int pos = outputString.IndexOf(';');
            if (pos > -1) outputString = outputString.Substring(0, pos);
            pos = outputString.IndexOf("//");
            if (pos > -1) outputString = outputString.Substring(0, pos);

            // Trim the end of the line.
            outputString = outputString.TrimEnd(new char[] { ' ', '\t', '\r', '\n', '\f' });

            // Check our modified string and make sure its not null or empty
            if (string.IsNullOrEmpty(outputString)) return (outputBool, outputString);

            // The string has been completely formatted, we can now return the string.
            outputBool = true;
            return (outputBool, outputString);
        }

        private List<SctLabelModel> GetSctLabels(string[] vs)
        {
            var result = new List<SctLabelModel>();

            Regex sRegexStaticText = new Regex(@"^""(.+)""\s+(\S+)\s+(\S+)\s+(\S+)", RegexOptions.Compiled);

            foreach (string line in vs)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                var match = sRegexStaticText.Match(_line);

                if (match.Success)
                {
                    result.Add(new SctLabelModel()
                    {
                        LabelText = match.Groups[1].Value,
                        Lat = match.Groups[2].Value,
                        Lon = match.Groups[3].Value,
                        Color = match.Groups[4].Value
                    });
                }

                //string[] splitLine;
                //string labelText;
                //string comments = "";
                
                //if (_line.Contains('"'))
                //{
                //    if (_line.Contains(';'))
                //    {
                //        comments = _line[_line.IndexOf(';')..];
                //        labelText = _line[..(_line.LastIndexOf('"') + 1)];
                //        splitLine = _line[(_line.LastIndexOf('"') + 1).._line.IndexOf(';')].Trim().Split(' ');
                //    }
                //    else
                //    {
                //        labelText = _line[..(_line.LastIndexOf('"') + 1)];
                //        splitLine = _line[(_line.LastIndexOf('"') + 1)..].Trim().Split(' ');
                //    }

                //    if (splitLine.Length > 2)
                //    {
                //        SctLabelModel model = new SctLabelModel()
                //        {
                //            LabelText = labelText.Replace("\"", string.Empty),
                //            Lat = splitLine[0],
                //            Lon = splitLine[1],
                //            Color = splitLine[2]
                //        };
                //        result.Add(model);
                //    }
                //}
            }

            return result;
        }

        private List<SctRegionModel> GetSctRegions(string[] vs)
        {
            // Current Sct_2_dxf.exe tool does not convert regions.... hmmm
            var result = new List<SctRegionModel>();
            SctRegionModel regionModel = null;

            Regex sRegexRegionBegin = new Regex(@"^(\S+)\s+(\S+)\s+(\S+)", RegexOptions.Compiled);
            Regex sRegexRegionContinuation = new Regex(@"^\s+(\S+)\s+(\S+)", RegexOptions.Compiled);

            foreach (string line in vs)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                if (!IsWhitespace(_line[0]))
                {
                    var match = sRegexRegionBegin.Match(_line);
                    if (match.Success)
                    {
                        if (regionModel is not null)
                        {
                            result.Add(regionModel);
                        }
                        regionModel = new SctRegionModel()
                        {
                            RegionColorName = match.Groups[1].Value,
                            Lat = match.Groups[2].Value,
                            Lon = match.Groups[3].Value,
                            AdditionalRegionInfo = new List<RegionPolygonPoints>()
                        };
                    }
                }
                else
                {
                    var match = sRegexRegionContinuation.Match(_line);
                    if (match.Success)
                    {
                        if (regionModel is null)
                        {
                            throw new Exception("Found region continuation without having found the region start");
                        }

                        regionModel.AdditionalRegionInfo.Add(new RegionPolygonPoints()
                        {
                            Lat = match.Groups[1].Value,
                            Lon = match.Groups[2].Value
                        });
                    }
                }
            }

            if (regionModel is not null)
            {
                result.Add(regionModel);
            }
            return result;
            //    if (string.IsNullOrEmpty(_line) || _line[0] == ';' || _line.Length < 29)
            //    {
            //        continue;
            //    }

            //    if (_line.Contains(';'))
            //    {
            //        _line = line[.._line.IndexOf(';')];
            //    }

            //    if (_line.Trim().Split(' ').Length == 3)
            //    {
            //        if (regionModel is not null)
            //        {
            //            result.Add(regionModel);
            //        }
            //        string regionColorName = _line.Split(' ')[0].Trim().ToString();
            //        regionModel = new SctRegionModel()
            //        {
            //            RegionColorName = regionColorName,
            //            Lat = _line.Split(' ')[1].Trim(),
            //            Lon = _line.Split(' ')[2].Trim(),
            //            AdditionalRegionInfo = new List<RegionPolygonPoints>()
            //        };
            //        continue;
            //    }

            //    regionModel.AdditionalRegionInfo.Add(new RegionPolygonPoints()
            //    {
            //        Lat = _line.Trim().Split(' ')[0].Trim(),
            //        Lon = _line.Trim().Split(' ')[1].Trim(),
            //    });
            //}
        }

        private List<SctGeoModel> GetSctGeo(string[] vs)
        {
            var result = new List<SctGeoModel>();
            Regex sRegexGeoSegment = new Regex(@"^(\S+)\s+(\S+)\s+(\S+)\s+(\S+)(?:\s+(\S+))?", RegexOptions.Compiled);

            foreach (string line in vs)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                var match = sRegexGeoSegment.Match(_line);
                if (match.Success)
                {
                    result.Add(new SctGeoModel()
                    {
                        StartLat = match.Groups[1].Value,
                        StartLon = match.Groups[2].Value,
                        EndLat = match.Groups[3].Value,
                        EndLon = match.Groups[4].Value,
                        Color = match.Groups[5].Value
                    });
                }
                //string[] splitLine;
                //string comments = "";
                //if (_line.Contains(';'))
                //{
                //    comments = _line[_line.IndexOf(';')..];
                //    splitLine = _line[.._line.IndexOf(';')].Trim().Split(' ');
                //}
                //else
                //{
                //    splitLine = _line.Split(' ');
                //}

                //if (splitLine.Length > 3)
                //{
                //    SctGeoModel sctRunwayModel = new SctGeoModel()
                //    {
                //        StartLat = splitLine[0],
                //        StartLon = splitLine[1],
                //        EndLat = splitLine[2],
                //        EndLon = splitLine[3],
                //        Color = splitLine[4]
                //    };

                //    result.Add(sctRunwayModel);
                //}
            }

            return result;
        }

        private List<SctRunwayModel> GetSctRunways(string[] vs)
        {
            var result = new List<SctRunwayModel>();
            Regex sRegexRunway = new Regex(@"^(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)", RegexOptions.Compiled);

            foreach (string line in vs)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                var match = sRegexRunway.Match(_line);
                if (match.Success)
                {
                    result.Add(new SctRunwayModel()
                    {
                        RunwayNumber = match.Groups[1].Value,
                        OppositeRunwayNumber = match.Groups[2].Value,
                        MagRunwayHeading = match.Groups[3].Value,
                        OppositeMagRunwayHeading = match.Groups[4].Value,
                        StartLat = match.Groups[5].Value,
                        StartLon = match.Groups[6].Value,
                        EndLat = match.Groups[7].Value,
                        EndLon = match.Groups[8].Value,
                    });
                }
                //string[] splitLine;
                //string comments = "";
                //if (_line.Contains(';'))
                //{
                //    comments = _line[_line.IndexOf(';')..];
                //    splitLine = _line[.._line.IndexOf(';')].Trim().Split(' ');
                //}
                //else
                //{
                //    splitLine = _line.Split(' ');
                //}

                //if (splitLine.Length > 7)
                //{
                //    SctRunwayModel sctRunwayModel = new SctRunwayModel()
                //    {
                //        RunwayNumber = splitLine[0],
                //        OppositeRunwayNumber = splitLine[1],
                //        MagRunwayHeading = splitLine[2],
                //        OppositeMagRunwayHeading = splitLine[3],
                //        StartLat = splitLine[4],
                //        StartLon = splitLine[5],
                //        EndLat = splitLine[6],
                //        EndLon = splitLine[7]
                //    };

                //    if (!string.IsNullOrWhiteSpace(comments))
                //    {
                //        sctRunwayModel.Comments = comments;
                //    }
                //    result.Add(sctRunwayModel);
                //}
            }

            return result;
        }

        private List<SctFixesModel> GetSctFixes(string[] vs)
        {
            var result = new List<SctFixesModel>();
            Regex sRegexFix = new Regex(@"^(\S+)\s+(\S+)\s+(\S+)", RegexOptions.Compiled);

            foreach (string line in vs)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                var match = sRegexFix.Match(_line);
                if (match.Success)
                {
                    if (!_navaidPositions.ContainsKey(match.Groups[1].Value))
                    {
                        _navaidPositions[match.Groups[1].Value] = new Dictionary<string, string>()
                        {
                            {"Lat", match.Groups[2].Value },
                            {"Lon", match.Groups[3].Value },
                        };
                    }

                    result.Add(new SctFixesModel()
                    {
                        FixName = match.Groups[1].Value,
                        Lat = match.Groups[2].Value,
                        Lon = match.Groups[3].Value,
                    });
                }

                //string[] splitLine;
                //string comments = "";
                //if (_line.Contains(';'))
                //{
                //    comments = _line[_line.IndexOf(';')..];
                //    splitLine = _line[.._line.IndexOf(';')].Trim().Split(' ');
                //}
                //else
                //{
                //    splitLine = _line.Split(' ');
                //}

                //if (splitLine.Length > 2)
                //{
                //    SctFixesModel model = new SctFixesModel()
                //    {
                //        FixName = splitLine[0],
                //        Lat = splitLine[1],
                //        Lon = splitLine[2],
                //    };
                //    if (!string.IsNullOrWhiteSpace(comments))
                //    {
                //        model.Comments = comments;
                //    }
                //    result.Add(model);
                //}
            }
            return result;
        }

        private List<VORNDBModel> GetSctNnbsAndVORS(string[] vs)
        {
            List<VORNDBModel> results = new List<VORNDBModel>();
            Regex sRegexVorNdb = new Regex(@"^(\S+)\s+(\S+)\s+(\S+)\s+(\S+)", RegexOptions.Compiled);

            foreach (string line in vs)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                var match = sRegexVorNdb.Match(_line);
                if (match.Success)
                {
                    if (!_navaidPositions.ContainsKey(match.Groups[1].Value))
                    {
                        _navaidPositions[match.Groups[1].Value] = new Dictionary<string, string>()
                        {
                            {"Lat", match.Groups[3].Value },
                            {"Lon", match.Groups[4].Value },
                        };
                    }

                    results.Add(new VORNDBModel()
                    {
                        Id = match.Groups[1].Value,
                        Frequency = match.Groups[2].Value,
                        Lat = match.Groups[3].Value,
                        Lon = match.Groups[4].Value,
                    });
                }
                //if (_line.Split(' ').Length > 3)
                //{
                //    string[] splitLine;
                //    string comments = "";
                //    if (_line.Contains(';'))
                //    {
                //        comments = _line[_line.IndexOf(';')..];
                //        splitLine = _line[.._line.IndexOf(';')].Trim().Split(' ');
                //    }
                //    else
                //    {
                //        splitLine = _line.Split(' ');
                //    }

                //    VORNDBModel newModel = new VORNDBModel()
                //    {
                //        Id = splitLine[0],
                //        Frequency = splitLine[1],
                //        Lat = splitLine[2],
                //        Lon = splitLine[3],
                //    };

                //    if (!string.IsNullOrEmpty(comments))
                //    {
                //        newModel.Comments = comments;
                //    }
                //    results.Add(newModel);
                //}
            }
            return results;
        }

        private List<SctArtccModel> ParseArtccModels(string[] vs)
        {
            // Incharge of doing the [ARTCC], [ARTCC HIGH], [ARTCC LOW], [AIRWAY LOW], [AIRWAY HIGH]
            List<SctArtccModel> results = new List<SctArtccModel>();

            // Is this ALL "Boundaries"????????
            Regex sRegexBoundarySegment = new Regex(@"^(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)", RegexOptions.Compiled);

            foreach (string line in vs)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                var match = sRegexBoundarySegment.Match(_line);
                if (match.Success)
                {
                    

                    results.Add(new SctArtccModel()
                    {
                        Name = match.Groups[1].Value,
                        StartLat = match.Groups[2].Value,
                        StartLon = match.Groups[3].Value,
                        EndLat = match.Groups[4].Value,
                        EndLon = match.Groups[5].Value
                    });
                }
                //if (_line.Split(' ').Length >= 5)
                //{
                //    string[] splitLine;
                //    string comments = "";
                //    if (_line.Contains(';'))
                //    {
                //        comments = _line[_line.IndexOf(';')..];
                //        splitLine = _line[.._line.IndexOf(';')].Trim().Split(' ');
                //    }
                //    else
                //    {
                //        splitLine = _line.Split(' ');
                //    }

                //    SctArtccModel airportModel = new SctArtccModel()
                //    {
                //        Name = splitLine[0],
                //        StartLat = splitLine[1],
                //        StartLon = splitLine[2],
                //        EndLat = splitLine[3],
                //        EndLon = splitLine[4],
                //    };

                //    if (!string.IsNullOrEmpty(comments))
                //    {
                //        airportModel.Comments = comments;
                //    }
                //    results.Add(airportModel);
                //}
            }
            return results;
        }

        private List<SctSidStarModel> GetSctSidsAndStars(string[] vs)
        {
            List<SctSidStarModel> result = new List<SctSidStarModel>();

            Regex sRegexDiagramBegin = new Regex(@"^(.{26})\s*(\S+)\s+(\S+)\s+(\S+)\s+(\S+)(?:\s+(\S+))?", RegexOptions.Compiled);
            Regex sRegexDiagramContinuation = new Regex(@"^\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)(?:\s+(\S+))?", RegexOptions.Compiled);


            bool inDiagram = false;
            string diagramName = "";
            SctSidStarModel diagramModel = null;

            foreach (string line in vs)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                if (!IsWhitespace(_line[0]))
                {
                    var match = sRegexDiagramBegin.Match(_line);
                    if (match.Success)
                    {
                        if (diagramModel is not null)
                        {
                            result.Add(diagramModel);
                        }
                        diagramModel = new SctSidStarModel()
                        {
                            DiagramName = match.Groups[1].Value,
                            StartLat = match.Groups[2].Value,
                            StartLon = match.Groups[3].Value,
                            EndLat = match.Groups[4].Value,
                            EndLon = match.Groups[5].Value,
                            Color = match.Groups[6].Value,
                            AdditionalLines = new List<SctAditionalDiagramLineSegments>()
                        };
                    }
                }
                else
                {
                    var match = sRegexDiagramContinuation.Match(_line);
                    if (match.Success)
                    {
                        if (diagramModel is null)
                        {
                            throw new Exception("Found diagram continuation without finding the diagram start");
                        }

                        diagramModel.AdditionalLines.Add(new SctAditionalDiagramLineSegments()
                        {
                            StartLat = match.Groups[1].Value,
                            StartLon = match.Groups[2].Value,
                            EndLat = match.Groups[3].Value,
                            EndLon = match.Groups[4].Value,
                            Color = match.Groups[5].Value,
                        });
                    }
                }

                //if (_line.Length < 26)
                //{
                //    // Line does not have enough characters to do the required parsing so we will just ignore it.
                //    inDiagram = false;
                //    continue;
                //}
                //if (!inDiagram && _line[..26].Trim() == "")
                //{
                //    // Line is blank or We have not read the first part of the diagram yet
                //    continue;
                //}
                //if (!string.IsNullOrWhiteSpace(_line[..26].Trim()) && !_line[..26].Contains('\t') && _line[..26].Trim() != diagramName)
                //{
                //    // Line starts with diagram name so we are either a new diagram or the first diagram
                //    if (inDiagram && diagramModel is not null)
                //    {
                //        if (diagramModel.Color is null)
                //        {
                //            diagramModel.Color = "NOCOLOR";
                //        }
                //        // We have completed the previous diagram so we need to add it to our list
                //        result.Add(diagramModel);
                //    }
                //    inDiagram = true;
                //    diagramName = _line[..26].Trim();
                //    diagramModel = new SctSidStarModel()
                //    {
                //        DiagramName = diagramName,
                //        StartLat = _line.Split(' ')[0],
                //        StartLon = _line.Split(' ')[1],
                //        EndLat = _line.Split(' ')[2],
                //        EndLon = _line.Split(' ')[3],
                //        AdditionalLines = new List<SctAditionalDiagramLineSegments>()
                //    };
                //    if (_line.Split(' ').Length > 4)
                //    {
                //        // Some diagrams name lines do not have colors assigned to them. If it is there put it in else ignore.
                //        diagramModel.Color = _line.Split(' ')[4];
                //    }
                //    continue;
                //}

                //// Some lines do not have colors associated with it. we have to check that. 
                //if (_line.Split(' ').Length > 4)
                //{
                //    // We are in a diagram this additional line segment goes with it
                //    diagramModel.AdditionalLines.Add(new SctAditionalDiagramLineSegments()
                //    {
                //        StartLat = _line.Split(' ')[0],
                //        StartLon = _line.Split(' ')[1],
                //        EndLat = _line.Split(' ')[2],
                //        EndLon = _line.Split(' ')[3],
                //        Color = _line.Split(' ')[4],
                //    });
                //    if (diagramModel.Color is null)
                //    {
                //        diagramModel.Color = _line.Split(' ')[4];
                //    }
                //}
                //else
                //{
                //    // We are in a diagram this additional line segment goes with it
                //    diagramModel.AdditionalLines.Add(new SctAditionalDiagramLineSegments()
                //    {
                //        StartLat = _line.Split(' ')[0],
                //        StartLon = _line.Split(' ')[1],
                //        EndLat = _line.Split(' ')[2],
                //        EndLon = _line.Split(' ')[3],
                //        Color = diagramModel.Color
                //    });
                //}
            }
            // This is the last diagram we need to be sure to add it to our list.
            if (diagramModel is not null)
            {
                //if (diagramModel.Color is null)
                //{
                //    diagramModel.Color = "NOCOLOR";
                //}
                result.Add(diagramModel);
            }
            return result;
        }

        private List<SctAirportModel> GetSctAirports(string[] vs)
        {
            List<SctAirportModel> results = new List<SctAirportModel>();
            Regex sRegexAirport = new Regex(@"^(\S+)\s+(\S+)\s+(\S+)\s+(\S+)(?:\s+([ABCDEG]))?", RegexOptions.Compiled);

            foreach (string line in vs)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                var match = sRegexAirport.Match(_line);
                if (match.Success)
                {
                    if (!_navaidPositions.ContainsKey(match.Groups[1].Value))
                    {
                        _navaidPositions[match.Groups[1].Value] = new Dictionary<string, string>()
                        {
                            {"Lat", match.Groups[3].Value },
                            {"Lon", match.Groups[4].Value },
                        };
                    }

                    results.Add(new SctAirportModel()
                    {
                        Id = match.Groups[1].Value,
                        Frequency = match.Groups[2].Value,
                        Lat = match.Groups[3].Value,
                        Lon = match.Groups[4].Value,
                        Airspace = match.Groups[5].Value,
                    });
                }

                //if (_line.Split(' ').Length > 3)
                //{
                //    string[] splitLine;
                //    string comments = "";
                //    if (_line.Contains(';'))
                //    {
                //        comments = _line[_line.IndexOf(';')..];
                //        splitLine = _line[.._line.IndexOf(';')].Trim().Split(' ');
                //    }
                //    else
                //    {
                //        splitLine = _line.Split(' ');
                //    }

                //    SctAirportModel airportModel = new SctAirportModel()
                //    {
                //        Id = splitLine[0],
                //        Frequency = splitLine[1],
                //        Lat = splitLine[2],
                //        Lon = splitLine[3],
                //    };

                //    if (splitLine.Length > 4)
                //    {
                //        airportModel.Airspace = splitLine[4];
                //    }

                //    if (!string.IsNullOrEmpty(comments))
                //    {
                //        airportModel.Comments = comments;
                //    }
                //    results.Add(airportModel);
                //}
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
            Regex sRegexColorDefinition = new Regex(@"^#define\s+(\S+)\s+(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach (string line in SctColorSection)
            {
                (bool goodLine, string _line) = VerifyLine(line);

                if (goodLine == false)
                {
                    continue;
                }

                var match = sRegexColorDefinition.Match(_line);
                if (match.Success)
                {
                    result.Add(new SctColorModel()
                    {
                        Name = match.Groups[1].Value,
                        ColorCode = match.Groups[2].Value
                    });
                }
                //result.Add(new SctColorModel()
                //{
                //    Name = _line.Split(' ')[1],
                //    ColorCode = _line.Split(' ')[2]
                //});
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

                "[FIXES]" => new Regex(@"(\[FIXES\](.|\n)*?(?=\[))").Matches(_sectorFileText).Cast<Match>().SelectMany(match => match.Value.Trim().Split("\n")).ToArray(),


                "[VOR]" => new Regex(@"(\[VOR\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[NDB]" => new Regex(@"(\[NDB\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[AIRPORT]" => new Regex(@"(\[AIRPORT\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                "[RUNWAY]" => new Regex(@"(\[RUNWAY\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
                //"[FIXES]" => new Regex(@"(\[FIXES\](.|\n)*?(?=\[))").Match(_sectorFileText).Value.Trim().Split("\n"),
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
