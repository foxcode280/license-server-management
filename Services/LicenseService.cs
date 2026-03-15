using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Repositories.Interfaces;
using LicenseManager.API.Services.Interfaces;

namespace LicenseManager.API.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly ILicenseRepository _repository;
        private readonly IConfiguration _configuration;

        public LicenseService(ILicenseRepository repository, IConfiguration configuration)
        {
            _repository = repository;
            _configuration = configuration;
        }

        public async Task<string> GenerateLicense(GenerateLicenseRequestDto request)
        {
            if (request.SubscriptionId <= 0)
            {
                throw new Exception("Invalid subscription");
            }

            return await _repository.GenerateLicense(
                request.SubscriptionId,
                request.CreatedBy
            );
        }

        public async Task<string> GenerateLicense(long subscriptionId)
        {
            var payload = await _repository.GetLicensePayload(subscriptionId);
            var privateKey = _configuration["LicenseSigning:PrivateKey"]
                ?? throw new InvalidOperationException("License signing private key is not configured.");
            var keyId = _configuration["LicenseSigning:KeyId"] ?? "v1";

            payload.LicenseId = Guid.NewGuid().ToString();
            payload.KeyId = keyId;

            var licenseText = LicenseGenerator.GenerateLicense(payload, keyId, privateKey);

            await _repository.SaveLicense(
                subscriptionId,
                payload.LicenseId,
                licenseText
            );

            return licenseText;
        }

        public async Task<ActivationResponseDto> ActivateLicense(ActivationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.LicenseKey))
            {
                throw new Exception("License key required");
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
