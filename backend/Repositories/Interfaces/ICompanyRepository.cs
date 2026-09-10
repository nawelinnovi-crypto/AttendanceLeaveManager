using System.Threading.Tasks;
using backend.Entities;

namespace backend.Repositories.Interfaces
{
    public interface ICompanyRepository : IMongoRepository<CompanyEntity>
    {
        Task<CompanyEntity?> GetByEmailAsync(string email);
        Task<CompanyEntity?> GetByCodeAsync(string companyCode);
    }
}
