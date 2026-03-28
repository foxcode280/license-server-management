using LicenseManager.API.Data;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace LicenseManager.API.Repositories
{
    public class OfflineActivationRepository : IOfflineActivationRepository
    {
        private readonly DbConnectionFactory _factory;

        public OfflineActivationRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyCollection<OfflineActivationQueueRecord>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_offline_activation_queue()", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var records = new List<OfflineActivationQueueRecord>();
            while (await reader.ReadAsync())
            {
                records.Add(Map(reader));
            }

            return records;
        }

        public async Task<OfflineActivationQueueRecord?> GetByLicenseId(long licenseId)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM sp_get_offline_activation_by_license_id(@p_license_id)", conn);
            cmd.Parameters.AddWithValue("@p_license_id", licenseId);
            using var reader = await cmd.ExecuteReaderAsync();

            return await reader.ReadAsync() ? Map(reader) : null;
        }

        public async Task SaveRequest(
            long licenseId,
            string requestFileName,
            string encryptedRequestPayload,
            string fingerprintHash,
            string machineName,
            string machineId,
            string hostName,
            string ipAddress,
            long? osTypeId,
            long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "CALL sp_upload_offline_activation_request(@p_license_id,@p_request_file_name,@p_encrypted_request_payload,@p_fingerprint_hash,@p_machine_name,@p_machine_id,@p_host_name,@p_ip_address,@p_os_type_id,@p_updated_by,@p_updated_at)",
                conn);

            cmd.Parameters.Add("@p_license_id", NpgsqlDbType.Bigint).Value = licenseId;
            cmd.Parameters.Add("@p_request_file_name", NpgsqlDbType.Varchar).Value = requestFileName;
            cmd.Parameters.Add("@p_encrypted_request_payload", NpgsqlDbType.Text).Value = encryptedRequestPayload;
            cmd.Parameters.Add("@p_fingerprint_hash", NpgsqlDbType.Varchar).Value = fingerprintHash;
            cmd.Parameters.Add("@p_machine_name", NpgsqlDbType.Varchar).Value = machineName;
            cmd.Parameters.Add("@p_machine_id", NpgsqlDbType.Varchar).Value = machineId;
            cmd.Parameters.Add("@p_host_name", NpgsqlDbType.Varchar).Value = hostName;
            cmd.Parameters.Add("@p_ip_address", NpgsqlDbType.Varchar).Value = string.IsNullOrWhiteSpace(ipAddress)
                ? DBNull.Value
                : ipAddress.Trim();
            cmd.Parameters.Add("@p_os_type_id", NpgsqlDbType.Bigint).Value = osTypeId.HasValue ? osTypeId.Value : DBNull.Value;
            cmd.Parameters.Add("@p_updated_by", NpgsqlDbType.Bigint).Value = updatedBy;
            cmd.Parameters.Add("@p_updated_at", NpgsqlDbType.Timestamp).Value =
                DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MarkValidated(long licenseId, long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "CALL sp_validate_offline_activation_request(@p_license_id,@p_updated_by,@p_updated_at)",
                conn);

            cmd.Parameters.AddWithValue("@p_license_id", licenseId);
            cmd.Parameters.AddWithValue("@p_updated_by", updatedBy);
            cmd.Parameters.AddWithValue("@p_updated_at", DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveFinalLicense(
            long licenseId,
            string finalLicenseFileName,
            string finalLicensePayload,
            long updatedBy)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "CALL sp_complete_offline_activation(@p_license_id,@p_final_license_file_name,@p_final_license_payload,@p_updated_by,@p_updated_at)",
                conn);

            cmd.Parameters.AddWithValue("@p_license_id", licenseId);
            cmd.Parameters.AddWithValue("@p_final_license_file_name", finalLicenseFileName);
            cmd.Parameters.AddWithValue("@p_final_license_payload", finalLicensePayload);
            cmd.Parameters.AddWithValue("@p_updated_by", updatedBy);
            cmd.Parameters.AddWithValue("@p_updated_at", DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string?> GetSourceLicenseKey(long licenseId)
        {
            using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT license_key FROM sp_get_offline_activation_source(@p_license_id)", conn);
            cmd.Parameters.AddWithValue("@p_license_id", licenseId);

            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : result.ToString();
        }

        private static OfflineActivationQueueRecord Map(NpgsqlDataReader reader)
        {
            return new OfflineActivationQueueRecord
            {
                LicenseId = Convert.ToInt64(reader["license_id"]),
                CompanyId = Convert.ToInt64(reader["company_id"]),
                CompanyName = reader["company_name"].ToString() ?? string.Empty,
                SubscriptionId = Convert.ToInt64(reader["subscription_id"]),
                SubscriptionStatus = reader["subscription_status"].ToString() ?? string.Empty,
                PlanId = Convert.ToInt64(reader["plan_id"]),
                PlanName = reader["plan_name"].ToString() ?? string.Empty,
                LicenseCode = reader["license_code"].ToString() ?? string.Empty,
                ProductType = reader["product_type"].ToString() ?? string.Empty,
                WorkflowStatus = reader["workflow_status"].ToString() ?? string.Empty,
                ActivationStatus = reader["activation_status"].ToString() ?? string.Empty,
                GenericLicenseFileName = reader["generic_license_file_name"].ToString() ?? string.Empty,
                RequestFileName = reader["request_file_name"] == DBNull.Value ? null : reader["request_file_name"].ToString(),
                FinalLicenseFileName = reader["final_license_file_name"] == DBNull.Value ? null : reader["final_license_file_name"].ToString(),
                EncryptedRequestPayload = reader["encrypted_request_payload"] == DBNull.Value ? null : reader["encrypted_request_payload"].ToString(),
                FinalLicensePayload = reader["final_license_payload"] == DBNull.Value ? null : reader["final_license_payload"].ToString(),
                FingerprintHash = reader["fingerprint_hash"] == DBNull.Value ? null : reader["fingerprint_hash"].ToString(),
                MachineName = reader["machine_name"] == DBNull.Value ? null : reader["machine_name"].ToString(),
                MachineId = reader["machine_id"] == DBNull.Value ? null : reader["machine_id"].ToString(),
                HostName = reader["host_name"] == DBNull.Value ? null : reader["host_name"].ToString(),
                IpAddress = reader["ip_address"] == DBNull.Value ? null : reader["ip_address"].ToString(),
                OsTypeId = reader["os_type_id"] == DBNull.Value ? null : Convert.ToInt64(reader["os_type_id"]),
                GenericIssuedAt = reader["generic_issued_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["generic_issued_at"]),
                RequestUploadedAt = reader["request_uploaded_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["request_uploaded_at"]),
                FinalIssuedAt = reader["final_issued_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["final_issued_at"]),
                ActivatedAt = reader["activated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["activated_at"]),
                Notes = reader["notes"] == DBNull.Value ? null : reader["notes"].ToString()
            };
        }
    }
}
