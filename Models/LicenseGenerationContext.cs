namespace LicenseManager.API.Models
{
    public class LicenseGenerationContext
    {
        public long SubscriptionId { get; set; }

        public string SubscriptionStatus { get; set; } = string.Empty;

        public string? ExistingLicenseKey { get; set; }
        public string LicenseCode { get; set; }
    }
}
