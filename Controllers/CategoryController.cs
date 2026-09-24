using BDTechMarket.Data;
using BDTechMarket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BDTechMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            int nextDisplayOrder =
                (await _context.Categories
                    .Select(c => (int?)c.DisplayOrder)
                    .MaxAsync() ?? 0) + 1;

            return View(new Category
            {
                DisplayOrder = Math.Min(nextDisplayOrder, 100)
            });
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Category category)
        {
            category.Name =
                category.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "Category name is required."
                );
            }

            bool duplicateName =
                await _context.Categories.AnyAsync(c =>
                    c.Name.ToLower() == category.Name.ToLower());

            if (duplicateName)
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "A category with this name already exists."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Category created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0)
                return NotFound();

            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Category category)
        {
            if (id != category.Id)
                return NotFound();

            category.Name =
                category.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "Category name is required."
                );
            }

            bool duplicateName =
                await _context.Categories.AnyAsync(c =>
                    c.Id != id &&
                    c.Name.ToLower() == category.Name.ToLower());

            if (duplicateName)
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "A category with this name already exists."
                );
            }

            var existingCategory =
                await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == id);

            if (existingCategory == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                category.Products =
                    await _context.Products
                        .Where(p => p.CategoryId == id)
                        .ToListAsync();

                return View(category);
            }

            existingCategory.Name = category.Name;
            existingCategory.DisplayOrder =
                category.DisplayOrder;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Category updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            int productCount =
                await _context.Products
                    .CountAsync(p => p.CategoryId == id);

            if (productCount > 0)
            {
                TempData["Error"] =
                    $"Cannot delete '{category.Name}' because it contains {productCount} product(s).";

                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Category deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}