namespace LicenseManager.API.Models
{
    public class OfflineFinalLicenseDocument
    {
        public string Version { get; set; } = string.Empty;

        public string DocumentType { get; set; } = string.Empty;

        public DateTime GeneratedAtUtc { get; set; }

        public OfflineActivationLicenseInfo License { get; set; } = new();

        public OfflineActivationMachineInfo Machine { get; set; } = new();

        public string SourceLicense { get; set; } = string.Empty;

        public string? RequestPayload { get; set; }
    }
}
