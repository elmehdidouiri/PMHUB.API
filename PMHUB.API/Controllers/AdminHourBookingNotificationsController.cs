using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/admin/hour-booking-notifications")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminHourBookingNotificationsController : ControllerBase
    {
        private readonly IHourBookingReminderService _reminderService;
        private readonly ILogger<AdminHourBookingNotificationsController> _logger;

        public AdminHourBookingNotificationsController(
            IHourBookingReminderService reminderService,
            ILogger<AdminHourBookingNotificationsController> logger)
        {
            _reminderService = reminderService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<AdminHourBookingNotificationDto>>>> GetNotifications(CancellationToken cancellationToken)
        {
            var notifications = await _reminderService.GetUsersWithoutRecentBookingsAsync(cancellationToken);
            return Ok(ApiResponse<IEnumerable<AdminHourBookingNotificationDto>>.Ok(notifications));
        }

        [HttpPost("{userId:guid}/send-reminder")]
        public async Task<ActionResult<ApiResponse>> SendSupervisorReminder(Guid userId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Admin requested supervisor reminder for user {UserId}", userId);

            await _reminderService.SendSupervisorReminderAsync(userId, cancellationToken);

            return Ok(ApiResponse.Ok("Reminder email sent successfully."));
        }
    }
}
