namespace LicenseManager.API.Models
{
    public class OfflineActivationRequestDocument
    {
        public string Version { get; set; } = string.Empty;

        public string RequestType { get; set; } = string.Empty;

        public string RequestId { get; set; } = string.Empty;

        public DateTime GeneratedAtUtc { get; set; }

        public OfflineActivationLicenseInfo License { get; set; } = new();

        public OfflineActivationMachineInfo Machine { get; set; } = new();

        public OfflineActivationContextInfo Context { get; set; } = new();

        public OfflineActivationSecurityInfo Security { get; set; } = new();
    }

    public class OfflineActivationLicenseInfo
    {
        public string LicenseCode { get; set; } = string.Empty;

        public string LicenseId { get; set; } = string.Empty;

        public string ProductCode { get; set; } = string.Empty;

        public string PlanCode { get; set; } = string.Empty;
    }

    public class OfflineActivationMachineInfo
    {
        public string MachineId { get; set; } = string.Empty;

        public string HostName { get; set; } = string.Empty;

        public string OsType { get; set; } = string.Empty;

        public string OsVersion { get; set; } = string.Empty;

        public string MacAddress { get; set; } = string.Empty;

        public string HardwareFingerprint { get; set; } = string.Empty;
    }

    public class OfflineActivationContextInfo
    {
        public string EmsServerId { get; set; } = string.Empty;

        public string EmsVersion { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;
    }

    public class OfflineActivationSecurityInfo
    {
        public string Nonce { get; set; } = string.Empty;

        public string PayloadHash { get; set; } = string.Empty;

        public string Signature { get; set; } = string.Empty;
    }
}
