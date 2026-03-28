namespace LicenseManager.API.DTOs.Subscriptions
{
    public class CreateSubscriptionRequestDto
    {
        public long CompanyId { get; set; }
        public string PlanCategory { get; set; } = string.Empty;
        public long? PlanId { get; set; }
        public long? ProductId { get; set; }
        public DateTime? StartDate { get; set; }
        public int? DurationDays { get; set; }
        public string LicenseMode { get; set; } = "ONLINE";
        public List<SubscriptionAllocationRequestDto> Allocations { get; set; } = new();
    }
}
