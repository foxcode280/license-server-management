using LicenseManager.API.DTOs.OfflineActivations;

namespace LicenseManager.API.Services.Interfaces
{
    public interface IOfflineActivationService
    {
        Task<IReadOnlyCollection<OfflineActivationQueueResponseDto>> GetAll();

        Task<OfflineActivationQueueResponseDto?> GetByLicenseId(long licenseId);

        Task<OfflineActivationQueueResponseDto> UploadRequest(long licenseId, UploadOfflineActivationRequestDto request, long userId);

        Task<OfflineActivationQueueResponseDto> ValidateRequest(long licenseId, long userId);

        Task<OfflineActivationQueueResponseDto> GenerateFinalLicense(long licenseId, long userId);

        Task<(string FileName, string Content)> DownloadGenericLicense(long licenseId, long userId);

        Task<(string FileName, string Content)> DownloadFinalLicense(long licenseId, long userId);
    }
}
