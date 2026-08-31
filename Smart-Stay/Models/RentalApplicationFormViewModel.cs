using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Smart_Stay.Models
{
    public class RentalApplicationFormViewModel
    {
        public int PropertyId { get; set; }

        public string PropertyTitle { get; set; } = "";

        // ============================
        // FIRST NAME
        // ============================

        [Required(ErrorMessage = "First name is required")]
        [RegularExpression(
            @"^[A-Za-z\s'-]+$",
            ErrorMessage = "First name may contain letters only")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";


        // ============================
        // LAST NAME
        // ============================

        [Required(ErrorMessage = "Last name is required")]
        [RegularExpression(
            @"^[A-Za-z\s'-]+$",
            ErrorMessage = "Last name may contain letters only")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";


        // ============================
        // ID NUMBER
        // ============================

        [Required(ErrorMessage = "ID number is required")]
        [RegularExpression(
            @"^\d{13}$",
            ErrorMessage = "ID number must contain exactly 13 digits")]
        [Display(Name = "ID Number")]
        public string IdNumber { get; set; } = "";


        // ============================
        // PHONE NUMBER
        // ============================

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(
            @"^\d{10}$",
            ErrorMessage = "Phone number must contain exactly 10 digits")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = "";


        // ============================
        // EMAIL
        // ============================

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = "";


        // ============================
        // EMPLOYMENT
        // ============================

        [Required(ErrorMessage = "Employment is required")]
        [Display(Name = "Employment")]
        public string Employment { get; set; } = "";


        // ============================
        // PAYSLIP
        // ============================

        [Required(ErrorMessage = "Please upload your payslip")]
        [Display(Name = "Upload Payslip")]
        public IFormFile? Payslip { get; set; }


        // ============================
        // TERMS AND CONDITIONS
        // ============================

        [Range(
            typeof(bool),
            "true",
            "true",
            ErrorMessage = "You must accept the Terms and Conditions")]
        [Display(Name = "Accept T's & C's")]
        public bool AcceptTerms { get; set; }
    }
}