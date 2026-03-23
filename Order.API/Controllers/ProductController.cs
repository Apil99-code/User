using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order.API.Models;
using Order.API.Services;
namespace Order.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(ProductService repo) : ControllerBase
    {
        [HttpGet]
        public async Task<IEnumerable<Product>> GetAll() => await repo.GetAllProducts();

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await repo.GetProduct(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            var id  =await repo.AddProduct(product);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(Guid id, [FromBody] int change)
        {
            await repo.UpdateStock(id, change);
            return NoContent();
        }
    }
}
