using LicenseManager.API.Data;
using LicenseManager.API.DTOs.Companies;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;

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

        public async Task<CompanyRecord?> Create(CreateCompanyRequestDto request, long createdBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_insert_company(@p_name,@p_industry,@p_contact_person,@p_primary_mobile,@p_alternate_mobile,@p_email,@p_status,@p_status_description,@p_created_by)",
                conn);

            cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = request.Name;
            cmd.Parameters.Add("@p_industry", NpgsqlDbType.Varchar).Value = request.Industry;
            cmd.Parameters.Add("@p_contact_person", NpgsqlDbType.Varchar).Value = request.ContactPerson;
            cmd.Parameters.Add("@p_primary_mobile", NpgsqlDbType.Varchar).Value = request.PrimaryMobile;
            cmd.Parameters.Add("@p_alternate_mobile", NpgsqlDbType.Varchar).Value =
                string.IsNullOrWhiteSpace(request.AlternateMobile) ? DBNull.Value : request.AlternateMobile.Trim();
            cmd.Parameters.Add("@p_email", NpgsqlDbType.Varchar).Value = request.Email;
            cmd.Parameters.Add("@p_status", NpgsqlDbType.Varchar).Value = request.Status;
            cmd.Parameters.Add("@p_status_description", NpgsqlDbType.Text).Value =
                string.IsNullOrWhiteSpace(request.StatusDescription) ? DBNull.Value : request.StatusDescription.Trim();
            cmd.Parameters.Add("@p_created_by", NpgsqlDbType.Bigint).Value = createdBy;

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<CompanyRecord?> Update(long id, UpdateCompanyRequestDto request, long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_update_company(@p_id,@p_name,@p_industry,@p_contact_person,@p_primary_mobile,@p_alternate_mobile,@p_email,@p_status,@p_status_description,@p_updated_by)",
                conn);

            cmd.Parameters.Add("@p_id", NpgsqlDbType.Bigint).Value = id;
            cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = request.Name;
            cmd.Parameters.Add("@p_industry", NpgsqlDbType.Varchar).Value = request.Industry;
            cmd.Parameters.Add("@p_contact_person", NpgsqlDbType.Varchar).Value = request.ContactPerson;
            cmd.Parameters.Add("@p_primary_mobile", NpgsqlDbType.Varchar).Value = request.PrimaryMobile;
            cmd.Parameters.Add("@p_alternate_mobile", NpgsqlDbType.Varchar).Value =
                string.IsNullOrWhiteSpace(request.AlternateMobile) ? DBNull.Value : request.AlternateMobile.Trim();
            cmd.Parameters.Add("@p_email", NpgsqlDbType.Varchar).Value = request.Email;
            cmd.Parameters.Add("@p_status", NpgsqlDbType.Varchar).Value = request.Status;
            cmd.Parameters.Add("@p_status_description", NpgsqlDbType.Text).Value =
                string.IsNullOrWhiteSpace(request.StatusDescription) ? DBNull.Value : request.StatusDescription.Trim();
            cmd.Parameters.Add("@p_updated_by", NpgsqlDbType.Bigint).Value = updatedBy;

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task<CompanyDetailsResponse?> GetDetails(long id)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var company = await GetById(id);
            if (company == null)
            {
                return null;
            }

            var response = new CompanyDetailsResponse
            {
                CompanyId = company.Id,
                CompanyName = company.Name,
                HasSubscriptions = company.LinkedSubscriptions.Count > 0,
                HasLicenses = company.LinkedLicenses.Count > 0
            };

            using (var subCmd = new NpgsqlCommand("SELECT * FROM sp_get_company_subscription_details(@p_company_id)", conn))
            {
                subCmd.Parameters.AddWithValue("@p_company_id", id);
                using var reader = await subCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    response.Subscriptions.Add(new CompanySubscriptionDetail
                    {
                        Id = Convert.ToInt64(reader["id"]),
                        PlanName = reader["plan_name"].ToString() ?? string.Empty,
                        Status = reader["status"].ToString() ?? string.Empty,
                        StartDate = reader["start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["start_date"]),
                        EndDate = reader["end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["end_date"]),
                        RequestedAt = reader["requested_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["requested_at"]),
                        ApprovedAt = reader["approved_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["approved_at"])
                    });
                }
            }

            using (var licCmd = new NpgsqlCommand("SELECT * FROM sp_get_company_license_details(@p_company_id)", conn))
            {
                licCmd.Parameters.AddWithValue("@p_company_id", id);
                using var reader = await licCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    response.Licenses.Add(new CompanyLicenseDetail
                    {
                        Id = Convert.ToInt64(reader["id"]),
                        LicenseCode = reader["license_code"].ToString() ?? string.Empty,
                        SubscriptionId = Convert.ToInt64(reader["subscription_id"]),
                        Status = reader["status"].ToString() ?? string.Empty,
                        ExpiryDate = reader["expiry_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["expiry_date"])
                    });
                }
            }

            response.HasSubscriptions = response.Subscriptions.Count > 0;
            response.HasLicenses = response.Licenses.Count > 0;
            return response;
        }

        public async Task<bool> Ban(long id, long updatedBy, string? reason)
        {
            if (await GetById(id) == null)
            {
                return false;
            }

            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("CALL sp_ban_company(@p_id,@p_reason,@p_updated_by)", conn);
            cmd.Parameters.Add("@p_id", NpgsqlDbType.Bigint).Value = id;
            cmd.Parameters.Add("@p_reason", NpgsqlDbType.Text).Value =
                string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason.Trim();
            cmd.Parameters.Add("@p_updated_by", NpgsqlDbType.Bigint).Value = updatedBy;
            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static CompanyRecord Map(NpgsqlDataReader reader)
        {
            return new CompanyRecord
            {
                Id = Convert.ToInt64(reader["id"]),
                Name = reader["name"].ToString() ?? string.Empty,
                Email = reader["email"].ToString() ?? string.Empty,
                Industry = reader["industry"] == DBNull.Value ? string.Empty : reader["industry"].ToString() ?? string.Empty,
                ContactPerson = reader["contact_person"] == DBNull.Value ? string.Empty : reader["contact_person"].ToString() ?? string.Empty,
                PrimaryMobile = reader["primary_mobile"] == DBNull.Value ? string.Empty : reader["primary_mobile"].ToString() ?? string.Empty,
                AlternateMobile = reader["alternate_mobile"] == DBNull.Value ? null : reader["alternate_mobile"].ToString(),
                Status = reader["status"].ToString() ?? string.Empty,
                StatusDescription = reader["status_description"] == DBNull.Value ? null : reader["status_description"].ToString(),
                LinkedSubscriptions = ParseStringList(reader["linked_subscriptions"]),
                LinkedLicenses = ParseStringList(reader["linked_licenses"]),
                IsDisabled = string.Equals(reader["status"].ToString(), "Disabled", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(reader["status"].ToString(), "Suspended", StringComparison.OrdinalIgnoreCase),
                CreatedAt = reader["created_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["created_at"]),
                UpdatedAt = reader["updated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["updated_at"])
            };
        }

        private static List<string> ParseStringList(object value)
        {
            if (value == DBNull.Value || value == null)
            {
                return new List<string>();
            }

            if (value is string[] stringArray)
            {
                return stringArray.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }

            if (value is Array array)
            {
                return array.Cast<object>().Select(x => x?.ToString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }

            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            if (text.TrimStart().StartsWith("["))
            {
                return JsonSerializer.Deserialize<List<string>>(text) ?? new List<string>();
            }

            return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
    }
}
