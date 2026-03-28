namespace LicenseManager.API.DTOs.Licenses
{
    public class GenerateOfflineRequestFromLicenseDto
    {
        public string LicenseDocument { get; set; } = string.Empty;

        public string? PublicKey { get; set; }

        public string? Version { get; set; }

        public string? RequestType { get; set; }

        public string? RequestId { get; set; }

        public DateTime? GeneratedAtUtc { get; set; }

        public OfflineActivationContextDto Context { get; set; } = new();
    }
}
