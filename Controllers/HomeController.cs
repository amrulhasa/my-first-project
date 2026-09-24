using Microsoft.AspNetCore.Mvc;
using BDTechMarket.Models;
using BDTechMarket.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BDTechMarket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        // =========================================================
        // HOME PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Homepage should only show active products.
            var latestProducts = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .AsNoTracking()
                .ToListAsync();

            return View(latestProducts);
        }

        // =========================================================
        // LEGACY PRODUCTS ROUTE
        // =========================================================
        //
        // Old URL:
        // /Home/Products
        //
        // New main Product page:
        // /Product/Index
        //
        // Existing links will continue to work, but users
        // will be redirected to the new ProductController.
        // =========================================================

        [HttpGet]
        public IActionResult Products(
            string? searchString,
            int? categoryId,
            string? sortOrder,
            string? status,
            int pageNumber = 1)
        {
            return RedirectToAction(
                "Index",
                "Product",
                new
                {
                    searchString,
                    categoryId,
                    sortOrder,
                    status,
                    pageNumber
                });
        }

        // =========================================================
        // LEGACY PRODUCT DETAILS ROUTE
        // =========================================================
        //
        // Old URL:
        // /Home/Details/5
        //
        // New product details:
        // /Product/Details/5
        // =========================================================

        [HttpGet]
        public IActionResult Details(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            return RedirectToAction(
                "Details",
                "Product",
                new
                {
                    id
                });
        }

        // =========================================================
        // PRIVACY
        // =========================================================

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        // =========================================================
        // ERROR
        // =========================================================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                });
        }
    }
}