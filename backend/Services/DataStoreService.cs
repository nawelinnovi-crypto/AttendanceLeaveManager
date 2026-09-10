using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using backend.Entities;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services
{
    public interface IDataStoreService
    {
        Task<List<Employee>> GetEmployeesAsync();
        Task<Employee?> GetEmployeeByIdAsync(string id);
        Task<Employee> CreateEmployeeAsync(Employee emp);
        Task<Employee?> UpdateEmployeeAsync(string id, Employee emp);
        Task<bool> DeleteEmployeeAsync(string id);

        Task<List<AttendanceRecord>> GetAttendanceRecordsAsync(string? employeeId = null, DateTime? date = null, string? department = null);
        Task<AttendanceRecord?> GetTodayAttendanceAsync(string employeeId);
        Task<AttendanceRecord> ClockInAsync(string employeeId, string? notes = null);
        Task<AttendanceRecord> ClockOutAsync(string employeeId, string? notes = null);
        Task<AttendanceRecord> AddManualAttendanceAsync(ManualAttendanceRequest req);

        Task<List<LeaveRequest>> GetLeaveRequestsAsync(string? employeeId = null, string? status = null);
        Task<LeaveRequest> CreateLeaveRequestAsync(CreateLeaveRequest req);
        Task<LeaveRequest?> ProcessLeaveRequestAsync(string leaveId, bool approved, string? managerComment);
        Task<LeaveRequest?> CancelLeaveRequestAsync(string leaveId);
        Task<LeaveBalance> GetLeaveBalanceAsync(string employeeId);

        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    }

    public class DataStoreService : IDataStoreService
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IMongoRepository<AttendanceRecordEntity> _attendanceRepo;
        private readonly IMongoRepository<LeaveRequestEntity> _leaveRequestRepo;
        private readonly IMongoRepository<LeaveBalanceEntity> _leaveBalanceRepo;
        private readonly ITenantService _tenantService;

        public DataStoreService(
            IEmployeeRepository employeeRepo,
            IMongoRepository<AttendanceRecordEntity> attendanceRepo,
            IMongoRepository<LeaveRequestEntity> leaveRequestRepo,
            IMongoRepository<LeaveBalanceEntity> leaveBalanceRepo,
            ITenantService tenantService)
        {
            _employeeRepo = employeeRepo;
            _attendanceRepo = attendanceRepo;
            _leaveRequestRepo = leaveRequestRepo;
            _leaveBalanceRepo = leaveBalanceRepo;
            _tenantService = tenantService;

            // Seed MongoDB database if empty on startup
            _ = SeedDatabaseIfEmptyAsync();
        }

        private async Task SeedDatabaseIfEmptyAsync()
        {
            try
            {
                var empFilter = Builders<EmployeeEntity>.Filter.Eq(e => e.IsDeleted, false);
                var count = await _employeeRepo.GetCount(empFilter);

                if (count == 0)
                {
                    var seedEmployees = new List<Employee>
                    {
                        new Employee { FullName = "Sarah Jenkins", Email = "sarah.j@company.com", Department = "Engineering", JobTitle = "Senior Lead Developer", Role = "Manager", AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&q=80&w=120", HireDate = new DateTime(2021, 3, 15) },
                        new Employee { FullName = "Karim Benali", Email = "karim.b@company.com", Department = "Engineering", JobTitle = "Fullstack Developer", Role = "Employee", AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&q=80&w=120", HireDate = new DateTime(2022, 6, 1) },
                        new Employee { FullName = "Elena Rostova", Email = "elena.r@company.com", Department = "Human Resources", JobTitle = "HR Specialist", Role = "Manager", AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=120", HireDate = new DateTime(2020, 1, 10) },
                        new Employee { FullName = "David Chen", Email = "david.c@company.com", Department = "Marketing", JobTitle = "Marketing Manager", Role = "Employee", AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&q=80&w=120", HireDate = new DateTime(2023, 2, 20) },
                        new Employee { FullName = "Amina Mansouri", Email = "amina.m@company.com", Department = "Finance", JobTitle = "Financial Analyst", Role = "Employee", AvatarUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?auto=format&fit=crop&q=80&w=120", HireDate = new DateTime(2022, 11, 5) }
                    };

                    foreach (var emp in seedEmployees)
                    {
                        var createdEmp = await _employeeRepo.Create(ToEntity(emp));
                        var empIdStr = createdEmp.Id.ToString();

                        var balance = new LeaveBalance
                        {
                            EmployeeId = empIdStr,
                            AnnualTotal = 25,
                            AnnualUsed = emp.FullName.Contains("Karim") ? 8 : (emp.FullName.Contains("David") ? 12 : 5),
                            SickTotal = 10,
                            SickUsed = emp.FullName.Contains("Karim") ? 2 : 0,
                            TeleworkAllowed = 24,
                            TeleworkUsed = emp.FullName.Contains("Karim") ? 10 : 4
                        };
                        await _leaveBalanceRepo.Create(ToEntity(balance));
                    }
                }
            }
            catch (Exception)
            {
                // Soft ignore seed errors if DB is starting up
            }
        }

        // Employee Operations
        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var builder = Builders<EmployeeEntity>.Filter;
            var filter = builder.Eq(e => e.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(companyId))
            {
                filter &= builder.Eq(e => e.CompanyId, companyId);
            }

            var entities = await _employeeRepo.SearchFor(filter, 0, 1000, "CreationDate", "asc");
            return entities.Select(ToModel).ToList();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var entity = await _employeeRepo.Get(id);
            return entity != null ? ToModel(entity) : null;
        }

        public async Task<Employee> CreateEmployeeAsync(Employee emp)
        {
            if (emp.HireDate == default)
            {
                emp.HireDate = DateTime.Today;
            }

            var companyId = _tenantService.GetCurrentCompanyId();
            var entity = ToEntity(emp);
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                entity.CompanyId = companyId;
            }

            var createdEntity = await _employeeRepo.Create(entity);
            var createdModel = ToModel(createdEntity);

            int annualTotal = createdModel.ContractType?.Equals("SIVP", StringComparison.OrdinalIgnoreCase) == true ? 18 : 21;

            var balanceEntity = new LeaveBalanceEntity
            {
                CompanyId = companyId,
                EmployeeId = createdModel.Id,
                AnnualTotal = annualTotal,
                AnnualUsed = 0,
                SickTotal = 10,
                SickUsed = 0,
                TeleworkAllowed = 20,
                TeleworkUsed = 0
            };
            await _leaveBalanceRepo.Create(balanceEntity);

            return createdModel;
        }

        public async Task<Employee?> UpdateEmployeeAsync(string id, Employee emp)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var dbEntity = await _employeeRepo.Get(id);
            if (dbEntity == null) return null;

            bool contractChanged = !string.IsNullOrWhiteSpace(emp.ContractType) && !emp.ContractType.Equals(dbEntity.ContractType, StringComparison.OrdinalIgnoreCase);

            dbEntity.FullName = string.IsNullOrWhiteSpace(emp.FullName) ? dbEntity.FullName : emp.FullName;
            dbEntity.Email = string.IsNullOrWhiteSpace(emp.Email) ? dbEntity.Email : emp.Email;
            dbEntity.Department = string.IsNullOrWhiteSpace(emp.Department) ? dbEntity.Department : emp.Department;
            dbEntity.JobTitle = string.IsNullOrWhiteSpace(emp.JobTitle) ? dbEntity.JobTitle : emp.JobTitle;
            dbEntity.AvatarUrl = string.IsNullOrWhiteSpace(emp.AvatarUrl) ? dbEntity.AvatarUrl : emp.AvatarUrl;
            dbEntity.Role = string.IsNullOrWhiteSpace(emp.Role) ? dbEntity.Role : emp.Role;
            dbEntity.ShiftSchedule = string.IsNullOrWhiteSpace(emp.ShiftSchedule) ? dbEntity.ShiftSchedule : emp.ShiftSchedule;
            dbEntity.ContractType = string.IsNullOrWhiteSpace(emp.ContractType) ? dbEntity.ContractType : emp.ContractType;
            if (emp.HireDate != default) dbEntity.HireDate = emp.HireDate;

            var updated = await _employeeRepo.Update(dbEntity);

            if (updated != null && contractChanged)
            {
                var balFilter = Builders<LeaveBalanceEntity>.Filter.And(
                    Builders<LeaveBalanceEntity>.Filter.Eq(b => b.EmployeeId, id),
                    Builders<LeaveBalanceEntity>.Filter.Eq(b => b.IsDeleted, false)
                );
                var dbBal = await _leaveBalanceRepo.SearchbyQuery(balFilter);
                if (dbBal != null)
                {
                    dbBal.AnnualTotal = updated.ContractType.Equals("SIVP", StringComparison.OrdinalIgnoreCase) ? 18 : 21;
                    await _leaveBalanceRepo.Update(dbBal);
                }
            }

            return updated != null ? ToModel(updated) : null;
        }

        public async Task<bool> DeleteEmployeeAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            return await _employeeRepo.Delete(id);
        }

        // Attendance Operations
        public async Task<List<AttendanceRecord>> GetAttendanceRecordsAsync(string? employeeId = null, DateTime? date = null, string? department = null)
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var builder = Builders<AttendanceRecordEntity>.Filter;
            var filter = builder.Eq(a => a.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(companyId))
            {
                filter &= builder.Eq(a => a.CompanyId, companyId);
            }

            if (!string.IsNullOrWhiteSpace(employeeId))
            {
                filter &= builder.Eq(a => a.EmployeeId, employeeId);
            }

            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                filter &= builder.Gte(a => a.Date, startDate) & builder.Lt(a => a.Date, endDate);
            }

            if (!string.IsNullOrWhiteSpace(department) && !department.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filter &= builder.Regex(a => a.Department, new MongoDB.Bson.BsonRegularExpression($"^{department}$", "i"));
            }

            var entities = await _attendanceRepo.SearchFor(filter, 0, 1000, "Date", "desc");
            return entities.Select(ToModel).ToList();
        }

        public async Task<AttendanceRecord?> GetTodayAttendanceAsync(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId)) return null;

            var today = DateTime.Today;
            var companyId = _tenantService.GetCurrentCompanyId();
            var builder = Builders<AttendanceRecordEntity>.Filter;
            var filter = builder.Eq(a => a.EmployeeId, employeeId) &
                         builder.Eq(a => a.IsDeleted, false) &
                         builder.Gte(a => a.Date, today) &
                         builder.Lt(a => a.Date, today.AddDays(1));

            if (!string.IsNullOrWhiteSpace(companyId))
            {
                filter &= builder.Eq(a => a.CompanyId, companyId);
            }

            var entity = await _attendanceRepo.SearchbyQuery(filter);
            return entity != null ? ToModel(entity) : null;
        }

        public async Task<AttendanceRecord> ClockInAsync(string employeeId, string? notes = null)
        {
            var emp = await GetEmployeeByIdAsync(employeeId);
            if (emp == null) throw new Exception("Employee not found");

            var companyId = _tenantService.GetCurrentCompanyId();
            var today = DateTime.Today;
            var existing = await GetTodayAttendanceAsync(employeeId);
            var now = DateTime.Now;

            if (existing != null)
            {
                if (existing.ClockInTime.HasValue) return existing;

                var dbRecord = await _attendanceRepo.Get(existing.Id);
                if (dbRecord != null)
                {
                    dbRecord.ClockInTime = now;
                    dbRecord.Status = now.TimeOfDay > new TimeSpan(9, 15, 0) ? "Late" : "OnTime";
                    dbRecord.Notes = notes ?? dbRecord.Notes;
                    await _attendanceRepo.Update(dbRecord);
                    return ToModel(dbRecord);
                }
            }

            var newRecord = new AttendanceRecord
            {
                EmployeeId = emp.Id,
                EmployeeName = emp.FullName,
                Department = emp.Department,
                Date = today,
                ClockInTime = now,
                ClockOutTime = null,
                Status = now.TimeOfDay > new TimeSpan(9, 15, 0) ? "Late" : "OnTime",
                WorkingHours = 0,
                Notes = notes ?? "Clocked in via system"
            };

            var recordEntity = ToEntity(newRecord);
            if (!string.IsNullOrWhiteSpace(companyId)) recordEntity.CompanyId = companyId;

            var created = await _attendanceRepo.Create(recordEntity);
            return ToModel(created);
        }

        public async Task<AttendanceRecord> ClockOutAsync(string employeeId, string? notes = null)
        {
            var existing = await GetTodayAttendanceAsync(employeeId);
            if (existing == null || !existing.ClockInTime.HasValue)
            {
                throw new Exception("Cannot clock out without clocking in first today.");
            }

            var dbRecord = await _attendanceRepo.Get(existing.Id);
            if (dbRecord == null) throw new Exception("Attendance record not found in database.");

            var now = DateTime.Now;
            var clockInTime = dbRecord.ClockInTime ?? existing.ClockInTime ?? now;
            var duration = now - clockInTime;

            dbRecord.ClockOutTime = now;
            dbRecord.WorkingHours = Math.Round(duration.TotalHours, 2);
            if (!string.IsNullOrWhiteSpace(notes))
            {
                dbRecord.Notes = string.IsNullOrWhiteSpace(dbRecord.Notes) ? notes : $"{dbRecord.Notes} | {notes}";
            }

            var updated = await _attendanceRepo.Update(dbRecord);
            return ToModel(updated);
        }

        public async Task<AttendanceRecord> AddManualAttendanceAsync(ManualAttendanceRequest req)
        {
            var emp = await GetEmployeeByIdAsync(req.EmployeeId);
            if (emp == null) throw new Exception("Employee not found");

            var companyId = _tenantService.GetCurrentCompanyId();
            DateTime clockIn = req.Date.Date;
            DateTime clockOut = req.Date.Date;

            if (TimeSpan.TryParse(req.ClockIn, out var inTs)) clockIn = clockIn.Add(inTs);
            else clockIn = clockIn.AddHours(9);

            if (TimeSpan.TryParse(req.ClockOut, out var outTs)) clockOut = clockOut.Add(outTs);
            else clockOut = clockOut.AddHours(17);

            var hours = Math.Round((clockOut - clockIn).TotalHours, 2);
            var status = inTs > new TimeSpan(9, 15, 0) ? "Late" : "OnTime";

            var newRecord = new AttendanceRecord
            {
                EmployeeId = emp.Id,
                EmployeeName = emp.FullName,
                Department = emp.Department,
                Date = req.Date.Date,
                ClockInTime = clockIn,
                ClockOutTime = clockOut,
                Status = status,
                WorkingHours = hours > 0 ? hours : 8.0,
                Notes = req.Notes
            };

            var recordEntity = ToEntity(newRecord);
            if (!string.IsNullOrWhiteSpace(companyId)) recordEntity.CompanyId = companyId;

            var created = await _attendanceRepo.Create(recordEntity);
            return ToModel(created);
        }

        // Leave Requests Operations
        public async Task<List<LeaveRequest>> GetLeaveRequestsAsync(string? employeeId = null, string? status = null)
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var builder = Builders<LeaveRequestEntity>.Filter;
            var filter = builder.Eq(l => l.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(companyId))
            {
                filter &= builder.Eq(l => l.CompanyId, companyId);
            }

            if (!string.IsNullOrWhiteSpace(employeeId))
            {
                filter &= builder.Eq(l => l.EmployeeId, employeeId);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filter &= builder.Regex(l => l.Status, new MongoDB.Bson.BsonRegularExpression($"^{status}$", "i"));
            }

            var entities = await _leaveRequestRepo.SearchFor(filter, 0, 1000, "SubmittedDate", "desc");
            return entities.Select(ToModel).ToList();
        }

        public async Task<LeaveRequest> CreateLeaveRequestAsync(CreateLeaveRequest req)
        {
            var emp = await GetEmployeeByIdAsync(req.EmployeeId);
            if (emp == null) throw new Exception("Employee not found");

            var companyId = _tenantService.GetCurrentCompanyId();
            var totalDays = (int)(req.EndDate.Date - req.StartDate.Date).TotalDays + 1;
            if (totalDays <= 0) totalDays = 1;

            var leave = new LeaveRequest
            {
                EmployeeId = emp.Id,
                EmployeeName = emp.FullName,
                Department = emp.Department,
                LeaveType = req.LeaveType,
                StartDate = req.StartDate.Date,
                EndDate = req.EndDate.Date,
                TotalDays = totalDays,
                Reason = req.Reason,
                Status = "Pending",
                SubmittedDate = DateTime.Now
            };

            var leaveEntity = ToEntity(leave);
            if (!string.IsNullOrWhiteSpace(companyId)) leaveEntity.CompanyId = companyId;

            var created = await _leaveRequestRepo.Create(leaveEntity);
            return ToModel(created);
        }

        public async Task<LeaveRequest?> ProcessLeaveRequestAsync(string leaveId, bool approved, string? managerComment)
        {
            if (string.IsNullOrWhiteSpace(leaveId)) return null;

            var dbLeave = await _leaveRequestRepo.Get(leaveId);
            if (dbLeave == null) return null;

            dbLeave.Status = approved ? "Approved" : "Rejected";
            dbLeave.ManagerComment = managerComment;
            await _leaveRequestRepo.Update(dbLeave);

            if (approved)
            {
                var balFilter = Builders<LeaveBalanceEntity>.Filter.And(
                    Builders<LeaveBalanceEntity>.Filter.Eq(b => b.EmployeeId, dbLeave.EmployeeId),
                    Builders<LeaveBalanceEntity>.Filter.Eq(b => b.IsDeleted, false)
                );
                var dbBal = await _leaveBalanceRepo.SearchbyQuery(balFilter);
                if (dbBal != null)
                {
                    switch (dbLeave.LeaveType.ToLower())
                    {
                        case "annual":
                            dbBal.AnnualUsed += dbLeave.TotalDays;
                            break;
                        case "sick":
                            dbBal.SickUsed += dbLeave.TotalDays;
                            break;
                        case "telework":
                            dbBal.TeleworkUsed += dbLeave.TotalDays;
                            break;
                        case "unpaid":
                            dbBal.UnpaidUsed += dbLeave.TotalDays;
                            break;
                    }
                    await _leaveBalanceRepo.Update(dbBal);
                }
            }

            return ToModel(dbLeave);
        }

        public async Task<LeaveRequest?> CancelLeaveRequestAsync(string leaveId)
        {
            if (string.IsNullOrWhiteSpace(leaveId)) return null;

            var dbLeave = await _leaveRequestRepo.Get(leaveId);
            if (dbLeave == null) return null;

            var previousStatus = dbLeave.Status;
            dbLeave.Status = "Canceled";
            await _leaveRequestRepo.Update(dbLeave);

            // Revert used leave days balance if request was previously Approved
            if (previousStatus != null && previousStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                var balFilter = Builders<LeaveBalanceEntity>.Filter.And(
                    Builders<LeaveBalanceEntity>.Filter.Eq(b => b.EmployeeId, dbLeave.EmployeeId),
                    Builders<LeaveBalanceEntity>.Filter.Eq(b => b.IsDeleted, false)
                );
                var dbBal = await _leaveBalanceRepo.SearchbyQuery(balFilter);
                if (dbBal != null)
                {
                    switch (dbLeave.LeaveType.ToLower())
                    {
                        case "annual":
                            dbBal.AnnualUsed = Math.Max(0, dbBal.AnnualUsed - dbLeave.TotalDays);
                            break;
                        case "sick":
                            dbBal.SickUsed = Math.Max(0, dbBal.SickUsed - dbLeave.TotalDays);
                            break;
                        case "telework":
                            dbBal.TeleworkUsed = Math.Max(0, dbBal.TeleworkUsed - dbLeave.TotalDays);
                            break;
                        case "unpaid":
                            dbBal.UnpaidUsed = Math.Max(0, dbBal.UnpaidUsed - dbLeave.TotalDays);
                            break;
                    }
                    await _leaveBalanceRepo.Update(dbBal);
                }
            }

            return ToModel(dbLeave);
        }

        public async Task<LeaveBalance> GetLeaveBalanceAsync(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return new LeaveBalance { EmployeeId = string.Empty };
            }

            var companyId = _tenantService.GetCurrentCompanyId();
            var filter = Builders<LeaveBalanceEntity>.Filter.And(
                Builders<LeaveBalanceEntity>.Filter.Eq(b => b.EmployeeId, employeeId),
                Builders<LeaveBalanceEntity>.Filter.Eq(b => b.IsDeleted, false)
            );
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                filter &= Builders<LeaveBalanceEntity>.Filter.Eq(b => b.CompanyId, companyId);
            }

            var emp = await GetEmployeeByIdAsync(employeeId);
            int expectedAnnualTotal = (emp != null && emp.ContractType?.Equals("SIVP", StringComparison.OrdinalIgnoreCase) == true) ? 18 : 21;

            var dbBal = await _leaveBalanceRepo.SearchbyQuery(filter);
            if (dbBal != null)
            {
                if (dbBal.AnnualTotal != expectedAnnualTotal)
                {
                    dbBal.AnnualTotal = expectedAnnualTotal;
                    await _leaveBalanceRepo.Update(dbBal);
                }
                return ToModel(dbBal);
            }

            var newBal = new LeaveBalanceEntity { CompanyId = companyId, EmployeeId = employeeId, AnnualTotal = expectedAnnualTotal, SickTotal = 10, TeleworkAllowed = 20 };
            var created = await _leaveBalanceRepo.Create(newBal);
            return ToModel(created);
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var employees = await GetEmployeesAsync();
            var today = DateTime.Today;
            var todayRecords = await GetAttendanceRecordsAsync(date: today);

            int presentToday = todayRecords.Count(a => a.ClockInTime.HasValue);
            int lateToday = todayRecords.Count(a => a.Status == "Late");

            var allLeaves = await GetLeaveRequestsAsync();
            int onLeaveToday = allLeaves.Count(l =>
                l.Status == "Approved" &&
                l.StartDate.Date <= today &&
                l.EndDate.Date >= today);

            int pendingLeaves = allLeaves.Count(l => l.Status == "Pending");

            var recentPointages = await GetAttendanceRecordsAsync();
            var recentLeaves = await GetLeaveRequestsAsync();

            return new DashboardSummaryDto
            {
                TotalEmployees = employees.Count,
                PresentToday = presentToday,
                LateToday = lateToday,
                OnLeaveToday = onLeaveToday,
                PendingLeavesCount = pendingLeaves,
                RecentPointages = recentPointages.Take(5).ToList(),
                RecentLeaves = recentLeaves.Take(5).ToList()
            };
        }

        // Entity <-> Model Mappings
        private static Employee ToModel(EmployeeEntity entity) => new Employee
        {
            Id = entity.Id.ToString(),
            FullName = entity.FullName,
            Email = entity.Email,
            Department = entity.Department,
            JobTitle = entity.JobTitle,
            AvatarUrl = entity.AvatarUrl,
            Role = entity.Role,
            ShiftSchedule = entity.ShiftSchedule,
            ContractType = string.IsNullOrWhiteSpace(entity.ContractType) ? "CDI" : entity.ContractType,
            HireDate = entity.HireDate
        };

        private static EmployeeEntity ToEntity(Employee model) => new EmployeeEntity
        {
            Id = !string.IsNullOrWhiteSpace(model.Id) && ObjectId.TryParse(model.Id, out var objId) ? objId : ObjectId.Empty,
            FullName = model.FullName,
            Email = model.Email,
            Department = model.Department,
            JobTitle = model.JobTitle,
            AvatarUrl = model.AvatarUrl,
            Role = model.Role,
            ShiftSchedule = model.ShiftSchedule,
            ContractType = string.IsNullOrWhiteSpace(model.ContractType) ? "CDI" : model.ContractType,
            HireDate = model.HireDate
        };

        private static AttendanceRecord ToModel(AttendanceRecordEntity entity) => new AttendanceRecord
        {
            Id = entity.Id.ToString(),
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.EmployeeName,
            Department = entity.Department,
            Date = entity.Date,
            ClockInTime = entity.ClockInTime,
            ClockOutTime = entity.ClockOutTime,
            Status = entity.Status,
            WorkingHours = entity.WorkingHours,
            Notes = entity.Notes
        };

        private static AttendanceRecordEntity ToEntity(AttendanceRecord model) => new AttendanceRecordEntity
        {
            Id = !string.IsNullOrWhiteSpace(model.Id) && ObjectId.TryParse(model.Id, out var objId) ? objId : ObjectId.Empty,
            EmployeeId = model.EmployeeId,
            EmployeeName = model.EmployeeName,
            Department = model.Department,
            Date = model.Date,
            ClockInTime = model.ClockInTime,
            ClockOutTime = model.ClockOutTime,
            Status = model.Status,
            WorkingHours = model.WorkingHours,
            Notes = model.Notes
        };

        private static LeaveRequest ToModel(LeaveRequestEntity entity) => new LeaveRequest
        {
            Id = entity.Id.ToString(),
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.EmployeeName,
            Department = entity.Department,
            LeaveType = entity.LeaveType,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TotalDays = entity.TotalDays,
            Reason = entity.Reason,
            Status = entity.Status,
            SubmittedDate = entity.SubmittedDate,
            ManagerComment = entity.ManagerComment
        };

        private static LeaveRequestEntity ToEntity(LeaveRequest model) => new LeaveRequestEntity
        {
            Id = !string.IsNullOrWhiteSpace(model.Id) && ObjectId.TryParse(model.Id, out var objId) ? objId : ObjectId.Empty,
            EmployeeId = model.EmployeeId,
            EmployeeName = model.EmployeeName,
            Department = model.Department,
            LeaveType = model.LeaveType,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            TotalDays = model.TotalDays,
            Reason = model.Reason,
            Status = model.Status,
            SubmittedDate = model.SubmittedDate,
            ManagerComment = model.ManagerComment
        };

        private static LeaveBalance ToModel(LeaveBalanceEntity entity) => new LeaveBalance
        {
            EmployeeId = entity.EmployeeId,
            AnnualTotal = entity.AnnualTotal,
            AnnualUsed = entity.AnnualUsed,
            SickTotal = entity.SickTotal,
            SickUsed = entity.SickUsed,
            TeleworkAllowed = entity.TeleworkAllowed,
            TeleworkUsed = entity.TeleworkUsed,
            UnpaidUsed = entity.UnpaidUsed
        };

        private static LeaveBalanceEntity ToEntity(LeaveBalance model) => new LeaveBalanceEntity
        {
            Id = ObjectId.Empty,
            EmployeeId = model.EmployeeId,
            AnnualTotal = model.AnnualTotal,
            AnnualUsed = model.AnnualUsed,
            SickTotal = model.SickTotal,
            SickUsed = model.SickUsed,
            TeleworkAllowed = model.TeleworkAllowed,
            TeleworkUsed = model.TeleworkUsed,
            UnpaidUsed = model.UnpaidUsed
        };
    }
}
