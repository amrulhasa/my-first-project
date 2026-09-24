using Stripe;
using BDTechMarket.Data;
using BDTechMarket.Models;
using BDTechMarket.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// SERVICES
// =========================================================

builder.Services.AddControllersWithViews();

// =========================================================
// DATABASE
// =========================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

// =========================================================
// IDENTITY
// =========================================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // -------------------------
    // Password
    // -------------------------

    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // -------------------------
    // User
    // -------------------------

    options.User.RequireUniqueEmail = true;

    // -------------------------
    // Lockout
    // -------------------------

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(15);

    // -------------------------
    // Email / Account confirmation
    // -------------------------

    // Email verification is NOT required for login.
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// =========================================================
// APPLICATION COOKIE
// =========================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";

    options.Cookie.HttpOnly = true;

    options.ExpireTimeSpan =
        TimeSpan.FromDays(5);

    options.SlidingExpiration = true;
});

// =========================================================
// HTTP CONTEXT
// =========================================================

builder.Services.AddHttpContextAccessor();

// =========================================================
// SESSION
// =========================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =========================================================
// EMAIL SERVICE
// =========================================================

// Still needed for Forgot Password.
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// =========================================================
// BUILD APP
// =========================================================

var app = builder.Build();

// =========================================================
// HTTP PIPELINE
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// =========================================================
// STRIPE
// =========================================================

StripeConfiguration.ApiKey =
    builder.Configuration
        .GetSection("Stripe:SecretKey")
        .Get<string>();

// =========================================================
// AUTHENTICATION / AUTHORIZATION
// =========================================================

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// =========================================================
// DATABASE + ROLE SEED
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context =
            services.GetRequiredService<ApplicationDbContext>();

        context.Database.Migrate();

        var roleManager =
            services.GetRequiredService<
                RoleManager<IdentityRole>>();

        var userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();

        // -------------------------
        // Roles
        // -------------------------

        string[] roleNames =
        {
            "Admin",
            "User"
        };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(roleName));
            }
        }

        // -------------------------
        // Development Admin
        // -------------------------

        var adminEmail =
            "admin@bdtechmarket.com";

        var adminUser =
            await userManager.FindByEmailAsync(
                adminEmail);

        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true
            };

            var createAdmin =
                await userManager.CreateAsync(
                    user,
                    "Admin@123");

            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    user,
                    "Admin");
            }
        }
        else
        {
            // Make sure existing admin has Admin role.

            if (!await userManager.IsInRoleAsync(
                    adminUser,
                    "Admin"))
            {
                await userManager.AddToRoleAsync(
                    adminUser,
                    "Admin");
            }

            // Keep development admin confirmed.
            if (!adminUser.EmailConfirmed)
            {
                adminUser.EmailConfirmed = true;

                await userManager.UpdateAsync(
                    adminUser);
            }
        }
    }
    catch (Exception ex)
    {
        var logger =
            services.GetRequiredService<
                ILogger<Program>>();

        logger.LogError(
            ex,
            "Database seeding failed.");
    }
}

// =========================================================
// ROUTING
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();