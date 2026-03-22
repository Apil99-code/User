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

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<AppUser?> GetByIdAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = "SELECT id, username, email, password_hash, roles, created_at FROM users WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<AppUser>(query, new { Id = id });
        }

        public async Task<AppUser?> GetByUsernameAsync(string username)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = "SELECT id, username, email, password_hash, roles, created_at FROM users WHERE username = @Username";
            return await connection.QueryFirstOrDefaultAsync<AppUser>(query, new { Username = username });
        }

        public async Task<IEnumerable<AppUser>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = "SELECT id, username, email, password_hash, roles, created_at FROM users ORDER BY created_at DESC";
            return await connection.QueryAsync<AppUser>(query);
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
