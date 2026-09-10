using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterCompanyAsync(CompanyRegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}
