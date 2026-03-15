using LicenseManager.API.Data;
using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;

namespace LicenseManager.API.Repositories
{
    public class LicenseRepository : ILicenseRepository
    {
        private readonly DbConnectionFactory _factory;

        public LicenseRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<string> GenerateLicense(long subscriptionId, long createdBy)
        {
            using var conn = _factory.CreateConnection();

            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_generate_license(@p_subscription_id,@p_created_by)",
                conn);

            cmd.Parameters.AddWithValue("@p_subscription_id", subscriptionId);
            cmd.Parameters.AddWithValue("@p_created_by", createdBy);

            var result = await cmd.ExecuteScalarAsync();

            return result?.ToString()
                ?? throw new InvalidOperationException("License generation did not return a license key.");
        }

        public async Task<ActivationResponseDto> ActivateLicense(
            string licenseKey,
            string machineId,
            string hostname,
            string ipAddress)
        {
            using var conn = _factory.CreateConnection();

            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_activate_license(@p_license_key,@p_machine_id,@p_hostname,@p_ip_address)",
                conn);

            cmd.Parameters.AddWithValue("@p_license_key", licenseKey);
            cmd.Parameters.AddWithValue("@p_machine_id", machineId);
            cmd.Parameters.AddWithValue("@p_hostname", hostname ?? string.Empty);
            cmd.Parameters.AddWithValue("@p_ip_address", ipAddress ?? string.Empty);

            using var reader = await cmd.ExecuteReaderAsync();

            var response = new ActivationResponseDto();

            if (await reader.ReadAsync())
            {
                response.StatusCode = reader["status_code"].ToString();
                response.StatusMessage = reader["status_message"].ToString();
            }

            return response;
        }

        public async Task SaveLicense(long subscriptionId, string licenseId, string licenseKey)
        {
            using var conn = _factory.CreateConnection();

            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_insert_license(@p_subscription_id,@p_license_id,@p_license_key)",
                conn);

            cmd.Parameters.AddWithValue("@p_subscription_id", subscriptionId);
            cmd.Parameters.AddWithValue("@p_license_id", licenseId);
            cmd.Parameters.AddWithValue("@p_license_key", licenseKey);

            //await cmd.ExecuteNonQueryAsync();
        }

        public async Task<LicensePayload> GetLicensePayload(long subscriptionId)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var payload = new LicensePayload();

            using (var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_get_license_payload(@sid)", conn))
            {
                cmd.Parameters.AddWithValue("@sid", subscriptionId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    payload.CompanyId = Convert.ToInt64(reader["company_id"]);
                    payload.CompanyName = reader["company_name"].ToString();
                    payload.PlanName = reader["plan_name"].ToString();
                    payload.LicenseDurationType = reader["license_duration_type"].ToString();
                    payload.LicenseMode = reader["license_mode"].ToString();
                    payload.StartDate = Convert.ToDateTime(reader["start_date"]);
                    payload.ExpiryDate = Convert.ToDateTime(reader["expiry_date"]);
                    payload.SubscriptionId = subscriptionId;
                }
            }

            payload.LicenseAllocations = new List<LicenseAllocation>();

            using (var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_get_license_allocations(@sid)", conn))
            {
                cmd.Parameters.AddWithValue("@sid", subscriptionId);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    payload.LicenseAllocations.Add(new LicenseAllocation
                    {
                        OsTypeId = Convert.ToInt32(reader["os_type_id"]),
                        AllocatedCount = Convert.ToInt32(reader["allocated_count"])
                    });
                }
            }

            payload.Features = new List<string>();

            using (var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_get_product_features(@sid)", conn))
            {
                cmd.Parameters.AddWithValue("@sid", subscriptionId);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var featureName = reader["feature_name"].ToString();

                    if (!string.IsNullOrWhiteSpace(featureName))
                    {
                        payload.Features.Add(featureName);
                    }
                }
            }

            return payload;
        }
    }
}
