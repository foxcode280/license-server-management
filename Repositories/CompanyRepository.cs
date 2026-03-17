using LicenseManager.API.Data;
using LicenseManager.API.DTOs.Companies;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;

namespace LicenseManager.API.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly DbConnectionFactory _factory;

        public CompanyRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyCollection<CompanyRecord>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_companies()", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var companies = new List<CompanyRecord>();
            while (await reader.ReadAsync())
            {
                companies.Add(Map(reader));
            }

            return companies;
        }

        public async Task<CompanyRecord?> GetById(long id)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_company_by_id(@p_id)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<CompanyRecord?> Update(long id, UpdateCompanyRequestDto request, long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_update_company(@p_id,@p_company_name,@p_email,@p_contact_number,@p_is_active,@p_updated_by)",
                conn);

            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_company_name", request.CompanyName);
            cmd.Parameters.AddWithValue("@p_email", request.Email);
            cmd.Parameters.AddWithValue("@p_contact_number", request.ContactNumber);
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
            using var cmd = new NpgsqlCommand("CALL sp_deactivate_company(@p_id,@p_updated_by)", conn);
            cmd.Parameters.AddWithValue("@p_id", id);
            cmd.Parameters.AddWithValue("@p_updated_by", updatedBy);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static CompanyRecord Map(NpgsqlDataReader reader)
        {
            return new CompanyRecord
            {
                Id = Convert.ToInt64(reader["id"]),
                CompanyName = reader["company_name"].ToString() ?? string.Empty,
                Email = reader["email"].ToString() ?? string.Empty,
                ContactNumber = reader["contact_number"].ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["is_active"]),
                CreatedAt = reader["created_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["created_at"]),
                UpdatedAt = reader["updated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["updated_at"])
            };
        }
    }
}
