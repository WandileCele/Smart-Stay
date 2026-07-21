using System;
using System.Collections.Generic;

namespace Smart_Stay.Models;

public partial class Landlord
{
    public int UserId { get; set; }

    public string VerificationStatus { get; set; } = null!;

    public virtual ICollection<ListingApplication> ListingApplications { get; set; } = new List<ListingApplication>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public virtual ICollection<RentalApplication> RentalApplications { get; set; } = new List<RentalApplication>();

    public virtual User User { get; set; } = null!;
}
