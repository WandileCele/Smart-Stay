namespace Smart_Stay.Models
{
    public class ApplicationReviewViewModel
    {
        public int RentalApplicationId { get; set; }
        public string TenantName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Employment { get; set; } = "";
        public string PropertyTitle { get; set; } = "";
        public DateOnly ApplicationDate { get; set; }
        public string Status { get; set; } = "";
        public string? PayslipPath { get; set; }
        public string? PayslipFileName { get; set; }
        public DateOnly? LeaseStartDate { get; set; }
        public DateOnly? LeaseEndDate { get; set; }
    }
}