using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using backend.Entities;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using backend.Utils;

namespace backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly ICompanyRepository _companyRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IMongoRepository<LeaveBalanceEntity> _leaveBalanceRepo;
        private readonly IConfiguration _configuration;

        public AuthService(
            ICompanyRepository companyRepo,
            IEmployeeRepository employeeRepo,
            IMongoRepository<LeaveBalanceEntity> leaveBalanceRepo,
            IConfiguration configuration)
        {
            _companyRepo = companyRepo;
            _employeeRepo = employeeRepo;
            _leaveBalanceRepo = leaveBalanceRepo;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterCompanyAsync(CompanyRegisterDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Email)) throw new Exception("Email is required");
            if (string.IsNullOrWhiteSpace(dto.Password)) throw new Exception("Password is required");
            if (string.IsNullOrWhiteSpace(dto.CompanyName)) throw new Exception("Company Name is required");

            var existing = await _companyRepo.GetByEmailAsync(dto.Email);
            if (existing != null)
            {
                throw new Exception("A company registration with this email already exists.");
            }

            string encryptedPassword = PasswordSecurity.EncryptString(dto.Password, StaticKey.Key);
            string companyCode = GenerateCompanyCode(dto.CompanyName);

            var companyEntity = new CompanyEntity
            {
                CompanyName = dto.CompanyName.Trim(),
                CompanyCode = companyCode,
                AdminName = string.IsNullOrWhiteSpace(dto.AdminName) ? "Admin" : dto.AdminName.Trim(),
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = encryptedPassword,
                Plan = "Standard",
                Status = "Active",
                SubscriptionDate = DateTime.UtcNow
            };

            var createdCompany = await _companyRepo.Create(companyEntity);
            string companyId = createdCompany.Id.ToString();

            // Auto-create Company Admin Employee
            var adminEmp = new EmployeeEntity
            {
                CompanyId = companyId,
                FullName = createdCompany.AdminName,
                Email = createdCompany.Email,
                Department = "Management",
                JobTitle = "Company Director",
                Role = "Manager",
                AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&q=80&w=120",
                ShiftSchedule = "09:00 - 17:00",
                HireDate = DateTime.Today
            };

            var createdAdminEmp = await _employeeRepo.Create(adminEmp);

            // Initialize Leave Balance for Admin
            var adminBalance = new LeaveBalanceEntity
            {
                CompanyId = companyId,
                EmployeeId = createdAdminEmp.Id.ToString(),
                AnnualTotal = 25,
                AnnualUsed = 0,
                SickTotal = 10,
                SickUsed = 0,
                TeleworkAllowed = 20,
                TeleworkUsed = 0
            };
            await _leaveBalanceRepo.Create(adminBalance);

            string jwtToken = GenerateJwtToken(companyId, createdCompany.CompanyName, createdCompany.Email, "Manager");

            return new AuthResponseDto
            {
                Token = jwtToken,
                CompanyId = companyId,
                CompanyName = createdCompany.CompanyName,
                AdminName = createdCompany.AdminName,
                Email = createdCompany.Email,
                Role = "Manager",
                Plan = createdCompany.Plan
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new Exception("Email and Password are required.");
            }

            var company = await _companyRepo.GetByEmailAsync(dto.Email);
            if (company == null)
            {
                throw new Exception("Invalid email or password.");
            }

            string decrypted = PasswordSecurity.DecryptString(company.PasswordHash, StaticKey.Key);
            if (!string.Equals(decrypted, dto.Password))
            {
                throw new Exception("Invalid email or password.");
            }

            string companyId = company.Id.ToString();
            string jwtToken = GenerateJwtToken(companyId, company.CompanyName, company.Email, "Manager");

            return new AuthResponseDto
            {
                Token = jwtToken,
                CompanyId = companyId,
                CompanyName = company.CompanyName,
                AdminName = company.AdminName,
                Email = company.Email,
                Role = "Manager",
                Plan = company.Plan
            };
        }

        private string GenerateJwtToken(string companyId, string companyName, string email, string role)
        {
            var secretKey = _configuration["JwtSettings:Secret"] ?? "AttendanceLeaveManagerSuperSecretKey2026_AttendanceLeaveDb_Key";
            var issuer = _configuration["JwtSettings:Issuer"] ?? "AttendanceLeaveBackend";
            var audience = _configuration["JwtSettings:Audience"] ?? "AttendanceLeaveFrontend";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, companyId),
                new Claim(ClaimTypes.NameIdentifier, companyId),
                new Claim("companyId", companyId),
                new Claim("companyName", companyName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateCompanyCode(string companyName)
        {
            string clean = companyName.Replace(" ", "").ToUpper();
            clean = clean.Length >= 3 ? clean.Substring(0, 3) : clean.PadRight(3, 'X');
            return $"{clean}-{new Random().Next(100, 999)}";
        }
    }
}
