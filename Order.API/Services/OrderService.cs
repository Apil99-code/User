using Order.API.DTOs;
using Order.API.Models;
using Order.API.Repositories;
using Order.API.Repository; 

namespace Order.API.Services;

public class OrderService
{
    private readonly OrderRepository _repo;
    private readonly ProductRepository _productRepo; 

    public OrderService(OrderRepository repo, ProductRepository productRepo, IHttpClientFactory httpClientFactory)
    {
        _repo = repo;
        _productRepo = productRepo;
    }

    public async Task<Orders> CreateOrderAsync(OrderDto dto)
    {
        var product = await _productRepo.GetByIdAsync(dto.ProductId);

        if (product == null)
            throw new Exception("Product not found");

        if (product.Stock < dto.Quantity)
            throw new Exception("Not enough stock");
        var order = new Orders
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            TotalPrice = dto.Quantity * product.Rate,
            OrderDate = DateTime.UtcNow
        };

        await _repo.CreateAsync(order);

        await _productRepo.UpdateStockAsync(dto.ProductId, -dto.Quantity);

        return order;
    }

    public Task<Orders?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

    public Task<IEnumerable<Orders>> GetAllAsync() => _repo.GetAllAsync();
}