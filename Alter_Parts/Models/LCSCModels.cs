namespace Alter_Parts.Models
{
    public class LCSCPartResult
    {
        public string LcscPartNumber { get; set; }
        public string MpnNumber { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string Stock { get; set; }
        public string Package { get; set; }
        public string Price { get; set; }
        public string DatasheetUrl { get; set; }
        public string ProductUrl { get; set; }
        public double MatchScore { get; set; }
    }

    public class LCSCSearchRequest
    {
        public string Keyword { get; set; }
        public int Limit { get; set; } = 10;
    }

    public class LCSCSearchViewModel
    {
        public string Keyword { get; set; }
        public List<LCSCPartResult> Results { get; set; } = new();
        public int TotalFound { get; set; }
        public string Error { get; set; }
    }
}