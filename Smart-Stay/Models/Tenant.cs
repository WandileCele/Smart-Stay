using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class Tenant
    {
        [Key]
        public int userID { get; set; }
        public string employment_status { get; set; }
    }
}
