namespace LicenseManager.API.DTOs.Licenses
{
    public class GenerateLicenseRequestDto
    {
        public long SubscriptionId { get; set; }
        public long CreatedBy { get; set; }
    }
}
