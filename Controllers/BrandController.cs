using BDTechMarket.Data;
using BDTechMarket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BDTechMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BrandController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BrandController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var brands = await _context.Brands
                .Include(b => b.Products)
                .AsNoTracking()
                .OrderBy(b => b.DisplayOrder)
                .ThenBy(b => b.Name)
                .ToListAsync();

            return View(brands);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            int nextDisplayOrder =
                (await _context.Brands
                    .Select(b => (int?)b.DisplayOrder)
                    .MaxAsync() ?? 0) + 1;

            return View(new Brand
            {
                DisplayOrder = nextDisplayOrder,
                IsActive = true
            });
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand)
        {
            brand.Name =
                brand.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(brand.Name))
            {
                ModelState.AddModelError(
                    nameof(brand.Name),
                    "Brand name is required."
                );
            }

            bool duplicate =
                await _context.Brands.AnyAsync(b =>
                    b.Name.ToLower() == brand.Name.ToLower());

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(brand.Name),
                    "A brand with this name already exists."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            brand.CreatedAt = DateTime.Now;
            brand.UpdatedAt = null;
            brand.IsActive = true;

            _context.Brands.Add(brand);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Brand '{brand.Name}' created successfully.";

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

            var brand = await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
                return NotFound();

            return View(brand);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Brand brand)
        {
            if (id != brand.Id)
                return NotFound();

            brand.Name =
                brand.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(brand.Name))
            {
                ModelState.AddModelError(
                    nameof(brand.Name),
                    "Brand name is required."
                );
            }

            bool duplicate =
                await _context.Brands.AnyAsync(b =>
                    b.Id != id &&
                    b.Name.ToLower() == brand.Name.ToLower());

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(brand.Name),
                    "A brand with this name already exists."
                );
            }

            var existingBrand =
                await _context.Brands
                    .FirstOrDefaultAsync(b => b.Id == id);

            if (existingBrand == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                brand.Products =
                    await _context.Products
                        .Where(p => p.BrandId == id)
                        .ToListAsync();

                return View(brand);
            }

            existingBrand.Name = brand.Name;
            existingBrand.LogoUrl = brand.LogoUrl;
            existingBrand.Description = brand.Description;
            existingBrand.DisplayOrder = brand.DisplayOrder;
            existingBrand.IsActive = brand.IsActive;
            existingBrand.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Brand '{existingBrand.Name}' updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DEACTIVATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id)
        {
            if (id == null || id <= 0)
                return NotFound();

            var brand = await _context.Brands
                .Include(b => b.Products)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
                return NotFound();

            return View(brand);
        }

        // =========================================================
        // DEACTIVATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(
            int id)
        {
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
                return NotFound();

            if (!brand.IsActive)
            {
                TempData["Info"] =
                    $"Brand '{brand.Name}' is already inactive.";

                return RedirectToAction(nameof(Index));
            }

            brand.IsActive = false;
            brand.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Brand '{brand.Name}' deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ACTIVATE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(
            int id)
        {
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
                return NotFound();

            brand.IsActive = true;
            brand.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Brand '{brand.Name}' activated successfully.";

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
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
                return NotFound();

            int productCount =
                await _context.Products
                    .CountAsync(p => p.BrandId == id);

            if (productCount > 0)
            {
                TempData["Error"] =
                    $"Cannot delete '{brand.Name}' because it is linked to {productCount} product(s). Deactivate it instead.";

                return RedirectToAction(nameof(Index));
            }

            _context.Brands.Remove(brand);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Brand '{brand.Name}' deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}