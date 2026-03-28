namespace LicenseManager.API.Models
{
    public class SubscriptionAllocationRecord
    {
        public long OsTypeId { get; set; }
        public string OsName { get; set; } = string.Empty;
        public int AllocatedCount { get; set; }
    }
}
