namespace NewLifeThriftShop.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }

        public string ProductId { get; set; }

        public int Quantity { get; set; }

        public int OrderId { get; set; }
    }
}
