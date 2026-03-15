namespace LicenseManager.API.Models
{
    public class SignedLicenseDocument
    {
        public required string KeyId { get; set; }

        public required string Payload { get; set; }

        public required string Signature { get; set; }
    }
}
