using User.Data;

namespace User.Services
{
    public interface IRefreshTokenService
    {
        Task<bool> SaveRefreshTokenAsync(int userId, string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken);
    }

    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(IRefreshTokenRepository refreshTokenRepository, ILogger<RefreshTokenService> logger)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
        }

        public async Task<bool> SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            try
            {
                var result = await _refreshTokenRepository.SaveAsync(userId, refreshToken);
                if (result)
                    _logger.LogInformation("Refresh token saved for user: {UserId}", userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving refresh token for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var result = await _refreshTokenRepository.RevokeAsync(refreshToken);
                if (result)
                    _logger.LogInformation("Refresh token revoked");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token");
                return false;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken)
        {
            try
            {
                var result = await _refreshTokenRepository.ValidateAsync(userId, refreshToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token for user: {UserId}", userId);
                return false;
            }
        }
    }
}
