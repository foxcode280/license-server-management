namespace LicenseManager.API.DTOs
{
    public class LoginResponse
    {
        public required string AccessToken { get; set; }

        public required string RefreshToken { get; set; }

        public required string Email { get; set; }

        public required string Role { get; set; }
    }
}
