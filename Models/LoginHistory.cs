namespace LicenseManager.API.Models
{
    public class LoginHistory
    {
        public long UserId { get; set; }

        public string Email { get; set; }

        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public string LoginStatus { get; set; }

        public string FailureReason { get; set; }

        public DateTime LoginTime { get; set; }
    }
}
