namespace LicenseManager.API.DTOs.Licenses
{
    public class GenerateOfflineActivationRequestDto
    {
        public string? Version { get; set; }

        public string? RequestType { get; set; }

        public string? RequestId { get; set; }

        public DateTime? GeneratedAtUtc { get; set; }

        public OfflineActivationLicenseDto License { get; set; } = new();

        public OfflineActivationMachineDto Machine { get; set; } = new();

        public OfflineActivationContextDto Context { get; set; } = new();
    }

    public class OfflineActivationLicenseDto
    {
        public string LicenseCode { get; set; } = string.Empty;

        public string LicenseId { get; set; } = string.Empty;

        public string ProductCode { get; set; } = string.Empty;

        public string PlanCode { get; set; } = string.Empty;
    }

    public class OfflineActivationMachineDto
    {
        public string MachineId { get; set; } = string.Empty;

        public string HostName { get; set; } = string.Empty;

        public string OsType { get; set; } = string.Empty;

        public string OsVersion { get; set; } = string.Empty;

        public string MacAddress { get; set; } = string.Empty;

        public string HardwareFingerprint { get; set; } = string.Empty;
    }

    public class OfflineActivationContextDto
    {
        public string EmsServerId { get; set; } = string.Empty;

        public string EmsVersion { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;
    }
}
