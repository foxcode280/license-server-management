using LicenseManager.API.DTOs.OfflineActivations;
using LicenseManager.API.Helpers;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;
using System.Text.Json;

namespace LicenseManager.API.Services
{
    public class OfflineActivationService : IOfflineActivationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly IOfflineActivationRepository _repository;
        private readonly IDeviceOsTypeRepository _deviceOsTypeRepository;
        private readonly LicenseProtectionService _licenseProtectionService;

        public OfflineActivationService(
            IOfflineActivationRepository repository,
            IDeviceOsTypeRepository deviceOsTypeRepository,
            LicenseProtectionService licenseProtectionService)
        {
            _repository = repository;
            _deviceOsTypeRepository = deviceOsTypeRepository;
            _licenseProtectionService = licenseProtectionService;
        }

        public async Task<IReadOnlyCollection<OfflineActivationQueueResponseDto>> GetAll()
        {
            var rows = await _repository.GetAll();
            return rows.Select(Map).ToList();
        }

        public async Task<OfflineActivationQueueResponseDto?> GetByLicenseId(long licenseId)
        {
            var row = await _repository.GetByLicenseId(licenseId);
            return row == null ? null : Map(row);
        }

        public async Task<OfflineActivationQueueResponseDto> UploadRequest(long licenseId, UploadOfflineActivationRequestDto request, long userId)
        {
            ValidateUserId(userId);

            if (string.IsNullOrWhiteSpace(request.RequestFileName))
            {
                throw new InvalidOperationException("Request file name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.EncryptedRequestPayload))
            {
                throw new InvalidOperationException("Encrypted request payload is required.");
            }

            var row = await _repository.GetByLicenseId(licenseId)
                ?? throw new InvalidOperationException("Offline activation record not found.");

            var signedRequest = _licenseProtectionService.ValidateSignedEncryptedDocument<OfflineActivationRequestDocument>(
                request.EncryptedRequestPayload);

            if (!signedRequest.IsValid || signedRequest.Payload is null)
            {
                throw new InvalidOperationException(signedRequest.Error ?? "Unable to validate offline request payload.");
            }

            var payload = signedRequest.Payload;

            if (!string.Equals(payload.License.LicenseCode, row.LicenseCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Offline request license code does not match the selected license.");
            }

            var osTypeId = await ResolveOsTypeId(payload.Machine.OsType);
            var fingerprintHash = BuildFingerprintHash(payload.Machine.HardwareFingerprint);

            await _repository.SaveRequest(
                licenseId,
                request.RequestFileName.Trim(),
                request.EncryptedRequestPayload.Trim(),
                fingerprintHash,
                payload.Machine.HostName,
                payload.Machine.MachineId,
                payload.Machine.HostName,
                string.Empty,
                osTypeId,
                userId);

            return Map(await _repository.GetByLicenseId(licenseId)
                ?? throw new InvalidOperationException("Offline activation record not found after upload."));
        }

        public async Task<OfflineActivationQueueResponseDto> ValidateRequest(long licenseId, long userId)
        {
            ValidateUserId(userId);

            var row = await _repository.GetByLicenseId(licenseId)
                ?? throw new InvalidOperationException("Offline activation record not found.");

            if (row.RequestUploadedAt == null)
            {
                throw new InvalidOperationException("No .req payload has been uploaded for this offline license.");
            }

            await _repository.MarkValidated(licenseId, userId);

            return Map(await _repository.GetByLicenseId(licenseId)
                ?? throw new InvalidOperationException("Offline activation record not found after validation."));
        }

        public async Task<OfflineActivationQueueResponseDto> GenerateFinalLicense(long licenseId, long userId)
        {
            ValidateUserId(userId);

            var row = await _repository.GetByLicenseId(licenseId)
                ?? throw new InvalidOperationException("Offline activation record not found.");

            if (string.IsNullOrWhiteSpace(row.EncryptedRequestPayload))
            {
                throw new InvalidOperationException("No uploaded .req payload was found for this license.");
            }

            if (!string.Equals(row.ActivationStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(row.ActivationStatus, "ACTIVATED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Offline request must be validated before final license generation.");
            }

            var sourceLicenseKey = await _repository.GetSourceLicenseKey(licenseId);
            if (string.IsNullOrWhiteSpace(sourceLicenseKey))
            {
                throw new InvalidOperationException("Source license key was not found.");
            }

            var finalDocument = new OfflineFinalLicenseDocument
            {
                Version = "1.0",
                DocumentType = "OfflineActivatedLicense",
                GeneratedAtUtc = DateTime.UtcNow,
                License = new OfflineActivationLicenseInfo
                {
                    LicenseCode = row.LicenseCode,
                    LicenseId = row.LicenseCode,
                    ProductCode = row.ProductType,
                    PlanCode = row.PlanName
                },
                Machine = new OfflineActivationMachineInfo
                {
                    MachineId = row.MachineId ?? string.Empty,
                    HostName = row.HostName ?? string.Empty,
                    OsType = row.OsTypeId?.ToString() ?? string.Empty,
                    OsVersion = string.Empty,
                    MacAddress = string.Empty,
                    HardwareFingerprint = row.FingerprintHash ?? string.Empty
                },
                SourceLicense = sourceLicenseKey,
                RequestPayload = row.EncryptedRequestPayload
            };

            var finalLicensePayload = _licenseProtectionService.CreateSignedEncryptedDocument(finalDocument);
            var fileName = $"MXOFF-{row.LicenseCode}.lic";

            await _repository.SaveFinalLicense(licenseId, fileName, finalLicensePayload, userId);

            return Map(await _repository.GetByLicenseId(licenseId)
                ?? throw new InvalidOperationException("Offline activation record not found after final license generation."));
        }

        public async Task<(string FileName, string Content)> DownloadGenericLicense(long licenseId, long userId)
        {
            ValidateUserId(userId);

            var row = await _repository.GetByLicenseId(licenseId)
                ?? throw new InvalidOperationException("Offline activation record not found.");

            var sourceLicenseKey = await _repository.GetSourceLicenseKey(licenseId);
            if (string.IsNullOrWhiteSpace(sourceLicenseKey))
            {
                throw new InvalidOperationException("Generic offline license is not available for this record.");
            }

            var validation = _licenseProtectionService.ValidateEncryptedLicense(sourceLicenseKey);
            var licenseCode = string.IsNullOrWhiteSpace(validation.Payload?.LicenseCode)
                ? row.LicenseCode
                : validation.Payload.LicenseCode.Trim();

            if (string.IsNullOrWhiteSpace(licenseCode))
            {
                licenseCode = "UNKNOWN";
            }

            var fileName = $"MXGENOFF-{licenseCode}.lic";

            return (fileName, sourceLicenseKey);
        }

        public async Task<(string FileName, string Content)> DownloadFinalLicense(long licenseId, long userId)
        {
            ValidateUserId(userId);

            var row = await _repository.GetByLicenseId(licenseId)
                ?? throw new InvalidOperationException("Offline activation record not found.");

            if (string.IsNullOrWhiteSpace(row.FinalLicensePayload))
            {
                throw new InvalidOperationException("Final offline license has not been generated yet.");
            }

            return ($"MXOFF-{row.LicenseCode}.lic", row.FinalLicensePayload);
        }

        private async Task<long?> ResolveOsTypeId(string osType)
        {
            if (string.IsNullOrWhiteSpace(osType))
            {
                return null;
            }

            var osTypes = await _deviceOsTypeRepository.GetAll();
            var match = osTypes.FirstOrDefault(x => string.Equals(x.OsName, osType, StringComparison.OrdinalIgnoreCase));
            return match?.Id;
        }

        private static string BuildFingerprintHash(string hardwareFingerprint)
        {
            var source = string.IsNullOrWhiteSpace(hardwareFingerprint) ? Guid.NewGuid().ToString("N") : hardwareFingerprint.Trim();
            var compact = new string(source.Where(char.IsLetterOrDigit).ToArray());
            if (compact.Length > 12)
            {
                compact = compact[..12];
            }

            return $"FP-{compact.ToUpperInvariant()}";
        }

        private static string SanitizeFileSegment(string value)
        {
            var sanitized = new string(value.Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "offline-license" : sanitized;
        }

        private static void ValidateUserId(long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }
        }

        private static OfflineActivationQueueResponseDto Map(OfflineActivationQueueRecord row)
        {
            return new OfflineActivationQueueResponseDto
            {
                Id = row.LicenseId,
                CompanyId = row.CompanyId,
                CompanyName = row.CompanyName,
                SubscriptionId = row.SubscriptionId,
                SubscriptionStatus = row.SubscriptionStatus,
                PlanId = row.PlanId,
                PlanName = row.PlanName,
                LicenseCode = row.LicenseCode,
                ProductType = row.ProductType,
                WorkflowStatus = row.WorkflowStatus,
                ActivationStatus = row.ActivationStatus,
                GenericLicenseFileName = row.GenericLicenseFileName,
                RequestFileName = row.RequestFileName,
                FinalLicenseFileName = row.FinalLicenseFileName,
                EncryptedRequestPayload = row.EncryptedRequestPayload,
                FingerprintHash = row.FingerprintHash,
                MachineName = row.MachineName,
                MachineId = row.MachineId,
                HostName = row.HostName,
                IpAddress = row.IpAddress,
                OsTypeId = row.OsTypeId,
                GenericIssuedAt = row.GenericIssuedAt,
                RequestUploadedAt = row.RequestUploadedAt,
                FinalIssuedAt = row.FinalIssuedAt,
                ActivatedAt = row.ActivatedAt,
                Notes = row.Notes
            };
        }
    }
}
