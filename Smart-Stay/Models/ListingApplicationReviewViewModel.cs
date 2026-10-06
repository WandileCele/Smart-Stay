using System;
using System.Collections.Generic;

namespace Smart_Stay.Models
{
    public class ListingApplicationReviewViewModel
    {
        public int ListingApplicationId { get; set; }
        public string ApplicationStatus { get; set; } = string.Empty;
        public DateOnly ApplicationDate { get; set; }

        public int PropertyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? RejectionReason { get; set; }
        public decimal Price { get; set; }
        public string PropertyType { get; set; } = string.Empty;
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public string LandlordName { get; set; } = string.Empty;

        public string? AffidavitPath { get; set; }
        public List<string> ImagePaths { get; set; } = new List<string>();
    }
}