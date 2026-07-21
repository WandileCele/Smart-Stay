using System;
using System.Collections.Generic;

namespace Smart_Stay.Models;

public partial class ListingApplication
{
    public int ListingApplicationId { get; set; }

    public int AdminId { get; set; }

    public int? LandlordId { get; set; }

    public int? PropertyId { get; set; }

    public string ApplicationStatus { get; set; } = null!;

    public DateOnly ApplicationDate { get; set; }

    public virtual Admin Admin { get; set; } = null!;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Landlord? Landlord { get; set; }

    public virtual Property? Property { get; set; }
}
