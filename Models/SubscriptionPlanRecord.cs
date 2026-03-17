namespace LicenseManager.API.Models
{
    public class SubscriptionPlanRecord
    {
        public long Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public long ProductId { get; set; }
        public int DurationDays { get; set; }
        public int DeviceLimit { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}
