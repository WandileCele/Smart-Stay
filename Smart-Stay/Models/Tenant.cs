using System;
using System.Collections.Generic;

namespace Smart_Stay.Models;

public partial class Tenant
{
    public int UserId { get; set; }

    public string EmploymentStatus { get; set; } = null!;

    public virtual ICollection<RentalApplication> RentalApplications { get; set; } = new List<RentalApplication>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual User User { get; set; } = null!;
}
