using LicenseManager.API.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LicenseManager.API.Helpers
{
    public class LicenseProtectionService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        private readonly IConfiguration _configuration;

        public LicenseProtectionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateEncryptedLicenseDocument(LicensePayload payload)
        {
            var signingKeyId = _configuration["LicenseSigning:KeyId"] ?? "v1";
            var privateKey = _configuration["LicenseSigning:PrivateKey"]
                ?? throw new InvalidOperationException("License signing private key is not configured.");
            var encryptionKeyId = _configuration["LicenseProtection:EncryptionKeyId"] ?? "enc-v1";
            var encryptionKey = ResolveEncryptionKey(encryptionKeyId)
                ?? throw new InvalidOperationException("License protection encryption key is not configured.");

            payload.KeyId = signingKeyId;

            var signedDocument = LicenseGenerator.GenerateLicense(payload, signingKeyId, privateKey);
            return EncryptDocument(signedDocument, encryptionKeyId, encryptionKey);
        }

        public LicenseValidationResult ValidateEncryptedLicense(string encryptedLicense, string? publicKeyOverride = null)
        {
            if (!TryDecryptLicenseDocument(encryptedLicense, out var document, out var error) || document is null)
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Error = error
                };
            }

            var publicKey = string.IsNullOrWhiteSpace(publicKeyOverride)
                ? ResolvePublicKey(document.KeyId)
                : publicKeyOverride;

            if (!VerifySignature(document.Payload, document.Signature, publicKey))
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Error = "License signature is invalid.",
                    KeyId = document.KeyId,
                    Document = document
                };
            }

            return new LicenseValidationResult
            {
                IsValid = true,
                KeyId = document.KeyId,
                PayloadJson = document.Payload,
                Document = document,
                Payload = DeserializePayload(document.Payload)
            };
        }

        public bool TryDecryptLicenseDocument(
            string encryptedLicense,
            out SignedLicenseDocument? document,
            out string? error)
        {
            document = null;
            error = null;

            try
            {
                var packageJson = Encoding.UTF8.GetString(Convert.FromBase64String(encryptedLicense.Trim()));
                var package = JsonSerializer.Deserialize<EncryptedLicensePackage>(packageJson, JsonOptions);

                if (package is null)
                {
                    error = "License package could not be parsed.";
                    return false;
                }

                var encryptionKey = ResolveEncryptionKey(package.EncryptionKeyId);
                if (encryptionKey is null)
                {
                    error = $"Encryption key '{package.EncryptionKeyId}' is not configured.";
                    return false;
                }

                var nonce = Convert.FromBase64String(package.Nonce);
                var cipherText = Convert.FromBase64String(package.CipherText);
                var tag = Convert.FromBase64String(package.Tag);
                var plainBytes = new byte[cipherText.Length];

                using var aes = new AesGcm(encryptionKey, tag.Length);
                aes.Decrypt(nonce, cipherText, tag, plainBytes);

                var signedDocumentJson = Encoding.UTF8.GetString(plainBytes);
                if (!LicenseGenerator.TryParseLicenseDocument(signedDocumentJson, out document) || document is null)
                {
                    error = "Signed license document could not be parsed after decryption.";
                    return false;
                }

                return true;
            }
            catch (FormatException)
            {
                error = "License is not a valid Base64-encoded package.";
                return false;
            }
            catch (JsonException)
            {
                error = "Encrypted license package format is invalid.";
                return false;
            }
            catch (CryptographicException)
            {
                error = "License decryption failed.";
                return false;
            }
        }

        private string EncryptDocument(string signedDocument, string encryptionKeyId, byte[] encryptionKey)
        {
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plainBytes = Encoding.UTF8.GetBytes(signedDocument);
            var cipherText = new byte[plainBytes.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(encryptionKey, tag.Length);
            aes.Encrypt(nonce, plainBytes, cipherText, tag);

            var package = new EncryptedLicensePackage
            {
                EncryptionKeyId = encryptionKeyId,
                Nonce = Convert.ToBase64String(nonce),
                CipherText = Convert.ToBase64String(cipherText),
                Tag = Convert.ToBase64String(tag)
            };

            var packageJson = JsonSerializer.Serialize(package, JsonOptions);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(packageJson));
        }

        private byte[]? ResolveEncryptionKey(string keyId)
        {
            var rawKey = _configuration[$"LicenseProtection:EncryptionKeys:{keyId}"]
                ?? _configuration["LicenseProtection:EncryptionKey"];

            if (string.IsNullOrWhiteSpace(rawKey))
            {
                return null;
            }

            try
            {
                return Convert.FromBase64String(rawKey);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("License protection encryption key must be Base64-encoded.");
            }
        }

        private string? ResolvePublicKey(string keyId)
        {
            var activeKeyId = _configuration["LicenseSigning:KeyId"] ?? "v1";
            if (string.Equals(activeKeyId, keyId, StringComparison.Ordinal))
            {
                return _configuration["LicenseSigning:PublicKey"];
            }

            return _configuration[$"LicenseSigning:PublicKeys:{keyId}"];
        }

        private static bool VerifySignature(string payloadJson, string signature, string? publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return false;
            }

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);

                var data = Encoding.UTF8.GetBytes(payloadJson);
                var sig = Convert.FromBase64String(signature);

                return rsa.VerifyData(
                    data,
                    sig,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private static LicensePayload? DeserializePayload(string payloadJson)
        {
            try
            {
                return JsonSerializer.Deserialize<LicensePayload>(payloadJson, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
