using Order.API.Models;
using Order.API.Repository;

namespace Order.API.Services
{
    public class ProductService(ProductRepository repo)
    {
        public async Task<IEnumerable<Product>> GetAllProducts()
        {
            var result = await  repo.GetAllAsync();
            if (result == null)
                throw new Exception("no Product Data found ");
            return result;
        }
        public async Task<Product> GetProduct(Guid id )
        {
            var product = await repo.GetByIdAsync(id);
            if(product== null)
                throw new Exception($"Product {id} does not exists");
            return product;
        }
        public async Task<Guid> AddProduct(Product product)
        {
            if (product == null) throw new Exception("invalid input");
            if (product.Rate < 1)
                throw new Exception("Rate can not be less then 1");
            product.Id= Guid.NewGuid();
            await repo.CreateAsync(product);
            return product.Id;
        }
        public async Task UpdateStock(Guid id, int change)
        {
            await repo.UpdateStockAsync(id, change);

        }
    }
}
