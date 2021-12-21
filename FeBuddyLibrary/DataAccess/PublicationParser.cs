using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models;
using FeBuddyLibrary.Models.MetaFileModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FeBuddyLibrary.DataAccess
{
    public class PublicationParser
    {
        public static List<string> allArtcc = GlobalConfig.allArtcc;

        private string outputDirectory = GlobalConfig.outputDirectory + "PUBLICATIONS";

        public void WriteAirportInfoTxt(string responsibleArtcc)
        {
            Logger.LogMessage("INFO", "STARTING PUBLICATION PARSER");

            StringBuilder airportInArtccInfo = new StringBuilder();
            StringBuilder airportProcedureChanges = new StringBuilder();
            StringBuilder airportProcedures = new StringBuilder();

            if (responsibleArtcc == "FAA")
            {
                Logger.LogMessage("DEBUG", "RESPONSIBLE ARTCC IS FAA");

                foreach (string artcc in allArtcc)
                {
                    if (artcc == "FAA")
                    {
                        continue;
                    }
                    // This creates our Airport list that the ARTCC is responsible for.
                    airportInArtccInfo = new StringBuilder();
                    airportProcedureChanges = new StringBuilder();
                    airportProcedures = new StringBuilder();
                    foreach (AptModel airport in GlobalConfig.allAptModelsForCheck)
                    {
                        if (airport.ResArtcc == artcc)
                        {
                            string airportInfo = $"{airport.Id} - {airport.Icao}";
                            airportInArtccInfo.AppendLine(airportInfo);
                        }
                    }
                    string airportsInArtccFilePath = $"{outputDirectory}\\{artcc}_{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}\\{artcc}_Airports.txt";
                    CreateDirAndFile(airportsInArtccFilePath);
                    File.WriteAllText(airportsInArtccFilePath, airportInArtccInfo.ToString());

                    // this will create our ALL publications and Changes Publications file. 
                    foreach (MetaAirportModel apt in GlobalConfig.AllMetaFileAirports)
                    {
                        foreach (string resAirport in airportInArtccInfo.ToString().Split('\n'))
                        {
                            if (string.IsNullOrEmpty(resAirport))
                            {
                                continue;
                            }
                            string wantedAirport = resAirport.Substring(0, resAirport.IndexOf('-')).Trim();
                            if (wantedAirport == apt.AptIdent)
                            {
                                List<string> hasChanges = new List<string>();

                                airportProcedures.AppendLine($"[{apt.AptIdent}]");
                                foreach (MetaRecordModel recordModel in apt.Records)
                                {
                                    airportProcedures.AppendLine($"\t{recordModel.ChartName} | https://aeronav.faa.gov/d-tpp/{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}/{recordModel.PdfName}");
                                    if ((recordModel.UserAction != "" && recordModel.UserAction != " ") && !hasChanges.Contains(apt.AptIdent))
                                    {
                                        hasChanges.Add(apt.AptIdent);
                                    }
                                }

                                if (hasChanges.Contains(apt.AptIdent))
                                {
                                    airportProcedureChanges.AppendLine($"[{apt.AptIdent}]");
                                    foreach (MetaRecordModel recordModel1 in apt.Records)
                                    {
                                        if (recordModel1.UserAction != "" && recordModel1.UserAction != " ")
                                        {
                                            if (recordModel1.UserAction == "A")
                                            {
                                                airportProcedureChanges.AppendLine($"\t({recordModel1.UserAction}) {recordModel1.ChartName} | This procedure is new and has no previous version to compare.");
                                            }
                                            else if (recordModel1.UserAction == "D")
                                            {
                                                airportProcedureChanges.AppendLine($"\t({recordModel1.UserAction}) {recordModel1.ChartName} | This procedure has been deleted this AIRAC.");
                                            }
                                            else
                                            {
                                                string link = $"https://aeronav.faa.gov/d-tpp/{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}/compare_pdf/{recordModel1.PdfName.Substring(0, recordModel1.PdfName.Length - 4)}_cmp.pdf";
                                                airportProcedureChanges.AppendLine($"\t({recordModel1.UserAction}) {recordModel1.ChartName} | {link}");

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    string ProcedureFilePath = $"{outputDirectory}\\{artcc}_{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}\\Procedures.txt";
                    CreateDirAndFile(ProcedureFilePath);
                    File.WriteAllText(ProcedureFilePath, airportProcedures.ToString());

                    string ProcedureChangefilePath = $"{outputDirectory}\\{artcc}_{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}\\Procedure_Changes.txt";
                    CreateDirAndFile(ProcedureChangefilePath);

                    if (airportProcedureChanges.Length > 1)
                    {
                        airportProcedureChanges.Insert(0, "\n\n\nIn some cases, the link will return a 404 Error. This is because the FAA does not have a comparative document. This is common with DOD facilities.\n\n\n");
                    }
                    else
                    {
                        airportProcedureChanges.AppendLine("\n\n\nNo Changes this AIRAC\n\n\n");
                    }

                    File.WriteAllText(ProcedureChangefilePath, airportProcedureChanges.ToString());
                }
            }
            else if (allArtcc.Contains(responsibleArtcc))
            {
                Logger.LogMessage("DEBUG", $"RESPONSIBLE ARTCC IS {responsibleArtcc}");

                foreach (AptModel airport in GlobalConfig.allAptModelsForCheck)
                {
                    if (airport.ResArtcc == responsibleArtcc)
                    {
                        string output = $"{airport.Id} - {airport.Icao}";
                        airportInArtccInfo.AppendLine(output);
                    }
                }
                string airportsInArtccFilePath = $"{outputDirectory}\\{responsibleArtcc}_{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}\\Res_Artcc_Airports.txt";
                CreateDirAndFile(airportsInArtccFilePath);
                File.WriteAllText(airportsInArtccFilePath, airportInArtccInfo.ToString());

                foreach (MetaAirportModel apt in GlobalConfig.AllMetaFileAirports)
                {
                    foreach (string resAirport in airportInArtccInfo.ToString().Split('\n'))
                    {
                        if (string.IsNullOrEmpty(resAirport))
                        {
                            continue;
                        }
                        string wantedAirport = resAirport.Substring(0, resAirport.IndexOf('-')).Trim();
                        if (wantedAirport == apt.AptIdent)
                        {
                            List<string> hasChanges = new List<string>();

                            airportProcedures.AppendLine($"[{apt.AptIdent}]");
                            foreach (MetaRecordModel recordModel in apt.Records)
                            {
                                airportProcedures.AppendLine($"\t{recordModel.ChartName} | https://aeronav.faa.gov/d-tpp/{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}/{recordModel.PdfName}");
                                if ((recordModel.UserAction != "" && recordModel.UserAction != " ") && !hasChanges.Contains(apt.AptIdent))
                                {
                                    hasChanges.Add(apt.AptIdent);
                                }
                            }

                            if (hasChanges.Contains(apt.AptIdent))
                            {
                                airportProcedureChanges.AppendLine($"[{apt.AptIdent}]");
                                foreach (MetaRecordModel recordModel1 in apt.Records)
                                {
                                    if (recordModel1.UserAction != "" && recordModel1.UserAction != " ")
                                    {
                                        if (recordModel1.UserAction == "A")
                                        {
                                            airportProcedureChanges.AppendLine($"\t({recordModel1.UserAction}) {recordModel1.ChartName} | This procedure is new and has no previous version to compare.");
                                        }
                                        else if (recordModel1.UserAction == "D")
                                        {
                                            airportProcedureChanges.AppendLine($"\t({recordModel1.UserAction}) {recordModel1.ChartName} | This procedure has been deleted this AIRAC.");
                                        }
                                        else
                                        {
                                            string link = $"https://aeronav.faa.gov/d-tpp/{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}/compare_pdf/{recordModel1.PdfName.Substring(0, recordModel1.PdfName.Length - 4)}_cmp.pdf";
                                            airportProcedureChanges.AppendLine($"\t({recordModel1.UserAction}) {recordModel1.ChartName} | {link}");

                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                string ProcedureFilePath = $"{outputDirectory}\\{responsibleArtcc}_{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}\\Procedures.txt";
                CreateDirAndFile(ProcedureFilePath);
                File.WriteAllText(ProcedureFilePath, airportProcedures.ToString());

                string ProcedureChangefilePath = $"{outputDirectory}\\{responsibleArtcc}_{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}\\Procedure_Changes.txt";
                CreateDirAndFile(ProcedureChangefilePath);

                if (airportProcedureChanges.Length > 1)
                {
                    airportProcedureChanges.Insert(0, "\n\n\nIn some cases, the link will return a 404 Error. This is because the FAA does not have a comparative document. This is common with DOD facilities.\n\n\n");
                }
                else
                {
                    airportProcedureChanges.AppendLine("\n\n\nNo Changes this AIRAC\n\n\n");
                }
                File.WriteAllText(ProcedureChangefilePath, airportProcedureChanges.ToString());
            }
            else
            {
                Logger.LogMessage("ERROR", $"RESPONSIBLE ARTCC IS NOT VALID! NEW EXCEPTION THROWN, THIS CAUSED CRASH.");

                throw new NotImplementedException();
            }
            Logger.LogMessage("INFO", "COMPLETED PUBLICATION PARSER");

        }

        /// <summary>
        /// Create the Directories and File for the full file path put in.
        /// </summary>
        /// <param name="fullFilePath">Full File Path</param>
        private void CreateDirAndFile(string fullFilePath)
        {
            Logger.LogMessage("DEBUG", $"TRYING TO CREATE {fullFilePath}");

            if (!Directory.Exists(fullFilePath.Substring(0, fullFilePath.LastIndexOf('\\'))))
            {
                Logger.LogMessage("DEBUG", $"DIRECTORY DOES NOT EXIST, CREATING DIRECTORY FOR {fullFilePath}");

                Directory.CreateDirectory(fullFilePath.Substring(0, fullFilePath.LastIndexOf('\\')));
            }

            if (!File.Exists(fullFilePath))
            {
                Logger.LogMessage("WARNING", $"FILE ALREADY EXITS FOR {fullFilePath}");

                //File.Create(fullFilePath);
            }
        }
    }
}
