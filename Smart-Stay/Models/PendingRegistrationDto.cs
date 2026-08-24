namespace Smart_Stay.Models
{
    public class PendingRegistrationDto
    {
        public string FirstName { get; set; } = null!;
        public string SurName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNo { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? EmploymentStatus { get; set; }
        public string Code { get; set; } = null!;
        public DateTime ExpiryUtc { get; set; }
    }
}