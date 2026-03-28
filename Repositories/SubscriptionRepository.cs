using System.Text.Json;
using LicenseManager.API.Data;
using LicenseManager.API.DTOs.Subscriptions;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace LicenseManager.API.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly DbConnectionFactory _factory;

        public SubscriptionRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyCollection<SubscriptionRecord>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_subscriptions()", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var subscriptions = new List<SubscriptionRecord>();
            while (await reader.ReadAsync())
            {
                subscriptions.Add(Map(reader));
            }

            return subscriptions;
        }

        public async Task<SubscriptionRecord?> GetById(long id)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_subscription_by_id(@p_id)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<SubscriptionRecord?> Create(CreateSubscriptionRequestDto request, long createdBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_create_subscription(@p_company_id,@p_plan_category,@p_plan_id,@p_product_id,@p_start_date,@p_duration_days,@p_license_mode,@p_request_source,@p_source_reference,@p_allocations,@p_created_by)",
                conn);

            cmd.Parameters.Add("@p_company_id", NpgsqlDbType.Bigint).Value = request.CompanyId;
            cmd.Parameters.Add("@p_plan_category", NpgsqlDbType.Varchar).Value = request.PlanCategory;
            cmd.Parameters.Add("@p_plan_id", NpgsqlDbType.Bigint).Value = request.PlanId.HasValue ? request.PlanId.Value : DBNull.Value;
            cmd.Parameters.Add("@p_product_id", NpgsqlDbType.Bigint).Value = request.ProductId.HasValue ? request.ProductId.Value : DBNull.Value;
            cmd.Parameters.Add("@p_start_date", NpgsqlDbType.Timestamp).Value = request.StartDate.HasValue
                ? DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Unspecified)
                : DBNull.Value;
            cmd.Parameters.Add("@p_duration_days", NpgsqlDbType.Integer).Value = request.DurationDays.HasValue ? request.DurationDays.Value : DBNull.Value;
            cmd.Parameters.Add("@p_license_mode", NpgsqlDbType.Varchar).Value = string.IsNullOrWhiteSpace(request.LicenseMode) ? "OFFLINE" : request.LicenseMode;
            cmd.Parameters.Add("@p_request_source", NpgsqlDbType.Varchar).Value = "LICENSE_SERVER_OFFLINE";
            cmd.Parameters.Add("@p_source_reference", NpgsqlDbType.Varchar).Value = DBNull.Value;
            cmd.Parameters.Add("@p_allocations", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(request.Allocations);
            cmd.Parameters.Add("@p_created_by", NpgsqlDbType.Bigint).Value = createdBy;

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<bool> Reject(long id, long updatedBy, string? reason)
        {
            if (await GetById(id) == null)
            {
                return false;
            }

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "CALL sp_reject_subscription(@p_subscription_id,@p_reason,@p_updated_by,@p_updated_at)",
                conn);

            cmd.Parameters.Add("@p_subscription_id", NpgsqlDbType.Bigint).Value = id;
            cmd.Parameters.Add("@p_reason", NpgsqlDbType.Text).Value = string.IsNullOrWhiteSpace(reason)
                ? DBNull.Value
                : reason.Trim();
            cmd.Parameters.Add("@p_updated_by", NpgsqlDbType.Bigint).Value = updatedBy;
            cmd.Parameters.Add("@p_updated_at", NpgsqlDbType.Timestamp).Value =
                DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static SubscriptionRecord Map(NpgsqlDataReader reader)
        {
            return new SubscriptionRecord
            {
                Id = Convert.ToInt64(reader["id"]),
                CompanyId = Convert.ToInt64(reader["company_id"]),
                CompanyName = reader["company_name"].ToString() ?? string.Empty,
                PlanId = Convert.ToInt64(reader["plan_id"]),
                PlanName = reader["plan_name"].ToString() ?? string.Empty,
                PlanCategory = reader["plan_category"].ToString() ?? string.Empty,
                Status = reader["status"].ToString() ?? string.Empty,
                StatusDescription = reader["status_description"] == DBNull.Value ? null : reader["status_description"].ToString(),
                StartDate = reader["start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["start_date"]),
                EndDate = reader["end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["end_date"]),
                RequestedAt = reader["requested_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["requested_at"]),
                ApprovedAt = reader["approved_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["approved_at"]),
                LicenseMode = reader["license_mode"].ToString() ?? string.Empty,
                DeviceLimit = Convert.ToInt32(reader["device_limit"]),
                TotalAllocated = Convert.ToInt32(reader["total_allocated"]),
                Allocations = ParseAllocations(reader["allocations"])
            };
        }

        private static List<SubscriptionAllocationRecord> ParseAllocations(object value)
        {
            if (value == DBNull.Value)
            {
                return new List<SubscriptionAllocationRecord>();
            }

            var raw = value?.ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<SubscriptionAllocationRecord>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<SubscriptionAllocationRecord>>(raw) ?? new List<SubscriptionAllocationRecord>();
            }
            catch
            {
                return new List<SubscriptionAllocationRecord>();
            }
        }
    }
}
