namespace LicenseManager.API.Models
{
    public class CompanyLicenseDetail
    {
        public long Id { get; set; }
        public string LicenseCode { get; set; } = string.Empty;
        public long SubscriptionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
    }
}
