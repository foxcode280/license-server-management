namespace LicenseManager.API.Models
{
    public class EncryptedLicensePackage
    {
        public string EncryptionKeyId { get; set; } = string.Empty;

        public string Nonce { get; set; } = string.Empty;

        public string CipherText { get; set; } = string.Empty;

        public string Tag { get; set; } = string.Empty;
    }
}
