using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using backend.Entities;

namespace backend.Repositories.Interfaces
{
    public interface IMongoRepository<TEntity> where TEntity : EntityBase
    {
        Task<TEntity> Create(TEntity entity);
        Task<TEntity> Update(TEntity entity);
        Task<TEntity> Get(string Id);
        Task<bool> Delete(string Id);
        Task<bool> DeleteMany(FilterDefinition<TEntity> filter);
        Task<TEntity?> SearchbyQuery(FilterDefinition<TEntity> filter);
        Task<List<TEntity>> SearchFor(FilterDefinition<TEntity> filter, int index = 0, int pageSize = 70, string? order = null, string sort = "asc");
        Task<long> GetCount(FilterDefinition<TEntity> filter);
        MatchCollection VerifyObjectId(string Id);
    }
}
