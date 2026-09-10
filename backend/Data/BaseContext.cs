using System;
using MongoDB.Driver;
using backend.Data.Interfaces;
using backend.Settings;
using backend.Utils;

namespace backend.Data
{
    public class BaseContext : IBaseContext
    {
        private readonly IMongoDatabase? _db;
        private readonly MongoClient? _mongoClient;

        public IClientSessionHandle? Session { get; set; }

        public BaseContext()
        {
        }

        public BaseContext(AuthenticateDatabaseSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (!string.IsNullOrEmpty(settings.ConnectionString) && settings.ConnectionString.StartsWith("mongodb"))
            {
                _mongoClient = new MongoClient(settings.ConnectionString);
                _db = _mongoClient.GetDatabase(settings.DatabaseName);
            }
            else
            {
                string host = string.IsNullOrWhiteSpace(settings.ConnectionString) ? "127.0.0.1" : settings.ConnectionString;
                if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    host = "127.0.0.1";
                }
                int port = settings.Port > 0 ? settings.Port : 27017;
                bool isSsl = string.Equals(settings.IsSSL, "true", StringComparison.OrdinalIgnoreCase);

                MongoClientSettings clientSettings = new MongoClientSettings
                {
                    Server = new MongoServerAddress(host, port),
                    UseTls = isSsl
                };

                if (!string.IsNullOrEmpty(settings.Login))
                {
                    string pwd = !string.IsNullOrEmpty(settings.Password)
                        ? PasswordSecurity.DecryptString(settings.Password, StaticKey.Key)
                        : string.Empty;

                    clientSettings.Credential = MongoCredential.CreateCredential(
                        settings.DatabaseName,
                        settings.Login,
                        pwd
                    );
                }

                _mongoClient = new MongoClient(clientSettings);
                _db = _mongoClient.GetDatabase(string.IsNullOrWhiteSpace(settings.DatabaseName) ? "AttendanceLeaveDb" : settings.DatabaseName);
            }
        }

        public IMongoCollection<TEntity> GetCollection<TEntity>(string name)
        {
            if (_db == null)
            {
                throw new InvalidOperationException("Database context is not initialized.");
            }
            return _db.GetCollection<TEntity>(name);
        }
    }
}
