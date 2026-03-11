using Microsoft.AspNetCore.Mvc;
using BDTechMarket.Models;
using BDTechMarket.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BDTechMarket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // DisplayOrder bad diye Id diye sort kora holo performance er jonno
            var latestProducts = await _db.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id) 
                .Take(8)
                .AsNoTracking()
                .ToListAsync();

            return View(latestProducts);
        }

        public async Task<IActionResult> Products(string searchString, int? categoryId)
        {
            var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.CurrentSearch = searchString;

            var productsQuery = _db.Products
                .Include(p => p.Category)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => 
                    p.Name.Contains(searchString) || 
                    (p.Description != null && p.Description.Contains(searchString))
                );
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
            }

            return View(await productsQuery.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}