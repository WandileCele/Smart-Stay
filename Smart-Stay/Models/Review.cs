using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class Review
    {
        [Key]
        public int reviewID { get; set; }
        public int propertyID { get; set; }
        public Property Property { get; set }
        public int tenantID { get; set; }
        public Tenant Tenant { get; set; }
        public string comment { get; set; }
        public int rating { get; set; }
        public DateOnly reviewDate { get; set; }
    }
}
