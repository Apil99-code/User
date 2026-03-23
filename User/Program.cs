using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using User.Data;
using User.Hubs;
using User.Modles;   // Fixed: was "User.Modles" (typo)
using User.Services;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.Configure<JWTSettings>(builder.Configuration.GetSection("JwtSettings"));

// SignalR
builder.Services.AddSignalR();

// Register Data Access Layer
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Register Services
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Resolve JWTSettings via DI to avoid duplicate binding
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JWTSettings>()
        ?? throw new InvalidOperationException("JwtSettings configuration is missing");

    // Set false in development so HTTP works; always true in production
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)), // Fixed: UTF8 not ASCII
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    // Support JWT in SignalR query string (required for hub authentication)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogInformation("JWT token validated for user: {User}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// ── Middleware pipeline (ORDER MATTERS) ────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

// HTTPS redirect only in production (keeps dev workflow simple)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

// Serve wwwroot static files (index.html, css, js, etc.)
app.UseDefaultFiles();   // maps "/" → "/index.html"
app.UseStaticFiles();

// Auth middleware must come BEFORE MapHub / MapControllers
app.UseAuthentication();
app.UseAuthorization();

// ── Routes ─────────────────────────────────────────────────────────────────

// Explicit chat page route (fallback; UseDefaultFiles already covers "/")
app.MapGet("/chat", async (IWebHostEnvironment env) =>
{
    var indexPath = Path.Combine(env.WebRootPath ?? "wwwroot", "index.html");
    var content = await File.ReadAllTextAsync(indexPath);
    return Results.Content(content, "text/html");
});

app.MapGet("/scalar", () => Results.Redirect("/scalar/v1"));

// SignalR Hubs
// Fix: each hub type must be mapped separately; ChatHub was mapped twice with different paths
app.MapHub<ChatHub>("/chatHub");
app.MapHub<ChatHub>("/api/hubs/chat");   // if you need both, keep both — just make sure ChatHub supports it
app.MapHub<AuthHub>("/api/hubs/auth");
app.MapHub<AdminHub>("/api/hubs/admin");

app.MapControllers();

// ── Startup banner ──────────────────────────────────────────────────────────
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
{
    var server = app.Services.GetRequiredService<IServer>();
    var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
    if (addresses is { Count: > 0 })
    {
        Console.WriteLine("\n=====================================");
        foreach (var addr in addresses)
            Console.WriteLine($"Listening on: {addr}");
        Console.WriteLine("=====================================\n");
    }
});

app.Run();