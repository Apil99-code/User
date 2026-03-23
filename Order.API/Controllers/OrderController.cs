using Microsoft.AspNetCore.Mvc;
using Order.API.DTOs;
using Order.API.Services;

[ApiController]
[Route("api/[controller]")]
public class OrderController(OrderService service) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> CreateOrder(OrderDto dto)
    {
        try
        {
            var order = await service.CreateOrderAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await service.GetByIdAsync(id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await service.GetAllAsync();
        return Ok(orders);
    }
}