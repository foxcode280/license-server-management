using LicenseManager.API.DTOs.Licenses;

namespace LicenseManager.API.Services.Interfaces
{
    public interface ILicenseService
    {
        Task<string> GenerateLicense(GenerateLicenseRequestDto request);
        Task<string> GenerateLicense(long subscriptionId);
        Task<ActivationResponseDto> ActivateLicense(ActivationRequestDto request);
    }
}
