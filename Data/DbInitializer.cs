using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using BDTechMarket.Models; // custom ApplicationUser model er jonno
using System;
using System.Threading.Tasks;

namespace BDTechMarket.Data
{
    /// <summary>
    /// DbInitializer: Marketplace-er Identity framework setup kore. 
    /// Roles create kora ebong primary Administrator account initialize korai er prodhan kaj.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            try
            {
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                // Note: IdentityUser er poriborte amader custom ApplicationUser use kora hoyeche
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                // 1. Roles Definition
                string[] roleNames = { "Admin", "User" };

                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // 2. Default Administrator Configuration
                const string adminEmail = "admin@bdtechmarket.com";
                const string adminPassword = "Admin@123";

                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FullName = "System Admin", // Custom property
                        EmailConfirmed = true,
                        PhoneNumber = "01700000000"
                    };

                    var result = await userManager.CreateAsync(user, adminPassword);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Admin");
                    }
                }
            }
            catch (Exception ex)
            {
                // In production, log this error to a file or cloud service
                throw new Exception("Identity seeding fail hoyeche. System security-r jonno eta check kora dorkar.", ex);
            }
        }
    }
}