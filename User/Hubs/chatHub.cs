using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace User.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinRoom(string room)
        {
            var normalizedRoom = NormalizeRoom(room);
            var username = GetUsername();

            await Groups.AddToGroupAsync(Context.ConnectionId, normalizedRoom);
            await Clients.Group(normalizedRoom).SendAsync("UserJoined", username);

            _logger.LogInformation("{Username} joined room {Room}", username, normalizedRoom);
        }

        public async Task LeaveRoom(string room)
        {
            var normalizedRoom = NormalizeRoom(room);
            var username = GetUsername();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, normalizedRoom);
            await Clients.Group(normalizedRoom).SendAsync("UserLeft", username);

            _logger.LogInformation("{Username} left room {Room}", username, normalizedRoom);
        }

        public async Task SendMessage(string message, string room)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new HubException("Message cannot be empty.");

            var normalizedRoom = NormalizeRoom(room);
            var text = message.Trim();
            var username = GetUsername();

            await Clients.Group(normalizedRoom).SendAsync("ReceiveMessage", username, text, normalizedRoom);
        }

        private string GetUsername()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value
                ?? Context.User?.Identity?.Name
                ?? "Unknown";
        }

        private static string NormalizeRoom(string? room)
        {
            if (string.IsNullOrWhiteSpace(room))
                return "general";

            return room.Trim().ToLowerInvariant();
        }
    }
}
