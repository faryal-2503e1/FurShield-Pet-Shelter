using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    public class ProductController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Product/Index (Shop View for Everyone)
        public async Task<IActionResult> Index(string category, string searchString, string sort)
        {
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) || p.Description.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }

            products = sort switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                "name" => products.OrderBy(p => p.Name),
                _ => products.OrderByDescending(p => p.DateAdded) // "newest" / default
            };

            ViewBag.Categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.CurrentSort = sort;

            return View(await products.ToListAsync());
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            ViewBag.RatingWidget = await BuildRatingWidgetAsync("Product", product.ProductId);

            return View(product);
        }

        // Loads every rating for a target and wraps it (plus the current
        // user's own existing rating, if any) for the shared _RatingWidget partial.
        private async Task<FurShield.Models.RatingWidgetViewModel> BuildRatingWidgetAsync(string targetType, int targetId)
        {
            var ratings = await _context.Ratings
                .Include(r => r.Rater)
                .Where(r => r.TargetType == targetType && r.TargetId == targetId)
                .ToListAsync();

            var userId = User.Identity != null && User.Identity.IsAuthenticated
                ? _userManager.GetUserId(User)
                : null;

            var mine = userId != null ? ratings.FirstOrDefault(r => r.RaterUserId == userId) : null;

            return new FurShield.Models.RatingWidgetViewModel
            {
                TargetType = targetType,
                TargetId = targetId,
                Ratings = ratings,
                CanRate = User.IsInRole("PetOwner"),
                MyScore = mine?.Score,
                MyComment = mine?.Comment
            };
        }
    }
}