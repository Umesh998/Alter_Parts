namespace Alter_Parts.Models
{
    public class BomRow
    {
        public int RowNumber { get; set; }
        public string OriginalPart { get; set; }
        public string AlternatePart { get; set; }
        public string Remark { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string ComparisonResult { get; set; }
        public string Status { get; set; } // Pending, Done, Error

        public string Source { get; set; } // "DigiKey / DigiKey", "Mouser / DigiKey" etc
     
    }

    public class BomUploadResult
    {
        public List<BomRow> Rows { get; set; } = new();
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int CompatibleCount { get; set; }
        public int IncompatibleCount { get; set; }
        public int ManualCheckCount { get; set; }
        public string FileName { get; set; }
    }
}
