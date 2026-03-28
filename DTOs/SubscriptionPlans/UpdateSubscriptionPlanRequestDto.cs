namespace LicenseManager.API.DTOs.SubscriptionPlans
{
    public class UpdateSubscriptionPlanRequestDto
    {
        public string PlanCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public long ProductId { get; set; }
        public string Mode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public string BillingLabel { get; set; } = string.Empty;
        public int DeviceLimit { get; set; }
        public string DeviceLimitLabel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Highlights { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public bool IsActive { get; set; }
    }
}
