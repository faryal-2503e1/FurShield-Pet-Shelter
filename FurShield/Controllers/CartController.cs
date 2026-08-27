using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    // A real multi-item cart: add products, change quantities, remove items,
    // then check out everything in the cart in one Order.
    // (Order/PlaceOrder still exists separately as a quick "Buy Now" for a single item.)
    [Authorize(Roles = "PetOwner")]
    public class CartController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Cart/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.OwnerId == userId)
                .OrderByDescending(c => c.DateAdded)
                .ToListAsync();

            ViewBag.Total = items.Sum(i => i.Quantity * i.Product.Price);

            return View(items);
        }

        // POST: Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            if (quantity < 1) quantity = 1;

            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.OwnerId == userId && c.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
                _context.CartItems.Update(existing);
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    OwnerId = userId,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{product.Name} added to your cart.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var userId = _userManager.GetUserId(User);

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CartItemId == id && c.OwnerId == userId);
            if (item == null) return NotFound();

            if (quantity < 1)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                _context.CartItems.Update(item);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CartItemId == id && c.OwnerId == userId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/Checkout - turns every item currently in the cart into one Order.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.OwnerId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["SuccessMessage"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            var order = new Order
            {
                OwnerId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = cartItems.Sum(i => i.Quantity * i.Product.Price),
                Status = "Pending"
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PriceEach = item.Product.Price
                });
            }

            // Cart is now converted into the order - clear it.
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order placed successfully!";
            return RedirectToAction("Details", "Order", new { id = order.OrderId });
        }
    }
}
