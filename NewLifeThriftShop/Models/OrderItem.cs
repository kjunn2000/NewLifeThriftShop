using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewLifeThriftShop.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [ForeignKey("ProductId")]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public int Quantity { get; set; }

        public double Price { get; set; }

        public string Status    { get; set; }

        [ForeignKey("OrderId")]
        public string OrderId { get; set; }
        public virtual Order Order { get; set; }
    }
}
