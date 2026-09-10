using System;
using System.Collections.Generic;

namespace backend.Models
{
    public class Employee
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee"; // "Employee" or "Manager"
        public string ShiftSchedule { get; set; } = "09:00 - 17:00";
        public string ContractType { get; set; } = "CDI"; // "SIVP", "CDD", "CDI"
        public DateTime HireDate { get; set; }
    }

    public class AttendanceRecord
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }
        public string Status { get; set; } = "OnTime"; // "OnTime", "Late", "EarlyDeparture", "Absent"
        public double WorkingHours { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class LeaveRequest
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string LeaveType { get; set; } = "Annual"; // "Annual", "Sick", "Unpaid", "Telework", "Maternity"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
        public DateTime SubmittedDate { get; set; }
        public string? ManagerComment { get; set; }
    }

    public class LeaveBalance
    {
        public string EmployeeId { get; set; } = string.Empty;
        public int AnnualTotal { get; set; } = 25;
        public int AnnualUsed { get; set; } = 5;
        public int AnnualRemaining => AnnualTotal - AnnualUsed;

        public int SickTotal { get; set; } = 10;
        public int SickUsed { get; set; } = 1;
        public int SickRemaining => SickTotal - SickUsed;

        public int TeleworkAllowed { get; set; } = 20;
        public int TeleworkUsed { get; set; } = 6;

        public int UnpaidUsed { get; set; } = 0;
    }

    public class ClockInRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class ClockOutRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class ManualAttendanceRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string ClockIn { get; set; } = "09:00"; // HH:mm format
        public string ClockOut { get; set; } = "17:00"; // HH:mm format
        public string Notes { get; set; } = string.Empty;
    }

    public class CreateLeaveRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string LeaveType { get; set; } = "Annual";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LeaveApprovalRequest
    {
        public bool Approved { get; set; }
        public string? ManagerComment { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int TotalEmployees { get; set; }
        public int PresentToday { get; set; }
        public int LateToday { get; set; }
        public int OnLeaveToday { get; set; }
        public int PendingLeavesCount { get; set; }
        public List<AttendanceRecord> RecentPointages { get; set; } = new();
        public List<LeaveRequest> RecentLeaves { get; set; } = new();
    }
}
