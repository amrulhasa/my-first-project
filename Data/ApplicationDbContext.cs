using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BDTechMarket.Models;

namespace BDTechMarket.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================================================
        // DB SETS
        // =========================================================

        public DbSet<Product> Products { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }

        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ProductSpecification> ProductSpecifications { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // =====================================================
            // PRODUCT
            // =====================================================

            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Entity<Product>()
                .Property(p => p.Description)
                .HasMaxLength(2000);

            builder.Entity<Product>()
                .Property(p => p.ImageUrl)
                .HasMaxLength(500);

            builder.Entity<Product>()
                .Property(p => p.Brand)
                .HasMaxLength(100);

            builder.Entity<Product>()
                .Property(p => p.SKU)
                .HasMaxLength(50);

            builder.Entity<Product>()
                .Property(p => p.IsActive)
                .HasDefaultValue(true);

            builder.Entity<Product>()
                .Property(p => p.StockQuantity)
                .HasDefaultValue(0);

            builder.Entity<Product>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");


            // =====================================================
            // PRODUCT SKU - UNIQUE
            // =====================================================

            // SKU must be unique when it has a value.
            // Existing legacy products with NULL SKU are allowed.
            builder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique()
                .HasFilter("[SKU] IS NOT NULL");


            // =====================================================
            // PRODUCT → CATEGORY
            // =====================================================

            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // CART ITEM
            // =====================================================

            // A user can have only ONE cart row
            // for the same product.
            //
            // Example:
            // User A + Product 10 → one CartItem only
            //
            // This prevents duplicate cart rows.
            builder.Entity<CartItem>()
                .HasIndex(c => new
                {
                    c.UserId,
                    c.ProductId
                })
                .IsUnique();


            // =====================================================
            // CART ITEM → PRODUCT
            // =====================================================

            builder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ORDER
            // =====================================================

            builder.Entity<Order>()
                .Property(o => o.OrderTotal)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.DeliveryFee)
                .HasPrecision(18, 2);


            // =====================================================
            // ORDER → APPLICATION USER
            // =====================================================

            builder.Entity<Order>()
                .HasOne(o => o.ApplicationUser)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // ORDER DETAIL
            // =====================================================

            builder.Entity<OrderDetail>()
                .Property(od => od.Price)
                .HasPrecision(18, 2);


            // =====================================================
            // ORDER → ORDER DETAILS
            // =====================================================

            builder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // ORDER DETAIL → PRODUCT
            // =====================================================

            builder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // INDEXES
            // =====================================================

            // Faster lookup of a user's orders.
            builder.Entity<Order>()
                .HasIndex(o => new
                {
                    o.UserId,
                    o.OrderDate
                });
                builder.Entity<Brand>()
    .Property(b => b.Name)
    .IsRequired()
    .HasMaxLength(100);

builder.Entity<Brand>()
    .Property(b => b.LogoUrl)
    .HasMaxLength(500);

builder.Entity<Brand>()
    .Property(b => b.Description)
    .HasMaxLength(500);

builder.Entity<Brand>()
    .Property(b => b.IsActive)
    .HasDefaultValue(true);

builder.Entity<Brand>()
    .Property(b => b.DisplayOrder)
    .HasDefaultValue(1);

builder.Entity<Brand>()
    .Property(b => b.CreatedAt)
    .HasDefaultValueSql("GETDATE()");

builder.Entity<Brand>()
    .HasIndex(b => b.Name)
    .IsUnique();

builder.Entity<Product>()
    .HasOne(p => p.BrandEntity)
    .WithMany(b => b.Products)
    .HasForeignKey(p => p.BrandId)
    .OnDelete(DeleteBehavior.Restrict);
    builder.Entity<ProductSpecification>()
    .Property(s => s.Name)
    .IsRequired()
    .HasMaxLength(100);

builder.Entity<ProductSpecification>()
    .Property(s => s.Value)
    .IsRequired()
    .HasMaxLength(500);

builder.Entity<ProductSpecification>()
    .Property(s => s.DisplayOrder)
    .HasDefaultValue(1);

builder.Entity<ProductSpecification>()
    .Property(s => s.CreatedAt)
    .HasDefaultValueSql("GETDATE()");

builder.Entity<ProductSpecification>()
    .HasOne(s => s.Product)
    .WithMany(p => p.Specifications)
    .HasForeignKey(s => s.ProductId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<ProductSpecification>()
    .HasIndex(s => new { s.ProductId, s.Name })
    .IsUnique();

            // Faster lookup of order details.
            builder.Entity<OrderDetail>()
                .HasIndex(od => od.OrderId);
        }
    }
}