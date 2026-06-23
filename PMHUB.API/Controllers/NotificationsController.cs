using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using System.Security.Claims;

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
            var userId = GetAuthenticatedUserId();

            var notifications = await _headerNotificationService.GetHeaderNotificationsAsync(
                dueSoonDays,
                recentUpdatedDays,
                lowProgressThreshold,
                maxItems,
                includeAdminNotifications,
                userId,
                cancellationToken);

            return Ok(ApiResponse<HeaderNotificationSummaryDto>.Ok(notifications));
        }

        [HttpPost("header/mark-all-as-read")]
        public async Task<ActionResult<ApiResponse<HeaderNotificationStateChangeDto>>> MarkAllAsRead(
            [FromQuery] int dueSoonDays = 14,
            [FromQuery] int recentUpdatedDays = 7,
            [FromQuery] int lowProgressThreshold = 70,
            CancellationToken cancellationToken = default)
        {
            var result = await _headerNotificationService.MarkAllAsReadAsync(
                GetAuthenticatedUserId(),
                dueSoonDays,
                recentUpdatedDays,
                lowProgressThreshold,
                User.HasClaim("isAdmin", "true"),
                cancellationToken);

            return Ok(ApiResponse<HeaderNotificationStateChangeDto>.Ok(result, "All notifications marked as read."));
        }

        [HttpPost("header/read-all")]
        public Task<ActionResult<ApiResponse<HeaderNotificationStateChangeDto>>> ReadAll(
            [FromQuery] int dueSoonDays = 14,
            [FromQuery] int recentUpdatedDays = 7,
            [FromQuery] int lowProgressThreshold = 70,
            CancellationToken cancellationToken = default)
            => MarkAllAsRead(dueSoonDays, recentUpdatedDays, lowProgressThreshold, cancellationToken);

        [HttpPost("header/clear")]
        public async Task<ActionResult<ApiResponse<HeaderNotificationStateChangeDto>>> ClearAll(
            [FromQuery] int dueSoonDays = 14,
            [FromQuery] int recentUpdatedDays = 7,
            [FromQuery] int lowProgressThreshold = 70,
            CancellationToken cancellationToken = default)
        {
            var result = await _headerNotificationService.ClearAllAsync(
                GetAuthenticatedUserId(),
                dueSoonDays,
                recentUpdatedDays,
                lowProgressThreshold,
                User.HasClaim("isAdmin", "true"),
                cancellationToken);

            return Ok(ApiResponse<HeaderNotificationStateChangeDto>.Ok(result, "All notifications cleared."));
        }

        [HttpDelete("header")]
        public Task<ActionResult<ApiResponse<HeaderNotificationStateChangeDto>>> DeleteAll(
            [FromQuery] int dueSoonDays = 14,
            [FromQuery] int recentUpdatedDays = 7,
            [FromQuery] int lowProgressThreshold = 70,
            CancellationToken cancellationToken = default)
            => ClearAll(dueSoonDays, recentUpdatedDays, lowProgressThreshold, cancellationToken);

        [HttpPost("header/reset")]
        public async Task<ActionResult<ApiResponse<HeaderNotificationStateChangeDto>>> Reset(CancellationToken cancellationToken)
        {
            var result = await _headerNotificationService.ResetAsync(GetAuthenticatedUserId(), cancellationToken);
            return Ok(ApiResponse<HeaderNotificationStateChangeDto>.Ok(result, "Notification state reset."));
        }

        private Guid GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Authenticated user could not be resolved.");
            }

            return userId;
        }
    }
}
