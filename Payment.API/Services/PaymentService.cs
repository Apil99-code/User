using Payment.API.DTOs;
using Payment.API.Models;
using Payment.API.Repositories;

namespace Payment.API.Services;

public class PaymentService(PaymentRepository repo)
{
    public async Task<PaymentRecord> ProcessPaymentAsync(PaymentRequestDto dto)
    {
        bool paymentSuccessful = dto.Amount > 0;

        var payment = new PaymentRecord
        {
            Id = Guid.NewGuid(),
            OrderId = dto.OrderId,
            Amount = dto.Amount,
            Status = paymentSuccessful ? "Success" : "Failed",
            CreatedAt = DateTime.UtcNow
        };

        await repo.CreateAsync(payment);

        if (!paymentSuccessful)
        {
            throw new Exception("Payment gateway rejected the transaction.");
        }

        return payment;
    }
}