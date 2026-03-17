namespace LicenseManager.API.DTOs.Products
{
    public class UpdateProductRequestDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
