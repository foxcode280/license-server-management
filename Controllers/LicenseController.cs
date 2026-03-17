using LicenseManager.API.DTOs.Licenses;
using LicenseManager.API.Helpers;
using LicenseManager.API.Models;
using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace LicenseManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/license")]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseService _licenseService;
        private readonly LicenseProtectionService _licenseProtectionService;

        public LicenseController(
            ILicenseService licenseService,
            LicenseProtectionService licenseProtectionService)
        {
            _licenseService = licenseService;
            _licenseProtectionService = licenseProtectionService;
        }

        [HttpPost("activate")]
        public async Task<IActionResult> ActivateLicense([FromBody] ActivationRequestDto request)
        {
            try
            {
                var userId = GetLoggedInUserId();
                var result = await _licenseService.ActivateLicense(request, userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("activate-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ActivateLicenseFile([FromForm] ActivationFileRequestDto request)
        {
            try
            {
                using var reader = new StreamReader(request.LicenseFile.OpenReadStream(), Encoding.UTF8);
                var licenseContent = await reader.ReadToEndAsync();

                var activationRequest = new ActivationRequestDto
                {
                    LicenseKey = licenseContent,
                    MachineId = request.MachineId,
                    Hostname = request.Hostname,
                    IpAddress = request.IpAddress
                };

                var userId = GetLoggedInUserId();
                var result = await _licenseService.ActivateLicense(activationRequest, userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("verify-license")]
        public ActionResult<VerifyLicenseResponseDto> VerifyLicense([FromBody] VerifyRequest request)
        {
            try
            {
                var result = _licenseProtectionService.ValidateEncryptedLicense(
                    request.LicenseDocument,
                    request.PublicKey);

                return Ok(MapVerifyResult(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new VerifyLicenseResponseDto
                {
                    IsValid = false,
                    Reason = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new VerifyLicenseResponseDto
                {
                    IsValid = false,
                    Reason = ex.Message
                });
            }
        }

        [HttpPost("verify-file")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<VerifyLicenseResponseDto>> VerifyLicenseFile([FromForm] VerifyLicenseFileRequestDto request)
        {
            try
            {
                using var reader = new StreamReader(request.LicenseFile.OpenReadStream(), Encoding.UTF8);
                var licenseContent = await reader.ReadToEndAsync();

                var result = _licenseProtectionService.ValidateEncryptedLicense(
                    licenseContent,
                    request.PublicKey);

                return Ok(MapVerifyResult(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new VerifyLicenseResponseDto
                {
                    IsValid = false,
                    Reason = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new VerifyLicenseResponseDto
                {
                    IsValid = false,
                    Reason = ex.Message
                });
            }
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateLicense([FromQuery] long subscriptionId)
        {
            try
            {
                var userId = GetLoggedInUserId();
                var license = await _licenseService.GenerateLicense(subscriptionId, userId);

                return Ok(new
                {
                    subscriptionId,
                    license,
                    message = "License generated successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadLicense([FromQuery] long subscriptionId)
        {
            try
            {
                var userId = GetLoggedInUserId();

                var result = await _licenseService.DownloadLicense(subscriptionId, userId);

                var fileName = $"METRONUX-{result.LicenseCode}.lic";

                var bytes = Encoding.UTF8.GetBytes(result.LicenseKey);

                return File(bytes, "application/octet-stream", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private long GetLoggedInUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(userId, out var parsedUserId))
            {
                throw new InvalidOperationException("Logged in user id is missing from the JWT token.");
            }

            return parsedUserId;
        }

        private static VerifyLicenseResponseDto MapVerifyResult(LicenseValidationResult result)
        {
            return new VerifyLicenseResponseDto
            {
                IsValid = result.IsValid,
                Reason = result.Error,
                KeyId = result.KeyId,
                Payload = result.Payload
            };
        }
    }
}
