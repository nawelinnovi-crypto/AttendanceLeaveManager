using System;

namespace backend.Entities
{
    public class EmployeeEntity : EntityBase
    {
        public string CompanyId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
        public string ShiftSchedule { get; set; } = "09:00 - 17:00";
        public string ContractType { get; set; } = "CDI"; // "SIVP", "CDD", "CDI"
        public DateTime HireDate { get; set; }
    }
}
