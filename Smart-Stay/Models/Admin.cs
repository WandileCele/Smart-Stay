using System;
using System.Collections.Generic;

namespace Smart_Stay.Models;

public partial class Admin
{
    public int UserId { get; set; }

    public virtual ICollection<ListingApplication> ListingApplications { get; set; } = new List<ListingApplication>();

    public virtual User User { get; set; } = null!;
}
