using BDTechMarket.Data;
using BDTechMarket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BDTechMarket.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private const long MaxFileSize = 5 * 1024 * 1024;

        private const int PageSize = 12;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ProductController(
            ApplicationDbContext context,
            IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }


        // =========================================================
        // PRODUCT LIST
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchString,
            int? categoryId,
            int? brandId,
            decimal? minPrice,
            decimal? maxPrice,
            string? stockStatus,
            string? sortOrder,
            string? status,
            int pageNumber = 1)
        {
            // =====================================================
            // PAGE VALIDATION
            // =====================================================

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }


            var isAdmin = User.IsInRole("Admin");


            // =====================================================
            // CLEAN INPUT
            // =====================================================

            searchString =
                string.IsNullOrWhiteSpace(searchString)
                    ? null
                    : searchString.Trim();

            sortOrder =
                string.IsNullOrWhiteSpace(sortOrder)
                    ? null
                    : sortOrder.Trim().ToLowerInvariant();

            stockStatus =
                string.IsNullOrWhiteSpace(stockStatus)
                    ? "all"
                    : stockStatus.Trim().ToLowerInvariant();


            // =====================================================
            // PRICE RANGE
            // =====================================================

            if (minPrice.HasValue && minPrice.Value < 0)
            {
                minPrice = 0;
            }

            if (maxPrice.HasValue && maxPrice.Value < 0)
            {
                maxPrice = 0;
            }

            // If minimum is greater than maximum,
            // swap the values instead of rebuilding the query.
            if (minPrice.HasValue &&
                maxPrice.HasValue &&
                minPrice.Value > maxPrice.Value)
            {
                var temp = minPrice.Value;

                minPrice = maxPrice.Value;
                maxPrice = temp;
            }


            // =====================================================
            // BASE QUERY
            // =====================================================

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.BrandEntity)
                .AsNoTracking()
                .AsQueryable();


            // =====================================================
            // STATUS FILTER
            // =====================================================

            if (!isAdmin)
            {
                // Customers can only see active products.
                query = query.Where(p => p.IsActive);

                status = "active";
            }
            else
            {
                status = string.IsNullOrWhiteSpace(status)
                    ? "active"
                    : status.Trim().ToLowerInvariant();

                if (status == "active")
                {
                    query = query.Where(p => p.IsActive);
                }
                else if (status == "inactive")
                {
                    query = query.Where(p => !p.IsActive);
                }

                // "all" means no active/inactive filter.
            }


            // =====================================================
            // SEARCH
            // =====================================================

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(p =>
                    // Product name
                    p.Name.Contains(searchString)

                    ||

                    // Description
                    (
                        p.Description != null &&
                        p.Description.Contains(searchString)
                    )

                    ||

                    // Legacy Brand field
                    (
                        p.Brand != null &&
                        p.Brand.Contains(searchString)
                    )

                    ||

                    // New Brand entity
                    (
                        p.BrandEntity != null &&
                        p.BrandEntity.Name.Contains(searchString)
                    )

                    ||

                    // SKU
                    (
                        p.SKU != null &&
                        p.SKU.Contains(searchString)
                    )

                    ||

                    // Product Specifications
                    p.Specifications.Any(s =>
                        s.Name.Contains(searchString) ||
                        s.Value.Contains(searchString)
                    )
                );
            }


            // =====================================================
            // CATEGORY FILTER
            // =====================================================

            if (categoryId.HasValue &&
                categoryId.Value > 0)
            {
                query = query.Where(p =>
                    p.CategoryId == categoryId.Value);
            }


            // =====================================================
            // BRAND FILTER
            // =====================================================

            if (brandId.HasValue &&
                brandId.Value > 0)
            {
                query = query.Where(p =>
                    p.BrandId == brandId.Value);
            }


            // =====================================================
            // MINIMUM PRICE
            // =====================================================

            if (minPrice.HasValue)
            {
                query = query.Where(p =>
                    p.Price >= minPrice.Value);
            }


            // =====================================================
            // MAXIMUM PRICE
            // =====================================================

            if (maxPrice.HasValue)
            {
                query = query.Where(p =>
                    p.Price <= maxPrice.Value);
            }


            // =====================================================
            // STOCK FILTER
            // =====================================================

            switch (stockStatus)
            {
                case "instock":

                    query = query.Where(p =>
                        p.StockQuantity > 0);

                    break;


                case "outofstock":

                    query = query.Where(p =>
                        p.StockQuantity <= 0);

                    break;


                case "lowstock":

                    query = query.Where(p =>
                        p.StockQuantity > 0 &&
                        p.StockQuantity <= 5);

                    break;


                case "all":
                default:

                    // No stock filter.
                    break;
            }


            // =====================================================
            // SORTING
            // =====================================================

            query = sortOrder switch
            {
                "price_asc" =>
                    query
                        .OrderBy(p => p.Price)
                        .ThenByDescending(p => p.Id),

                "price_desc" =>
                    query
                        .OrderByDescending(p => p.Price)
                        .ThenByDescending(p => p.Id),

                "name_asc" =>
                    query
                        .OrderBy(p => p.Name)
                        .ThenByDescending(p => p.Id),

                "name_desc" =>
                    query
                        .OrderByDescending(p => p.Name)
                        .ThenByDescending(p => p.Id),

                "stock_asc" =>
                    query
                        .OrderBy(p => p.StockQuantity)
                        .ThenByDescending(p => p.Id),

                "stock_desc" =>
                    query
                        .OrderByDescending(p => p.StockQuantity)
                        .ThenByDescending(p => p.Id),

                "oldest" =>
                    query
                        .OrderBy(p => p.CreatedAt)
                        .ThenBy(p => p.Id),

                "newest" =>
                    query
                        .OrderByDescending(p => p.CreatedAt)
                        .ThenByDescending(p => p.Id),

                _ =>
                    query
                        .OrderByDescending(p => p.Id)
            };


            // =====================================================
            // TOTAL PRODUCTS
            // =====================================================

            var totalProducts =
                await query.CountAsync();


            // =====================================================
            // TOTAL PAGES
            // =====================================================

            var totalPages =
                (int)Math.Ceiling(
                    totalProducts /
                    (double)PageSize);


            if (totalPages > 0 &&
                pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }


            // =====================================================
            // PAGINATION
            // =====================================================

            var products =
                await query
                    .Skip(
                        (pageNumber - 1) *
                        PageSize)
                    .Take(PageSize)
                    .ToListAsync();


            // =====================================================
            // CATEGORY DROPDOWN
            // =====================================================

            ViewBag.Categories =
                new SelectList(
                    await _context.Categories
                        .AsNoTracking()
                        .OrderBy(c =>
                            c.DisplayOrder)
                        .ThenBy(c =>
                            c.Name)
                        .ToListAsync(),
                    "Id",
                    "Name",
                    categoryId);


            // =====================================================
            // BRAND DROPDOWN
            // =====================================================

            ViewBag.Brands =
                new SelectList(
                    await _context.Brands
                        .AsNoTracking()
                        .Where(b =>
                            b.IsActive)
                        .OrderBy(b =>
                            b.DisplayOrder)
                        .ThenBy(b =>
                            b.Name)
                        .ToListAsync(),
                    "Id",
                    "Name",
                    brandId);


            // =====================================================
            // VIEW DATA
            // =====================================================

            ViewBag.SearchString =
                searchString;

            ViewBag.CategoryId =
                categoryId;

            ViewBag.BrandId =
                brandId;

            ViewBag.MinPrice =
                minPrice;

            ViewBag.MaxPrice =
                maxPrice;

            ViewBag.StockStatus =
                stockStatus;

            ViewBag.SortOrder =
                sortOrder;

            ViewBag.Status =
                status;

            ViewBag.CurrentPage =
                pageNumber;

            ViewBag.TotalPages =
                totalPages;

            ViewBag.TotalProducts =
                totalProducts;


            return View(products);
        }


        // =========================================================
        // PRODUCT DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }


            var product =
                await _context.Products

                    .Include(p =>
                        p.Category)

                    .Include(p =>
                        p.BrandEntity)

                    .Include(p =>
                        p.Specifications
                            .OrderBy(s =>
                                s.DisplayOrder)
                            .ThenBy(s =>
                                s.Name))

                    .AsNoTracking()

                    .FirstOrDefaultAsync(p =>
                        p.Id == id &&
                        p.IsActive);


            if (product == null)
            {
                return NotFound();
            }


            // =====================================================
            // RELATED PRODUCTS
            // =====================================================

            var relatedProducts =
                await _context.Products

                    .Include(p =>
                        p.Category)

                    .Include(p =>
                        p.BrandEntity)

                    .Where(p =>
                        p.IsActive &&
                        p.Id != product.Id &&
                        p.CategoryId ==
                        product.CategoryId)

                    .OrderByDescending(p =>
                        p.StockQuantity > 0)

                    .ThenByDescending(p =>
                        p.CreatedAt)

                    .Take(4)

                    .AsNoTracking()

                    .ToListAsync();


            ViewBag.RelatedProducts =
                relatedProducts;


            return View(product);
        }


        // =========================================================
        // CREATE - GET
        // =========================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();

            await LoadBrandsAsync();


            return View(
                new Product
                {
                    IsActive = true,
                    StockQuantity = 0
                });
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product)
        {
            // SKU is generated by the server.
            product.SKU = null;


            // =====================================================
            // CLEAN INPUT
            // =====================================================

            product.Name =
                product.Name?.Trim()
                ?? string.Empty;

            product.Description =
                product.Description?.Trim();


            // =====================================================
            // IMAGE VALIDATION
            // =====================================================

            if (product.ImageFile != null)
            {
                ValidateImage(
                    product.ImageFile);
            }


            // =====================================================
            // CATEGORY VALIDATION
            // =====================================================

            var category =
                await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Id ==
                        product.CategoryId);


            if (category == null)
            {
                ModelState.AddModelError(
                    nameof(product.CategoryId),
                    "Please select a valid category.");
            }


            // =====================================================
            // BRAND VALIDATION
            // =====================================================

            Brand? brand = null;


            if (product.BrandId.HasValue &&
                product.BrandId.Value > 0)
            {
                brand =
                    await _context.Brands
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b =>
                            b.Id ==
                            product.BrandId.Value
                            &&
                            b.IsActive);


                if (brand == null)
                {
                    ModelState.AddModelError(
                        nameof(product.BrandId),
                        "Please select a valid active brand.");
                }
            }
            else
            {
                ModelState.AddModelError(
                    nameof(product.BrandId),
                    "Please select a brand.");
            }


            // =====================================================
            // MODEL VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(
                    product.CategoryId);

                await LoadBrandsAsync(
                    product.BrandId);

                return View(product);
            }


            // =====================================================
            // LEGACY BRAND
            // =====================================================

            product.Brand =
                brand!.Name;


            // =====================================================
            // IMAGE
            // =====================================================

            if (product.ImageFile != null)
            {
                product.ImageUrl =
                    await SaveFileAsync(
                        product.ImageFile);
            }


            // =====================================================
            // DATES / STATUS
            // =====================================================

            product.CreatedAt =
                DateTime.Now;

            product.UpdatedAt =
                null;

            product.IsActive =
                true;


            // =====================================================
            // SAVE PRODUCT
            // =====================================================

            _context.Products.Add(product);

            await _context.SaveChangesAsync();


            // =====================================================
            // GENERATE SKU
            // =====================================================

            product.SKU =
                GenerateSku(
                    category!.Name,
                    brand.Name,
                    product.Id);


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Product added successfully. SKU: {product.SKU}";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(
            int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }


            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);


            if (product == null)
            {
                return NotFound();
            }


            await LoadCategoriesAsync(
                product.CategoryId);

            await LoadBrandsAsync(
                product.BrandId);


            return View(product);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }


            // =====================================================
            // EXISTING PRODUCT
            // =====================================================

            var existingProduct =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);


            if (existingProduct == null)
            {
                return NotFound();
            }


            // =====================================================
            // CLEAN INPUT
            // =====================================================

            product.Name =
                product.Name?.Trim()
                ?? string.Empty;

            product.Description =
                product.Description?.Trim();


            // =====================================================
            // IMAGE VALIDATION
            // =====================================================

            if (product.ImageFile != null)
            {
                ValidateImage(
                    product.ImageFile);
            }


            // =====================================================
            // CATEGORY VALIDATION
            // =====================================================

            var category =
                await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Id ==
                        product.CategoryId);


            if (category == null)
            {
                ModelState.AddModelError(
                    nameof(product.CategoryId),
                    "Please select a valid category.");
            }


            // =====================================================
            // BRAND VALIDATION
            // =====================================================

            Brand? brand = null;


            if (product.BrandId.HasValue &&
                product.BrandId.Value > 0)
            {
                brand =
                    await _context.Brands
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b =>
                            b.Id ==
                            product.BrandId.Value
                            &&
                            b.IsActive);


                if (brand == null)
                {
                    ModelState.AddModelError(
                        nameof(product.BrandId),
                        "Please select a valid active brand.");
                }
            }
            else
            {
                ModelState.AddModelError(
                    nameof(product.BrandId),
                    "Please select a brand.");
            }


            // =====================================================
            // MODEL VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                product.SKU =
                    existingProduct.SKU;

                product.ImageUrl =
                    existingProduct.ImageUrl;

                await LoadCategoriesAsync(
                    product.CategoryId);

                await LoadBrandsAsync(
                    product.BrandId);

                return View(product);
            }


            // =====================================================
            // UPDATE BASIC INFORMATION
            // =====================================================

            existingProduct.Name =
                product.Name;

            existingProduct.Description =
                product.Description;

            existingProduct.Price =
                product.Price;

            existingProduct.StockQuantity =
                product.StockQuantity;

            existingProduct.CategoryId =
                product.CategoryId;

            existingProduct.BrandId =
                brand!.Id;

            existingProduct.Brand =
                brand.Name;

            existingProduct.IsActive =
                product.IsActive;

            existingProduct.UpdatedAt =
                DateTime.Now;


            // =====================================================
            // IMAGE UPDATE
            // =====================================================

            if (product.ImageFile != null)
            {
                var oldImage =
                    existingProduct.ImageUrl;


                existingProduct.ImageUrl =
                    await SaveFileAsync(
                        product.ImageFile);


                DeletePhysicalFile(
                    oldImage);
            }


            // =====================================================
            // SAVE
            // =====================================================

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Product updated successfully.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // DELETE / DEACTIVATE - GET
        // =========================================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(
            int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }


            var product =
                await _context.Products

                    .Include(p =>
                        p.Category)

                    .Include(p =>
                        p.BrandEntity)

                    .AsNoTracking()

                    .FirstOrDefaultAsync(p =>
                        p.Id == id);


            if (product == null)
            {
                return NotFound();
            }


            return View(product);
        }


        // =========================================================
        // DELETE / DEACTIVATE - POST
        // =========================================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);


            if (product == null)
            {
                return NotFound();
            }


            // Soft delete.
            product.IsActive =
                false;

            product.UpdatedAt =
                DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Product deactivated successfully.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // ACTIVATE
        // =========================================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(
            int id)
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);


            if (product == null)
            {
                return NotFound();
            }


            product.IsActive =
                true;

            product.UpdatedAt =
                DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Product activated successfully.";


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // SKU GENERATOR
        // =========================================================

        private string GenerateSku(
            string categoryName,
            string? brandName,
            int productId)
        {
            var categoryPrefix =
                CreatePrefix(
                    categoryName,
                    "PRO");


            var brandPrefix =
                CreatePrefix(
                    brandName,
                    "GEN");


            return
                $"{categoryPrefix}-{brandPrefix}-{productId:D6}";
        }


        // =========================================================
        // CREATE PREFIX
        // =========================================================

        private string CreatePrefix(
            string? value,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }


            var cleaned =
                new string(
                    value
                        .Where(
                            char.IsLetterOrDigit)
                        .ToArray());


            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return fallback;
            }


            cleaned =
                cleaned.ToUpperInvariant();


            if (cleaned.Length > 3)
            {
                cleaned =
                    cleaned[..3];
            }


            return cleaned;
        }


        // =========================================================
        // CATEGORY DROPDOWN
        // =========================================================

        private async Task LoadCategoriesAsync(
            int? selectedId = null)
        {
            ViewBag.CategoryId =
                new SelectList(
                    await _context.Categories
                        .AsNoTracking()
                        .OrderBy(c =>
                            c.DisplayOrder)
                        .ThenBy(c =>
                            c.Name)
                        .ToListAsync(),
                    "Id",
                    "Name",
                    selectedId);
        }


        // =========================================================
        // BRAND DROPDOWN
        // =========================================================

        private async Task LoadBrandsAsync(
            int? selectedId = null)
        {
            ViewBag.BrandId =
                new SelectList(
                    await _context.Brands
                        .AsNoTracking()
                        .Where(b =>
                            b.IsActive)
                        .OrderBy(b =>
                            b.DisplayOrder)
                        .ThenBy(b =>
                            b.Name)
                        .ToListAsync(),
                    "Id",
                    "Name",
                    selectedId);
        }


        // =========================================================
        // IMAGE VALIDATION
        // =========================================================

        private void ValidateImage(
            IFormFile file)
        {
            if (file.Length == 0)
            {
                ModelState.AddModelError(
                    "ImageFile",
                    "The selected image is empty.");

                return;
            }


            if (file.Length > MaxFileSize)
            {
                ModelState.AddModelError(
                    "ImageFile",
                    "Image size cannot exceed 5 MB.");

                return;
            }


            var extension =
                Path.GetExtension(
                    file.FileName)
                    .ToLowerInvariant();


            if (!AllowedExtensions.Contains(
                    extension))
            {
                ModelState.AddModelError(
                    "ImageFile",
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }
        }


        // =========================================================
        // SAVE IMAGE
        // =========================================================

        private async Task<string> SaveFileAsync(
            IFormFile file)
        {
            var extension =
                Path.GetExtension(
                    file.FileName)
                    .ToLowerInvariant();


            var fileName =
                $"{Guid.NewGuid():N}{extension}";


            var productDirectory =
                Path.Combine(
                    _hostEnvironment.WebRootPath,
                    "images",
                    "products");


            Directory.CreateDirectory(
                productDirectory);


            var filePath =
                Path.Combine(
                    productDirectory,
                    fileName);


            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.Create);


            await file.CopyToAsync(
                stream);


            return
                $"/images/products/{fileName}";
        }


        // =========================================================
        // DELETE PHYSICAL IMAGE
        // =========================================================

        private void DeletePhysicalFile(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(
                    imageUrl))
            {
                return;
            }


            var relativePath =
                imageUrl
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar);


            var filePath =
                Path.Combine(
                    _hostEnvironment.WebRootPath,
                    relativePath);


            if (System.IO.File.Exists(
                    filePath))
            {
                System.IO.File.Delete(
                    filePath);
            }
        }
    }
}