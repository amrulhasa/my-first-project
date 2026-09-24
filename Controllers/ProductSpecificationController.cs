using BDTechMarket.Data;
using BDTechMarket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BDTechMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductSpecificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductSpecificationController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(int productId)
        {
            if (productId <= 0)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.BrandEntity)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound();

            var specifications = await _context.ProductSpecifications
                .Where(s => s.ProductId == productId)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Product = product;

            return View(specifications);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(int productId)
        {
            if (productId <= 0)
                return NotFound();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound();

            var nextOrder =
                await _context.ProductSpecifications
                    .Where(s => s.ProductId == productId)
                    .Select(s => (int?)s.DisplayOrder)
                    .MaxAsync() ?? 0;

            var specification = new ProductSpecification
            {
                ProductId = productId,
                DisplayOrder = nextOrder + 1
            };

            ViewBag.Product = product;

            return View(specification);
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ProductSpecification specification)
        {
            specification.Name =
                specification.Name?.Trim() ?? string.Empty;

            specification.Value =
                specification.Value?.Trim() ?? string.Empty;

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == specification.ProductId);

            if (product == null)
                return NotFound();

            var duplicate =
                await _context.ProductSpecifications
                    .AnyAsync(s =>
                        s.ProductId == specification.ProductId &&
                        s.Name.ToLower() ==
                        specification.Name.ToLower());

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(specification.Name),
                    "This specification already exists for this product.");
            }

            if (specification.DisplayOrder < 1)
            {
                specification.DisplayOrder = 1;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Product = product;

                return View(specification);
            }

            specification.CreatedAt = DateTime.Now;
            specification.UpdatedAt = null;

            _context.ProductSpecifications.Add(specification);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Specification '{specification.Name}' added successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    productId = specification.ProductId
                });
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0)
                return NotFound();

            var specification =
                await _context.ProductSpecifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);

            if (specification == null)
                return NotFound();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == specification.ProductId);

            if (product == null)
                return NotFound();

            ViewBag.Product = product;

            return View(specification);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ProductSpecification specification)
        {
            if (id != specification.Id)
                return NotFound();

            var existing =
                await _context.ProductSpecifications
                    .FirstOrDefaultAsync(s => s.Id == id);

            if (existing == null)
                return NotFound();

            specification.Name =
                specification.Name?.Trim() ?? string.Empty;

            specification.Value =
                specification.Value?.Trim() ?? string.Empty;

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == existing.ProductId);

            if (product == null)
                return NotFound();

            var duplicate =
                await _context.ProductSpecifications
                    .AnyAsync(s =>
                        s.Id != id &&
                        s.ProductId == existing.ProductId &&
                        s.Name.ToLower() ==
                        specification.Name.ToLower());

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(specification.Name),
                    "This specification already exists for this product.");
            }

            if (specification.DisplayOrder < 1)
            {
                specification.DisplayOrder = 1;
            }

            if (!ModelState.IsValid)
            {
                specification.ProductId =
                    existing.ProductId;

                ViewBag.Product = product;

                return View(specification);
            }

            existing.Name = specification.Name;

            existing.Value = specification.Value;

            existing.DisplayOrder =
                specification.DisplayOrder;

            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Specification '{existing.Name}' updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    productId = existing.ProductId
                });
        }

        // =========================================================
        // DELETE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var specification =
                await _context.ProductSpecifications
                    .FirstOrDefaultAsync(s => s.Id == id);

            if (specification == null)
                return NotFound();

            var productId =
                specification.ProductId;

            var specificationName =
                specification.Name;

            _context.ProductSpecifications.Remove(
                specification);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Specification '{specificationName}' deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    productId
                });
        }
    }
}