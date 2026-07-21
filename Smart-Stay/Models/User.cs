using System;
using System.Collections.Generic;

namespace Smart_Stay.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string SurName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNo { get; set; } = null!;

    public DateOnly DateRegistered { get; set; }

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual Admin? Admin { get; set; }

    public virtual Landlord? Landlord { get; set; }

    public virtual Tenant? Tenant { get; set; }
}
