using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyLibrary.Dxf.Models
{
    public class SctFileModel
    {
        public List<SctColorModel> SctFileColors { get; set; } // Example: #define Building 723723
        public SctInfoModel SctInfoSection { get; set; } // Contains ENTIRE Info section, .Allinfo returns everything needed for the [INFO] section
        public List<VORNDBModel> SctVORSection { get; set; } // Contains [VOR] Section, Need to put header before printing out if doing that.
        public List<VORNDBModel> SctNDBSection { get; set; } // Contains [NDB] Section, Need to put header before printing out if doing that.
        public List<SctAirportModel> SctAirportSection { get; set; } // Contains [AIRPORT] Section, Need to put header before printing out if doing that.
        public List<SctRunwayModel> SctRunwaySection { get; set; } // Contains [RUNWAY] Section, Need to put header before printing out if doing that.
        public List<SctFixesModel> SctFixesSection { get; set; } // Contains [FIXES] Section, Need to put header before printing out if doing that.
        public List<SctArtccModel> SctArtccSection { get; set; } // Contains [ARTCC] Section, Need to put header before printing out if doing that.
        public List<SctArtccModel> SctArtccHighSection { get; set; } // Contains [ARTCC HIGH] Section, Need to put header before printing out if doing that.
        public List<SctArtccModel> SctArtccLowSection { get; set; }  // Contains [ARTCC LOW] Section, Need to put header before printing out if doing that.
        public List<SctSidStarModel> SctSidSection { get; set; } // Contains [SID] Section, Need to put header before printing out if doing that.
        public List<SctSidStarModel> SctStarSection { get; set; } // Contains [STAR] Section, Need to put header before printing out if doing that.
        public List<SctArtccModel> SctLowAirwaySection { get; set; } // Contains [LOW AIRWAY] Section, Need to put header before printing out if doing that.
        public List<SctArtccModel> SctHighAirwaySection { get; set; } // Contains [HGIH AIRWAY] Section, Need to put header before printing out if doing that.
        public List<SctGeoModel> SctGeoSection { get; set; } // Contains [GEO] Section, Need to put header before printing out if doing that.
        public List<SctRegionModel> SctRegionsSection { get; set; } // Contains [REGIONS] Section, Need to put header before printing out if doing that.
        public List<SctLabelModel> SctLabelSection { get; set; } // Contains [LABELS] Section, Need to put header before printing out if doing that.
    }
}
