using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Helpers;
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

        public async Task<string> GenerateLicense(long subscriptionId, long userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Logged in user id is required.");
            }

            var context = await _repository.GetLicenseGenerationContext(subscriptionId);

            if (!string.Equals(context.SubscriptionStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "License can only be generated or downloaded while the subscription is in PENDING status.");
            }

            if (!string.IsNullOrWhiteSpace(context.ExistingLicenseKey))
            {
                return context.ExistingLicenseKey;
            }

            var payload = await _repository.GetLicensePayload(subscriptionId);
            payload.LicenseId = Guid.NewGuid().ToString();

            var encryptedLicense = _licenseProtectionService.CreateEncryptedLicenseDocument(payload);

            await _repository.SaveLicense(
                subscriptionId,
                encryptedLicense,
                payload.LicenseDurationType,
                payload.LicenseMode
            );

            return encryptedLicense;
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
