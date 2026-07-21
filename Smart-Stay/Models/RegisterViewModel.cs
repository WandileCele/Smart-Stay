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
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }

        // Only used when Role == "Tenant"
        public string? EmploymentStatus { get; set; }
    }
}