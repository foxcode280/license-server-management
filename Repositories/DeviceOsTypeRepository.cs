using LicenseManager.API.Data;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;

namespace LicenseManager.API.Repositories
{
    public class DeviceOsTypeRepository : IDeviceOsTypeRepository
    {
        private readonly DbConnectionFactory _factory;

        public DeviceOsTypeRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyCollection<DeviceOsTypeRecord>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_device_os_types()", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<DeviceOsTypeRecord>();
            while (await reader.ReadAsync())
            {
                items.Add(new DeviceOsTypeRecord
                {
                    Id = Convert.ToInt64(reader["id"]),
                    OsName = reader["os_name"].ToString() ?? string.Empty,
                    Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                    Status = reader["status"].ToString() ?? string.Empty
                });
            }

            return items;
        }
    }
}
