# Redis Implementation Guide for User Authentication API

## Overview

Redis can be integrated into this authentication system for:

1. **Session/Token Caching** - Cache valid JWT tokens and refresh tokens
2. **Rate Limiting** - Prevent brute force login attacks
3. **Refresh Token Management** - Store and manage refresh tokens with automatic expiration
4. **User Sessions** - Track active user sessions
5. **Temporary OTP/2FA Codes** - Store temporary authentication codes

---

## Installation

### 1. Install Redis Server

**Windows (using WSL2 or Docker):**

```bash
docker run -d -p 6379:6379 --name redis redis:latest
```

**Or use Redis for Windows:**
Download from: https://github.com/microsoftarchive/redis/releases

**Linux:**

```bash
sudo apt-get install redis-server
sudo systemctl start redis-server
```

### 2. Install NuGet Package

```bash
dotnet add package StackExchange.Redis
```

---

## Implementation Steps

### Step 1: Create Redis Service Abstraction

Create file: `User/Services/ICacheService.cs`

```csharp
namespace User.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task RemoveByPatternAsync(string pattern);
    }
}
```

### Step 2: Implement Redis Cache Service

Create file: `User/Services/RedisCacheService.cs`

```csharp
using StackExchange.Redis;
using System.Text.Json;

namespace User.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var db = _redis.GetDatabase();
                var value = await db.StringGetAsync(key);

                if (value.IsNull)
                    return default;

                return JsonSerializer.Deserialize<T>(value.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var serialized = JsonSerializer.Serialize(value);
                await db.StringSetAsync(key, serialized, expiration);

                _logger.LogInformation("Cache set for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key: {Key}", key);
            }
        }

        public async Task<bool> RemoveAsync(string key)
        {
            try
            {
                var db = _redis.GetDatabase();
                return await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var db = _redis.GetDatabase();
                return await db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache key: {Key}", key);
                return false;
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var db = _redis.GetDatabase();
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = await server.KeysAsync(pattern: pattern);

                foreach (var key in keys)
                {
                    await db.KeyDeleteAsync(key);
                }

                _logger.LogInformation("Removed cache keys matching pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
            }
        }
    }
}
```

### Step 3: Configure Redis in Program.cs

```csharp
// Add Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379")
);

builder.Services.AddSingleton<ICacheService, RedisCacheService>();
```

### Step 4: Update appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=userdb;...",
    "Redis": "localhost:6379"
  }
}
```

---

## Use Cases

### 1. Cache JWT Tokens (Blacklist Pattern)

Update `RefreshTokenService.cs`:

```csharp
public class RefreshTokenService : IRefreshTokenService
{
    private readonly ICacheService _cacheService;

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        // Mark token as revoked in Redis (fast lookup)
        await _cacheService.SetAsync($"revoked_token:{refreshToken}", true, TimeSpan.FromDays(7));

        // Also remove from database
        return await _refreshTokenRepository.RevokeAsync(refreshToken);
    }

    public async Task<bool> IsTokenRevokedAsync(string refreshToken)
    {
        return await _cacheService.ExistsAsync($"revoked_token:{refreshToken}");
    }
}
```

### 2. Rate Limiting

Create file: `User/Services/RateLimitService.cs`

```csharp
public class RateLimitService
{
    private readonly ICacheService _cacheService;
    private const int MAX_LOGIN_ATTEMPTS = 5;
    private const int LOCKOUT_MINUTES = 15;

    public async Task<bool> IsLockedOutAsync(string username)
    {
        var key = $"lockout:{username}";
        return await _cacheService.ExistsAsync(key);
    }

    public async Task RecordFailedAttemptAsync(string username)
    {
        var key = $"login_attempts:{username}";
        var db = ConnectionMultiplexer.Connect("localhost:6379").GetDatabase();

        var attempts = await db.StringIncrementAsync(key);

        if (attempts == 1)
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(15));

        if (attempts >= MAX_LOGIN_ATTEMPTS)
            await _cacheService.SetAsync($"lockout:{username}", true, TimeSpan.FromMinutes(LOCKOUT_MINUTES));
    }

    public async Task ClearFailedAttemptsAsync(string username)
    {
        await _cacheService.RemoveAsync($"login_attempts:{username}");
    }
}
```

### 3. Session Management

```csharp
// Store active sessions
await _cacheService.SetAsync(
    $"session:{userId}",
    new { username = user.Username, loginTime = DateTime.UtcNow },
    TimeSpan.FromHours(24)
);

// Get active sessions
var session = await _cacheService.GetAsync<dynamic>($"session:{userId}");

// Logout clears session
await _cacheService.RemoveAsync($"session:{userId}");
```

---

## Performance Benefits

- **Sub-millisecond Response Times** - Redis lookup is ~1000x faster than database
- **Reduced Database Load** - Cache frequent lookups
- **Automatic Expiration** - TTL-based cleanup without manual maintenance
- **Session Scalability** - Handle thousands of concurrent sessions

---

## Monitoring & Debugging

```bash
# Connect to Redis CLI
redis-cli

# View all keys
KEYS *

# View specific key
GET session:1

# Monitor real-time commands
MONITOR

# Memory stats
INFO memory
```

---

## Security Considerations

1. **Password Protection** - Enable Redis authentication

   ```
   requirepass "your-strong-password"
   ```

2. **Network Security** - Keep Redis on private network
3. **SSL/TLS** - Use Redis in containerized environments with TLS
4. **Data Persistence** - Enable AOF (Append Only File) for production
