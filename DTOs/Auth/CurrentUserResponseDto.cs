namespace LicenseManager.API.DTOs.Auth
{
    public class CurrentUserResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string Theme { get; set; } = "light";
        public string MenuPosition { get; set; } = "sidebar";
    }
}
