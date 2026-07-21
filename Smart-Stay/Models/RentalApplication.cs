using System;
using System.Collections.Generic;

namespace Smart_Stay.Models;

public partial class RentalApplication
{
    public int RentalApplicationId { get; set; }

    public int TenantId { get; set; }

    public DateOnly ApplicationDate { get; set; }

    public string RentalApplicationStatus { get; set; } = null!;

    public string IdNumber { get; set; } = null!;

    public int? LandlordId { get; set; }

    public int PropertyId { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Landlord? Landlord { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}
