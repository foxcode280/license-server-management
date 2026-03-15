using System.ComponentModel.DataAnnotations;

namespace LicenseManager.API.DTOs
{
    public class TokenRequest
    {
        [Required]
        public required string RefreshToken { get; set; }
    }
}
