namespace LicenseManager.API.DTOs.Users
{
    public class CreateUserRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? AlternateMobile { get; set; }
        public bool IsDisabled { get; set; }
    }
}
