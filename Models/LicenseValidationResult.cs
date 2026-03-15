namespace LicenseManager.API.Models
{
    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }

        public string? Error { get; set; }

        public string? KeyId { get; set; }

        public string? PayloadJson { get; set; }

        public SignedLicenseDocument? Document { get; set; }

        public LicensePayload? Payload { get; set; }
    }
}
