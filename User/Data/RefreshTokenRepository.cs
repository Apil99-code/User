using Dapper;
using Npgsql;

namespace User.Data
{
    public interface IRefreshTokenRepository
    {
        Task<string?> GetAsync(string token);
        Task<bool> SaveAsync(int userId, string token);
        Task<bool> RevokeAsync(string token);
        Task<bool> ValidateAsync(int userId, string token);
        Task<bool> DeleteExpiredAsync();
    }

    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly string _connectionString;

        public RefreshTokenRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<string?> GetAsync(string token)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = @"
                SELECT token FROM refresh_tokens 
                WHERE token = @Token AND is_revoked = false AND expires_at > NOW() 
                LIMIT 1";

            return await connection.ExecuteScalarAsync<string?>(query, new { Token = token });
        }

        public async Task<bool> SaveAsync(int userId, string token)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = @"
                INSERT INTO refresh_tokens (user_id, token, expires_at, is_revoked, created_at) 
                VALUES (@UserId, @Token, NOW() + INTERVAL '7 days', false, NOW())";

            var result = await connection.ExecuteAsync(query, new { UserId = userId, Token = token });
            return result > 0;
        }

        public async Task<bool> RevokeAsync(string token)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = "UPDATE refresh_tokens SET is_revoked = true WHERE token = @Token";
            var result = await connection.ExecuteAsync(query, new { Token = token });
            return result > 0;
        }

        public async Task<bool> ValidateAsync(int userId, string token)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = @"
                SELECT COUNT(1) FROM refresh_tokens 
                WHERE user_id = @UserId AND token = @Token 
                AND is_revoked = false AND expires_at > NOW()";

            var count = await connection.ExecuteScalarAsync<int>(query, new { UserId = userId, Token = token });
            return count > 0;
        }

        public async Task<bool> DeleteExpiredAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string query = "DELETE FROM refresh_tokens WHERE expires_at <= NOW()";
            await connection.ExecuteAsync(query);
            return true;
        }
    }
}
