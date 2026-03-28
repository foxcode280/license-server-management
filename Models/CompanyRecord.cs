namespace LicenseManager.API.Models
{
    public class CompanyRecord
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string PrimaryMobile { get; set; } = string.Empty;
        public string? AlternateMobile { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? StatusDescription { get; set; }
        public List<string> LinkedSubscriptions { get; set; } = new();
        public List<string> LinkedLicenses { get; set; } = new();
        public bool IsDisabled { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
