# SignalR Implementation Guide for Real-Time User Management

## Overview

SignalR enables real-time bidirectional communication for:

1. **Live Notifications** - Notify admin when users login/logout
2. **Real-Time User Status** - Show which users are currently online
3. **Admin Alerts** - Alert admins of suspicious activities or security events
4. **Real-Time Dashboard** - Live user activity monitoring
5. **Chat/Messaging** - Direct messaging between users

---

## Installation

### 1. Install NuGet Packages

```bash
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

---

## Implementation Steps

### Step 1: Create SignalR Hub

Create file: `User/Hubs/AuthHub.cs`

```csharp

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
```

### Step 2: Create Admin Hub (Restricted Access)

Create file: `User/Hubs/AdminHub.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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
```

### Step 3: Configure SignalR in Program.cs

```csharp
// Add SignalR service
builder.Services.AddSignalR();

// In app configuration section, add:
app.MapHub<AuthHub>("/api/hubs/auth");
app.MapHub<AdminHub>("/api/hubs/admin");
```

### Step 4: Client-Side JavaScript/TypeScript

Create file: `wwwroot/js/auth-realtime.js`

```javascript
// Connect to SignalR hub
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/api/hubs/auth", {
    accessTokenFactory: () => localStorage.getItem("accessToken"),
  })
  .withAutomaticReconnect()
  .build();

// Handle notifications
connection.on("ReceiveNotification", (data) => {
  console.log(`Notification from ${data.username}: ${data.message}`);
  showNotification(data.message);
});

// Handle user status changes
connection.on("UserStatusChanged", (data) => {
  console.log(`User ${data.userId} status: ${data.status}`);
  updateUserStatus(data.userId, data.status);
});

// Handle force logout
connection.on("ForceLogout", (data) => {
  console.log("You've been logged out:", data.reason);
  localStorage.removeItem("accessToken");
  window.location.href = "/login";
});

// Handle security alerts
connection.on("SecurityAlert", (data) => {
  console.error("Security Alert:", data.message);
  showAlert(data.message, "danger");
});

// Start connection
connection
  .start()
  .catch((err) => console.error("SignalR Connection Error:", err));

// Send notification
function sendNotification(message) {
  connection
    .invoke("SendNotification", message)
    .catch((err) => console.error(err));
}

// Update UI on disconnect
connection.onclose(async () => {
  setTimeout(() => {
    connection.start().catch((err) => console.error(err));
  }, 5000);
});
```

### Step 5: Admin Dashboard Client

Create file: `wwwroot/js/admin-dashboard.js`

```javascript
const adminConnection = new signalR.HubConnectionBuilder()
  .withUrl("/api/hubs/admin", {
    accessTokenFactory: () => localStorage.getItem("accessToken"),
  })
  .withAutomaticReconnect()
  .build();

// Handle user connected notification
adminConnection.on("UserConnected", (data) => {
  console.log(`User connected: ${data.username}`);
  addUserToActiveLis(data);
  showNotification(`${data.username} just logged in`);
});

// Handle user disconnected notification
adminConnection.on("UserDisconnected", (data) => {
  console.log(`User disconnected: ${data.username}`);
  removeUserFromActiveList(data.userId);
  showNotification(`${data.username} logged out`);
});

// Handle other admin connected
adminConnection.on("AdminConnected", (data) => {
  showNotification(`Admin ${data.adminName} joined`);
});

// Get active users list
function loadActiveUsers() {
  adminConnection
    .invoke("GetActiveUsers")
    .then(() => console.log("Requesting active users..."))
    .catch((err) => console.error(err));
}

adminConnection.on("ActiveUsersList", (users) => {
  console.log("Active users:", users);
  populateUserTable(users);
});

// Send alert to specific user
function sendAlertToUser(userId, message) {
  adminConnection
    .invoke("SendAlertToUser", userId, message)
    .catch((err) => console.error(err));
}

// Broadcast security alert
function broadcastSecurityAlert(alert) {
  adminConnection
    .invoke("BroadcastSecurityAlert", alert)
    .catch((err) => console.error(err));
}

// Kick user offline
function kickUserOffline(userId) {
  if (confirm("Are you sure you want to kick this user offline?")) {
    adminConnection
      .invoke("KickUserOffline", userId)
      .catch((err) => console.error(err));
  }
}

// Start admin connection
adminConnection.start().catch((err) => console.error(err));
```

---

## Integration with AuthService

Update `AuthService.cs` to notify via SignalR on login:

```csharp
public class AuthService : IAuthService
{
    private readonly IHubContext<AuthHub> _hubContext;

    public async Task<AuthResponse?> AuthenticateAsync(LoginRequest request)
    {
        var user = await _userService.ValidateCredentialsAsync(request.Username, request.Password);
        if (user == null) return null;

        // ... token generation code ...

        // Notify all clients about login
        await _hubContext.Clients.All.SendAsync("UserLoggedIn", new
        {
            userId = user.Id,
            username = user.Username,
            timestamp = DateTime.UtcNow
        });

        return response;
    }
}
```

---

## Real-Time Features

### 1. Live User Count

```csharp
public int GetConnectedUserCount() => Clients.Connected.Count;
```

### 2. Typing Indicators

```javascript
// User starts typing
connection.invoke("UserStartedTyping", recipientId);

// User stops typing
connection.invoke("UserStoppedTyping", recipientId);
```

### 3. Presence System

```csharp
public async Task UpdateUserPresence(string status) // "Online", "Away", "Offline", "Busy"
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    await Clients.All.SendAsync("PresenceChanged", new { userId, status });
}
```

---

## Security Considerations

1. **Authentication** - All hubs require `[Authorize]` attribute
2. **Role-Based Access** - Use `[Authorize(Roles = "Admin")]` for sensitive hubs
3. **Rate Limiting** - Implement throttling on hub methods
4. **Input Validation** - Validate all incoming messages
5. **Token Refresh** - Handle JWT token refresh in real-time connections
6. **CORS** - Configure SignalR CORS for production

```csharp
app.UseCors(builder =>
{
    builder.WithOrigins("https://yourdomain.com")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
});
```

---

## Performance Optimization

1. **Connection Pooling** - Reuse hub connections
2. **Message Batching** - Batch multiple updates
3. **Selective Broadcasting** - Send to specific groups, not all clients
4. **Backpressure Handling** - Handle slow clients gracefully

```csharp
// Good: Send to specific group
await Clients.Group($"user_{userId}").SendAsync("UpdateReceived", data);

// Bad: Broadcast to all if not necessary
await Clients.All.SendAsync("UpdateReceived", data);
```

---

## Production Deployment

### Azure SignalR Service

```csharp
builder.Services.AddSignalR()
    .AddAzureSignalR(builder.Configuration.GetConnectionString("AzureSignalR"));
```

### Scale Out with Redis

```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(options =>
    {
        options.ConnectionFactory = async writer =>
        {
            var connection = await ConnectionMultiplexer.ConnectAsync("redis-server:6379");
            return connection;
        };
    });
```
