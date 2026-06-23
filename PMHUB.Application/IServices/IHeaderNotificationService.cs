using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IHeaderNotificationService
    {
        Task<HeaderNotificationSummaryDto> GetHeaderNotificationsAsync(
            int dueSoonDays = 14,
            int recentUpdatedDays = 7,
            int lowProgressThreshold = 70,
            int maxItems = 50,
            bool includeAdminNotifications = true,
            Guid? userId = null,
            CancellationToken cancellationToken = default);

        Task<HeaderNotificationStateChangeDto> MarkAllAsReadAsync(
            Guid userId,
            int dueSoonDays = 14,
            int recentUpdatedDays = 7,
            int lowProgressThreshold = 70,
            bool includeAdminNotifications = true,
            CancellationToken cancellationToken = default);

        Task<HeaderNotificationStateChangeDto> ClearAllAsync(
            Guid userId,
            int dueSoonDays = 14,
            int recentUpdatedDays = 7,
            int lowProgressThreshold = 70,
            bool includeAdminNotifications = true,
            CancellationToken cancellationToken = default);

        Task<HeaderNotificationStateChangeDto> ResetAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
