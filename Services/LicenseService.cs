using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Helpers;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LicenseManager.API.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly ILicenseRepository _repository;
        private readonly LicenseProtectionService _licenseProtectionService;
        private readonly SystemMachineInfoService _systemMachineInfoService;

        public LicenseService(
            ILicenseRepository repository,
            LicenseProtectionService licenseProtectionService,
            SystemMachineInfoService systemMachineInfoService)
        {
            _repository = repository;
            _licenseProtectionService = licenseProtectionService;
            _systemMachineInfoService = systemMachineInfoService;
        }

        public async Task ApproveSubscription(long subscriptionId, long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            var context = await _repository.GetLicenseGenerationContext(subscriptionId);

            if (!string.Equals(context.SubscriptionStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only pending subscriptions can be approved.");
            }

            ValidateExistingLicenseStatus(context);

            var payload = await _repository.GetLicensePayload(subscriptionId);
            ValidateSubscriptionWindow(payload);

            await _repository.UpdateSubscriptionStatus(
                subscriptionId,
                "APPROVED",
                userId,
                DateTime.UtcNow);

            try
            {
                await GenerateLicenseInternal(subscriptionId, context, payload);
            }
            catch
            {
                await _repository.UpdateSubscriptionStatus(
                    subscriptionId,
                    "PENDING",
                    userId,
                    DateTime.UtcNow);

                throw;
            }
        }

        public async Task RejectSubscription(long subscriptionId, long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            var context = await _repository.GetLicenseGenerationContext(subscriptionId);

            if (!string.Equals(context.SubscriptionStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only pending subscriptions can be rejected.");
            }

            await _repository.UpdateSubscriptionStatus(
                subscriptionId,
                "REJECTED",
                userId,
                DateTime.UtcNow);
        }

        public async Task<string> GenerateLicense(long subscriptionId, long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            var context = await _repository.GetLicenseGenerationContext(subscriptionId);

            if (!string.Equals(context.SubscriptionStatus, "APPROVED", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.SubscriptionStatus, "LICENSE_ISSUED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "License can only be generated after subscription approval.");
            }

            var payload = await _repository.GetLicensePayload(subscriptionId);
            ValidateSubscriptionWindow(payload);

            return await GenerateLicenseInternal(subscriptionId, context, payload);
        }

        public async Task<LicenseDownloadResponse> DownloadLicense(long subscriptionId, long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            var context = await _repository.GetLicenseGenerationContext(subscriptionId);

            if (string.IsNullOrWhiteSpace(context.ExistingLicenseKey))
            {
                throw new InvalidOperationException("License has not been generated yet for this subscription.");
            }

            var licenseCode = context.LicenseCode;

            if (string.IsNullOrWhiteSpace(licenseCode))
            {
                var validation = _licenseProtectionService.ValidateEncryptedLicense(context.ExistingLicenseKey);
                if (validation.IsValid && !string.IsNullOrWhiteSpace(validation.Payload?.LicenseCode))
                {
                    licenseCode = validation.Payload.LicenseCode;
                }
            }

            if (string.IsNullOrWhiteSpace(licenseCode))
            {
                licenseCode = subscriptionId.ToString();
            }

            return new LicenseDownloadResponse
            {
                LicenseKey = context.ExistingLicenseKey,
                LicenseCode = licenseCode
            };
        }

        public async Task<ActivationResponseDto> ActivateLicense(ActivationRequestDto request, long userId)
        {
            if (userId <= 0)
            {
                return new ActivationResponseDto
                {
                    StatusCode = "UNAUTHORIZED",
                    StatusMessage = "Logged in user id is required."
                };
            }

            if (string.IsNullOrWhiteSpace(request.LicenseKey))
            {
                return new ActivationResponseDto
                {
                    StatusCode = "INVALID_LICENSE",
                    StatusMessage = "License key is required."
                };
            }

            var validation = _licenseProtectionService.ValidateEncryptedLicense(request.LicenseKey);
            if (!validation.IsValid)
            {
                return new ActivationResponseDto
                {
                    StatusCode = "INVALID_LICENSE",
                    StatusMessage = validation.Error ?? "License validation failed."
                };
            }

            return await _repository.ActivateLicense(
                request.LicenseKey,
                request.MachineId,
                request.Hostname,
                request.IpAddress
            );
        }

        public Task<OfflineActivationRequestResult> GenerateOfflineActivationRequest(
            GenerateOfflineActivationRequestDto request,
            long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            ValidateOfflineActivationRequest(request);

            var version = string.IsNullOrWhiteSpace(request.Version) ? "1.0" : request.Version.Trim();
            var requestType = string.IsNullOrWhiteSpace(request.RequestType)
                ? "OfflineActivationRequest"
                : request.RequestType.Trim();
            var generatedAtUtc = request.GeneratedAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
            var requestId = string.IsNullOrWhiteSpace(request.RequestId)
                ? BuildRequestId(generatedAtUtc)
                : request.RequestId.Trim();
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            var machineInfo = _systemMachineInfoService.GetCurrentMachineInfo();

            var payloadHashSource = new
            {
                version,
                requestType,
                requestId,
                generatedAtUtc,
                license = request.License,
                machine = machineInfo,
                context = request.Context,
                security = new
                {
                    nonce
                }
            };

            var payloadHash = $"sha256:{ComputeSha256(JsonSerializer.Serialize(payloadHashSource))}";

            var signatureSource = new
            {
                version,
                requestType,
                requestId,
                generatedAtUtc,
                license = request.License,
                machine = machineInfo,
                context = request.Context,
                security = new
                {
                    nonce,
                    payloadHash
                }
            };

            var signature = _licenseProtectionService.SignDocument(JsonSerializer.Serialize(signatureSource));

            var document = new OfflineActivationRequestDocument
            {
                Version = version,
                RequestType = requestType,
                RequestId = requestId,
                GeneratedAtUtc = generatedAtUtc,
                License = new OfflineActivationLicenseInfo
                {
                    LicenseCode = request.License.LicenseCode.Trim(),
                    LicenseId = request.License.LicenseId.Trim(),
                    ProductCode = request.License.ProductCode.Trim(),
                    PlanCode = request.License.PlanCode.Trim()
                },
                Machine = new OfflineActivationMachineInfo
                {
                    MachineId = machineInfo.MachineId,
                    HostName = machineInfo.HostName,
                    OsType = machineInfo.OsType,
                    OsVersion = machineInfo.OsVersion,
                    MacAddress = machineInfo.MacAddress,
                    HardwareFingerprint = machineInfo.HardwareFingerprint
                },
                Context = new OfflineActivationContextInfo
                {
                    EmsServerId = request.Context.EmsServerId.Trim(),
                    EmsVersion = request.Context.EmsVersion.Trim(),
                    DeviceType = request.Context.DeviceType.Trim()
                },
                Security = new OfflineActivationSecurityInfo
                {
                    Nonce = nonce,
                    PayloadHash = payloadHash,
                    Signature = signature
                }
            };

            var encryptedRequest = _licenseProtectionService.CreateSignedEncryptedDocument(document);

            return Task.FromResult(new OfflineActivationRequestResult
            {
                RequestId = document.RequestId,
                FileName = $"MXREQOFF-{document.License.LicenseCode}.req",
                EncryptedRequest = encryptedRequest,
                Payload = document
            });
        }

        public Task<LicenseValidationResult> InspectOfflineGenericLicense(string licenseDocument, string? publicKey, long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            if (string.IsNullOrWhiteSpace(licenseDocument))
            {
                throw new InvalidOperationException("Generic license document is required.");
            }

            return Task.FromResult(_licenseProtectionService.ValidateEncryptedLicense(licenseDocument, publicKey));
        }

        public async Task<OfflineActivationRequestResult> GenerateOfflineActivationRequestFromLicense(
            GenerateOfflineRequestFromLicenseDto request,
            long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            if (string.IsNullOrWhiteSpace(request.LicenseDocument))
            {
                throw new InvalidOperationException("Generic license document is required.");
            }

            var licenseValidation = _licenseProtectionService.ValidateEncryptedLicense(
                request.LicenseDocument,
                request.PublicKey);

            if (!licenseValidation.IsValid || licenseValidation.Payload is null)
            {
                throw new InvalidOperationException(licenseValidation.Error ?? "Generic license validation failed.");
            }

            return await GenerateOfflineActivationRequest(new GenerateOfflineActivationRequestDto
            {
                Version = request.Version,
                RequestType = request.RequestType,
                RequestId = request.RequestId,
                GeneratedAtUtc = request.GeneratedAtUtc,
                License = new OfflineActivationLicenseDto
                {
                    LicenseCode = licenseValidation.Payload.LicenseCode ?? string.Empty,
                    LicenseId = licenseValidation.Payload.LicenseId ?? string.Empty,
                    ProductCode = "EMS",
                    PlanCode = ResolvePlanCode(licenseValidation.Payload.PlanName)
                },
                Context = request.Context
            }, userId);
        }

        public Task<ValidateOfflineFinalLicenseResponseDto> ValidateOfflineFinalLicense(
            string licenseDocument,
            string? publicKey,
            long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            if (string.IsNullOrWhiteSpace(licenseDocument))
            {
                throw new InvalidOperationException("Final license document is required.");
            }

            var result = _licenseProtectionService.ValidateSignedEncryptedDocument<OfflineFinalLicenseDocument>(
                licenseDocument,
                publicKey);

            var currentMachine = _systemMachineInfoService.GetCurrentMachineInfo();
            var matchesCurrentMachine = result.IsValid &&
                result.Payload is not null &&
                string.Equals(
                    result.Payload.Machine.HardwareFingerprint,
                    currentMachine.HardwareFingerprint,
                    StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(new ValidateOfflineFinalLicenseResponseDto
            {
                IsValid = result.IsValid && matchesCurrentMachine,
                Reason = result.IsValid
                    ? (matchesCurrentMachine ? null : "Final license is not bound to this EMS machine.")
                    : result.Error,
                KeyId = result.KeyId,
                MatchesCurrentMachine = matchesCurrentMachine,
                Payload = result.Payload
            });
        }

        private static void ValidateOfflineActivationRequest(GenerateOfflineActivationRequestDto request)
        {
            if (request.License == null)
            {
                throw new InvalidOperationException("License details are required.");
            }

            if (request.Context == null)
            {
                throw new InvalidOperationException("Context details are required.");
            }

            if (string.IsNullOrWhiteSpace(request.License.LicenseCode) ||
                string.IsNullOrWhiteSpace(request.License.LicenseId) ||
                string.IsNullOrWhiteSpace(request.License.ProductCode) ||
                string.IsNullOrWhiteSpace(request.License.PlanCode))
            {
                throw new InvalidOperationException("LicenseCode, LicenseId, ProductCode and PlanCode are required.");
            }

            if (string.IsNullOrWhiteSpace(request.Context.EmsServerId) ||
                string.IsNullOrWhiteSpace(request.Context.EmsVersion) ||
                string.IsNullOrWhiteSpace(request.Context.DeviceType))
            {
                throw new InvalidOperationException("EmsServerId, EmsVersion and DeviceType are required.");
            }
        }

        private static string BuildRequestId(DateTime generatedAtUtc)
        {
            var sequence = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return $"REQ-{generatedAtUtc:yyyyMMdd}-{sequence:000000}";
        }

        private static string ComputeSha256(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string ResolvePlanCode(string planName)
        {
            if (string.IsNullOrWhiteSpace(planName))
            {
                return "STANDARD";
            }

            var compact = new string(planName
                .Trim()
                .ToUpperInvariant()
                .Where(ch => char.IsLetterOrDigit(ch) || ch == ' ')
                .ToArray())
                .Replace(' ', '_');

            return string.IsNullOrWhiteSpace(compact) ? "STANDARD" : compact;
        }

        private async Task<string> GenerateLicenseInternal(
            long subscriptionId,
            LicenseGenerationContext context,
            LicensePayload payload)
        {
            ValidateExistingLicenseStatus(context);

            if (!string.IsNullOrWhiteSpace(context.ExistingLicenseKey))
            {
                return context.ExistingLicenseKey;
            }

            string code = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 12)
                .ToUpper();

            payload.LicenseId = code;
            payload.LicenseCode = code;

            var encryptedLicense = _licenseProtectionService.CreateEncryptedLicenseDocument(payload);

            await _repository.SaveLicense(
                subscriptionId,
                encryptedLicense,
                payload.LicenseDurationType,
                payload.LicenseMode,
                payload.LicenseCode
            );

            return encryptedLicense;
        }

        private static void ValidateSubscriptionWindow(LicensePayload payload)
        {
            if (payload.StartDate == default)
            {
                throw new InvalidOperationException("Subscription start date is missing.");
            }

            if (payload.ExpiryDate == default)
            {
                throw new InvalidOperationException("Subscription expiry date is missing.");
            }

            if (payload.ExpiryDate <= payload.StartDate)
            {
                throw new InvalidOperationException("Subscription expiry date must be greater than the start date.");
            }

            if (payload.ExpiryDate <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Subscription is already expired and cannot be approved.");
            }
        }

        private static void ValidateExistingLicenseStatus(LicenseGenerationContext context)
        {
            if (string.IsNullOrWhiteSpace(context.ExistingLicenseKey) || string.IsNullOrWhiteSpace(context.LicenseStatus))
            {
                return;
            }

            if (string.Equals(context.LicenseStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(context.LicenseStatus, "ISSUED", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Existing license is in '{context.LicenseStatus}' status and cannot be reused.");
        }
    }
}
