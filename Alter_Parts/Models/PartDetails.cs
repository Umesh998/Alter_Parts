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

        public string Price { get; set; }
        public Dictionary<string, string> Specs { get; set; } = new();

        public List<PriceBreak> PriceBreaks { get; set; } = new();

        public string LifecycleStatus { get; set; } = "Active"; // Active, Discontinued, EndOfLife, NRND, Unknown
        public bool IsObsolete => LifecycleStatus == "Discontinued" || LifecycleStatus == "EndOfLife";
    }

    public class PriceBreak
    {
        public int BreakQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => BreakQuantity * UnitPrice;
    }
}