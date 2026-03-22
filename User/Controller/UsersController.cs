using Microsoft.AspNetCore.Mvc;
using User.Services;
using User.Modles;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace User.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return Unauthorized(new { message = "Invalid user claims" });

            var user = await _userService.GetByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllAsync();
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Roles = u.Roles
            });

            return Ok(userDtos);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.CreateUserAsync(request);

            if (user == null)
                return BadRequest(new { message = "Failed to create user. Username may already exist." });

            _logger.LogInformation("User created by admin: {Username}", request.Username);

            return CreatedAtAction(nameof(GetProfile), new { userId = user.Id }, new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles
            });
        }

        [HttpPut("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.Email = request.Email ?? user.Email;
            user.Roles = request.Roles ?? user.Roles;

            var result = await _userService.UpdateUserAsync(user);

            if (!result)
                return BadRequest(new { message = "Failed to update user" });

            _logger.LogInformation("User updated by admin: {UserId}", userId);

            return Ok(new { message = "User updated successfully" });
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var result = await _userService.DeleteUserAsync(userId);

            if (!result)
                return BadRequest(new { message = "Failed to delete user" });

            _logger.LogInformation("User deleted by admin: {UserId}", userId);

            return Ok(new { message = "User deleted successfully" });
        }
    }
}