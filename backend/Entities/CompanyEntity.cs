using System;

namespace backend.Entities
{
    public class CompanyEntity : EntityBase
    {
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Plan { get; set; } = "Starter"; // "Starter", "Pro", "Enterprise"
        public string Status { get; set; } = "Active"; // "Active", "Pending", "Cancelled"
        public DateTime SubscriptionDate { get; set; } = DateTime.UtcNow;
    }
}
