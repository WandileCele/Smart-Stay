using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class Landlord
    {
        [Key]
        public int userID { get; set; }
        public string verification_Status { get; set; }
    }
}
