namespace backend.Settings
{
    public interface IAuthenticateDatabaseSettings
    {
        string CollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
        string Login { get; set; }
        string Password { get; set; }
        string IsSSL { get; set; }
        int Port { get; set; }
    }
}
