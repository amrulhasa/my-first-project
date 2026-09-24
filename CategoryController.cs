using Microsoft.AspNetCore.Mvc;
using BDTechMarket.Data;
using BDTechMarket.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BDTechMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Category/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Shajiye ana hoyeche DisplayOrder onujayi
            var categories = await _context.Categories
                .Include(c => c.Products) 
                .OrderBy(c => c.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();
                
            return View(categories);
        }

        // GET: Category/Create
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid) return View(category);

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Category created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (!ModelState.IsValid) return View(category);

            _context.Update(category);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Category updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            if (category.Products.Any())
            {
                TempData["Error"] = $"Cannot delete '{category.Name}' because it contains {category.Products.Count} products!";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Category removed successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}