using Microsoft.AspNetCore.Mvc;
using User.Services;
using User.Modles;
using Microsoft.AspNetCore.Authorization;

namespace User.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authservice;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authservice = authService;
            _logger = logger;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authservice.AuthenticateAsync(request);

            if (response == null)
                return Unauthorized(new { message = "Invalid username or password" });

            // Set refresh token in HTTP-only cookie for security
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // HTTPS only
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);

            // Return access token in response body (don't include refresh token)
            return Ok(new
            {
                accessToken = response.AccessToken,
                expiresAt = response.ExpiresAt,
                user = response.User
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authservice.RegisterAsync(request);

            if (response == null)
                return BadRequest(new { message = "Registration failed. User may already exist." });

            // Set refresh token in HTTP-only cookie for security
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);

            return Ok(new
            {
                message = "Registration successful",
                accessToken = response.AccessToken,
                expiresAt = response.ExpiresAt,
                user = response.User
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { message = "Refresh token not found" });

            var response = await _authservice.RefreshTokenAsync(refreshToken);
            if (response == null)
                return Unauthorized(new { message = "Invalid refresh token" });

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);

            return Ok(new
            {
                accessToken = response.AccessToken,
                expiresAt = response.ExpiresAt,
                user = response.User
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authservice.RevokeTokenAsync(refreshToken);
            }
            Response.Cookies.Delete("refreshToken");
            _logger.LogInformation("User {user} Logged out", User.Identity?.Name);
            return Ok(new { message = "Logged out successfully" });
        }
    }
}