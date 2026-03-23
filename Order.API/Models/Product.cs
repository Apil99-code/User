namespace Order.API.Models
{
    public class Product
    {
        public Guid Id { get; set; }
        public string ProductDescription { get; set; }
        public decimal Rate { get; set; }
        public int Stock { get; set; }

    }
}
