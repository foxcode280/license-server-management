using BCrypt.Net;
using LicenseManager.API.Data;
using LicenseManager.API.DTOs.Users;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace LicenseManager.API.Repositories
{
    public class UserManagementRepository : IUserManagementRepository
    {
        private const string DefaultTemporaryPassword = "Welcome@123";
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

        public async Task<UserManagementRecord?> Create(CreateUserRequestDto request, long createdBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_insert_user(@p_name,@p_email,@p_role,@p_designation,@p_mobile,@p_alternate_mobile,@p_is_disabled,@p_password_hash,@p_created_by)",
                conn);

            cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = request.Name;
            cmd.Parameters.Add("@p_email", NpgsqlDbType.Varchar).Value = request.Email;
            cmd.Parameters.Add("@p_role", NpgsqlDbType.Varchar).Value = request.Role;
            cmd.Parameters.Add("@p_designation", NpgsqlDbType.Varchar).Value = request.Designation;
            cmd.Parameters.Add("@p_mobile", NpgsqlDbType.Varchar).Value = request.Mobile;
            cmd.Parameters.Add("@p_alternate_mobile", NpgsqlDbType.Varchar).Value =
                string.IsNullOrWhiteSpace(request.AlternateMobile) ? DBNull.Value : request.AlternateMobile.Trim();
            cmd.Parameters.Add("@p_is_disabled", NpgsqlDbType.Boolean).Value = request.IsDisabled;
            cmd.Parameters.Add("@p_password_hash", NpgsqlDbType.Text).Value = BCrypt.Net.BCrypt.HashPassword(DefaultTemporaryPassword);
            cmd.Parameters.Add("@p_created_by", NpgsqlDbType.Bigint).Value = createdBy;

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<UserManagementRecord?> Update(long id, UpdateUserRequestDto request, long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_update_user(@p_id,@p_name,@p_email,@p_role,@p_designation,@p_mobile,@p_alternate_mobile,@p_is_disabled,@p_updated_by)",
                conn);

            cmd.Parameters.Add("@p_id", NpgsqlDbType.Bigint).Value = id;
            cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = request.Name;
            cmd.Parameters.Add("@p_email", NpgsqlDbType.Varchar).Value = request.Email;
            cmd.Parameters.Add("@p_role", NpgsqlDbType.Varchar).Value = request.Role;
            cmd.Parameters.Add("@p_designation", NpgsqlDbType.Varchar).Value = request.Designation;
            cmd.Parameters.Add("@p_mobile", NpgsqlDbType.Varchar).Value = request.Mobile;
            cmd.Parameters.Add("@p_alternate_mobile", NpgsqlDbType.Varchar).Value =
                string.IsNullOrWhiteSpace(request.AlternateMobile) ? DBNull.Value : request.AlternateMobile.Trim();
            cmd.Parameters.Add("@p_is_disabled", NpgsqlDbType.Boolean).Value = request.IsDisabled;
            cmd.Parameters.Add("@p_updated_by", NpgsqlDbType.Bigint).Value = updatedBy;

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
                Name = reader["name"].ToString() ?? string.Empty,
                Email = reader["email"].ToString() ?? string.Empty,
                Role = reader["role"].ToString() ?? string.Empty,
                Designation = reader["designation"] == DBNull.Value ? string.Empty : reader["designation"].ToString() ?? string.Empty,
                Mobile = reader["mobile"] == DBNull.Value ? string.Empty : reader["mobile"].ToString() ?? string.Empty,
                AlternateMobile = reader["alternate_mobile"] == DBNull.Value ? null : reader["alternate_mobile"].ToString(),
                LastLogin = reader["last_login"] == DBNull.Value ? null : Convert.ToDateTime(reader["last_login"]),
                IsDisabled = Convert.ToBoolean(reader["is_disabled"]),
                Theme = reader["theme"] == DBNull.Value ? "light" : reader["theme"].ToString() ?? "light",
                MenuPosition = reader["menu_position"] == DBNull.Value ? "sidebar" : reader["menu_position"].ToString() ?? "sidebar",
                ProfilePhoto = reader["profile_photo"] == DBNull.Value ? null : reader["profile_photo"].ToString(),
                CreatedAt = reader["created_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["created_at"]),
                UpdatedAt = reader["updated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["updated_at"])
            };
        }
    }
}
