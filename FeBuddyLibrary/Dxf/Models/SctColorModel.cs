





namespace FeBuddyLibrary.Dxf.Models
{
    public class SctColorModel
    {
        public string Name { get; set; }
        public string ColorCode { get; set; }

        public string AllInfo { get { return $"#define {Name} {ColorCode}"; } }
    }
}