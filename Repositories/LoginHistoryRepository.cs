using LicenseManager.API.Data;
using LicenseManager.API.Models;
using Npgsql;
using System.Net.NetworkInformation;

public class LoginHistoryRepository
{
    private readonly IConfiguration _config;
    private readonly DbConnectionFactory _factory;

    public LoginHistoryRepository(IConfiguration config, DbConnectionFactory factory)
    {
        _config = config;
        _factory = factory;
    }

    public async Task SaveLoginHistory(LoginHistory log)
    {
        using var conn = _factory.CreateConnection();

        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand(
            "SELECT sp_insert_login_history(@uid,@email,@ip,@agent,@status,@reason)",
            conn);

        cmd.Parameters.AddWithValue("@uid", log.UserId);
        cmd.Parameters.AddWithValue("@email", log.Email ?? "");
        cmd.Parameters.AddWithValue("@ip", log.IpAddress ?? "");
        cmd.Parameters.AddWithValue("@agent", log.UserAgent ?? "");
        cmd.Parameters.AddWithValue("@status", log.LoginStatus);
        cmd.Parameters.AddWithValue("@reason", log.FailureReason ?? "");

        await cmd.ExecuteNonQueryAsync();

    }
}