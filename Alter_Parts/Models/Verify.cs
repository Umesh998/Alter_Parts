using System.ComponentModel.DataAnnotations;

namespace Alter_Parts.Models
{
    public class VerifyRequest
    {
        [Required(ErrorMessage = "Part number is required")]
        [Display(Name = "Part Number")]
        public string PartNumber { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Your Description")]
        public string Description { get; set; }
    }

    public class VerifySource
    {
        public string Source { get; set; }
        public string FetchedDescription { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string Package { get; set; }
        public string DatasheetUrl { get; set; }
        public string ProductUrl { get; set; }
        public string Stock { get; set; }
        public string MatchVerdict { get; set; }

       
        public string MslLevel { get; set; } = "N/A";   // ADD
        public string MountType { get; set; } = "N/A";  // ADD
        public Dictionary<string, string> Specs { get; set; } = new();
    }

    public class VerifyResult
    {
        public string PartNumber { get; set; }
        public string UserDescription { get; set; }
        public string OverallVerdict { get; set; }
        public VerifySource DigiKeyResult { get; set; }
        public VerifySource MouserResult { get; set; }

        public VerifySource LCSCResult { get; set; }

        public VerifySource NexarResult { get; set; }
    }

    public class CompareRequest
    {
        [Required(ErrorMessage = "Original part number is required")]
        [Display(Name = "Original Part Number")]
        public string OriginalPart { get; set; }

        [Required(ErrorMessage = "Alternate part number is required")]
        [Display(Name = "Alternate Part Number")]
        public string AlternatePart { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Required Description / Use Case")]
        public string Description { get; set; }
    }

    public class SpecComparison
    {
        public string SpecName { get; set; }
        public string OriginalValue { get; set; }
        public string AlternateValue { get; set; }
        public string Status { get; set; } // "Match", "Mismatch", "Only Original", "Only Alternate"
    }

    public class CompareResult
    {
        public string OriginalPart { get; set; }
        public string AlternatePart { get; set; }
        public string UserDescription { get; set; }

        // Part details
        public VerifySource OriginalDetails { get; set; }
        public VerifySource AlternateDetails { get; set; }

        // Match scores
        public double OriginalMatchScore { get; set; }
        public double AlternateMatchScore { get; set; }

        // Spec comparison table
        public List<SpecComparison> SpecComparisons { get; set; } = new();

        // Final verdict
        public string Verdict { get; set; }
        public string VerdictReason { get; set; }
        public string RecommendedPart { get; set; } // "Original", "Alternate", "Either"
    }

    public class BomVerifyRow
    {
        public int RowNumber { get; set; }
        public string PartNumber { get; set; }
        public string UserDescription { get; set; }
        public string FetchedDescription { get; set; }
        public string Manufacturer { get; set; }
        public string Category { get; set; }
        public string Source { get; set; }
        public double MatchScore { get; set; }
        public string MatchVerdict { get; set; }
        public string Status { get; set; }

        // ✅ Per-source results
        public string DigiKeyDescription { get; set; }
        public string DigiKeyVerdict { get; set; }
        public double DigiKeyScore { get; set; }
        public string MouserDescription { get; set; }
        public string MouserVerdict { get; set; }
        public double MouserScore { get; set; }
        public string LCSCDescription { get; set; }
        public string LCSCVerdict { get; set; }
        public double LCSCScore { get; set; }
        public string BestSource { get; set; }

        public string Package { get; set; } = "N/A";    // ADD
        public string MslLevel { get; set; } = "N/A";   // ADD
        public string MountType { get; set; } = "N/A";  // ADD
    }

    public class BomVerifyResult
    {
        public string FileName { get; set; }
        public List<BomVerifyRow> Rows { get; set; } = new();
        public int TotalRows { get; set; }
        public int MatchedCount { get; set; }
        public int NotMatchedCount { get; set; }
        public int ManualCheckCount { get; set; }
        public int NotFoundCount { get; set; }
    }

    public class DescriptionSearchRequest
    {
        [Required(ErrorMessage = "Please enter a description")]
        [Display(Name = "Description / Keywords")]
        public string Description { get; set; }

        public int Limit { get; set; } = 10;
    }

    public class DescriptionSearchViewModel
    {
        public string Description { get; set; }
        public List<PartDetails> DigiKeyResults { get; set; } = new();
        public List<PartDetails> MouserResults { get; set; } = new();

        public List<PartDetails> LCSCResults { get; set; } = new();
        public int DigiKeyTotal { get; set; }
        public int MouserTotal { get; set; }

        public int LCSCTotal { get; set; }
        public string Error { get; set; }
    }
}