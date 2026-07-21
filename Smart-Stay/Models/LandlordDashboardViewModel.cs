using System.Collections.Generic;

namespace Smart_Stay.Models
{
    public class LandlordDashboardViewModel
    {
        public int TotalProperties { get; set; }

        public int TotalApplications { get; set; }

        public int AvailableProperties { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public List<PropertyCardViewModel> Properties { get; set; } = new();

    }
}