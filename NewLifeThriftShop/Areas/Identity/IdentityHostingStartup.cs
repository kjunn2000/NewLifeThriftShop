using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewLifeThriftShop.Areas.Identity.Data;
using NewLifeThriftShop.Data;

[assembly: HostingStartup(typeof(NewLifeThriftShop.Areas.Identity.IdentityHostingStartup))]
namespace NewLifeThriftShop.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<NewLifeThriftShopContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("NewLifeThriftShopContextConnection")));

                services.AddDefaultIdentity<NewLifeThriftShopUser>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<NewLifeThriftShopContext>();
            });
        }
    }
}