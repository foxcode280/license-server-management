using LicenseManager.API.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class LicenseGenerator
{
    public static string GenerateLicense(LicensePayload payload, string keyId, string privateKey)
    {
        var payloadJson = JsonSerializer.Serialize(payload, BuildJsonOptions());
        var signature = Sign(payloadJson, privateKey);

        var document = new SignedLicenseDocument
        {
            KeyId = keyId,
            Payload = payloadJson,
            Signature = signature
        };

        return JsonSerializer.Serialize(document, BuildJsonOptions());
    }

    public static bool TryParseLicenseDocument(string licenseDocument, out SignedLicenseDocument? document)
    {
        try
        {
            document = JsonSerializer.Deserialize<SignedLicenseDocument>(licenseDocument, BuildJsonOptions());
            return document is not null;
        }
        catch (JsonException)
        {
            document = null;
            return false;
        }
    }

    public static string? ExtractKeyId(string licenseDocument)
    {
        return TryParseLicenseDocument(licenseDocument, out var document)
            ? document?.KeyId
            : null;
    }

    private static string Sign(string payload, string privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);

        var data = Encoding.UTF8.GetBytes(payload);
        var signature = rsa.SignData(
            data,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signature);
    }

    private static JsonSerializerOptions BuildJsonOptions() =>
        new()
        {
            WriteIndented = true
        };
}
