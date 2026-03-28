using LicenseManager.API.DTOs;
using LicenseManager.API.Services;
using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace LicenseManager.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;
        private readonly IUserManagementService _userManagementService;
        public AuthController(AuthService service, IUserManagementService userManagementService)
        {
            _service = service;
            _userManagementService = userManagementService;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                var agent = Request.Headers["User-Agent"].ToString();
                var result = await _service.Login(request, ip, agent);
                if (result == null)
                {
                    return Unauthorized("Invalid credentials");
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                var user = await _userManagementService.GetById(GetLoggedInUserId());
                return user == null ? NotFound() : Ok(user);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] TokenRequest request)
        {
            try
            {
                await _service.RevokeRefreshToken(request.RefreshToken);
                return Ok("Logged out successfully");
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }
        [Authorize]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequest request)
        {
            try
            {
                var tokenData = await _service.ValidateRefreshToken(request.RefreshToken);
                if (tokenData == null)
                {
                    return Unauthorized("Invalid refresh token");
                }
                if (tokenData.ExpiryDate < DateTime.UtcNow)
                {
                    return Unauthorized("Refresh token expired");
                }
                var user = await _service.GetUserById(tokenData.UserId);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }
                var result = await _service.RotateRefreshToken(user, request.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
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
    }
}
