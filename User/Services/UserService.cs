using BCrypt.Net;
using User.Data;
using User.Modles;
using AppUser = User.Modles.User;

namespace User.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<AppUser?> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Username}", username);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    _logger.LogError("Stored password hash is missing for user: {Username}", username);
                    return null;
                }

                // Verify password using BCrypt
                var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (!isPasswordValid)
                {
                    _logger.LogWarning("Invalid password for user: {Username}", username);
                    return null;
                }

                _logger.LogInformation("User credentials validated: {Username}", username);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for user: {Username}", username);
                return null;
            }
        }

        public async Task<AppUser?> GetByIdAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by id: {UserId}", userId);
                return null;
            }
        }

        public async Task<IEnumerable<AppUser>> GetAllAsync()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                return users ?? Enumerable.Empty<AppUser>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return Enumerable.Empty<AppUser>();
            }
        }

        public async Task<AppUser?> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                // Check if user already exists
                if (await _userRepository.ExistsAsync(request.Username))
                {
                    _logger.LogWarning("User already exists: {Username}", request.Username);
                    return null;
                }

                var user = new AppUser
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Roles = new[] { "User" },
                    CreatedAt = DateTime.UtcNow
                };

                var userId = await _userRepository.CreateAsync(user);
                user.Id = userId;

                _logger.LogInformation("User created: {Username}", request.Username);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", request.Username);
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(AppUser user)
        {
            try
            {
                var result = await _userRepository.UpdateAsync(user);
                if (result)
                    _logger.LogInformation("User updated: {UserId}", user.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var result = await _userRepository.DeleteAsync(userId);
                if (result)
                    _logger.LogInformation("User deleted: {UserId}", userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return false;
            }
        }
    }
}
