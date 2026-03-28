using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Models;

namespace LicenseManager.API.Services.Interfaces
{
    public interface ILicenseService
    {
        Task ApproveSubscription(long subscriptionId, long userId);

        Task RejectSubscription(long subscriptionId, long userId);

        Task<string> GenerateLicense(long subscriptionId, long userId);

        Task<LicenseDownloadResponse> DownloadLicense(long subscriptionId, long userId);

        Task<ActivationResponseDto> ActivateLicense(ActivationRequestDto request, long userId);

        Task<OfflineActivationRequestResult> GenerateOfflineActivationRequest(GenerateOfflineActivationRequestDto request, long userId);

        Task<LicenseValidationResult> InspectOfflineGenericLicense(string licenseDocument, string? publicKey, long userId);

        Task<OfflineActivationRequestResult> GenerateOfflineActivationRequestFromLicense(GenerateOfflineRequestFromLicenseDto request, long userId);

        Task<ValidateOfflineFinalLicenseResponseDto> ValidateOfflineFinalLicense(string licenseDocument, string? publicKey, long userId);
    }
}

