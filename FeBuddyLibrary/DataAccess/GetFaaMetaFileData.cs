using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models.MetaFileModels;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace FeBuddyLibrary.DataAccess
{
    public class GetFaaMetaFileData
    {
        private List<MetaAirportModel> AllAirports = GlobalConfig.AllMetaFileAirports;

        public void QuarterbackFunc()
        {
            Logger.LogMessage("DEBUG", $"STARTING META FILE PARSING");

            ParseMetaFile();

            Logger.LogMessage("DEBUG", $"COMPLETED META FILE PARSING");

        }

        private void ParseMetaFile()
        {
            string baseURL = $"https://aeronav.faa.gov/d-tpp/{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}/";

            StringBuilder aliasCommandSB = new StringBuilder();
            var xmlDoc = XDocument.Parse(File.ReadAllText($"{GlobalConfig.tempPath}\\{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}_FAA_Meta.xml"));
            var airports = xmlDoc.Descendants("airport_name");

            foreach (var aptData in airports)
            {
                MetaAirportModel apt = new MetaAirportModel()
                {
                    AirportName = aptData.Attribute("ID").Value,
                    Military = aptData.Attribute("military").Value,
                    AptIdent = aptData.Attribute("apt_ident").Value,
                    Icao = aptData.Attribute("icao_ident").Value,
                    Alnum = aptData.Attribute("alnum").Value
                };

                MetaRecordModel prevRecord = new MetaRecordModel();
                int count = 1;
                foreach (var recordData in aptData.Elements())
                {
                    MetaRecordModel record = new MetaRecordModel()
                    {
                        ChartSeq = recordData.Element("chartseq").Value,
                        ChartCode = recordData.Element("chart_code").Value,
                        ChartName = recordData.Element("chart_name").Value,
                        FAAChartName = recordData.Element("chart_name").Value,
                        UserAction = recordData.Element("useraction").Value,
                        PdfName = recordData.Element("pdf_name").Value,
                        CnFlag = recordData.Element("cn_flg").Value,
                        CnPage = recordData.Element("cnpage").Value,
                        CnSection = recordData.Element("cnsection").Value,
                        BvSection = recordData.Element("bvsection").Value,
                        BvPage = recordData.Element("bvpage").Value,
                        ProcUid = recordData.Element("procuid").Value,
                        TwoColored = recordData.Element("two_colored").Value,
                        Civil = recordData.Element("civil").Value,
                        Faanfd18 = recordData.Element("faanfd18").Value,
                        Copter = recordData.Element("copter").Value,
                        AmdtNum = recordData.Element("amdtnum").Value,
                        AmdtDate = recordData.Element("amdtdate").Value
                    };

                    if ((string.IsNullOrEmpty(record.Faanfd18) && record.ChartCode == "DP") || (string.IsNullOrEmpty(record.Faanfd18) && record.ChartCode == "STAR"))
                    {
                        string newFaanfd18 = "";

                        if (record.ChartName.Trim().Replace(" ", string.Empty).Length <= 5)
                        {
                            // This apears to be redundant check. As chart name Trimed and No spaces will always be greater than 5 Chars.
                            newFaanfd18 = record.ChartName.Trim().Replace(" ", string.Empty);
                        }
                        else
                        {
                            newFaanfd18 = record.ChartName.Trim().Replace(" ", string.Empty).Substring(0, 5);
                        }

                        newFaanfd18 += "?";

                        if (record.ChartCode == "DP")
                        {
                            record.Faanfd18 = newFaanfd18 + "." + apt.AptIdent;
                        }
                        else
                        {
                            record.Faanfd18 = apt.AptIdent + "." + newFaanfd18;
                        }
                        //Console.WriteLine(apt.AptIdent + " - " + record.ChartCode + " - " + record.ChartSeq + " - " + record.ChartName);
                    }

                    if (
                            record.ChartName.IndexOf("CONVERGING") == -1 &&
                            record.ChartName.IndexOf("COPTER") == -1 &&
                            record.ChartName.IndexOf("HI-") == -1 &&
                            record.ChartName.IndexOf("PRM") == -1 &&
                            record.ChartName.IndexOf("BC") == -1 &&
                            record.ChartName.IndexOf("CAT ") == -1 &&
                            record.ChartName.IndexOf("TACAN 056") == -1 &&
                            record.PdfName.IndexOf("DELETED") == -1)
                    {
                        // Move these.
                        record.CreateAliasComand(apt.AptIdent);
                        //record.ORIGINAL___CreateAliasComand(apt.AptIdent);
                        apt.Records.Add(record);


                        if (prevRecord.ProcUid == record.ProcUid && prevRecord.AliasCommand == record.AliasCommand)
                        {
                            count += 1;
                            if (record.AliasCommand.IndexOf('/') == -1)
                            {
                                aliasCommandSB.AppendLine($".{apt.AptIdent}{record.AliasCommand}{count}C .OPENURL {baseURL}{record.PdfName}  ; {apt.AirportName}-{record.FAAChartName}");
                            }
                            else
                            {
                                foreach (string str in record.AliasCommand.Split('/'))
                                {
                                    string strtrimed = str.Trim();

                                    if (string.IsNullOrEmpty(strtrimed))
                                    {
                                        continue;
                                    }

                                    if (strtrimed == "*")
                                    {
                                        aliasCommandSB.AppendLine($".{apt.AptIdent}APDC .OPENURL {baseURL}{record.PdfName}  ; {apt.AirportName}-{record.FAAChartName}");
                                    }
                                    else if (record.ChartCode == "TM" || record.ChartCode == "DVA")
                                    {
                                        aliasCommandSB.AppendLine($".{apt.AptIdent}{strtrimed}C .OPENURL {baseURL}{record.PdfName}#nameddest=({apt.AptIdent})  ; {apt.AirportName}-{record.FAAChartName}");
                                    }
                                    else if (record.ChartCode == "STAR" || record.ChartCode == "DP" || record.ChartCode == "ODP" && !string.IsNullOrEmpty(strtrimed))
                                    {
                                        aliasCommandSB.AppendLine($".{strtrimed}{count}C .OPENURL {baseURL}{record.PdfName}  ; {apt.AirportName}-{record.FAAChartName}");
                                    }
                                    else if (!string.IsNullOrEmpty(strtrimed))
                                    {
                                        aliasCommandSB.AppendLine($".{apt.AptIdent}{strtrimed}{count}C .OPENURL {baseURL}{record.PdfName}  ; {apt.AirportName}-{record.FAAChartName}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            count = 1;
                            if (record.AliasCommand.IndexOf('/') == -1)
                            {
                                aliasCommandSB.AppendLine($".{apt.AptIdent}{record.AliasCommand}C .OPENURL {baseURL}{record.PdfName}  ; {apt.AirportName}-{record.FAAChartName}");
                            }
                            else
                            {
                                foreach (string str in record.AliasCommand.Split('/'))
                                {
                                    string strTrimmed = str.Trim();
                                    if (string.IsNullOrEmpty(strTrimmed))
                                    {
                                        continue;
                                    }

                                    if (strTrimmed == "*")
                                    {
                                        aliasCommandSB.AppendLine($".{apt.AptIdent}APDC .OPENURL {baseURL}{record.PdfName}  ; {apt.AirportName}-{record.FAAChartName}");
                                    }
                                    else if (record.ChartCode == "MIN")
                                    {
                                        aliasCommandSB.AppendLine($".{apt.AptIdent}{strTrimmed}C .OPENURL {baseURL}{record.PdfName}#nameddest=({apt.AptIdent})  ; {apt.AirportName}-{record.FAAChartName}");
                                    }
                                    else if (record.ChartCode == "STAR" || record.ChartCode == "DP" || record.ChartCode == "ODP" && !string.IsNullOrEmpty(strTrimmed))
                                    {
                                        aliasCommandSB.AppendLine($".{strTrimmed}C .OPENURL {baseURL}{record.PdfName}  ; {apt.AirportName}-{record.FAAChartName}");
                                    }
                                    else if (!string.IsNullOrEmpty(strTrimmed))
                                    {
                                        aliasCommandSB.AppendLine($".{apt.AptIdent}{strTrimmed}C .OPENURL {baseURL}{record.PdfName}  ; {apt.AirportName}-{record.FAAChartName}");
                                    }
                                }
                            }
                        }
                        prevRecord = record;
                    }
                }

                AllAirports.Add(apt);
            }

            File.WriteAllText($"{GlobalConfig.outputDirectory}\\ALIAS\\FAA_CHART_RECALL.txt", aliasCommandSB.ToString());
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\ALIAS\\AliasTestFile.txt", aliasCommandSB.ToString());

        }
    }
}
