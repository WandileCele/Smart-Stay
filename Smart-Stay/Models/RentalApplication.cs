using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class RentalApplication
    {
        [Key]
        public int rentalApplication {  get; set; }
        public int tenantID { get; set; }
        public Tenant Tenant { get; set; }
        public DateOnly application_date { get; set; }
        public string rentalApplicationStatus { get; set; }
        public string id_Number { get; set; }
        public int landlordID { get; set; }
        public Landlord Landlord { get; set; }
        public int propertyID { get; set; }
        public Property Property { get; set; }
    }
}
