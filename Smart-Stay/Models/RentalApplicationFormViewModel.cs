using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Smart_Stay.Models
{
    public class RentalApplicationFormViewModel
    {
        public int PropertyId { get; set; }

        public string PropertyTitle { get; set; } = "";


        [Required(ErrorMessage = "First name is required")]
        [RegularExpression(
            @"^[A-Za-z\s'-]+$",
            ErrorMessage = "First name may contain letters only")]
        public string FirstName { get; set; } = "";


        [Required(ErrorMessage = "Last name is required")]
        [RegularExpression(
            @"^[A-Za-z\s'-]+$",
            ErrorMessage = "Last name may contain letters only")]
        public string LastName { get; set; } = "";


        [Required(ErrorMessage = "ID number is required")]
        [RegularExpression(
            @"^\d{13}$",
            ErrorMessage = "ID number must contain exactly 13 digits")]
        public string IdNumber { get; set; } = "";


        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(
            @"^\d{10}$",
            ErrorMessage = "Phone number must contain exactly 10 digits")]
        public string PhoneNumber { get; set; } = "";


        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = "";


        [Required(ErrorMessage = "Employment is required")]
        public string Employment { get; set; } = "";


        // Validated manually in the controller (required, .pdf, <=3MB).
        // No [Required] here because IFormFile validation via data
        // annotations is unreliable across browsers/binders.
        public IFormFile? Payslip { get; set; }


        // Deliberately NOT using [Range(typeof(bool), "true", "true")].
        // That attribute is the classic cause of "submit does nothing":
        // it fights with any client-side JS check on the same field.
        // AcceptTerms is validated only in the controller instead.
        [Display(Name = "Accept T's & C's")]
        public bool AcceptTerms { get; set; }
    }
}