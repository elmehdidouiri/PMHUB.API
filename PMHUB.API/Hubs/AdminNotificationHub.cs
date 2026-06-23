using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PMHUB.API.Hubs
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminNotificationHub : Hub
    {
        public const string AdminGroupName = "admins";

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroupName);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminGroupName);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
