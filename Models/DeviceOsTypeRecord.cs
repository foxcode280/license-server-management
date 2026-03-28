namespace LicenseManager.API.Models
{
    public class DeviceOsTypeRecord
    {
        public long Id { get; set; }
        public string OsName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
