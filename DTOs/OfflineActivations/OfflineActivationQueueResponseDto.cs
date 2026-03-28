namespace LicenseManager.API.DTOs.OfflineActivations
{
    public class OfflineActivationQueueResponseDto
    {
        public long Id { get; set; }

        public long CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public long SubscriptionId { get; set; }

        public string SubscriptionStatus { get; set; } = string.Empty;

        public long PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string LicenseCode { get; set; } = string.Empty;

        public string ProductType { get; set; } = string.Empty;

        public string WorkflowStatus { get; set; } = string.Empty;

        public string ActivationStatus { get; set; } = string.Empty;

        public string GenericLicenseFileName { get; set; } = string.Empty;

        public string? RequestFileName { get; set; }

        public string? FinalLicenseFileName { get; set; }

        public string? EncryptedRequestPayload { get; set; }

        public string? FingerprintHash { get; set; }

        public string? MachineName { get; set; }

        public string? MachineId { get; set; }

        public string? HostName { get; set; }

        public string? IpAddress { get; set; }

        public long? OsTypeId { get; set; }

        public DateTime? GenericIssuedAt { get; set; }

        public DateTime? RequestUploadedAt { get; set; }

        public DateTime? FinalIssuedAt { get; set; }

        public DateTime? ActivatedAt { get; set; }

        public string? Notes { get; set; }
    }
}
