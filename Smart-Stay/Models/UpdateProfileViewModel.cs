using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string SurName { get; set; } = "";

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{10}$",
            ErrorMessage = "Phone number must contain exactly 10 digits.")]
        public string PhoneNo { get; set; } = "";

        // OPTIONAL
        public string? NewPassword { get; set; }

        // OPTIONAL
        public string? ConfirmPassword { get; set; }

        public IFormFile? ProfileImage { get; set; }

        public string? CurrentImagePath { get; set; }
    }
}