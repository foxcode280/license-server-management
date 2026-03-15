using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface ILicenseRepository
    {
        Task<string> GenerateLicense(long subscriptionId, long createdBy);

        Task<ActivationResponseDto> ActivateLicense(
            string licenseKey,
            string machineId,
            string hostname,
            string ipAddress
        );

        Task<LicensePayload> GetLicensePayload(long subscriptionId);
        Task SaveLicense(long subscriptionId, string licenseId, string licenseKey);
    }

}