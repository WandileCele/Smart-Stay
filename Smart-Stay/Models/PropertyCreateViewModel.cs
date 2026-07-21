using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class PropertyCreateViewModel
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required]
        public string Location { get; set; } = "";

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string PropertyType { get; set; } = "";

        public int? Bedrooms { get; set; }

        public int? Bathrooms { get; set; }

        // Property Images
        [Required(ErrorMessage = "Upload at least 3 images.")]
        public List<IFormFile> PropertyImages { get; set; } = new();

        // Affidavit PDF
        [Required(ErrorMessage = "Affidavit is required.")]
        public IFormFile Affidavit { get; set; } = null!;
    }
}