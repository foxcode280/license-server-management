using LicenseManager.API.DTOs.Subscriptions;
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
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ILicenseService licenseService, ISubscriptionService subscriptionService)
        {
            _licenseService = licenseService;
            _subscriptionService = subscriptionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                return Ok(await _subscriptionService.GetAll());
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var subscription = await _subscriptionService.GetById(id);
                return subscription == null ? NotFound() : Ok(subscription);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequestDto request)
        {
            try
            {
                var subscription = await _subscriptionService.Create(request, GetLoggedInUserId());
                return Ok(subscription);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
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
                    message = "Subscription approved and license generated successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return LicenseManager.API.Helpers.ApiExceptionResponseFactory.Create(this, ex);
            }
        }

        [HttpPost("{subscriptionId:long}/reject")]
        public async Task<IActionResult> RejectSubscription(long subscriptionId, [FromBody] RejectSubscriptionRequestDto request)
        {
            try
            {
                var userId = GetLoggedInUserId();
                await _licenseService.RejectSubscription(subscriptionId, userId);
                return Ok(new
                {
                    subscriptionId,
                    message = string.IsNullOrWhiteSpace(request?.Reason)
                        ? "Subscription rejected successfully."
                        : $"Subscription rejected successfully. Reason: {request.Reason}"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
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


