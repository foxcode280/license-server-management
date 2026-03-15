using Npgsql;

namespace LicenseManager.API.Data
{
    public class DbConnectionFactory
    {
        private readonly IConfiguration _config;

        public DbConnectionFactory(IConfiguration config)
        {
            _config = config;
        }

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(
                _config.GetConnectionString("DefaultConnection"));
        }
    }
}