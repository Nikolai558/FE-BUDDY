using System.Linq;

namespace FeBuddyLibrary.Models
{
    public class RunwayModel
    {
        public string RwyGroup { get; set; }

        public string BaseRwy
        {
            get
            {
                string[] split = RwyGroup.Split('/');
                return split[0];
            }
        }

        public string RecRwy
        {
            get
            {
                string[] split = RwyGroup.Split('/');

                if (split.Count() == 2)
                {
                    return split[1];
                }
                else
                {
                    return "";
                }
            }
        }

        public string RwyLength { get; set; }

        public string RwyWidth { get; set; }

        public string BaseStartLat { get; set; }

        public string BaseStartLon { get; set; }

        public string BaseEndLat { get; set; }

        public string BaseEndLon { get; set; }

        public string RecStartLat { get { return BaseEndLat; } }

        public string RecStartLon { get { return BaseEndLon; } }

        public string RecEndLat { get { return BaseStartLat; } }

        public string RecEndLon { get { return BaseStartLon; } }

        public string BaseRwyHdg { get; set; }

        public string RecRwyHdg { get; set; }
    }
}
