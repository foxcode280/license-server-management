namespace LicenseManager.API.DTOs.ProductFeatures
{
    public class UpdateProductFeatureRequestDto
    {
        public long ProductId { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
