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

        [Required(ErrorMessage = "Upload at least 3 images.")]
        public List<IFormFile> PropertyImages { get; set; } = new();

        [Required(ErrorMessage = "Affidavit is required.")]
        public IFormFile Affidavit { get; set; } = null!;
    }
}