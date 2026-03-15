using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LicenseManager.API.DTOs.Licenses
{
    public class VerifyLicenseFileRequestDto
    {
        [Required]
        public required IFormFile LicenseFile { get; set; }

        public string? PublicKey { get; set; }
    }
}
