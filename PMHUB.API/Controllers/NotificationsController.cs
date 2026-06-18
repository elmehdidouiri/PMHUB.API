using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IHeaderNotificationService _headerNotificationService;

        public NotificationsController(IHeaderNotificationService headerNotificationService)
        {
            _headerNotificationService = headerNotificationService;
        }

        [HttpGet("header")]
        public async Task<ActionResult<ApiResponse<HeaderNotificationSummaryDto>>> GetHeaderNotifications(
            [FromQuery] int dueSoonDays = 14,
            [FromQuery] int recentUpdatedDays = 7,
            [FromQuery] int lowProgressThreshold = 70,
            [FromQuery] int maxItems = 50,
            CancellationToken cancellationToken = default)
        {
            var includeAdminNotifications = User.HasClaim("isAdmin", "true");

            var notifications = await _headerNotificationService.GetHeaderNotificationsAsync(
                dueSoonDays,
                recentUpdatedDays,
                lowProgressThreshold,
                maxItems,
                includeAdminNotifications,
                cancellationToken);

            return Ok(ApiResponse<HeaderNotificationSummaryDto>.Ok(notifications));
        }
    }
}
