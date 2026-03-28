namespace LicenseManager.API.DTOs.Subscriptions
{
    public class SubscriptionAllocationRequestDto
    {
        public long OsTypeId { get; set; }
        public int AllocatedCount { get; set; }
    }
}
