using System.ComponentModel.DataAnnotations;

namespace Alter_Parts.Models
{
    public class Alter
    {
        [Key]
        public int Part_Id { get; set; }

        [Required(ErrorMessage = "Please enter the Original Part Number")]
        public string Original_Part_Number { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the Alternate Part Number")]
        public string Alter_Part_Number { get; set; } = string.Empty;

        // Default to "Pending"
        // New fields used by controller and views
        public string Status { get; set; } = "Pending";

        // Optional notes
        public string? ComparisonNotes { get; set; }

        // MAKE THIS NULLABLE SO IT DOESN'T BLOCK CREATION
        public string? Final_Result { get; set; }

        public DateTime LastChecked { get; set; } = DateTime.Now;
    }
}