using Microsoft.AspNetCore.SignalR;
using PMHUB.API.Hubs;
using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;

namespace PMHUB.API.Services
{
    public class SignalRBookingActivityNotificationPublisher : IBookingActivityNotificationPublisher
    {
        private readonly IHubContext<AdminNotificationHub> _hubContext;

        public SignalRBookingActivityNotificationPublisher(IHubContext<AdminNotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishAsync(
            BookingActivityNotificationDto notification,
            CancellationToken cancellationToken = default)
        {
            return _hubContext.Clients
                .Group(AdminNotificationHub.AdminGroupName)
                .SendAsync("BookingActivityReceived", notification, cancellationToken);
        }
    }
}
