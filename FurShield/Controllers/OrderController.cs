using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "PetOwner")]
    public class OrderController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Returns the current user's own Id, plus every other member of their
        // family group (if they belong to one).
        private async Task<List<string>> GetFamilyUserIdsAsync(string currentUserId)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);

            if (currentUser?.FamilyAccountId != null)
            {
                return await _context.Users
                    .Where(u => u.FamilyAccountId == currentUser.FamilyAccountId)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            return new List<string> { currentUserId };
        }

        // GET: Order/Index (My Orders)
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            var orders = await _context.Orders
                .Include(o => o.Owner)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => familyUserIds.Contains(o.OwnerId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.IsInFamily = familyUserIds.Count > 1;
            ViewBag.CurrentUserId = userId;

            return View(orders);
        }

        // GET: Order/Checkout/5
        public async Task<IActionResult> Checkout(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            ViewBag.Quantity = quantity;
            ViewBag.TotalPrice = product.Price * quantity;

            return View(product);
        }

        // POST: Order/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(int productId, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var order = new Order
            {
                OwnerId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = product.Price * quantity,
                Status = "Pending"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderItem = new OrderItem
            {
                OrderId = order.OrderId,
                ProductId = productId,
                Quantity = quantity,
                PriceEach = product.Price
            };

            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var familyUserIds = await GetFamilyUserIdsAsync(userId!);

            var order = await _context.Orders
                .Include(o => o.Owner)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.OrderId == id && familyUserIds.Contains(m.OwnerId));

            if (order == null) return NotFound();

            ViewBag.IsMine = order.OwnerId == userId;

            return View(order);
        }
    }
}