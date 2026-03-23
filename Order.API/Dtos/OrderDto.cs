namespace Order.API.DTOs
{
    public class OrderDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
