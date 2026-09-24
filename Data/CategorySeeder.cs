using BDTechMarket.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BDTechMarket.Data
{
    /// <summary>
    /// Static utility class responsible for populating the database with 
    /// initial category data if the table is empty.
    /// </summary>
    public static class CategorySeeder
    {
        /// <summary>
        /// Seeds default categories into the database asynchronously.
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve the database context.</param>
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Ensure the database is migrated to the latest version
                // context.Database.Migrate(); 

                // Check if any categories already exist
                if (await context.Categories.AnyAsync())
                {
                    return; // Data already exists, no need to seed
                }

                // Add default categories for a Tech Market
                var defaultCategories = new List<Category>
                {
                    new Category { Name = "Mobile" },
                    new Category { Name = "Laptop" },
                    new Category { Name = "Accessories" },
                    new Category { Name = "Smart Watch" },
                    new Category { Name = "Gaming Console" },
                    new Category { Name = "Monitor" },
                    new Category { Name = "Networking" }
                };

                await context.Categories.AddRangeAsync(defaultCategories);
                await context.SaveChangesAsync();
            }
        }
    }
}