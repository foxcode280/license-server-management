using LicenseManager.API.Data;
using LicenseManager.API.DTOs.Products;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;

namespace LicenseManager.API.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly DbConnectionFactory _factory;

        public ProductRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyCollection<ProductRecord>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_products()", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var products = new List<ProductRecord>();
            while (await reader.ReadAsync())
            {
                products.Add(Map(reader));
            }

            return products;
        }

        public async Task<ProductRecord?> GetById(long id)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_product_by_id(@p_id)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<ProductRecord?> Update(long id, UpdateProductRequestDto request, long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_update_product(@p_id,@p_product_name,@p_product_code,@p_is_active,@p_updated_by)",
                conn);

            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_product_name", request.ProductName);
            cmd.Parameters.AddWithValue("@p_product_code", request.ProductCode);
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
            using var cmd = new NpgsqlCommand("CALL sp_deactivate_product(@p_id,@p_updated_by)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_updated_by", updatedBy);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static ProductRecord Map(NpgsqlDataReader reader)
        {
            return new ProductRecord
            {
                Id = Convert.ToInt64(reader["id"]),
                ProductName = reader["product_name"].ToString() ?? string.Empty,
                ProductCode = reader["product_code"].ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["is_active"])
            };
        }
    }
}
