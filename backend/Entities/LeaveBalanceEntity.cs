namespace backend.Entities
{
    public class LeaveBalanceEntity : EntityBase
    {
        public string CompanyId { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public int AnnualTotal { get; set; } = 25;
        public int AnnualUsed { get; set; } = 5;
        public int SickTotal { get; set; } = 10;
        public int SickUsed { get; set; } = 1;
        public int TeleworkAllowed { get; set; } = 20;
        public int TeleworkUsed { get; set; } = 6;
        public int UnpaidUsed { get; set; } = 0;
    }
}
