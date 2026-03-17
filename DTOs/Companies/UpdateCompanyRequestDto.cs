namespace LicenseManager.API.DTOs.Companies
{
    public class UpdateCompanyRequestDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
