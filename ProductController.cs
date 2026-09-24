using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BDTechMarket.Data;
using BDTechMarket.Models;
using Microsoft.AspNetCore.Authorization;

namespace BDTechMarket.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Product/Index (Search & Filter Logic)
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            var productsQuery = _context.Products.Include(p => p.Category).AsNoTracking().AsQueryable();

            // Category list for dropdown
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchString) || 
                                                        (p.Description != null && p.Description.Contains(searchString)));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
            }

            return View(await productsQuery.ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                if (product.ImageFile != null)
                {
                    product.ImageUrl = await SaveFile(product.ImageFile);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product added successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }
        
        #region File Operations & Helpers

        private async Task<string> SaveFile(IFormFile file)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string productPath = Path.Combine(wwwRootPath, "images", "products");

            if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);

            using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return "/images/products/" + fileName;
        }

        private void DeletePhysicalFile(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
        }

        private async Task<bool> ProductExists(int id) => await _context.Products.AnyAsync(e => e.Id == id);

        #endregion
    }
}