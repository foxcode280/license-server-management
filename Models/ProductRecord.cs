namespace LicenseManager.API.Models
{
    public class ProductRecord
    {
        public long Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
