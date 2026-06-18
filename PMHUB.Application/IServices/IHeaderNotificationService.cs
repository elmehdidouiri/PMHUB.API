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
            CancellationToken cancellationToken = default);
    }
}
