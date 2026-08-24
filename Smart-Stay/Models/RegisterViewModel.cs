using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string Role { get; set; } = "Tenant";

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNo { get; set; }

        [Required]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?"":{}|<>]).{8,}$", ErrorMessage = "Password must be at least 8 characters and include 1 uppercase letter, 1 number, and 1 special character.")]
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }

        public string? EmploymentStatus { get; set; }
    }
}