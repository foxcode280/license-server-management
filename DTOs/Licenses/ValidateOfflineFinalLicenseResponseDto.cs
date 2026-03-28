using LicenseManager.API.Models;

namespace LicenseManager.API.DTOs.Licenses
{
    public class ValidateOfflineFinalLicenseResponseDto
    {
        public bool IsValid { get; set; }

        public string? Reason { get; set; }

        public string? KeyId { get; set; }

        public bool MatchesCurrentMachine { get; set; }

        public OfflineFinalLicenseDocument? Payload { get; set; }
    }
}
