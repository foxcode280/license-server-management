using System.ComponentModel.DataAnnotations;

namespace LicenseManager.API.Models
{
    public class VerifyRequest
    {
        [Required]
        public required string LicenseDocument { get; set; }

        public string? PublicKey { get; set; }
    }
}
