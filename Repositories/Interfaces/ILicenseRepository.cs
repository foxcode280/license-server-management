using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface ILicenseRepository
    {
        Task<ActivationResponseDto> ActivateLicense(
            string licenseKey,
            string machineId,
            string hostname,
            string ipAddress);

        Task<LicenseGenerationContext> GetLicenseGenerationContext(long subscriptionId);

        Task<LicensePayload> GetLicensePayload(long subscriptionId);

        Task SaveLicense(
            long subscriptionId,
            string licenseKey,
            string licenseDurationType,
            string licenseMode);
    }
}
