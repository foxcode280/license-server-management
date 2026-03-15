using LicenseManager.API.DTOs.Licenses;

namespace LicenseManager.API.Services.Interfaces
{
    public interface ILicenseService
    {
        Task<string> GenerateLicense(long subscriptionId, long userId);

        Task<ActivationResponseDto> ActivateLicense(ActivationRequestDto request, long userId);
    }
}
