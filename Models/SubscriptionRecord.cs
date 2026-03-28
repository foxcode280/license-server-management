namespace LicenseManager.API.Models
{
    public class SubscriptionRecord
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public long PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string PlanCategory { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? StatusDescription { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string LicenseMode { get; set; } = string.Empty;
        public int DeviceLimit { get; set; }
        public int TotalAllocated { get; set; }
        public List<SubscriptionAllocationRecord> Allocations { get; set; } = new();
    }
}
