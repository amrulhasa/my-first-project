using Microsoft.AspNetCore.Mvc;
using BDTechMarket.Data;
using BDTechMarket.Models;
using BDTechMarket.Models.ViewModels;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BDTechMarket.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 1. GET: Cart/Index ---
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            ShoppingCartVM shoppingCartVM = new()
            {
                ListCart = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToListAsync(),
                Order = new Order()
            };

            foreach (var item in shoppingCartVM.ListCart)
            {
                shoppingCartVM.OrderTotal += (item.Product.Price * item.Count);
            }

            return View(shoppingCartVM);
        }

        // --- 2. POST: Cart/AddToCart (Missing Method Fixed) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int count)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check koro cart-e ei product age theke ache kina
            var cartFromDb = await _context.CartItems.FirstOrDefaultAsync(
                u => u.UserId == userId && u.ProductId == productId);

            if (cartFromDb == null)
            {
                // Jodi na thake, tobe notun item add koro
                CartItem cartItem = new()
                {
                    ProductId = productId,
                    Count = count,
                    UserId = userId
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                // Jodi thake, tobe count baraye dao
                cartFromDb.Count += count;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product added to cart!";
            
            return RedirectToAction("Index", "Home");
        }

        // --- 3. GET: Cart/Checkout ---
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            var cartItems = await _context.CartItems
                .Include(u => u.Product)
                .Where(u => u.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            Order order = new()
            {
                UserId = userId,
                FullName = user?.FullName ?? "",
                Phone = user?.PhoneNumber ?? "",
                Email = user?.Email ?? "",
                DeliveryFee = 60.00m // Initialize delivery fee
            };

            decimal subtotal = 0;
            foreach (var item in cartItems)
            {
                subtotal += (item.Product.Price * item.Count);
            }

            ViewBag.Subtotal = subtotal;
            ViewBag.DeliveryFee = order.DeliveryFee;
            ViewBag.Total = subtotal + order.DeliveryFee;
            ViewBag.ItemCount = cartItems.Count;

            return View("../Order/Checkout", order);
        }

        // --- 4. POST: Cart/PlaceOrder ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _context.CartItems
                .Include(u => u.Product)
                .Where(u => u.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            // Order Header Calculation
            order.UserId = userId;
            order.OrderDate = DateTime.Now;
            order.OrderStatus = "Pending";
            order.PaymentStatus = (order.PaymentMethod == "COD") ? "Pending" : "Completed";
            
            decimal subtotal = 0;
            foreach (var item in cartItems)
            {
                subtotal += (item.Product.Price * item.Count);
            }
            
            // Fixed delivery fee calculation
            order.DeliveryFee = 60.00m; 
            order.OrderTotal = subtotal + order.DeliveryFee;

            // Save Order to DB
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); 

            // Save Order Details (Line Items)
            foreach (var cart in cartItems)
            {
                OrderDetail detail = new()
                {
                    OrderId = order.Id,
                    ProductId = cart.ProductId,
                    Count = cart.Count,
                    Price = cart.Product.Price
                };
                _context.OrderDetails.Add(detail);
            }

            // Clear Cart items for this user
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // Success message and redirect
            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction("MyOrders", "Order");
        }

        // --- Cart Item Actions ---
        public async Task<IActionResult> Plus(int cartId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartId);
            if (cartItem != null) { cartItem.Count++; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartId);
            if (cartItem != null)
            {
                if (cartItem.Count <= 1) _context.CartItems.Remove(cartItem);
                else cartItem.Count--;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem != null) { _context.CartItems.Remove(cartItem); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = await _context.CartItems.Where(u => u.UserId == userId).ToListAsync();
            if (items.Any()) { _context.CartItems.RemoveRange(items); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}