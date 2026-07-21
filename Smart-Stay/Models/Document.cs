using System;
using System.Collections.Generic;

namespace Smart_Stay.Models;

public partial class Document
{
    public int DocumentId { get; set; }

    public int? ListingApplication { get; set; }

    public int? RentalApplicationId { get; set; }

    public string DocumentType { get; set; } = null!;

    public DateOnly UploadDate { get; set; }

    public string DocumentPath { get; set; } = null!;

    public virtual ListingApplication? ListingApplicationNavigation { get; set; }

    public virtual RentalApplication RentalApplication { get; set; } = null!;
}
