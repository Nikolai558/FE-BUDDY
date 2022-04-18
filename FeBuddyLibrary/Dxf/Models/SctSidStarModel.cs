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
        public List<SctAditionalDiagramLineSegments> AdditionalLines { get; set; }

        public string AllInfo 
        { 
            get 
            {
                StringBuilder output = new StringBuilder();
                output.Append($"{DiagramName.PadRight(26)}{StartLat} {StartLon} {EndLat} {EndLon} {Color}".Trim());
                if (AdditionalLines.Count >= 1)
                {
                    output.Append("\n");
                    foreach (var item in AdditionalLines)
                    {
                        output.Append($"{" ".PadRight(26)}{item.StartLat} {item.StartLon} {item.EndLat} {item.EndLon} {item.Color}\n");
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
    }
}