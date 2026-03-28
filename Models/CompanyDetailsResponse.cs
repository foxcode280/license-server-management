namespace LicenseManager.API.Models
{
    public class CompanyDetailsResponse
    {
        public long CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public bool HasSubscriptions { get; set; }
        public bool HasLicenses { get; set; }
        public List<CompanySubscriptionDetail> Subscriptions { get; set; } = new();
        public List<CompanyLicenseDetail> Licenses { get; set; } = new();
    }
}
