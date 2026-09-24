using BDTechMarket.Data;
using BDTechMarket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BDTechMarket.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;

        private const decimal DeliveryFee = 60.00m;

        private static readonly string[] AllowedPaymentMethods =
        {
            "COD"
        };

        private static readonly string[] AllowedOrderStatuses =
        {
            "Pending",
            "Processing",
            "Shipped",
            "Delivered",
            "Cancelled"
        };

        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        // =========================================================
        // PLACE ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            Order order,
            string? PaymentMethod)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // -----------------------------------------------------
            // Validate Payment Method
            // -----------------------------------------------------

            PaymentMethod = PaymentMethod?.Trim();

            if (string.IsNullOrWhiteSpace(PaymentMethod) ||
                !AllowedPaymentMethods.Contains(
                    PaymentMethod,
                    StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Selected payment method is not available.";

                return RedirectToAction("Checkout", "Cart");
            }

            // Normalize payment method
            PaymentMethod = "COD";

            // -----------------------------------------------------
            // Load Current User
            // -----------------------------------------------------

            var currentUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser == null)
            {
                return Challenge();
            }

            // -----------------------------------------------------
            // Load Cart
            // -----------------------------------------------------

            var cartItems = await _db.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["Error"] =
                    "Your cart is empty. Add products before placing an order.";

                return RedirectToAction("Index", "Cart");
            }

            // -----------------------------------------------------
            // Validate Cart + Stock
            // -----------------------------------------------------

            foreach (var cartItem in cartItems)
            {
                if (cartItem.Product == null)
                {
                    TempData["Error"] =
                        "One or more products in your cart are no longer available.";

                    return RedirectToAction("Index", "Cart");
                }

                if (!cartItem.Product.IsActive)
                {
                    TempData["Error"] =
                        $"{cartItem.Product.Name} is no longer available.";

                    return RedirectToAction("Index", "Cart");
                }

                if (cartItem.Count < 1)
                {
                    TempData["Error"] =
                        "Invalid quantity found in your cart.";

                    return RedirectToAction("Index", "Cart");
                }

                if (cartItem.Count > 100)
                {
                    TempData["Error"] =
                        "Maximum quantity allowed for a product is 100.";

                    return RedirectToAction("Index", "Cart");
                }

                if (cartItem.Count > cartItem.Product.StockQuantity)
                {
                    TempData["Error"] =
                        $"Insufficient stock for {cartItem.Product.Name}. " +
                        $"Available stock: {cartItem.Product.StockQuantity}.";

                    return RedirectToAction("Index", "Cart");
                }
            }

            // -----------------------------------------------------
            // Start Database Transaction
            // -----------------------------------------------------

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                // -------------------------------------------------
                // Re-check stock immediately before modifying DB
                // -------------------------------------------------

                foreach (var cartItem in cartItems)
                {
                    var product = await _db.Products
                        .FirstOrDefaultAsync(p =>
                            p.Id == cartItem.ProductId);

                    if (product == null ||
                        !product.IsActive)
                    {
                        throw new InvalidOperationException(
                            "A product in the cart is no longer available.");
                    }

                    if (cartItem.Count > product.StockQuantity)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for {product.Name}.");
                    }
                }

                // -------------------------------------------------
                // Calculate subtotal from DATABASE prices
                // Never trust submitted OrderTotal
                // -------------------------------------------------

                decimal subtotal = 0m;

                foreach (var cartItem in cartItems)
                {
                    var product = await _db.Products
                        .FirstAsync(p =>
                            p.Id == cartItem.ProductId);

                    subtotal += product.Price * cartItem.Count;
                }

                var finalTotal = subtotal + DeliveryFee;

                // -------------------------------------------------
                // Create Order
                // -------------------------------------------------

                var newOrder = new Order
                {
                    // Server-controlled
                    UserId = userId,
                    OrderDate = DateTime.Now,

                    // Customer information
                    FullName = string.IsNullOrWhiteSpace(order.FullName)
                        ? currentUser.FullName ?? string.Empty
                        : order.FullName.Trim(),

                    Phone = string.IsNullOrWhiteSpace(order.Phone)
                        ? currentUser.PhoneNumber ?? string.Empty
                        : order.Phone.Trim(),

                    Email = string.IsNullOrWhiteSpace(order.Email)
                        ? currentUser.Email
                        : order.Email.Trim(),

                    Address = order.Address?.Trim() ?? string.Empty,

                    // Server-controlled payment information
                    PaymentMethod = PaymentMethod,
                    PaymentStatus = "Pending",

                    // Server-controlled order information
                    OrderStatus = "Pending",

                    DeliveryFee = DeliveryFee,
                    OrderTotal = finalTotal,

                    // COD reference
                    TransactionId =
                        "COD-" +
                        Guid.NewGuid()
                            .ToString("N")
                            .Substring(0, 8)
                            .ToUpperInvariant()
                };

                // -------------------------------------------------
                // Validate Required Customer Information
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(newOrder.FullName))
                {
                    ModelState.AddModelError(
                        nameof(Order.FullName),
                        "Full name is required.");
                }

                if (string.IsNullOrWhiteSpace(newOrder.Phone))
                {
                    ModelState.AddModelError(
                        nameof(Order.Phone),
                        "Phone number is required.");
                }

                if (string.IsNullOrWhiteSpace(newOrder.Address))
                {
                    ModelState.AddModelError(
                        nameof(Order.Address),
                        "Shipping address is required.");
                }

                if (!ModelState.IsValid)
                {
                    await transaction.RollbackAsync();

                    TempData["Error"] =
                        "Please provide all required customer information.";

                    return RedirectToAction("Checkout", "Cart");
                }

                // -------------------------------------------------
                // Save Order
                // -------------------------------------------------

                _db.Orders.Add(newOrder);

                await _db.SaveChangesAsync();

                // -------------------------------------------------
                // Create Order Details + Decrease Stock
                // -------------------------------------------------

                foreach (var cartItem in cartItems)
                {
                    var product = await _db.Products
                        .FirstAsync(p =>
                            p.Id == cartItem.ProductId);

                    // Final stock protection
                    if (product.StockQuantity < cartItem.Count)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for {product.Name}.");
                    }

                    // Snapshot the CURRENT product price
                    var orderDetail = new OrderDetail
                    {
                        OrderId = newOrder.Id,
                        ProductId = product.Id,
                        Count = cartItem.Count,
                        Price = product.Price
                    };

                    _db.OrderDetails.Add(orderDetail);

                    // Decrease stock
                    product.StockQuantity -= cartItem.Count;

                    // Automatically deactivate when stock reaches zero
                    if (product.StockQuantity == 0)
                    {
                        product.IsActive = false;
                    }

                    product.UpdatedAt = DateTime.Now;
                }

                // -------------------------------------------------
                // Remove Cart Items
                // -------------------------------------------------

                _db.CartItems.RemoveRange(cartItems);

                await _db.SaveChangesAsync();

                // -------------------------------------------------
                // Commit Transaction
                // -------------------------------------------------

                await transaction.CommitAsync();

                TempData["Success"] =
                    $"Order #{newOrder.Id} placed successfully! " +
                    "Thank you for shopping with BDTechMarket.";

                return RedirectToAction(nameof(MyOrders));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "We could not complete your order. " +
                    "Your cart and stock were not changed.";

                return RedirectToAction("Checkout", "Cart");
            }
        }

        // =========================================================
        // MY ORDERS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            return View(orders);
        }

        // =========================================================
        // ORDER DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o =>
                    o.Id == id &&
                    o.UserId == userId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // =========================================================
        // ORDER STATUS VALIDATION HELPER
        // =========================================================

        private static bool IsValidOrderStatus(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) &&
                   AllowedOrderStatuses.Contains(
                       status,
                       StringComparer.OrdinalIgnoreCase);
        }
    }
}