using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace User.Hubs
{
    [Authorize]
    public class AuthHub : Hub
    {
        private readonly ILogger<AuthHub> _logger;
        private readonly IHubContext<AdminHub> _adminHubContext;

        public AuthHub(ILogger<AuthHub> logger, IHubContext<AdminHub> adminHubContext)
        {
            _logger = logger;
            _adminHubContext = adminHubContext;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            if (userId != null && username != null)
            {
                // Store user connection ID
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                _logger.LogInformation("User connected: {Username} (ID: {UserId}, ConnectionId: {ConnectionId})",
                    username, userId, Context.ConnectionId);

                // Notify admins
                await _adminHubContext.Clients.Group("admin").SendAsync(
                    "UserConnected",
                    new { userId, username, timestamp = DateTime.UtcNow }
                );
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            if (userId != null && username != null)
            {
                _logger.LogInformation("User disconnected: {Username} (ID: {UserId})", username, userId);

                // Notify admins
                await _adminHubContext.Clients.Group("admin").SendAsync(
                    "UserDisconnected",
                    new { userId, username, timestamp = DateTime.UtcNow }
                );
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotification(string message)
        {
            var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            _logger.LogInformation("Notification from {Username}: {Message}", username, message);

            await Clients.All.SendAsync("ReceiveNotification", new
            {
                username,
                message,
                timestamp = DateTime.UtcNow
            });
        }

        public async Task SendPrivateMessage(string targetUserId, string message)
        {
            var senderUsername = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            _logger.LogInformation("Message from {Sender} to User {Target}: {Message}",
                senderUsername, targetUserId, message);

           
            await Clients.Group($"user_{targetUserId}").SendAsync("ReceivePrivateMessage", senderUsername, message);
        }

        public async Task BroadcastUserUpdate(int userId, string status)
        {
            await Clients.All.SendAsync("UserStatusChanged", new
            {
                userId,
                status,
                timestamp = DateTime.UtcNow
            });
        }
    }
}