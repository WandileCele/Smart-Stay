using System;
using System.Collections.Generic;

namespace Smart_Stay.Models
{
    public class TenantDashboardViewModel
    {
        public string TenantName { get; set; } = "";
        public int TotalApplications { get; set; }
        public int ApprovedApplications { get; set; }
        public int PendingApplications { get; set; }
        public int RejectedApplications { get; set; }

        public List<TenantApplicationViewModel> Applications { get; set; }
            = new List<TenantApplicationViewModel>();
    }

    public class TenantApplicationViewModel
    {
        public int RentalApplicationId { get; set; }
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; } = "";
        public string Location { get; set; } = "";
        public decimal Price { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public string? ImagePath { get; set; }

        public DateOnly ApplicationDate { get; set; }
        public string ApplicationStatus { get; set; } = "";
        public DateOnly? LeaseStartDate { get; set; }
        public DateOnly? LeaseEndDate { get; set; }

        public bool CanRate { get; set; }
        public bool HasRated { get; set; }

        public string FilterCategory
        {
            get
            {
                if (ApplicationStatus == "Rejected")
                    return "Rejected";

                bool leaseEnded = LeaseEndDate.HasValue
                    && LeaseEndDate.Value < DateOnly.FromDateTime(DateTime.Now);

                if (ApplicationStatus == "Approved" && leaseEnded)
                    return "Past";

                return "Current";
            }
        }
    }
}