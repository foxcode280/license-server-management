namespace LicenseManager.API.Models
{
    public class UserManagementRecord
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? AlternateMobile { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsDisabled { get; set; }
        public string Theme { get; set; } = "light";
        public string MenuPosition { get; set; } = "sidebar";
        public string? ProfilePhoto { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
