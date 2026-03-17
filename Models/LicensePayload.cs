namespace LicenseManager.API.Models
{
    public class LicensePayload
    {
        public string LicenseId { get; set; } = string.Empty;

        public string KeyId { get; set; } = string.Empty;

        public long CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public long SubscriptionId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string LicenseDurationType { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public string LicenseMode { get; set; } = string.Empty;

        public List<LicenseAllocation> LicenseAllocations { get; set; } = new();

        public List<string> Features { get; set; } = new();
        public string LicenseCode { get; set; }
    }
}
