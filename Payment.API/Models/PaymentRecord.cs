namespace Payment.API.Models
{
    public class PaymentRecord
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } //  "Pending" "Success", "Failed"
        public DateTime CreatedAt { get; set; }
    }
}
