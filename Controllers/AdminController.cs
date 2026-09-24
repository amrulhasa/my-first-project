using BDTechMarket.Data;
using BDTechMarket.Models;
using BDTechMarket.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BDTechMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly HashSet<string> AllowedStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Pending",
                "Approved",
                "Processing",
                "Shipped",
                "Delivered",
                "Cancelled"
            };

        public AdminController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // DASHBOARD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalOrders =
                await _context.Orders.CountAsync();

            ViewBag.PendingOrders =
                await _context.Orders.CountAsync(o =>
                    o.OrderStatus == "Pending" ||
                    o.OrderStatus == "Approved" ||
                    o.OrderStatus == "Processing");

            ViewBag.DeliveredOrders =
                await _context.Orders.CountAsync(o =>
                    o.OrderStatus == "Delivered");

            ViewBag.TotalRevenue =
                await _context.Orders
                    .Where(o => o.OrderStatus == "Delivered")
                    .SumAsync(o => (decimal?)o.OrderTotal)
                    ?? 0m;

            var recentOrders =
                await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .AsNoTracking()
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync();

            return View(recentOrders);
        }

        // =========================================================
        // ADMIN ORDERS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AdminOrders()
        {
            var allOrders =
                await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .AsNoTracking()
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

            return View(allOrders);
        }

        // =========================================================
        // SALES SUMMARY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SalesSummary()
        {
            var summary =
                await _context.OrderDetails
                    .Include(od => od.Product)
                    .Include(od => od.Order)
                    .Where(od =>
                        od.Order != null &&
                        od.Order.OrderStatus == "Delivered")
                    .GroupBy(od => od.Product!.Name)
                    .Select(g => new SalesSummaryVM
                    {
                        ProductName = g.Key ?? "N/A",
                        TotalSold = g.Sum(x => x.Count),
                        TotalRevenue =
                            g.Sum(x =>
                                (decimal)x.Count * x.Price)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToListAsync();

            return View(summary);
        }

        // =========================================================
        // UPDATE ORDER STATUS
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(
            int orderId,
            string? newStatus)
        {
            var order =
                await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            newStatus =
                newStatus?.Trim();

            if (string.IsNullOrWhiteSpace(newStatus))
            {
                TempData["Error"] =
                    "Please select a valid order status.";

                return RedirectToAction(
                    nameof(AdminOrders));
            }

            if (!AllowedStatuses.Contains(newStatus))
            {
                TempData["Error"] =
                    "Invalid order status.";

                return RedirectToAction(
                    nameof(AdminOrders));
            }

            // Prevent reopening final states
            if (order.OrderStatus == "Delivered" &&
                !newStatus.Equals(
                    "Delivered",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    $"Order #{orderId} is already delivered and cannot be moved back.";

                return RedirectToAction(
                    nameof(AdminOrders));
            }

            if (order.OrderStatus == "Cancelled" &&
                !newStatus.Equals(
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    $"Order #{orderId} is already cancelled.";

                return RedirectToAction(
                    nameof(AdminOrders));
            }

            order.OrderStatus = newStatus;

            if (newStatus.Equals(
                    "Delivered",
                    StringComparison.OrdinalIgnoreCase))
            {
                order.PaymentStatus = "Completed";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Order #{orderId} status updated to {newStatus}.";

            return RedirectToAction(
                nameof(AdminOrders));
        }
    }
}