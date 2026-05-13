namespace Alter_Parts.ViewModels
{
    public sealed class ProfileViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}