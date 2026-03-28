namespace LicenseManager.API.DTOs.Companies
{
    public class CreateCompanyRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string PrimaryMobile { get; set; } = string.Empty;
        public string? AlternateMobile { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string? StatusDescription { get; set; }
    }
}
