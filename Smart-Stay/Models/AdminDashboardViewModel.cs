using System.Collections.Generic;

namespace Smart_Stay.Models
{
    public class AdminDashboardViewModel
    {
        public string FirstName { get; set; } = "";
        public int PendingApplications { get; set; }

        public int ApprovedApplications { get; set; }

        public int RejectedApplications { get; set; }

        public List<ListingApplicationCardViewModel> Applications { get; set; }
            = new List<ListingApplicationCardViewModel>();
    }
}