using Dapper;
using Order.API.Models;
using Order.API;
namespace Order.API.Repositories
{
    public class OrderRepository
    {
        private readonly DapperContext _context;

        public OrderRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Orders>> GetAllAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Orders>("SELECT * FROM Orders");
        }

        public async Task<Orders?> GetByIdAsync(Guid id)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Orders>("SELECT * FROM Orders WHERE Id = @Id", new { Id = id });
        }

        public async Task CreateAsync(Orders order)
        {
            using var connection = _context.CreateConnection();
            var sql = @"INSERT INTO Orders (Id, ProductId, Quantity, TotalPrice, OrderDate)
                    VALUES (@Id, @ProductId, @Quantity, @TotalPrice, @OrderDate)";
            await connection.ExecuteAsync(sql, order);
        }
    }
}

