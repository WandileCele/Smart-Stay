using System.ComponentModel.DataAnnotations;

namespace Smart_Stay.Models
{
    public class User
    {
        [Key]
        public int userID {get; set;}
        public string firstName {get; set;}
        public string surName {get; set;}
        public string email {get; set;}
        public string phoneNo {get; set;}
        public DateOnly date_Registered {get; set;}
        public string password {get; set;}
        public string Role {get; set;}
    }
}
