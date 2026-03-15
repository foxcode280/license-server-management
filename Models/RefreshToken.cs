namespace LicenseManager.API.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; }

        public long UserId { get; set; }

        public string Token { get; set; }

        public DateTime ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsRevoked { get; set; }
    }
}
