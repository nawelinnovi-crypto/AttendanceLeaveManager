using System;

namespace backend.Entities
{
    public class AttendanceRecordEntity : EntityBase
    {
        public string CompanyId { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }
        public string Status { get; set; } = "OnTime";
        public double WorkingHours { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
