namespace LicenseManager.API.DTOs.Licenses
{
    public class InspectOfflineLicenseRequestDto
    {
        public string LicenseDocument { get; set; } = string.Empty;

        public string? PublicKey { get; set; }
    }
}
