using System.Text.Json;
using LicenseManager.API.Data;
using LicenseManager.API.DTOs.SubscriptionPlans;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace LicenseManager.API.Repositories
{
    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly DbConnectionFactory _factory;

        public SubscriptionPlanRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyCollection<SubscriptionPlanRecord>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_subscription_plans()", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var records = new List<SubscriptionPlanRecord>();
            while (await reader.ReadAsync())
            {
                records.Add(Map(reader));
            }

            return records;
        }

        public async Task<SubscriptionPlanRecord?> GetById(long id)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_subscription_plan_by_id(@p_id)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<SubscriptionPlanRecord?> Create(CreateSubscriptionPlanRequestDto request, long createdBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_insert_subscription_plan(@p_plan_code,@p_plan_name,@p_product_id,@p_mode,@p_status,@p_price,@p_duration_days,@p_billing_label,@p_device_limit,@p_device_limit_label,@p_description,@p_highlights,@p_features,@p_is_active,@p_created_by)",
                conn);

            AddPlanParameters(cmd, request.PlanCode, request.PlanName, request.ProductId, request.Mode, request.Status, request.Price,
                request.DurationDays, request.BillingLabel, request.DeviceLimit, request.DeviceLimitLabel, request.Description,
                request.Highlights, request.Features, request.IsActive);
            cmd.Parameters.AddWithValue("@p_created_by", createdBy);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<SubscriptionPlanRecord?> Update(long id, UpdateSubscriptionPlanRequestDto request, long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_update_subscription_plan(@p_id,@p_plan_code,@p_plan_name,@p_product_id,@p_mode,@p_status,@p_price,@p_duration_days,@p_billing_label,@p_device_limit,@p_device_limit_label,@p_description,@p_highlights,@p_features,@p_is_active,@p_updated_by)",
                conn);

            cmd.Parameters.AddWithValue("@p_id", id);
            AddPlanParameters(cmd, request.PlanCode, request.PlanName, request.ProductId, request.Mode, request.Status, request.Price,
                request.DurationDays, request.BillingLabel, request.DeviceLimit, request.DeviceLimitLabel, request.Description,
                request.Highlights, request.Features, request.IsActive);
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
            using var cmd = new NpgsqlCommand("CALL sp_deactivate_subscription_plan(@p_id,@p_updated_by)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_updated_by", updatedBy);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static void AddPlanParameters(
            NpgsqlCommand cmd,
            string planCode,
            string planName,
            long productId,
            string mode,
            string status,
            decimal price,
            int durationDays,
            string billingLabel,
            int deviceLimit,
            string deviceLimitLabel,
            string description,
            List<string> highlights,
            List<string> features,
            bool isActive)
        {
            cmd.Parameters.AddWithValue("@p_plan_code", planCode ?? string.Empty);
            cmd.Parameters.AddWithValue("@p_plan_name", planName ?? string.Empty);
            cmd.Parameters.AddWithValue("@p_product_id", productId);
            cmd.Parameters.AddWithValue("@p_mode", mode ?? string.Empty);
            cmd.Parameters.AddWithValue("@p_status", status ?? string.Empty);
            cmd.Parameters.AddWithValue("@p_price", price);
            cmd.Parameters.AddWithValue("@p_duration_days", durationDays);
            cmd.Parameters.AddWithValue("@p_billing_label", billingLabel ?? string.Empty);
            cmd.Parameters.AddWithValue("@p_device_limit", deviceLimit);
            cmd.Parameters.AddWithValue("@p_device_limit_label", deviceLimitLabel ?? string.Empty);
            cmd.Parameters.AddWithValue("@p_description", description ?? string.Empty);
            cmd.Parameters.AddWithValue("@p_highlights", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(highlights ?? new List<string>()));
            cmd.Parameters.AddWithValue("@p_features", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(features ?? new List<string>()));
            cmd.Parameters.AddWithValue("@p_is_active", isActive);
        }

        private static SubscriptionPlanRecord Map(NpgsqlDataReader reader)
        {
            return new SubscriptionPlanRecord
            {
                Id = Convert.ToInt64(reader["id"]),
                PlanCode = reader["plan_code"].ToString() ?? string.Empty,
                PlanName = reader["plan_name"].ToString() ?? string.Empty,
                ProductId = Convert.ToInt64(reader["product_id"]),
                ProductName = reader["product_name"].ToString() ?? string.Empty,
                Mode = reader["mode"].ToString() ?? string.Empty,
                Status = reader["status"].ToString() ?? string.Empty,
                Price = Convert.ToDecimal(reader["price"]),
                DurationDays = Convert.ToInt32(reader["duration_days"]),
                BillingLabel = reader["billing_label"].ToString() ?? string.Empty,
                DeviceLimit = Convert.ToInt32(reader["device_limit"]),
                DeviceLimitLabel = reader["device_limit_label"].ToString() ?? string.Empty,
                Description = reader["description"].ToString() ?? string.Empty,
                Highlights = ParseStringArray(reader["highlights"]),
                Features = ParseStringArray(reader["features"]),
                IsActive = Convert.ToBoolean(reader["is_active"])
            };
        }

        private static List<string> ParseStringArray(object value)
        {
            if (value == DBNull.Value)
            {
                return new List<string>();
            }

            var raw = value?.ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
