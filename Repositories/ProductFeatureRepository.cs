using LicenseManager.API.Data;
using LicenseManager.API.DTOs.ProductFeatures;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;

namespace LicenseManager.API.Repositories
{
    public class ProductFeatureRepository : IProductFeatureRepository
    {
        private readonly DbConnectionFactory _factory;

        public ProductFeatureRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyCollection<ProductFeatureRecord>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_product_features()", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var records = new List<ProductFeatureRecord>();
            while (await reader.ReadAsync())
            {
                records.Add(Map(reader));
            }

            return records;
        }

        public async Task<ProductFeatureRecord?> GetById(long id)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_product_feature_by_id(@p_id)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<ProductFeatureRecord?> Update(long id, UpdateProductFeatureRequestDto request, long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_update_product_feature(@p_id,@p_product_id,@p_feature_name,@p_description,@p_is_active,@p_updated_by)",
                conn);

            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_product_id", request.ProductId);
            cmd.Parameters.AddWithValue("@p_feature_name", request.FeatureName);
            cmd.Parameters.AddWithValue("@p_description", request.Description);
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
            using var cmd = new NpgsqlCommand("CALL sp_deactivate_product_feature(@p_id,@p_updated_by)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_updated_by", updatedBy);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static ProductFeatureRecord Map(NpgsqlDataReader reader)
        {
            return new ProductFeatureRecord
            {
                Id = Convert.ToInt64(reader["id"]),
                ProductId = Convert.ToInt64(reader["product_id"]),
                FeatureName = reader["feature_name"].ToString() ?? string.Empty,
                Description = reader["description"].ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["is_active"])
            };
        }
    }
}
