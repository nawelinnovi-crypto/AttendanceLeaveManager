using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Entities;

namespace backend.Repositories.Interfaces
{
    public interface IEmployeeRepository : IMongoRepository<EmployeeEntity>
    {
        Task<List<EmployeeEntity>> GetAllEmployeesAsync();
        Task<EmployeeEntity?> GetByEmployeeIdAsync(string id);
        Task<EmployeeEntity?> GetByEmailAsync(string email);
    }
}
