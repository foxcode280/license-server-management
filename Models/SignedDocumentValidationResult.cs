namespace LicenseManager.API.Models
{
    public class SignedDocumentValidationResult<T>
    {
        public bool IsValid { get; set; }

        public string? Error { get; set; }

        public string? KeyId { get; set; }

        public string? PayloadJson { get; set; }

        public SignedLicenseDocument? Document { get; set; }

        public T? Payload { get; set; }
    }
}
