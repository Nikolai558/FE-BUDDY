using FeBuddyLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FeBuddyLibrary.DataAccess
{
    public class GetAptData
    {
        private List<AptModel> allAptModels = new List<AptModel>();

        /// <summary>
        /// Calls all internal functions inorder to process the Airport and Weather Station Data.
        /// </summary>
        /// <param name="effectiveDate">Airacc Effective Date (e.x. "2021-10-07")</param>
        /// <param name="artcc">Selected ARTCC (e.x. "FAA")</param>
        public void AptAndWxMain(string effectiveDate, string artcc)
        {
            ParseAptData(effectiveDate);
            WriteAptISR(artcc);
            WriteAptSctData();

            WriteEramAirportsXML(effectiveDate);
            StoreWaypointsXMLData();
            WriteRunwayData();

            WriteAptGeoMap();
            WriteAptTextGeoMap();

            ParseAndWriteWxStation(effectiveDate);
            WriteWxXmlOutput();
        }

        /// <summary>
        /// Write the vERAM Airports Geomap to an XML File
        /// </summary>
        public void WriteAptGeoMap()
        {
            string saveFilePath = $"{GlobalConfig.outputDirectory}\\VERAM\\AIRPORTS_GEOMAP.xml";
            StringBuilder sb = new StringBuilder();

            // Top of the XML file for the vERAM Airport Geomap
            sb.AppendLine("        <GeoMapObject Description=\"APT\" TdmOnly=\"false\">");
            sb.AppendLine("          <SymbolDefaults Bcg=\"10\" Filters=\"10\" Style=\"Airport\" Size=\"1\" />");
            sb.AppendLine("          <Elements>");

            foreach (AptModel apt in allAptModels)
            {
                // Airport status "O" means it is in operation. 
                if (apt.Status == "O")
                {
                    sb.AppendLine($"            <Element xsi:type=\"Symbol\" Filters=\"\" Size=\"2\" Lat=\"{ apt.Lat_Dec }\" Lon=\"{ apt.Lon_Dec }\" />");
                }
            }

            // Bottom of the XML file for the vERAM Airport Geomap
            sb.AppendLine("          </Elements>");
            sb.AppendLine("        </GeoMapObject>");

            File.WriteAllText(saveFilePath, sb.ToString());
        }

        /// <summary>
        /// Write the vERAM Airports Text Geomap to an XML File
        /// </summary>
        private void WriteAptTextGeoMap()
        {
            string saveFilePath = $"{GlobalConfig.outputDirectory}\\vERAM\\AIRPORT_TEXT_GEOMAP.xml";
            StringBuilder sb = new StringBuilder();

            // Top of the XML file for the vERAM Airport Text Geomap
            sb.AppendLine("        <GeoMapObject Description=\"AIRPORT TEXT\" TdmOnly=\"true\">");
            sb.AppendLine("          <TextDefaults Bcg=\"10\" Filters=\"10\" Size=\"1\" Underline=\"false\" Opaque=\"false\" XOffset=\"12\" YOffset=\"0\" />");
            sb.AppendLine("          <Elements>");

            string id;
            List<char> badChar = new List<char>() { '&', '"' };
            foreach (AptModel apt in allAptModels)
            {
                string tempAptName = apt.Name;

                // Grab the FAA or ICAO code for the current Airport.
                if (string.IsNullOrEmpty(apt.Icao))
                {
                    id = apt.Id;
                }
                else
                {
                    id = apt.Icao;
                }

                // Replace all "Bad Characters" with a "-"
                foreach (char bad in badChar)
                {
                    tempAptName = tempAptName.Replace(bad, '-');
                }

                // Verify the airport is operational.
                if (apt.Status == "O")
                {
                    sb.AppendLine($"            <Element xsi:type=\"Text\" Filters=\"\" Lat=\"{apt.Lat_Dec}\" Lon=\"{apt.Lon_Dec}\" Lines=\"{id} {tempAptName}\" />");
                }
            }

            // Bottom of the XML file for the vERAM Airport Text Geomap
            sb.AppendLine("          </Elements>");
            sb.AppendLine("        </GeoMapObject>");

            File.WriteAllText(saveFilePath, sb.ToString());
        }

        /// <summary>
        /// Parse through the NWS WX Station XML file and write the VRC [LABELS].sct2 file.
        /// </summary>
        /// <param name="effectiveDate">Airacc Effective Date (e.x. "2021-10-07")</param>
        private void ParseAndWriteWxStation(string effectiveDate)
        {
            string metarDataFilepath = $"{GlobalConfig.tempPath}\\{effectiveDate}_NWS-WX-STATIONS.xml";
            string outputFilepath = $"{GlobalConfig.outputDirectory}\\VRC\\[LABELS].sct2";
            Dictionary<string, List<double>> stationInfo = new Dictionary<string, List<double>>();
            Dictionary<string, List<string>> aptInfo = new Dictionary<string, List<string>>();
            StringBuilder sb = new StringBuilder();

            // VRC Labels Header
            sb.AppendLine("[LABELS]");

            // Load Metar XML File and populate our stationInfo Dictionary
            XDocument metarXML = XDocument.Load(metarDataFilepath);
            foreach (XElement xElement in metarXML.Descendants("station"))
            {
                string id = xElement.Element("station_id").Value;
                double lat = Convert.ToDouble(xElement.Element("latitude").Value);
                double lon = Convert.ToDouble(xElement.Element("longitude").Value);

                stationInfo.Add(id, new List<double> { lat, lon });
            }

            // Load all of our Airports and Populate our aptInfo Dictionary
            foreach (AptModel apt in allAptModels)
            {
                if (string.IsNullOrEmpty(apt.Icao))
                {
                    aptInfo.Add(apt.Id, new List<string> { apt.Name, apt.Lat_Dec, apt.Lon_Dec });
                }
                else
                {
                    aptInfo.Add(apt.Icao, new List<string> { apt.Name, apt.Lat_Dec, apt.Lon_Dec });
                }
            }

            string labelLineToBeAdded;
            foreach (string metar_id in stationInfo.Keys)
            {
                // Metar Id and Airport Code (FAA or ICAO) matches Exactly.
                if (aptInfo.Keys.Contains(metar_id))
                {
                    labelLineToBeAdded = $"\"{metar_id} {aptInfo[metar_id][0].Replace('"', '-')}\" {GlobalConfig.CreateDMS(stationInfo[metar_id][0], true)} {GlobalConfig.CreateDMS(stationInfo[metar_id][1], false)} 11579568";
                    sb.AppendLine(labelLineToBeAdded);

                }
                // Metar Id (Without Prefixing character) and Airport Code (FAA or ICAO) matches.
                else if (aptInfo.Keys.Contains(metar_id.Substring(1)))
                {
                    foreach (var apt in aptInfo.Keys)
                    {
                        // Find what airport matches the Metar ID (Without Prefixing character).
                        if (metar_id.Substring(1) == apt)
                        {
                            /* 
                             * In some cases there will be an airport that matches the Metar ID (Without Prefixing character) that does not actually belong to that airport.
                             * We have to verify the Degrees of both the airport and the WX station to make sure that station actually belongs to the airport.
                             * e.x. CWVI and WVI are two valid airports, however, they are in very different locations.
                             * The reason we do not check the entire Degrees, minutes, seconds, is because the full Lat and Lon can be different slightly.
                             */

                            string station_lat = stationInfo[metar_id][0].ToString().Split('.')[0];
                            string airport_lat = aptInfo[apt][1].Split('.')[0];
                            string station_lon = stationInfo[metar_id][1].ToString().Split('.')[0];
                            string airport_lon = aptInfo[apt][2].Split('.')[0];

                            if (station_lat == airport_lat && station_lon == airport_lon)
                            {
                                labelLineToBeAdded = $"\"{metar_id} {aptInfo[metar_id.Substring(1)][0].Replace('"', '-')}\" {GlobalConfig.CreateDMS(stationInfo[metar_id][0], true)} {GlobalConfig.CreateDMS(stationInfo[metar_id][1], false)} 11579568";
                                sb.AppendLine(labelLineToBeAdded);
                                break;
                            }
                        }
                    }
                }
                // Metar ID does not match, in any way, the airport code. If they do not match we do not want the WX station. 
                else
                {
                    continue;
                }
            }

            // Bottom of VRC [LABELS].sct2 file.
            sb.AppendLine("\n\n\n\n\n\n");

            File.WriteAllText(outputFilepath, sb.ToString());
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(outputFilepath));
        }

        /// <summary>
        /// Create the vSTARS and vERAM WX XML file.
        /// </summary>
        public static void WriteWxXmlOutput()
        {
            string readFilePath = $"{GlobalConfig.outputDirectory}\\VRC\\[LABELS].sct2";
            string saveFilePath = $"{GlobalConfig.outputDirectory}\\VERAM\\WX_STATIONS_GEOMAP.xml";
            StringBuilder sb = new StringBuilder();

            // Top of XML File
            sb.AppendLine("        <GeoMapObject Description=\"WX STATIONS\" TdmOnly=\"false\">");
            sb.AppendLine("          <TextDefaults Bcg=\"9\" Filters=\"9\" Size=\"1\" Underline=\"false\" Opaque=\"false\" XOffset=\"3\" YOffset=\"0\" />");
            sb.AppendLine("          <Elements>");

            // Read through the [Labels].sct2 file
            foreach (string line in File.ReadAllLines(readFilePath))
            {
                if (line != string.Empty)
                {
                    if (line.Substring(0, 1) != " " && line.Substring(0, 1) != "[")
                    {
                        string split = line.Substring(line.LastIndexOf('"') + 2);
                        List<string> splitValue = split.Split(' ').ToList();

                        if (splitValue.Count >= 3)
                        {
                            string printString = $"            <Element xsi:type=\"Text\" Filters=\"\" Lat=\"{GlobalConfig.CreateDecFormat(splitValue[0], true)}\" Lon=\"{GlobalConfig.CreateDecFormat(splitValue[1], true)}\" Lines={line.Substring(0, line.LastIndexOf('"') + 1)} />";
                            sb.AppendLine(printString);
                        }
                    }
                }
            }

            // End of XML file.
            sb.AppendLine("          </Elements>");
            sb.AppendLine("        </GeoMapObject>");

            File.WriteAllText(saveFilePath, sb.ToString());
        }

        /// <summary>
        /// Parse and sort through the FAA Apt.txt file and create the Airport Model.
        /// </summary>
        /// <param name="effectiveDate">Airacc Effective Date (e.x. "2021-10-07")</param>
        private void ParseAptData(string effectiveDate)
        {
            char[] removeChars = { ' ', '.' };
            AptModel airport = null;

            foreach (string line in File.ReadAllLines($"{GlobalConfig.tempPath}\\{effectiveDate}_APT\\APT.txt"))
            {
                if (line.Substring(0, 3) == "APT")
                {
                    if (airport != null && airport.Status == "O")
                    {
                        allAptModels.Add(airport);
                    }

                    airport = new AptModel
                    {
                        Runways = new List<RunwayModel>(),
                        Type = line.Substring(14, 13).Trim(removeChars),
                        Id = line.Substring(27, 4).Trim(removeChars),
                        Name = line.Substring(133, 50).Trim(removeChars),
                        Lat = GlobalConfig.CorrectLatLon(line.Substring(523, 15).Trim(removeChars), true, GlobalConfig.Convert),
                        Elv = Math.Round(double.Parse(line.Substring(578, 7).Trim(removeChars)), 0).ToString(),
                        ResArtcc = line.Substring(674, 4).Trim(removeChars),
                        Status = line.Substring(840, 2).Trim(removeChars),
                        Twr = line.Substring(980, 1).Trim(removeChars),
                        Ctaf = line.Substring(988, 7).Trim(removeChars),
                        Icao = line.Substring(1210, 7).Trim(removeChars),
                        Lon = GlobalConfig.CorrectLatLon(line.Substring(550, 15).Trim(removeChars), false, GlobalConfig.Convert)
                    };

                    airport.Lat_Dec = GlobalConfig.CreateDecFormat(airport.Lat, true);
                    airport.Lon_Dec = GlobalConfig.CreateDecFormat(airport.Lon, true);
                    airport.magVariation = line.Substring(586, 3).Trim();

                    if (airport.magVariation != string.Empty)
                    {
                        if (airport.magVariation[2] == 'W')
                        {
                            airport.magVariation = $"{airport.magVariation.Substring(0, 2)}";
                        }
                        else
                        {
                            airport.magVariation = $"-{airport.magVariation.Substring(0, 2)}";
                        }
                    }
                }
                else if (line.Substring(0, 3) == "RWY")
                {
                    RunwayModel rwy = new RunwayModel
                    {
                        RwyGroup = line.Substring(16, 7).Trim(),
                        RwyLength = line.Substring(23, 5).Trim(),
                        RwyWidth = line.Substring(28, 4).Trim(),
                        BaseRwyHdg = line.Substring(68, 3).Trim(),
                        RecRwyHdg = line.Substring(290, 3).Trim()
                    };

                    if (rwy.BaseRwyHdg != string.Empty && airport.magVariation != string.Empty)
                    {
                        rwy.BaseRwyHdg = (double.Parse(rwy.BaseRwyHdg) + double.Parse(airport.magVariation)).ToString();

                        if (double.Parse(rwy.BaseRwyHdg) > 360)
                        {
                            rwy.BaseRwyHdg = (double.Parse(rwy.BaseRwyHdg) - 360).ToString();
                        }
                        else if (double.Parse(rwy.BaseRwyHdg) <= 0)
                        {
                            rwy.BaseRwyHdg = (double.Parse(rwy.BaseRwyHdg) + 360).ToString();
                        }
                    }

                    if (rwy.RecRwyHdg != string.Empty && airport.magVariation != string.Empty)
                    {
                        rwy.RecRwyHdg = (double.Parse(rwy.RecRwyHdg) + double.Parse(airport.magVariation)).ToString();

                        if (double.Parse(rwy.RecRwyHdg) > 360)
                        {
                            rwy.RecRwyHdg = (double.Parse(rwy.RecRwyHdg) - 360).ToString();
                        }
                        else if (double.Parse(rwy.RecRwyHdg) <= 0)
                        {
                            rwy.RecRwyHdg = (double.Parse(rwy.RecRwyHdg) + 360).ToString();
                        }
                    }

                    if (line.Substring(88, 15).Trim() != string.Empty && line.Substring(115, 15).Trim() != string.Empty)
                    {
                        rwy.BaseStartLat = GlobalConfig.CorrectLatLon(line.Substring(88, 15).Trim(), true, GlobalConfig.Convert);
                        rwy.BaseStartLon = GlobalConfig.CorrectLatLon(line.Substring(115, 15).Trim(), false, GlobalConfig.Convert);
                    }

                    if (line.Substring(310, 15).Trim() != string.Empty && line.Substring(337, 15).Trim() != string.Empty)
                    {
                        rwy.BaseEndLat = GlobalConfig.CorrectLatLon(line.Substring(310, 15).Trim(), true, GlobalConfig.Convert);
                        rwy.BaseEndLon = GlobalConfig.CorrectLatLon(line.Substring(337, 15).Trim(), false, GlobalConfig.Convert);
                    }

                    airport.Runways.Add(rwy);
                }
            }

            if (airport.Status == "O")
            {
                allAptModels.Add(airport);
            }

            GlobalConfig.allAptModelsForCheck = allAptModels;
        }

        /// <summary>
        /// Write all the airports to vSTARS and vERAM Airports xml. 
        /// </summary>
        /// <param name="effectiveDate">Airacc Effective Date (e.x. "2021-10-07")</param>
        public void WriteEramAirportsXML(string effectiveDate)
        {
            string filePath = $"{GlobalConfig.outputDirectory}\\VERAM\\Airports.xml";
            List<Airport> allAptForXML = new List<Airport>();

            XmlRootAttribute xmlRootAttribute = new XmlRootAttribute("Airports");
            XmlSerializer serializer = new XmlSerializer(typeof(Airport[]), xmlRootAttribute);
            foreach (AptModel aptModel in allAptModels)
            {
                bool doNotUseThisRwy = false;
                string aptIdTempVar;
                List<Runway> rwysTempVar = new List<Runway>();

                if (aptModel.Icao != string.Empty)
                {
                    aptIdTempVar = aptModel.Icao;
                }
                else
                {
                    aptIdTempVar = aptModel.Id;
                }

                foreach (RunwayModel runwayModel in aptModel.Runways)
                {
                    List<string> rwyProperties = new List<string>()
                    {
                        runwayModel.RwyGroup,
                        runwayModel.BaseRwy,
                        runwayModel.RecRwy,
                        runwayModel.RwyLength,
                        runwayModel.RwyWidth,
                        runwayModel.BaseStartLat,
                        runwayModel.BaseStartLon,
                        runwayModel.BaseEndLat,
                        runwayModel.BaseEndLon,
                        runwayModel.RecStartLat,
                        runwayModel.RecStartLon,
                        runwayModel.RecEndLat,
                        runwayModel.RecEndLon,
                        runwayModel.BaseRwyHdg,
                        runwayModel.RecRwyHdg
                    };

                    foreach (string stringProperty in rwyProperties)
                    {
                        if (string.IsNullOrEmpty(stringProperty))
                        {
                            doNotUseThisRwy = true;
                            break;
                        }
                    }

                    if (doNotUseThisRwy == false)
                    {
                        Runway rwy1 = new Runway
                        {
                            ID = runwayModel.BaseRwy,
                            Heading = runwayModel.BaseRwyHdg,
                            Length = runwayModel.RwyLength,
                            Width = runwayModel.RwyWidth,
                            StartLoc = new StartLoc
                            {
                                Lon = GlobalConfig.CreateDecFormat(runwayModel.BaseStartLon, true),
                                Lat = GlobalConfig.CreateDecFormat(runwayModel.BaseStartLat, true)
                            },
                            EndLoc = new EndLoc
                            {
                                Lon = GlobalConfig.CreateDecFormat(runwayModel.BaseEndLon, true),
                                Lat = GlobalConfig.CreateDecFormat(runwayModel.BaseEndLat, true)
                            }
                        };

                        Runway rwy2 = new Runway
                        {
                            ID = runwayModel.RecRwy,
                            Heading = runwayModel.RecRwyHdg,
                            Length = runwayModel.RwyLength,
                            Width = runwayModel.RwyWidth,
                            StartLoc = new StartLoc
                            {
                                Lon = GlobalConfig.CreateDecFormat(runwayModel.RecStartLon, true),
                                Lat = GlobalConfig.CreateDecFormat(runwayModel.RecStartLat, true)
                            },
                            EndLoc = new EndLoc
                            {
                                Lon = GlobalConfig.CreateDecFormat(runwayModel.RecEndLon, true),
                                Lat = GlobalConfig.CreateDecFormat(runwayModel.RecEndLat, true)
                            }
                        };

                        rwysTempVar.Add(rwy1);
                        rwysTempVar.Add(rwy2);
                    }
                }

                Airport aptXMLFormat = new Airport
                {
                    ID = aptIdTempVar,
                    Name = aptModel.Name,
                    Elevation = aptModel.Elv,
                    MagVar = aptModel.magVariation,
                    Frequency = "0",
                    Location = new Location
                    {
                        Lon = aptModel.Lon_Dec,
                        Lat = aptModel.Lat_Dec
                    },
                    Runways = new Runways
                    {
                        Runway = rwysTempVar
                    }
                };

                if (rwysTempVar.Count >= 1 && aptXMLFormat.MagVar != "")
                {
                    allAptForXML.Add(aptXMLFormat);
                }
            }

            Airport[] aptArrayForXML = allAptForXML.ToArray();
            TextWriter writer = new StreamWriter(filePath);
            serializer.Serialize(writer, aptArrayForXML);
            writer.Close();

            File.AppendAllText(filePath, $"\n<!--AIRAC_EFFECTIVE_DATE {effectiveDate}-->");
            File.Copy($"{GlobalConfig.outputDirectory}\\VERAM\\Airports.xml", $"{GlobalConfig.outputDirectory}\\VSTARS\\Airports.xml");
        }

        /// <summary>
        /// Save the airport waypoints into the Global Waypoints list so that we can create the XML file with ALL waypoints properly added. 
        /// </summary>
        public void StoreWaypointsXMLData()
        {
            List<Waypoint> waypointList = new List<Waypoint>();

            foreach (AptModel apt in allAptModels)
            {
                Location loc = new Location { Lat = apt.Lat_Dec, Lon = apt.Lon_Dec };
                Waypoint wpt = new Waypoint
                {
                    Type = "Airport",
                    Location = loc
                };

                if (apt.Icao != string.Empty)
                {
                    wpt.ID = apt.Icao;
                }
                else
                {
                    wpt.ID = apt.Id;
                }

                waypointList.Add(wpt);
            }

            foreach (Waypoint globalWaypoint in GlobalConfig.waypoints)
            {
                waypointList.Add(globalWaypoint);
            }

            GlobalConfig.waypoints = waypointList.ToArray();
        }

        /// <summary>
        /// Write the Airport In Scope Reference. 
        /// </summary>
        /// <param name="Artcc">Selected ARTCC (e.x. "FAA")</param>
        private void WriteAptISR(string Artcc)
        {
            string filePath = $"{GlobalConfig.outputDirectory}\\ALIAS\\ISR_APT.txt";
            StringBuilder sb = new StringBuilder();

            foreach (AptModel apt in allAptModels)
            {
                string icao;
                string tower;
                string elvation;

                if (apt.Icao == "") { icao = "N/A"; } else { icao = apt.Icao; }

                if (apt.Twr == "Y") { tower = "Towered"; } else { tower = "Not Towered"; }

                elvation = apt.Elv;

                sb.AppendLine($".APT{apt.Id} .MSG {Artcc}_ISR *** FAA-{apt.Id} : ICAO-{icao} ___ {apt.Name} {apt.Type} ___ {elvation}'MSL ___ {tower} ___ {apt.ResArtcc}");

                if (apt.Icao != "")
                {
                    sb.AppendLine($".APT{apt.Icao} .MSG {Artcc}_ISR *** FAA-{apt.Id} : ICAO-{icao} ___ {apt.Name} {apt.Type} ___ {elvation}'MSL ___ {tower} ___ {apt.ResArtcc}");
                }
            }

            File.WriteAllText(filePath, sb.ToString());
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\ALIAS\\AliasTestFile.txt", sb.ToString());
        }

        /// <summary>
        /// Write the VRC Runway data to the .sct2 file.
        /// </summary>
        private void WriteRunwayData()
        {
            string filePath = $"{GlobalConfig.outputDirectory}\\VRC\\[RUNWAY].sct2";
            string aptId;
            StringBuilder sb = new StringBuilder();

            // Runway File Header
            sb.AppendLine("[RUNWAY]");

            foreach (AptModel apt in allAptModels)
            {
                if (string.IsNullOrEmpty(apt.Icao)) { aptId = apt.Id; } else { aptId = apt.Icao; }

                foreach (RunwayModel runwayModel in apt.Runways)
                {
                    bool doNotUseThisRwy = false;

                    // Get all the runway properties
                    List<string> rwyProperties = new List<string>()
                    {
                        runwayModel.RwyGroup,
                        runwayModel.BaseRwy,
                        runwayModel.RecRwy,
                        runwayModel.RwyLength,
                        runwayModel.RwyWidth,
                        runwayModel.BaseStartLat,
                        runwayModel.BaseStartLon,
                        runwayModel.BaseEndLat,
                        runwayModel.BaseEndLon,
                        runwayModel.RecStartLat,
                        runwayModel.RecStartLon,
                        runwayModel.RecEndLat,
                        runwayModel.RecEndLon,
                        runwayModel.BaseRwyHdg,
                        runwayModel.RecRwyHdg
                    };

                    // Make sure NONE of the runway properties are empty or null. 
                    foreach (string stringProperty in rwyProperties)
                    {
                        if (string.IsNullOrEmpty(stringProperty))
                        {
                            doNotUseThisRwy = true;
                            break;
                        }
                    }

                    if (doNotUseThisRwy == false)
                    {
                        sb.AppendLine($"{runwayModel.BaseRwy.PadRight(4)}{runwayModel.RecRwy.PadRight(4)}{runwayModel.BaseRwyHdg.PadRight(4)}{runwayModel.RecRwyHdg.PadRight(4)}{runwayModel.BaseStartLat.PadRight(15)}{runwayModel.BaseStartLon.Substring(0, 14).PadRight(15)}{runwayModel.BaseEndLat.PadRight(15)}{runwayModel.BaseEndLon.Substring(0, 14).PadRight(14)}; {aptId} - {apt.Name}");
                    }
                }
            }

            File.WriteAllText(filePath, sb.ToString());
            File.AppendAllText(filePath, $"\n\n\n\n\n\n");
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(filePath));
        }

        /// <summary>
        /// Write the VRC Airport.Sct2 File
        /// </summary>
        private void WriteAptSctData()
        {
            string filePath = $"{GlobalConfig.outputDirectory}\\VRC\\[AIRPORT].sct2";
            StringBuilder sb = new StringBuilder();

            // Airport File Header
            sb.AppendLine("[AIRPORT]");

            foreach (AptModel dataforEachApt in allAptModels)
            {
                string id_icao;
                string ctaf;

                if (dataforEachApt.Icao == "") { id_icao = dataforEachApt.Id; } else { id_icao = dataforEachApt.Icao; }

                if (dataforEachApt.Ctaf == "") { ctaf = "000.000"; } else { ctaf = dataforEachApt.Ctaf; }

                sb.AppendLine($"{id_icao.PadRight(5)}{ctaf.PadRight(8)}{dataforEachApt.Lat} {dataforEachApt.Lon} ;{dataforEachApt.Name} {dataforEachApt.Type}");
            }

            // Airport File Ending
            sb.AppendLine("\n\n\n\n\n\n");

            File.WriteAllText(filePath, sb.ToString());
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(filePath));
        }
    }
}
