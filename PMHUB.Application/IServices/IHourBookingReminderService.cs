using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IHourBookingReminderService
    {
        Task<HourBookingReminderResultDto> SendWeeklyHourAllocationRemindersAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<AdminHourBookingNotificationDto>> GetUsersWithoutRecentBookingsAsync(CancellationToken cancellationToken = default);
        Task SendSupervisorReminderAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
