using LicenseManager.API.Data;
using LicenseManager.API.Models;
using Npgsql;

namespace LicenseManager.API.Repositories
{
    public class UserRepository
    {
        private readonly DbConnectionFactory _factory;

        public UserRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT * FROM fn_user_login(@email)", conn);

            cmd.Parameters.AddWithValue("email", email);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.Read())
                return null;

            return new User
            {
                Id = reader.GetInt64(0),
                Email = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Role = reader.GetString(3)
            };
        }

        public async Task SaveRefreshToken(long userId, string token)
        {
            using var conn = _factory.CreateConnection();

            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_insert_refresh_token(@uid,@token,@expiry)", conn);

            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@expiry", DateTime.UtcNow.AddDays(7));

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<User?> GetUserById(long id)
        {
            using var conn = _factory.CreateConnection();

            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT id,email,role FROM users WHERE id=@id",
                conn);

            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt64(0),
                    Email = reader.GetString(1),
                    Role = reader.GetString(2)
                };
            }

            return null;
        }
    }
}