using LicenseManager.API.Models;

namespace LicenseManager.API.DTOs.Licenses
{
    public class VerifyLicenseResponseDto
    {
        public bool IsValid { get; set; }

        public string? Reason { get; set; }

        public string? KeyId { get; set; }

        public LicensePayload? Payload { get; set; }
    }
}
