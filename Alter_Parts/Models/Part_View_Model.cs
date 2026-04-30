namespace Alter_Parts.Models
{
    public class Part_View_Model
    {
        public IEnumerable<Alter> Fruits { get; set; } = new List<Alter>();

        // Existing Sort Orders
        public string NameSortOrder { get; set; } = "name";
        public string QtySortOrder { get; set; } = "quantity";
        public string IdSort { get; set; } = "id";

        // New MPN Sort Orders
        public string OriginalMpnSortOrder { get; set; } = "original_mpn";
        public string AlterMpnSortOrder { get; set; } = "alter_mpn";

        // Current applied sort and filter
        public string OrderBy { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;

        // Pagination
        public int PageSize { get; set; } = 5;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}