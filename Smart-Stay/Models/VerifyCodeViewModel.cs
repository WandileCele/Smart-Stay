using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class VerifyCodeViewModel
    {
        [Required]
        public string Token { get; set; } = null!;

        [Required]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Enter the 4-digit code.")]
        public string Code { get; set; } = null!;
    }
}