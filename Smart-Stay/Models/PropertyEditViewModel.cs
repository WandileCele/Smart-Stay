using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class PropertyEditViewModel
    {
        public int PropertyId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string PropertyType { get; set; } = string.Empty;

        public int? Bedrooms { get; set; }

        public int? Bathrooms { get; set; }

       
        public string? ExistingImagePath { get; set; }

        
        public IFormFile? ImageFile { get; set; }
    }
}