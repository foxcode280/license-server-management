using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Models;
using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace LicenseManager.API.Controllers
{
    [ApiController]
    [Route("api/license")]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseService _licenseService;
        private readonly IConfiguration _configuration;

        public LicenseController(
            ILicenseService licenseService,
            IConfiguration configuration)
        {
            _licenseService = licenseService;
            _configuration = configuration;
        }

        [HttpPost("generate1")]
        public async Task<IActionResult> GenerateLicense1([FromBody] GenerateLicenseRequestDto request)
        {
            var licenseKey = await _licenseService.GenerateLicense(request);

            return Ok(new
            {
                LicenseKey = licenseKey
            });
        }

        [HttpPost("activate")]
        public async Task<IActionResult> ActivateLicense([FromBody] ActivationRequestDto request)
        {
            var result = await _licenseService.ActivateLicense(request);

            return Ok(result);
        }

        [HttpPost("verify-license")]
        public IActionResult VerifyLicense([FromBody] VerifyRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.LicenseDocument))
            {
                if (!LicenseGenerator.TryParseLicenseDocument(request.LicenseDocument, out var document) || document is null)
                {
                    return Ok(new { isValid = false, reason = "Invalid license document format" });
                }

                var publicKey = ResolvePublicKey(document.KeyId, request.PublicKey);
                var result = VerifyLicenseSignature(document.Payload, document.Signature, publicKey);

                return Ok(new { isValid = result, keyId = document.KeyId });
            }

            if (!request.HasLegacyFields())
            {
                return BadRequest("Provide either LicenseDocument or both LicenseJson and Signature.");
            }

            var resolvedPublicKey = ResolvePublicKey(request.KeyId, request.PublicKey);
            var isValid = VerifyLicenseSignature(request.LicenseJson!, request.Signature!, resolvedPublicKey);

            return Ok(new { isValid, keyId = request.KeyId ?? (_configuration["LicenseSigning:KeyId"] ?? "v1") });
        }

        public static bool VerifyLicenseSignature(string licenseJson, string signature, string? publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return false;
            }

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);

                var data = Encoding.UTF8.GetBytes(licenseJson);
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

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateLicense([FromQuery] long subscriptionId)
        {
            var license = await _licenseService.GenerateLicense(subscriptionId);

            return Ok(new
            {
                subscriptionId,
                license
            });
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadLicense([FromQuery] long subscriptionId)
        {
            var license = await _licenseService.GenerateLicense(subscriptionId);
            var fileName = $"subscription-{subscriptionId}.lic";
            var bytes = Encoding.UTF8.GetBytes(license);

            return File(bytes, "application/octet-stream", fileName);
        }

        private string? ResolvePublicKey(string? keyId, string? requestPublicKey)
        {
            if (!string.IsNullOrWhiteSpace(requestPublicKey))
            {
                return requestPublicKey;
            }

            var activeKeyId = _configuration["LicenseSigning:KeyId"] ?? "v1";
            if (string.IsNullOrWhiteSpace(keyId) || string.Equals(keyId, activeKeyId, StringComparison.Ordinal))
            {
                return _configuration["LicenseSigning:PublicKey"];
            }

            return _configuration[$"LicenseSigning:PublicKeys:{keyId}"];
        }
    }
}
