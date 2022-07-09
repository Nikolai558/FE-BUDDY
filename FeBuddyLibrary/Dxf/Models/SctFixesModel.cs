namespace FeBuddyLibrary.Dxf.Models
{
    public class SctFixesModel
    {
        public string FixName { get; set; }
        public string Lat { get; set; }
        public string Lon { get; set; }
        public string Comments { get; set; }
        public string AllInfo
        {
            get
            {
                string output = $"{FixName} {Lat} {Lon}";

                if (!string.IsNullOrEmpty(Comments))
                {
                    output += $" {Comments}";
                }

                return output;
            }
        }
    }
}
