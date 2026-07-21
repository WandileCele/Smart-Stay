namespace Smart_Stay.Models
{
    public class ListingApplicationCardViewModel
    {
        public int ListingApplicationId { get; set; }

        public int PropertyId { get; set; }

        public string PropertyTitle { get; set; } = string.Empty;

        public string LandlordName { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string PropertyType { get; set; } = string.Empty;

        public DateOnly ApplicationDate { get; set; }

        public string ApplicationStatus { get; set; } = string.Empty;
    }
}