using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BDTechMarket.Data;
using BDTechMarket.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BDTechMarket.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrderController(ApplicationDbContext db) { _db = db; }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order, string PaymentMethod)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var cartItems = await _db.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["Error"] = "Your cart is empty! Add some gadgets first.";
                return RedirectToAction("Index", "Cart");
            }

            // Transaction ensure kore: "Hoy shob kaj hobe, na hoy kichu-i hobe na"
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // Order Setup
                order.UserId = userId;
                order.OrderDate = DateTime.Now;
                order.OrderStatus = "Pending"; // Initial Status
                order.PaymentMethod = PaymentMethod;
                order.DeliveryFee = 60.00m; 

                // Smart Transaction ID Generation
                string prefix = PaymentMethod == "COD" ? "COD-" : "TRX-";
                order.TransactionId = prefix + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                // Calculate Order Total from Snapshot prices
                order.OrderTotal = cartItems.Sum(item => item.Count * item.Product.Price) + order.DeliveryFee;

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(); // Ekhane Order ID generate hoye jay

                // Create OrderDetails (Price Snapshotting)
                foreach (var item in cartItems)
                {
                    OrderDetail detail = new()
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Count = item.Count,
                        Price = item.Product.Price // Saving current price to handle future price changes
                    };
                    _db.OrderDetails.Add(detail);
                }

                // Clear Cart
                _db.CartItems.RemoveRange(cartItems);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] = $"Order # {order.Id} placed successfully! Thank you for shopping with us.";
                return RedirectToAction(nameof(MyOrders));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Critical error during checkout. No charges were made.";
                return RedirectToAction("Index", "Cart");
            }
        }

        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _db.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}