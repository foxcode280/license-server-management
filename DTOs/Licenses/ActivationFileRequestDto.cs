using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LicenseManager.API.DTOs.Licenses
{
    public class ActivationFileRequestDto
    {
        [Required]
        public required IFormFile LicenseFile { get; set; }

        [Required]
        public required string MachineId { get; set; }

        public string Hostname { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;
    }
}
