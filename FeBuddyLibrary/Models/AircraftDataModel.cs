namespace FeBuddyLibrary.Models
{
    public class AircraftDataRootObject
    {
        public AircraftDataInformation[] AllAircraftData { get; set; }
    }

    public class AircraftDataInformation
    {
        public string ModelFullName { get; set; }
        public string Description { get; set; }
        public string WTC { get; set; }
        public string WTG { get; set; }
        public string Designator { get; set; }
        public string ManufacturerCode { get; set; }
        public string AircraftDescription { get; set; }
        public string EngineCount { get; set; }
        public string EngineType { get; set; }
    }

}
