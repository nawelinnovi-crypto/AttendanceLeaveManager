using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using backend.Data.Interfaces;
using backend.Entities;
using backend.Repositories.Interfaces;

namespace backend.Repositories
{
    public class EmployeeRepository : MongoRepository<EmployeeEntity>, IEmployeeRepository
    {
        public EmployeeRepository(IBaseContext context) : base(context)
        {
        }

        public async Task<List<EmployeeEntity>> GetAllEmployeesAsync()
        {
            var filter = Builders<EmployeeEntity>.Filter.Eq(e => e.IsDeleted, false);
            return await SearchFor(filter, 0, 1000, "CreationDate", "asc");
        }

        public async Task<EmployeeEntity?> GetByEmployeeIdAsync(string id)
        {
            return await Get(id);
        }

        public async Task<EmployeeEntity?> GetByEmailAsync(string email)
        {
            var filter = Builders<EmployeeEntity>.Filter.And(
                Builders<EmployeeEntity>.Filter.Eq(e => e.Email, email),
                Builders<EmployeeEntity>.Filter.Eq(e => e.IsDeleted, false)
            );
            return await SearchbyQuery(filter);
        }
    }
}
