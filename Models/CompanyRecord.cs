namespace LicenseManager.API.Models
{
    public class CompanyRecord
    {
        public long Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
