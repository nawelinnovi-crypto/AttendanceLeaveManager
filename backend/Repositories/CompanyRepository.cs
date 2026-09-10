using System.Threading.Tasks;
using MongoDB.Driver;
using backend.Data.Interfaces;
using backend.Entities;
using backend.Repositories.Interfaces;

namespace backend.Repositories
{
    public class CompanyRepository : MongoRepository<CompanyEntity>, ICompanyRepository
    {
        public CompanyRepository(IBaseContext context) : base(context)
        {
        }

        public async Task<CompanyEntity?> GetByEmailAsync(string email)
        {
            var filter = Builders<CompanyEntity>.Filter.And(
                Builders<CompanyEntity>.Filter.Eq(c => c.Email, email.ToLower().Trim()),
                Builders<CompanyEntity>.Filter.Eq(c => c.IsDeleted, false)
            );
            return await SearchbyQuery(filter);
        }

        public async Task<CompanyEntity?> GetByCodeAsync(string companyCode)
        {
            var filter = Builders<CompanyEntity>.Filter.And(
                Builders<CompanyEntity>.Filter.Eq(c => c.CompanyCode, companyCode.ToUpper().Trim()),
                Builders<CompanyEntity>.Filter.Eq(c => c.IsDeleted, false)
            );
            return await SearchbyQuery(filter);
        }
    }
}
