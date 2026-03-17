using LicenseManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LicenseManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ILicenseService _licenseService;

        public SubscriptionController(ILicenseService licenseService)
        {
            _licenseService = licenseService;
        }

        [HttpPost("{subscriptionId:long}/approve")]
        public async Task<IActionResult> ApproveSubscription(long subscriptionId)
        {
            try
            {
                var userId = GetLoggedInUserId();
                await _licenseService.ApproveSubscription(subscriptionId, userId);

                return Ok(new
                {
                    subscriptionId,
                    message = "Subscription approved successfully."
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
