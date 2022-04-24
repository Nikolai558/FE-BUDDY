using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models;

namespace FeBuddyLibrary.DataAccess
{
    public class GetTelephony
    {
        private List<TelephonyModel> allTelephony = new List<TelephonyModel>();

        public void readFAAData(string websiteFilePath)
        {
            Logger.LogMessage("DEBUG", $"STARTING TELEPHONY");

            string[] allLines = File.ReadAllLines(websiteFilePath);

            bool inTableRow = false;
            bool inTableData = false;
            bool inParagraph = false;

            TelephonyModel currentTelephony = new TelephonyModel();

            int count = 0;
            string completedLine = "";

            foreach (string line in allLines)
            {
                if (inParagraph == false)
                {
                    completedLine = "";
                }

                if (line.Contains("<tr>"))
                {
                    inTableRow = true;
                    currentTelephony = new TelephonyModel();
                    continue;
                }
                if (line.Contains("<td>"))
                {
                    inTableData = true;
                    count += 1;
                    continue;
                }

                if (line.Contains("</td>"))
                {
                    inTableData = false;
                    continue;
                }
                if (line.Contains("</tr>"))
                {
                    inTableRow = false;
                    count = 0;
                    continue;
                }

                if (inTableRow && inTableData)
                {
                    string[] badCharacters = new string[] { " ", ",", ".", "/", "!", "@", "#", "$", "%", "^", "&", "*", "\'", ";", "_", "(", ")", ":", "|", "[", "]", "-", "~", "`", "+", "\"" };

                    if (line.Contains("<p") && line.Contains("</p>"))
                    {
                        completedLine = line.Trim();
                        inParagraph = false;
                    }
                    else if (line.Contains("<p"))
                    {
                        inParagraph = true;
                        completedLine += " " + line.Trim();
                        continue;
                    }
                    else if (line.Contains("</p>"))
                    {
                        inParagraph = false;
                        completedLine += " " + line.Trim();
                    }
                    else if (inParagraph)
                    {
                        completedLine += " " + line.Trim();
                        continue;
                    }
                    else
                    {
                        continue;
                    }


                    if (completedLine == "")
                    {
                        throw new Exception("Error creating full paragraph tag.");
                    }
                    if (count == 1)
                    {
                        string telephonyData = completedLine.Split('>')[1];

                        if (telephonyData.Contains('<'))
                        {
                            telephonyData = telephonyData.Split('<')[0];
                        }
                        telephonyData = telephonyData.Trim();

                        string telephonyDataAltered = telephonyData;
                        foreach (string badCharacter in badCharacters)
                        {
                            telephonyDataAltered = telephonyDataAltered.Replace(badCharacter, string.Empty);
                        }

                        currentTelephony.Telephony = telephonyData;
                        currentTelephony.TelephonyAltered = telephonyDataAltered;
                        continue;
                    }
                    else if (count == 4)
                    {
                        string threeLDData = completedLine.Split('>')[1];

                        if (threeLDData.Contains('<'))
                        {
                            threeLDData = threeLDData.Split('<')[0];
                        }

                        threeLDData = threeLDData.Trim();
                        foreach (string badCharacter in badCharacters)
                        {
                            threeLDData = threeLDData.Replace(badCharacter, string.Empty);
                        }

                        currentTelephony.ThreeLD = threeLDData;

                        if (currentTelephony.ThreeLD.Length < 2)
                        {
                            continue;
                        }

                        allTelephony.Add(currentTelephony);
                        continue;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            Logger.LogMessage("DEBUG", $"COMPLETED TELEPHONY MODEL");

            WriteTelephony();
        }

        public void WriteTelephony()
        {
            Logger.LogMessage("DEBUG", $"SAVING TELEPHONY MODEL");

            string filePath = $"{GlobalConfig.outputDirectory}ALIAS\\TELEPHONY.txt";
            StringBuilder sb = new StringBuilder();

            foreach (TelephonyModel telephony in allTelephony)
            {
                sb.AppendLine($".id{telephony.ThreeLD} .MSG FAA_ISR *** 3LD: {telephony.ThreeLD} ___ TELEPHONY: {telephony.Telephony}");
                sb.AppendLine($".id{telephony.TelephonyAltered} .MSG FAA_ISR *** 3LD: {telephony.ThreeLD} ___ TELEPHONY: {telephony.Telephony}");
            }

            File.WriteAllText(filePath, sb.ToString());
            Logger.LogMessage("DEBUG", $"COMPLETED TELEPHONY");

        }
    }
}
