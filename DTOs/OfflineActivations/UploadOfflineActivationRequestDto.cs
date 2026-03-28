namespace LicenseManager.API.DTOs.OfflineActivations
{
    public class UploadOfflineActivationRequestDto
    {
        public string RequestFileName { get; set; } = string.Empty;

        public string EncryptedRequestPayload { get; set; } = string.Empty;
    }
}
