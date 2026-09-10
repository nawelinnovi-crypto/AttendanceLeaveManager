using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using backend.Data.Interfaces;
using backend.Entities;
using backend.Repositories.Interfaces;

namespace backend.Repositories
{
    public class MongoRepository<TEntity> : IMongoRepository<TEntity> where TEntity : EntityBase
    {
        private readonly IMongoCollection<TEntity> _collection;
        private readonly IBaseContext _context;

        public MongoRepository(IBaseContext context)
        {
            _context = context;
            _collection = _context.GetCollection<TEntity>(typeof(TEntity).Name);
        }

        public async Task<TEntity> Create(TEntity entity)
        {
            if (entity.Id == ObjectId.Empty)
            {
                entity.Id = ObjectId.GenerateNewId();
            }
            entity.CreationDate = DateTime.UtcNow;
            entity.UpdatedDate = DateTime.UtcNow;
            await _collection.InsertOneAsync(entity);
            return entity;
        }

        public async Task<TEntity> Update(TEntity entity)
        {
            entity.UpdatedDate = DateTime.UtcNow;
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
            var result = await _collection.ReplaceOneAsync(filter, entity);
            return result.ModifiedCount > 0 ? entity : null!;
        }

        public async Task<bool> Delete(string Id)
        {
            if (VerifyObjectId(Id).Count == 0) return false;
            var objectId = new ObjectId(Id);
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, objectId);
            var update = Builders<TEntity>.Update.Set(e => e.IsDeleted, true).Set(e => e.UpdatedDate, DateTime.UtcNow);
            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteMany(FilterDefinition<TEntity> filter)
        {
            var update = Builders<TEntity>.Update.Set(e => e.IsDeleted, true).Set(e => e.UpdatedDate, DateTime.UtcNow);
            var result = await _collection.UpdateManyAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<TEntity> Get(string Id)
        {
            if (VerifyObjectId(Id).Count == 0) return null!;
            var objectId = new ObjectId(Id);
            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Eq(e => e.Id, objectId),
                Builders<TEntity>.Filter.Eq(e => e.IsDeleted, false)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<TEntity>> SearchFor(FilterDefinition<TEntity> filter, int index = 0, int pageSize = 70, string? order = null, string sort = "asc")
        {
            var combinedFilter = Builders<TEntity>.Filter.And(
                filter,
                Builders<TEntity>.Filter.Eq(e => e.IsDeleted, false)
            );

            var findFluent = _collection.Find(combinedFilter);

            if (!string.IsNullOrEmpty(order))
            {
                var sortDef = sort == "desc"
                    ? Builders<TEntity>.Sort.Descending(order)
                    : Builders<TEntity>.Sort.Ascending(order);
                findFluent = findFluent.Sort(sortDef);
            }

            return await findFluent.Skip(index * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<long> GetCount(FilterDefinition<TEntity> filterParam)
        {
            var filter = Builders<TEntity>.Filter.And(
                filterParam,
                Builders<TEntity>.Filter.Eq(e => e.IsDeleted, false)
            );
            return await _collection.CountDocumentsAsync(filter);
        }

        public async Task<TEntity?> SearchbyQuery(FilterDefinition<TEntity> filter)
        {
            var combinedFilter = Builders<TEntity>.Filter.And(
                filter,
                Builders<TEntity>.Filter.Eq(e => e.IsDeleted, false)
            );
            return await _collection.Find(combinedFilter).FirstOrDefaultAsync();
        }

        public MatchCollection VerifyObjectId(string Id)
        {
            string pattern = @"^[0-9a-fA-F]{24}$";
            Regex checkForHexRegExp = new Regex(pattern);
            return checkForHexRegExp.Matches(Id ?? string.Empty);
        }
    }
}
