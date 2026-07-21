using System;
using System.Collections.Generic;

namespace Smart_Stay.Models;

public partial class Property
{
    public int PropertyId { get; set; }

    public int LandlordId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Location { get; set; } = null!;

    public decimal Price { get; set; }

    public string PropertyType { get; set; } = null!;

    public DateOnly DateListed { get; set; }

    public string Status { get; set; } = null!;
    public string? ImagePath { get; set; }

    public int? Bedrooms { get; set; }

    public int? Bathrooms { get; set; }

    public virtual Landlord Landlord { get; set; } = null!;

    public virtual ICollection<ListingApplication> ListingApplications { get; set; } = new List<ListingApplication>();

    public virtual ICollection<RentalApplication> RentalApplications { get; set; } = new List<RentalApplication>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
