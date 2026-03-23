using Microsoft.AspNetCore.Mvc;
using Payment.API.DTOs;
using Payment.API.Models;
using Payment.API.Services;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(PaymentService paymentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] PaymentRequestDto request)
    {
        try
        {
            var result = await paymentService.ProcessPaymentAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}