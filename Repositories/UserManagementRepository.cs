using LicenseManager.API.Data;
using LicenseManager.API.DTOs.Users;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;

namespace LicenseManager.API.Repositories
{
    public class UserManagementRepository : IUserManagementRepository
    {
        private readonly DbConnectionFactory _factory;

        public UserManagementRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyCollection<UserManagementRecord>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_users()", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var users = new List<UserManagementRecord>();
            while (await reader.ReadAsync())
            {
                users.Add(Map(reader));
            }

            return users;
        }

        public async Task<UserManagementRecord?> GetById(long id)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_user_by_id(@p_id)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<UserManagementRecord?> Update(long id, UpdateUserRequestDto request, long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_update_user(@p_id,@p_username,@p_email,@p_role,@p_is_active,@p_updated_by)",
                conn);

            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_username", request.Username);
            cmd.Parameters.AddWithValue("@p_email", request.Email);
            cmd.Parameters.AddWithValue("@p_role", request.Role);
            cmd.Parameters.AddWithValue("@p_is_active", request.IsActive);
            cmd.Parameters.AddWithValue("@p_updated_by", updatedBy);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<bool> Deactivate(long id, long updatedBy)
        {
            if (await GetById(id) == null)
            {
                return false;
            }

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("CALL sp_deactivate_user(@p_id,@p_updated_by)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_updated_by", updatedBy);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static UserManagementRecord Map(NpgsqlDataReader reader)
        {
            return new UserManagementRecord
            {
                Id = Convert.ToInt64(reader["id"]),
                Username = reader["username"].ToString() ?? string.Empty,
                Email = reader["email"].ToString() ?? string.Empty,
                Role = reader["role"].ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["is_active"]),
                CreatedAt = reader["created_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["created_at"]),
                UpdatedAt = reader["updated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["updated_at"])
            };
        }
    }
}
