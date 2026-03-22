using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using User.Modles;
using AppUser = User.Modles.User;
using JwtSettings = User.Modles.JWTSettings;


namespace User.Services
{
    public interface IUserService
    {
        Task<AppUser?> ValidateCredentialsAsync(string username, string password);
        Task<AppUser?> GetByIdAsync(int userId);
        Task<IEnumerable<AppUser>> GetAllAsync();
        Task<AppUser?> CreateUserAsync(CreateUserRequest request);
        Task<bool> UpdateUserAsync(AppUser user);
        Task<bool> DeleteUserAsync(int userId);
    }



    public interface IAuthService
    {
        Task<AuthResponse?> AuthenticateAsync(LoginRequest request);
        Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<AuthResponse?> RegisterAsync(CreateUserRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IUserService _userService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IJwtService jwtService,
            IUserService userService,
            IRefreshTokenService refreshTokenService,
            ILogger<AuthService> logger)
        {
            _jwtService = jwtService;
            _userService = userService;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        public async Task<AuthResponse?> AuthenticateAsync(LoginRequest request)
        {
            var user = await _userService.ValidateCredentialsAsync(request.Username, request.Password);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed for username: {Username}", request.Username);
                return null;
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            await _refreshTokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

            _logger.LogInformation("User {Username} authenticated successfully", user.Username);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // Match token expiration
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Roles = user.Roles
                }
            };
        }

        public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // In a real implementation, you would:
                // 1. Validate the refresh token from the database
                // 2. Extract the user ID from the token
                // 3. Generate a new access token
                // 4. Generate a new refresh token and save it
                // 5. Revoke the old refresh token

                _logger.LogWarning("Refresh token flow needs database lookup implementation");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return null;
            }
        }

        public Task<bool> RevokeTokenAsync(string refreshToken)
        {
            return _refreshTokenService.RevokeRefreshTokenAsync(refreshToken);
        }

        public async Task<AuthResponse?> RegisterAsync(CreateUserRequest request)
        {
            try
            {
                var user = await _userService.CreateUserAsync(request);
                if (user == null)
                {
                    _logger.LogWarning("Registration failed for username: {Username}", request.Username);
                    return null;
                }

                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                await _refreshTokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

                _logger.LogInformation("User registered successfully: {Username}", request.Username);

                return new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Roles = user.Roles
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return null;
            }
        }
    }
}