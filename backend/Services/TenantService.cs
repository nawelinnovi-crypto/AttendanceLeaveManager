using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using backend.Services.Interfaces;

namespace backend.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetCurrentCompanyId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return string.Empty;

            // 1. Try extracting companyId claim from JWT token
            var companyIdClaim = context.User?.FindFirst("companyId")?.Value
                                 ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(companyIdClaim))
            {
                return companyIdClaim;
            }

            // 2. Try extracting from X-Company-Id HTTP header
            if (context.Request.Headers.TryGetValue("X-Company-Id", out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
            {
                return headerVal.ToString();
            }

            return string.Empty;
        }
    }
}
