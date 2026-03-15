using LicenseManager.API.Data;
using LicenseManager.API.Models;
using Npgsql;

namespace LicenseManager.API.Repositories
{
    public class RefreshTokenRepository
    {
        private readonly DbConnectionFactory _factory;

        public RefreshTokenRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<RefreshToken?> GetRefreshToken(string token)
        {
            using var conn = _factory.CreateConnection();

            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT user_id, expiry_date FROM refresh_tokens WHERE token=@token AND is_revoked=false",
                conn);

            cmd.Parameters.AddWithValue("@token", token);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new RefreshToken
                {
                    UserId = reader.GetInt64(0),
                    ExpiryDate = reader.GetDateTime(1)
                };
            }

            return null;
        }

        public async Task SaveRefreshToken(long userId, string token, DateTime expiry)
        {
            using var conn = _factory.CreateConnection();

            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO refresh_tokens (user_id, token, expiry_date)
                  VALUES (@userId, @token, @expiry)",
                conn);

            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@expiry", expiry);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RotateRefreshToken(string oldToken, string newToken, DateTime expiry)
        {
            using var conn = _factory.CreateConnection();

            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                using var revokeCmd = new NpgsqlCommand(
                    @"UPDATE refresh_tokens
                      SET is_revoked=true
                      WHERE token=@oldToken AND is_revoked=false
                      RETURNING user_id",
                    conn,
                    transaction);

                revokeCmd.Parameters.AddWithValue("@oldToken", oldToken);
                var userId = await revokeCmd.ExecuteScalarAsync();

                if (userId is null)
                {
                    throw new InvalidOperationException("Refresh token is invalid or already revoked.");
                }

                using var insertCmd = new NpgsqlCommand(
                    @"INSERT INTO refresh_tokens (user_id, token, expiry_date)
                      VALUES (@userId, @newToken, @expiry)",
                    conn,
                    transaction);

                insertCmd.Parameters.AddWithValue("@userId", (long)userId);
                insertCmd.Parameters.AddWithValue("@newToken", newToken);
                insertCmd.Parameters.AddWithValue("@expiry", expiry);

                await insertCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RevokeRefreshToken(string token)
        {
            using var conn = _factory.CreateConnection();

            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "UPDATE refresh_tokens SET is_revoked=true WHERE token=@token",
                conn);

            cmd.Parameters.AddWithValue("@token", token);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
