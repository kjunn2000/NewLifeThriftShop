namespace NewLifeThriftShop.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string Category { get; set; }

        public double Price { get; set; }

        public int Quantity { get; set; }

        public string ImgExt { get; set; }

        public string SellerId { get; set; }
    }
}
