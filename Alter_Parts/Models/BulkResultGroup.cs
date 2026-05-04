using Alter_Parts.Models;

namespace Alter_Parts.Models
{
    /// <summary>
    /// Holds the aggregated sourcing results for one component description
    /// from the bulk upload file. Up to 9 results total (3 per distributor).
    /// </summary>
    public class BulkResultGroup
    {
        /// <summary>Original text from Column A of the uploaded .xlsx file.</summary>
        public string RequestedDescription { get; set; } = string.Empty;

        /// <summary>
        /// Up to 9 PartDetails entries — top 3 from Mouser, DigiKey, and LCSC.
        /// May contain fewer if a distributor returns limited results.
        /// </summary>
        public List<PartDetails> Results { get; set; } = new();

        /// <summary>
        /// Per-distributor error messages. Key = "Mouser" / "DigiKey" / "LCSC".
        /// Populated only when a service call fails; does not affect other distributors.
        /// </summary>
        public Dictionary<string, string> Errors { get; set; } = new();
    }
}
