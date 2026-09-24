using BDTechMarket.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BDTechMarket.Data
{
    /// <summary>
    /// ProductSeeder: Database-ke prothom bar demo product diye populate kore.
    /// CategorySeeder-er upor nirbhorshil.
    /// </summary>
    public static class ProductSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // 1. Skip if products already exist
                if (await context.Products.AnyAsync()) return;

                // 2. Lookup Categories safely
                var mobileCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Mobile");
                var laptopCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Laptop");

                // Check if categories are missing
                if (mobileCat == null || laptopCat == null) return;

                // 3. Define Demo Products
                var demoProducts = new List<Product>
                {
                    new Product 
                    { 
                        Name = "Samsung Galaxy A14", 
                        Price = 30000m, 
                        Description = "High-performance smartphone with a stunning display.",
                        CategoryId = mobileCat.Id,
                        ImageUrl = "/images/products/samsung.jpg" 
                    },
                    new Product 
                    { 
                        Name = "iPhone 13", 
                        Price = 95000m, 
                        Description = "Advanced dual-camera system and lightning-fast chip.",
                        CategoryId = mobileCat.Id,
                        ImageUrl = "/images/products/iphone.jpg" 
                    },
                    new Product 
                    { 
                        Name = "Dell Inspiron 15", 
                        Price = 75000m, 
                        Description = "Versatile laptop for work, study, and entertainment.",
                        CategoryId = laptopCat.Id,
                        ImageUrl = "/images/products/dell.jpg" 
                    }
                };

                await context.Products.AddRangeAsync(demoProducts);
                await context.SaveChangesAsync();
            }
        }
    }
}