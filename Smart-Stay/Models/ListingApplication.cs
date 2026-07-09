using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class ListingApplication
    {
        [Key]
        public int listingApplicationID { get; set; }
        public int adminID { get; set; }
        public Admin Admin { get; set; }
        public int landlordID { get; set; }
        public Landlord Landlord { get; set; }
        public int propertyID { get; set; }
        public Property Property { get; set; }
        public string application_status { get; set; }
        public DateOnly application_date {  get; set; }
    }
}
