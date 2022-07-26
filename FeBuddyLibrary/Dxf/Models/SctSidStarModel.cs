using System.Collections.Generic;
using System.Text;

namespace FeBuddyLibrary.Dxf.Models
{
    public class SctSidStarModel
    {
        public string DiagramName { get; set; }
        public string StartLat { get; set; }
        public string StartLon { get; set; }
        public string EndLat { get; set; }
        public string EndLon { get; set; }
        public string Color { get; set; }
        public string Comment { get; set; }
        public List<SctAditionalDiagramLineSegments> AdditionalLines { get; set; }

        public string AllInfo
        {
            get
            { 
                StringBuilder output = new StringBuilder();
                string outputString = $"{DiagramName.PadRight(26)}{StartLat} {StartLon} {EndLat} {EndLon}";
                if (!string.IsNullOrEmpty(Color) && !string.IsNullOrWhiteSpace(Color)) outputString += $" {Color}";
                if (!string.IsNullOrEmpty(Comment) && !string.IsNullOrWhiteSpace(Comment)) outputString += $" {Comment}";
                output.Append(outputString);

                //output.Append($"{DiagramName.PadRight(26)}{StartLat} {StartLon} {EndLat} {EndLon} {Color} {Comment}".Trim());
                if (AdditionalLines.Count >= 1)
                {
                    output.Append("\n");
                    foreach (var item in AdditionalLines)
                    {
                        outputString = $"{" ".PadRight(26)}{item.StartLat} {item.StartLon} {item.EndLat} {item.EndLon}";
                        if (!string.IsNullOrEmpty(item.Color) && !string.IsNullOrWhiteSpace(item.Color)) outputString += $" {item.Color}";
                        if (!string.IsNullOrEmpty(item.Comment) && !string.IsNullOrWhiteSpace(item.Comment)) outputString += $" {item.Comment}";
                        outputString += "\n";
                        output.Append(outputString);
                        //output.Append($"{" ".PadRight(26)}{item.StartLat} {item.StartLon} {item.EndLat} {item.EndLon} {item.Color} {item.Comment}\n");
                    }
                    output.Remove(output.Length - 1, 1);
                }
                return output.ToString();
            }
        }
    }

    public class SctAditionalDiagramLineSegments
    {
        public string StartLat { get; set; }
        public string StartLon { get; set; }
        public string EndLat { get; set; }
        public string EndLon { get; set; }
        public string Color { get; set; }
        public string Comment { get; set; }
    }
}
