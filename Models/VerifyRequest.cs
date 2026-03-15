using System.ComponentModel.DataAnnotations;

namespace LicenseManager.API.Models
{
    public class VerifyRequest
    {
        public string? LicenseDocument { get; set; }

        public string? KeyId { get; set; }

        public string? LicenseJson { get; set; }

        public string? Signature { get; set; }

        public string? PublicKey { get; set; }

        public bool HasLegacyFields() =>
            !string.IsNullOrWhiteSpace(LicenseJson) &&
            !string.IsNullOrWhiteSpace(Signature);
    }
}
