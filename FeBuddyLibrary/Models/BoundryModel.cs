using System.Collections.Generic;

namespace FeBuddyLibrary.Models
{
    public class BoundryModel
    {
        public string Identifier { get; set; }

        public string Type { get; set; }

        public List<ArbModel> AllPoints { get; set; }
    }
}
