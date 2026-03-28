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
                return ApiExceptionResponseFactory.Create(this, ex);
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
                return ApiExceptionResponseFactory.Create(this, ex);
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
                return ApiExceptionResponseFactory.Create(
                    this,
                    ex,
                    message => new VerifyLicenseResponseDto
                    {
                        IsValid = false,
                        Reason = message
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
                return ApiExceptionResponseFactory.Create(
                    this,
                    ex,
                    message => new VerifyLicenseResponseDto
                    {
                        IsValid = false,
                        Reason = message
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
                return ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadLicense([FromQuery] long subscriptionId)
        {
            try
            {
                var userId = GetLoggedInUserId();

                var result = await _licenseService.DownloadLicense(subscriptionId, userId);

                var fileName = $"MXGENOFF-{result.LicenseCode}.lic";

                var bytes = Encoding.UTF8.GetBytes(result.LicenseKey);

                return File(bytes, "application/octet-stream", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPost("offline-request/generate")]
        public async Task<IActionResult> GenerateOfflineActivationRequest([FromBody] GenerateOfflineActivationRequestDto request)
        {
            try
            {
                var userId = GetLoggedInUserId();
                var result = await _licenseService.GenerateOfflineActivationRequest(request, userId);

                return Ok(new
                {
                    result.RequestId,
                    result.FileName,
                    requestDocument = result.EncryptedRequest,
                    payload = result.Payload,
                    message = "Offline activation request generated successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPost("offline/generic/inspect")]
        public async Task<IActionResult> InspectOfflineGenericLicense([FromBody] InspectOfflineLicenseRequestDto request)
        {
            try
            {
                var userId = GetLoggedInUserId();
                var result = await _licenseService.InspectOfflineGenericLicense(
                    request.LicenseDocument,
                    request.PublicKey,
                    userId);

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
                return ApiExceptionResponseFactory.Create(
                    this,
                    ex,
                    message => new VerifyLicenseResponseDto
                    {
                        IsValid = false,
                        Reason = message
                    });
            }
        }

        [HttpPost("offline/request/generate-from-license")]
        public async Task<IActionResult> GenerateOfflineRequestFromLicense([FromBody] GenerateOfflineRequestFromLicenseDto request)
        {
            try
            {
                var userId = GetLoggedInUserId();
                var result = await _licenseService.GenerateOfflineActivationRequestFromLicense(request, userId);

                return Ok(new
                {
                    result.RequestId,
                    result.FileName,
                    requestDocument = result.EncryptedRequest,
                    payload = result.Payload,
                    message = "Offline activation request generated successfully from generic license."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPost("offline/final/inspect")]
        public async Task<IActionResult> InspectFinalOfflineLicense([FromBody] InspectOfflineLicenseRequestDto request)
        {
            try
            {
                var userId = GetLoggedInUserId();
                var result = await _licenseService.ValidateOfflineFinalLicense(
                    request.LicenseDocument,
                    request.PublicKey,
                    userId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ValidateOfflineFinalLicenseResponseDto
                {
                    IsValid = false,
                    Reason = ex.Message
                });
            }
            catch (Exception ex)
            {
                return ApiExceptionResponseFactory.Create(
                    this,
                    ex,
                    message => new ValidateOfflineFinalLicenseResponseDto
                    {
                        IsValid = false,
                        Reason = message
                    });
            }
        }

        [HttpGet("offline-request/system-details")]
        public IActionResult GetOfflineRequestSystemDetails([FromServices] SystemMachineInfoService systemMachineInfoService)
        {
            try
            {
                return Ok(systemMachineInfoService.GetCurrentMachineInfo());
            }
            catch (Exception ex)
            {
                return ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPost("offline-request/download")]
        public async Task<IActionResult> DownloadOfflineActivationRequest([FromBody] GenerateOfflineActivationRequestDto request)
        {
            try
            {
                var userId = GetLoggedInUserId();
                var result = await _licenseService.GenerateOfflineActivationRequest(request, userId);
                var bytes = Encoding.UTF8.GetBytes(result.EncryptedRequest);
                var licenseCode = string.IsNullOrWhiteSpace(result.Payload?.License?.LicenseCode)
                    ? "UNKNOWN"
                    : result.Payload.License.LicenseCode.Trim();
                var fileName = $"MXGENOFF-{licenseCode}.lic";

                return File(bytes, "application/octet-stream", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiExceptionResponseFactory.Create(this, ex);
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
