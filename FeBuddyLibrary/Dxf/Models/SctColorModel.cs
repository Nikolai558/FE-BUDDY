





namespace FeBuddyLibrary.Dxf.Models
{
    public class SctColorModel
    {
        public string Name { get; set; }
        public string ColorCode { get; set; }

        public string SctColorLine { get { return $"#define {Name} {ColorCode}"; } }
    }
}