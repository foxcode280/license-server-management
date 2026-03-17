namespace LicenseManager.API.Models
{
    public class ProductFeatureRecord
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
