using LicenseManager.API.Data;
using LicenseManager.API.DTOs.SubscriptionPlans;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;

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

        public async Task<SubscriptionPlanRecord?> Update(long id, UpdateSubscriptionPlanRequestDto request, long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_update_subscription_plan(@p_id,@p_plan_name,@p_product_id,@p_duration_days,@p_device_limit,@p_price,@p_is_active,@p_updated_by)",
                conn);

            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_plan_name", request.PlanName);
            cmd.Parameters.AddWithValue("@p_product_id", request.ProductId);
            cmd.Parameters.AddWithValue("@p_duration_days", request.DurationDays);
            cmd.Parameters.AddWithValue("@p_device_limit", request.DeviceLimit);
            cmd.Parameters.AddWithValue("@p_price", request.Price);
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
            using var cmd = new NpgsqlCommand("CALL sp_deactivate_subscription_plan(@p_id,@p_updated_by)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_updated_by", updatedBy);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static SubscriptionPlanRecord Map(NpgsqlDataReader reader)
        {
            return new SubscriptionPlanRecord
            {
                Id = Convert.ToInt64(reader["id"]),
                PlanName = reader["plan_name"].ToString() ?? string.Empty,
                ProductId = Convert.ToInt64(reader["product_id"]),
                DurationDays = Convert.ToInt32(reader["duration_days"]),
                DeviceLimit = Convert.ToInt32(reader["device_limit"]),
                Price = Convert.ToDecimal(reader["price"]),
                IsActive = Convert.ToBoolean(reader["is_active"])
            };
        }
    }
}
