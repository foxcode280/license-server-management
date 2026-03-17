using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Helpers;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;

namespace LicenseManager.API.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly ILicenseRepository _repository;
        private readonly LicenseProtectionService _licenseProtectionService;

        public LicenseService(
            ILicenseRepository repository,
            LicenseProtectionService licenseProtectionService)
        {
            _repository = repository;
            _licenseProtectionService = licenseProtectionService;
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

            await _repository.UpdateSubscriptionStatus(
                subscriptionId,
                "APPROVED",
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

            if (!string.IsNullOrWhiteSpace(context.ExistingLicenseKey))
            {
                return context.ExistingLicenseKey;
            }

            var payload = await _repository.GetLicensePayload(subscriptionId);
            string code = Guid.NewGuid()
                  .ToString("N")   // removes dashes
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

            await _repository.UpdateSubscriptionStatus(
                subscriptionId,
                "LICENSE_ISSUED",
                userId,
                DateTime.UtcNow);

            return encryptedLicense;
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
    }
}
