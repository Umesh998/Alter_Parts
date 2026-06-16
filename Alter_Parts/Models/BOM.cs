namespace Alter_Parts.Models
{
    public class BomRow
    {
        public int RowNumber { get; set; }
        public string OriginalPart { get; set; }
        public string AlternatePart { get; set; }
        public string Remark { get; set; }
        public string Description { get; set; }         // Your description from Excel

        public string Manufacturer { get; set; }
        public string Source { get; set; }              // e.g. "DigiKey/Mouser"

        // ── Step 1: Original Part vs Your Description ───────────────
        public string OriginalDescription { get; set; } // Fetched online for original part
        public string BestMatchPercent { get; set; }    // e.g. "82%"
        public string OverallVerdict { get; set; }      // ✅ Strong Match / ❌ No Match / ⚠️

        // ── Step 2: Original Part vs Alternate Part ─────────────────
        public string ComparisonResult { get; set; }    // ✅ Compatible / ❌ Not Compatible / ⚠️

        public string Status { get; set; }              // Pending, Done, Error
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