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

        public List<ExistingPropertyImageViewModel> ExistingImages { get; set; } = new();

        public List<int> ImagesToDelete { get; set; } = new();

        public List<IFormFile> NewImages { get; set; } = new();

        // Used to determine whether this property was rejected
        public bool IsRejected { get; set; }

        // Reason provided by the Admin when rejecting
        public string? RejectionReason { get; set; }
    }

    public class ExistingPropertyImageViewModel
    {
        public int DocumentId { get; set; }

        public string ImagePath { get; set; } = string.Empty;
    }
}