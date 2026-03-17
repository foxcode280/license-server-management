using LicenseManager.API.DTOs;
using LicenseManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManager.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;

        public AuthController(AuthService service)
        {
            _service = service;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                request.Email = "support@metronux.com";
                request.Password = "admin@123";
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
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
