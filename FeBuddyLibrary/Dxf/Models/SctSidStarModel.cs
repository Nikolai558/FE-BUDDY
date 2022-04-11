using System.Collections.Generic;

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