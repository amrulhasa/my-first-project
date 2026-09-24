using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BDTechMarket.Data;
using BDTechMarket.Models;
using BDTechMarket.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace BDTechMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.PendingOrders = await _context.Orders
                .CountAsync(o => o.OrderStatus == "Pending" || o.OrderStatus == "Processing");
            ViewBag.DeliveredOrders = await _context.Orders
                .CountAsync(o => o.OrderStatus == "Delivered");
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.OrderStatus == "Delivered")
                .SumAsync(o => (decimal?)o.OrderTotal) ?? 0m;

            // Recent orders dashboard-e dekhano hobe
            var recentOrders = await _context.Orders
                .Include(o => o.ApplicationUser) 
                .OrderByDescending(o => o.OrderDate)
                .Take(10) 
                .AsNoTracking()
                .ToListAsync();

            return View(recentOrders);
        }

        // GET: Admin/AdminOrders (Missing method fixed)
        public async Task<IActionResult> AdminOrders()
        {
            var allOrders = await _context.Orders
                .Include(o => o.ApplicationUser)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(allOrders);
        }

        // GET: Admin/SalesSummary
        public async Task<IActionResult> SalesSummary()
        {
            var summary = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order!.OrderStatus == "Delivered")
                .GroupBy(od => od.Product!.Name)
                .Select(g => new SalesSummaryVM 
                { 
                    ProductName = g.Key ?? "N/A", 
                    TotalSold = g.Sum(x => x.Count),
                    TotalRevenue = g.Sum(x => (decimal)x.Count * x.Price) 
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();
            
            return View(summary);
        }

        // POST: Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.OrderStatus = newStatus;
            if (newStatus == "Delivered") order.PaymentStatus = "Completed";

            _context.Update(order);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Order #{orderId} status updated to {newStatus}!";
            return RedirectToAction(nameof(AdminOrders));
        }
    }
}