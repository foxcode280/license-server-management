using System.ComponentModel.DataAnnotations;

namespace LicenseManager.API.DTOs.Licenses
{
    public class ActivationRequestDto
    {
        [Required]
        public required string LicenseKey { get; set; }

        [Required]
        public required string MachineId { get; set; }

        public string Hostname { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;
    }
}
