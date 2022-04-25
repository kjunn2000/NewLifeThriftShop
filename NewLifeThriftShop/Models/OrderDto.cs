using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewLifeThriftShop.Models
{
    public class OrderDto
    {
        [Key]
        public string OrderId { get; set; }

        public string CustomerId { get; set; }

        public double Price { get; set; }

        public string CreatedAt { get; set; }
    }
}
