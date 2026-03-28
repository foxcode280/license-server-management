namespace LicenseManager.API.Models
{
    public class OfflineActivationRequestResult
    {
        public string RequestId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string EncryptedRequest { get; set; } = string.Empty;

        public OfflineActivationRequestDocument Payload { get; set; } = new();
    }
}
