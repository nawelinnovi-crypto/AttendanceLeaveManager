using MongoDB.Driver;

namespace backend.Data.Interfaces
{
    public interface IBaseContext
    {
        IMongoCollection<TEntity> GetCollection<TEntity>(string name);
        IClientSessionHandle? Session { get; set; }
    }
}
