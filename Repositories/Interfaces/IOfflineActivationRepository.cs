using LicenseManager.API.Models;

namespace LicenseManager.API.Repositories.Interfaces
{
    public interface IOfflineActivationRepository
    {
        Task<IReadOnlyCollection<OfflineActivationQueueRecord>> GetAll();

        Task<OfflineActivationQueueRecord?> GetByLicenseId(long licenseId);

        Task SaveRequest(
            long licenseId,
            string requestFileName,
            string encryptedRequestPayload,
            string fingerprintHash,
            string machineName,
            string machineId,
            string hostName,
            string ipAddress,
            long? osTypeId,
            long updatedBy);

        Task MarkValidated(long licenseId, long updatedBy);

        Task SaveFinalLicense(
            long licenseId,
            string finalLicenseFileName,
            string finalLicensePayload,
            long updatedBy);

        Task<string?> GetSourceLicenseKey(long licenseId);
    }
}
