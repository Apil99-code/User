using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Reflection.Metadata;
using System.Security.Claims;

namespace User.Hubs
{
    [Authorize(Roles = "Admin")]
    public class AdminHub : Hub
    {
        private readonly ILogger<AdminHub> _logger;

        public AdminHub(ILogger<AdminHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var adminName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            _logger.LogInformation("Admin connected: {AdminName}", adminName);

            // Add to admin group
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin");

            // Notify other admins
            await Clients.OthersInGroup("admin").SendAsync("AdminConnected", new
            {
                adminName,
                timestamp = DateTime.UtcNow
            });

            await base.OnConnectedAsync();
        }

        public async Task GetActiveUsers()
        {
            // This would typically fetch from a service/database
            var activeUsers = new[]
            {
                new { userId = 1, username = "testuser", status = "Online", loginTime = DateTime.UtcNow },
                new { userId = 2, username = "admin", status = "Online", loginTime = DateTime.UtcNow }
            };

            await Clients.Caller.SendAsync("ActiveUsersList", activeUsers);
        }

        public async Task SendAlertToUser(int userId, string alertMessage)
        {
            _logger.LogInformation("Sending alert to user {UserId}: {Message}", userId, alertMessage);

            await Clients.Group($"user_{userId}").SendAsync("ReceiveAlert", new
            {
                message = alertMessage,
                severity = "warning",
                timestamp = DateTime.UtcNow
            });
        }

        public async Task BroadcastSecurityAlert(string alert)
        {
            _logger.LogWarning("Security alert broadcasted: {Alert}", alert);

            // Alert all clients
            await Clients.All.SendAsync("SecurityAlert", new
            {
                message = alert,
                severity = "critical",
                timestamp = DateTime.UtcNow
            });
        }

        public async Task KickUserOffline(int userId)
        {
            var adminName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            _logger.LogInformation("Admin {AdminName} kicking user {UserId} offline", adminName, userId);

            await Clients.Group($"user_{userId}").SendAsync("ForceLogout", new
            {
                reason = "Terminated by administrator",
                timestamp = DateTime.UtcNow
            });
        }
    }
}

// 1. Include the SignalR library (if not already in your project)
