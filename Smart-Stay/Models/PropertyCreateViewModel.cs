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
        [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Price cannot be zero.")]
        public decimal Price { get; set; }

        [Required]
        public string PropertyType { get; set; } = "";

        [Range(1, 100, ErrorMessage = "Bedrooms cannot be zero. Enter at least 1.")]
        public int? Bedrooms { get; set; }

        [Range(1, 100, ErrorMessage = "Bathrooms cannot be zero. Enter at least 1.")]
        public int? Bathrooms { get; set; }

        [Required(ErrorMessage = "Please pin your property's location on the map.")]
        public string Address { get; set; } = "";

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [Required(ErrorMessage = "Upload at least 3 images.")]
        public List<IFormFile> PropertyImages { get; set; } = new();

        [Required(ErrorMessage = "Affidavit is required.")]
        public IFormFile Affidavit { get; set; } = null!;
    }
}