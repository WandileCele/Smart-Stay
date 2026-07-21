using System;
using System.Collections.Generic;

namespace Smart_Stay.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int PropertyId { get; set; }

    public int TenantId { get; set; }

    public string Comment { get; set; } = null!;

    public byte Rating { get; set; }

    public DateOnly ReviewDate { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}
