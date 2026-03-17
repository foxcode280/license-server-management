using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface ILicenseService
    {
        Task ApproveSubscription(long subscriptionId, long userId);

        Task<string> GenerateLicense(long subscriptionId, long userId);

        Task<LicenseDownloadResponse> DownloadLicense(long subscriptionId, long userId);

        Task<ActivationResponseDto> ActivateLicense(ActivationRequestDto request, long userId);
    }
}
