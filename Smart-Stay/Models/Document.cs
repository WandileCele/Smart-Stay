using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class Document
    {
        [Key]
        public int documentID {  get; set; }
        public int listingApplicationID {  get; set; }
        public ListingApplication ListingApplication { get; set; }
        public int rentalApplicationID { get; set; }
        public RentalApplication RentalApplication { get; set; }
        public string document_type { get; set; }
        public DateOnly upload_date { get; set; }
        public string documentPath { get; set; }
    }
}
