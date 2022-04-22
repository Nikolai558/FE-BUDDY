namespace FeBuddyLibrary.Dxf.Models
{
    public class SctLabelModel
    {
        public string LabelText { get; set; }
        public string Lat { get; set; }
        public string Lon { get; set; }
        public string Color { get; set; }

        public string AllInfo
        {
            get
            {
                string output = $"{LabelText} {Lat} {Lon} {Color}";
                return output;
            }
        }

    }
}