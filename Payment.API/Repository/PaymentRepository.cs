using Dapper;
using Payment.API.Models;
using System.Data;

namespace Payment.API.Repositories;

public class PaymentRepository(DapperContext context)
{
    public async Task CreateAsync(PaymentRecord payment)
    {
        using var connection = context.CreateConnection();
        var sql = @"INSERT INTO Payments (Id, OrderId, Amount, Status, CreatedAt) 
                    VALUES (@Id, @OrderId, @Amount, @Status, @CreatedAt)";

        await connection.ExecuteAsync(sql, payment);
    }

    public async Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId)
    {
        using var connection = context.CreateConnection();
        var sql = "SELECT * FROM Payments WHERE OrderId = @OrderId";
        return await connection.QueryFirstOrDefaultAsync<PaymentRecord>(sql, new { OrderId = orderId });
    }
}