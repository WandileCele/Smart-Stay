using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class UpdateProfileViewModel
    {
        public int UserId { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [Display(Name = "Surname")]
        public string SurName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must start with 0 and contain exactly 10 digits.")]
        [Display(Name = "Phone Number")]
        public string PhoneNo { get; set; } = null!;

        [Display(Name = "New Password")]
        [StringLength(15, MinimumLength = 4)]
        public string? NewPassword { get; set; }
    }
}