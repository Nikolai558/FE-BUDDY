using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models;
using HtmlAgilityPack;

namespace FeBuddyLibrary.DataAccess
{
    public class GetTelephony
    {
        private List<TelephonyModel> allTelephony = new List<TelephonyModel>();

        public void readFAAData(string websiteFilePath)
        {
            Logger.LogMessage("DEBUG", "STARTING TELEPHONY");

            HtmlDocument document = new HtmlDocument();
            document.Load(websiteFilePath);

            string[] badCharacters =
            {" ", ",", ".", "/", "!", "@", "#", "$", "%", "^", "&", "*", "\'", ";", "_", "(", ")", ":", "|", "[", "]", "-", "~", "`", "+", "\""};

            // Only select rows inside <tbody>.
            // This automatically excludes the table headers inside <thead>.
            HtmlNodeCollection? rows = document.DocumentNode.SelectNodes("//tbody/tr");

            if (rows == null)
            {
                throw new Exception("No telephony table rows were found in the HTML file.");
            }

            foreach (HtmlNode row in rows)
            {
                HtmlNodeCollection? cells = row.SelectNodes("./td");

                // Expected columns:
                // 0 = Telephony
                // 1 = Company
                // 2 = Country
                // 3 = 3-Ltr
                if (cells == null || cells.Count < 4)
                {
                    continue;
                }

                string telephonyData = HtmlEntity.DeEntitize(cells[0].InnerText).Trim();
                string threeLDData = HtmlEntity.DeEntitize(cells[3].InnerText).Trim();

                string telephonyDataAltered = telephonyData;

                foreach (string badCharacter in badCharacters)
                {
                    telephonyDataAltered =
                        telephonyDataAltered.Replace(badCharacter, string.Empty);
                }

                foreach (string badCharacter in badCharacters)
                {
                    threeLDData =
                        threeLDData.Replace(badCharacter, string.Empty);
                }

                if (threeLDData.Length < 2)
                {
                    continue;
                }

                TelephonyModel currentTelephony = new TelephonyModel
                {
                    Telephony = telephonyData,
                    TelephonyAltered = telephonyDataAltered,
                    ThreeLD = threeLDData
                };

                allTelephony.Add(currentTelephony);
            }

            Logger.LogMessage("DEBUG", "COMPLETED TELEPHONY MODEL");

            WriteTelephony();
        }

        public void WriteTelephony()
        {
            Logger.LogMessage("DEBUG", $"SAVING TELEPHONY MODEL");

            string filePath = $"{GlobalConfig.outputDirectory}ALIAS\\TELEPHONY.txt";
            string combinedFilePath = $"{GlobalConfig.outputDirectory}ALIAS\\AliasTestFile.txt";
            StringBuilder NameSB = new StringBuilder();
            StringBuilder threeLD_SB = new StringBuilder();

            foreach (TelephonyModel telephony in allTelephony)
            {
                threeLD_SB.AppendLine($".id{telephony.ThreeLD} .ECHO 3LD ISR\\n 3LD: {telephony.ThreeLD} ___ TELEPHONY: {telephony.Telephony}");

                if (telephony.TelephonyAltered != telephony.ThreeLD)
                {
                    NameSB.AppendLine($".id{telephony.TelephonyAltered} .ECHO 3LD ISR\\n 3LD: {telephony.ThreeLD} ___ TELEPHONY: {telephony.Telephony}");
                }
            }
            File.AppendAllText(filePath, threeLD_SB.ToString());
            File.AppendAllText(filePath, NameSB.ToString());
            File.AppendAllText(combinedFilePath, threeLD_SB.ToString());
            File.AppendAllText(combinedFilePath, NameSB.ToString());
            Logger.LogMessage("DEBUG", $"COMPLETED TELEPHONY");

        }
    }
}
