using Dapper;
using Npgsql;
using User.Modles;
using AppUser = User.Modles.User;

namespace User.Data
{
    public interface IUserRepository
    {
        Task<AppUser?> GetByIdAsync(int id);
        Task<AppUser?> GetByUsernameAsync(string username);
        Task<IEnumerable<AppUser>> GetAllAsync();
        Task<int> CreateAsync(AppUser user);
        Task<bool> UpdateAsync(AppUser user);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(string username);
    }

    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        private sealed class DbUser
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public string? Roles { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private static string[] ParseRoles(string? roles)
        {
            if (string.IsNullOrWhiteSpace(roles))
                return new[] { "User" };

            return roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToArray();
        }

        private static AppUser MapToAppUser(DbUser dbUser)
        {
            return new AppUser
            {
                Id = dbUser.Id,
                Username = dbUser.Username,
                Email = dbUser.Email,
                PasswordHash = dbUser.PasswordHash,
                Roles = ParseRoles(dbUser.Roles),
                CreatedAt = dbUser.CreatedAt
            };
        }

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<AppUser?> GetByIdAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = @"
                SELECT 
                    id,
                    username,
                    email,
                    password_hash AS PasswordHash,
                    roles,
                    created_at AS CreatedAt
                FROM users
                WHERE id = @Id";
            var dbUser = await connection.QueryFirstOrDefaultAsync<DbUser>(query, new { Id = id });
            return dbUser == null ? null : MapToAppUser(dbUser);
        }

        public async Task<AppUser?> GetByUsernameAsync(string username)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = @"
                SELECT 
                    id,
                    username,
                    email,
                    password_hash AS PasswordHash,
                    roles,
                    created_at AS CreatedAt
                FROM users
                WHERE username = @Username";
            var dbUser = await connection.QueryFirstOrDefaultAsync<DbUser>(query, new { Username = username });
            return dbUser == null ? null : MapToAppUser(dbUser);
        }

        public async Task<IEnumerable<AppUser>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = @"
                SELECT 
                    id,
                    username,
                    email,
                    password_hash AS PasswordHash,
                    roles,
                    created_at AS CreatedAt
                FROM users
                ORDER BY created_at DESC";
            var dbUsers = await connection.QueryAsync<DbUser>(query);
            return dbUsers.Select(MapToAppUser);
        }

        public async Task<int> CreateAsync(AppUser user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = @"
                INSERT INTO users (username, email, password_hash, roles, created_at) 
                VALUES (@Username, @Email, @PasswordHash, @Roles, @CreatedAt)
                RETURNING id";

            return await connection.ExecuteScalarAsync<int>(query, new
            {
                user.Username,
                user.Email,
                user.PasswordHash,
                Roles = user.Roles.Length > 0 ? string.Join(",", user.Roles) : "User",
                user.CreatedAt
            });
        }

        public async Task<bool> UpdateAsync(AppUser user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = @"
                UPDATE users 
                SET username = @Username, email = @Email, password_hash = @PasswordHash, roles = @Roles 
                WHERE id = @Id";

            var result = await connection.ExecuteAsync(query, new
            {
                user.Id,
                user.Username,
                user.Email,
                user.PasswordHash,
                Roles = user.Roles.Length > 0 ? string.Join(",", user.Roles) : "User"
            });

            return result > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = "DELETE FROM users WHERE id = @Id";
            var result = await connection.ExecuteAsync(query, new { Id = id });
            return result > 0;
        }

        public async Task<bool> ExistsAsync(string username)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = "SELECT COUNT(1) FROM users WHERE username = @Username";
            var count = await connection.ExecuteScalarAsync<int>(query, new { Username = username });
            return count > 0;
        }
    }
}
