namespace Alter_Parts.Models
{
    public class PartDetails
    {
        public string Mpn { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string DatasheetUrl { get; set; }
        public string ProductUrl { get; set; }
        public string Stock { get; set; }
        public string Category { get; set; }
        public string Source { get; set; } // "DigiKey", "Mouser", "Not Found"
        public Dictionary<string, string> Specs { get; set; } = new();
    }
}