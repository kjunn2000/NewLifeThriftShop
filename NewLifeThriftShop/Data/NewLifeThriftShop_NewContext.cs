using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewLifeThriftShop.Models;

namespace NewLifeThriftShop.Data
{
    public class NewLifeThriftShop_NewContext : DbContext
    {
        public NewLifeThriftShop_NewContext (DbContextOptions<NewLifeThriftShop_NewContext> options)
            : base(options)
        {
        }

        public DbSet<NewLifeThriftShop.Models.Product> Product { get; set; }

        public DbSet<NewLifeThriftShop.Models.CartItem> CartItem { get; set; }
    }
}
