using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IBookingActivityNotificationPublisher
    {
        Task PublishAsync(
            BookingActivityNotificationDto notification,
            CancellationToken cancellationToken = default);
    }
}
