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

        public async Task<LicenseGenerationContext> GetLicenseGenerationContext(long subscriptionId)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                @"SELECT s.id,
                         s.status,
                         l.license_key
                  FROM subscriptions s
                  LEFT JOIN LATERAL (
                      SELECT license_key
                      FROM licenses
                      WHERE subscription_id = s.id
                      ORDER BY issue_date DESC
                      LIMIT 1
                  ) l ON true
                  WHERE s.id = @sid",
                conn);

            cmd.Parameters.AddWithValue("@sid", subscriptionId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("Subscription not found.");
            }

            return new LicenseGenerationContext
            {
                SubscriptionId = reader.GetInt64(0),
                SubscriptionStatus = reader.GetString(1),
                ExistingLicenseKey = reader.IsDBNull(2) ? null : reader.GetString(2)
            };
        }

        public async Task SaveLicense(
            long subscriptionId,
            string licenseKey,
            string licenseDurationType,
            string licenseMode)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_insert_license(@p_subscription_id,@p_license_key,@p_license_duration_type,@p_license_mode)",
                conn);

            cmd.Parameters.AddWithValue("@p_subscription_id", subscriptionId);
            cmd.Parameters.AddWithValue("@p_license_key", licenseKey);
            cmd.Parameters.AddWithValue("@p_license_duration_type", licenseDurationType);
            cmd.Parameters.AddWithValue("@p_license_mode", licenseMode);

            await cmd.ExecuteNonQueryAsync();
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
                    payload.CompanyName = reader["company_name"].ToString() ?? string.Empty;
                    payload.PlanName = reader["plan_name"].ToString() ?? string.Empty;
                    payload.LicenseDurationType = reader["license_duration_type"].ToString() ?? string.Empty;
                    payload.LicenseMode = reader["license_mode"].ToString() ?? string.Empty;
                    payload.StartDate = Convert.ToDateTime(reader["start_date"]);
                    payload.ExpiryDate = Convert.ToDateTime(reader["expiry_date"]);
                    payload.SubscriptionId = subscriptionId;
                }
            }

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
