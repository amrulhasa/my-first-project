using BDTechMarket.Data;
using BDTechMarket.Models;
using BDTechMarket.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BDTechMarket.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const decimal DeliveryFee = 60.00m;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // CART INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Challenge();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            var shoppingCartVM = new ShoppingCartVM
            {
                ListCart = cartItems,
                Order = new Order()
            };

            foreach (var item in cartItems)
            {
                if (item.Product != null && item.Product.IsActive)
                {
                    shoppingCartVM.OrderTotal +=
                        item.Product.Price * item.Count;
                }
            }

            return View(shoppingCartVM);
        }

        // =========================================================
        // ADD TO CART
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(
            int productId,
            int count = 1)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Challenge();

            // Basic quantity validation
            if (count < 1)
            {
                TempData["Error"] = "Invalid product quantity.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            if (count > 100)
            {
                TempData["Error"] = "You cannot add more than 100 units.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            // Load product from database.
            // Never trust price/stock from the browser.
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == productId &&
                    p.IsActive);

            if (product == null)
            {
                TempData["Error"] = "Product is no longer available.";
                return RedirectToAction("Index", "Product");
            }

            // Existing cart item for THIS user only
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.ProductId == productId);

            var existingQuantity = cartItem?.Count ?? 0;
            var requestedQuantity = existingQuantity + count;

            // Prevent adding more than available stock
            if (requestedQuantity > product.StockQuantity)
            {
                TempData["Error"] =
                    $"Only {product.StockQuantity} unit(s) of this product are available.";

                return RedirectToAction("Details", "Product",
                    new { id = productId });
            }

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    ProductId = productId,
                    UserId = userId,
                    Count = count
                };

                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Count = requestedQuantity;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Product added to cart!";

            return RedirectToAction("Index", "Home");
        }

        // =========================================================
        // CHECKOUT GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Challenge();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            // Check for unavailable products before checkout
            foreach (var item in cartItems)
            {
                if (item.Product == null || !item.Product.IsActive)
                {
                    TempData["Error"] =
                        "One or more products in your cart are no longer available.";

                    return RedirectToAction(nameof(Index));
                }

                if (item.Count > item.Product.StockQuantity)
                {
                    TempData["Error"] =
                        $"Insufficient stock for {item.Product.Name}. " +
                        $"Available stock: {item.Product.StockQuantity}.";

                    return RedirectToAction(nameof(Index));
                }
            }

            var order = new Order
            {
                // UserId is set server-side.
                UserId = userId,

                // Customer information comes from current account
                // and can be edited during checkout.
                FullName = user?.FullName ?? string.Empty,
                Phone = user?.PhoneNumber ?? string.Empty,
                Email = user?.Email,

                DeliveryFee = DeliveryFee,

                PaymentMethod = "COD",
                PaymentStatus = "Pending",
                OrderStatus = "Pending"
            };

            decimal subtotal = cartItems.Sum(item =>
                item.Product!.Price * item.Count);

            ViewBag.Subtotal = subtotal;
            ViewBag.DeliveryFee = DeliveryFee;
            ViewBag.Total = subtotal + DeliveryFee;
            ViewBag.ItemCount = cartItems.Sum(item => item.Count);

            return View("../Order/Checkout", order);
        }

        // =========================================================
        // INCREASE CART QUANTITY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Plus(int cartId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Challenge();

            // IMPORTANT:
            // cartId + userId together prevent IDOR.
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c =>
                    c.Id == cartId &&
                    c.UserId == userId);

            if (cartItem == null)
            {
                TempData["Error"] = "Cart item not found.";
                return RedirectToAction(nameof(Index));
            }

            if (cartItem.Product == null ||
                !cartItem.Product.IsActive)
            {
                TempData["Error"] = "This product is no longer available.";
                return RedirectToAction(nameof(Index));
            }

            if (cartItem.Count >= 100)
            {
                TempData["Error"] =
                    "Maximum quantity allowed is 100.";
                return RedirectToAction(nameof(Index));
            }

            if (cartItem.Count >= cartItem.Product.StockQuantity)
            {
                TempData["Error"] =
                    $"Only {cartItem.Product.StockQuantity} unit(s) are available.";

                return RedirectToAction(nameof(Index));
            }

            cartItem.Count++;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DECREASE CART QUANTITY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Minus(int cartId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Challenge();

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c =>
                    c.Id == cartId &&
                    c.UserId == userId);

            if (cartItem == null)
            {
                TempData["Error"] = "Cart item not found.";
                return RedirectToAction(nameof(Index));
            }

            if (cartItem.Count <= 1)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Count--;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // REMOVE SINGLE CART ITEM
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Challenge();

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.UserId == userId);

            if (cartItem == null)
            {
                TempData["Error"] = "Cart item not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.CartItems.Remove(cartItem);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Product removed from cart.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CLEAR CART
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Challenge();

            var items = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (items.Any())
            {
                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Cart cleared successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CURRENT USER ID
        // =========================================================

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(
                ClaimTypes.NameIdentifier);
        }
    }
}