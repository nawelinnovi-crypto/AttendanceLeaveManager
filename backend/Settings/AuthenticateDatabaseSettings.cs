namespace backend.Settings
{
    public class AuthenticateDatabaseSettings : IAuthenticateDatabaseSettings
    {
        public string CollectionName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string IsSSL { get; set; } = "false";
        public int Port { get; set; } = 27017;
    }
}
