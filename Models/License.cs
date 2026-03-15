namespace LicenseManager.API.Models
{
    public class License
    {
        public long Id { get; set; }

        public string LicenseKey { get; set; }

        public DateTime IssueDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public string Status { get; set; }
    }
}
