using System.ComponentModel.DataAnnotations;

namespace Alter_Parts.ViewModels
{
    public sealed class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public List<string> CurrentRoles { get; set; } = new();
    }
}