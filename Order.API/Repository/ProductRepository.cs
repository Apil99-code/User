using Order.API.Models;
using Dapper;

namespace Order.API.Repository
{
    public class ProductRepository (DapperContext context)
    {
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            using var connection = context.CreateConnection();
            var sql = "SELECT * FROM Products";
            return await connection.QueryAsync<Product>(sql);
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            using var connection = context.CreateConnection();
            var sql = "SELECT * FROM Products WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id });
        }

        public async Task CreateAsync(Product product)
        {
            using var connection = context.CreateConnection();
            var sql = "INSERT INTO Products (Id, ProductDescription, Rate, Stock) VALUES (@Id, @ProductDescription, @Rate, @Stock)";
            await connection.ExecuteAsync(sql, product);
        }

        public async Task UpdateStockAsync(Guid id, int change)
        {
            using var connection = context.CreateConnection();
            var sql = "UPDATE Products SET Stock = Stock + @Change WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id, Change = change });
        }
    }
}
