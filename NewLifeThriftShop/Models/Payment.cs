using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewLifeThriftShop.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public string CustomerId { get; set; }

        public string PaymentMethod { get; set; }

        public double Price { get; set; }

        [ForeignKey("OrderId")]
        public string OrderId { get; set; }
        public virtual Order Order { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime CreatedAt { get; set; }
    }
}
