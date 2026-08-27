using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FurShield.Models;
using FurShield.Data;
using System.Diagnostics;

namespace FurShield.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // GET: / (public landing page)
        public async Task<IActionResult> Index()
        {
            ViewBag.FeaturedPets = await _context.AdoptionListings
                .Where(a => a.Status == "Available")
                .OrderByDescending(a => a.AdoptionListingId)
                .Take(3)
                .ToListAsync();

            ViewBag.FeaturedProducts = await _context.Products
                .OrderByDescending(p => p.ProductId)
                .Take(4)
                .ToListAsync();

            ViewBag.Veterinarians = await _context.Veterinarians
                .Include(v => v.User)
                .Take(3)
                .ToListAsync();

            return View();
        }

        // GET: /Home/About
        public IActionResult About()
        {
            return View();
        }

        // GET: /Home/Contact
        public IActionResult Contact()
        {
            return View();
        }

        // POST: /Home/Contact - persists contact message to database for admin review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string name, string email, string subject, string message)
        {
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(message))
            {
                var contactMsg = new ContactMessage
                {
                    Name = name.Trim(),
                    Email = email.Trim(),
                    Subject = string.IsNullOrWhiteSpace(subject) ? "General Inquiry" : subject.Trim(),
                    Message = message.Trim(),
                    DateSent = DateTime.Now,
                    IsRead = false
                };

                _context.ContactMessages.Add(contactMsg);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact message saved from {Name} <{Email}>: {Subject}", name, email, subject);
                TempData["ContactSent"] = "Thanks for reaching out! Your message has been submitted successfully to our team.";
            }

            return RedirectToAction(nameof(Contact));
        }

        // GET: /Home/Vets - public directory of all registered veterinarians
        public async Task<IActionResult> Vets()
        {
            var veterinarians = await _context.Veterinarians
                .Include(v => v.User)
                .ToListAsync();

            return View(veterinarians);
        }

        // GET: /Home/VetDetails/5 - public profile + ratings for one veterinarian
        public async Task<IActionResult> VetDetails(int id)
        {
            var vet = await _context.Veterinarians
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.VeterinarianId == id);

            if (vet == null) return NotFound();

            ViewBag.RatingWidget = await BuildRatingWidgetAsync("Veterinarian", vet.VeterinarianId);

            return View(vet);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Loads every rating for a target and wraps it (plus the current
        // user's own existing rating, if any) for the shared _RatingWidget partial.
        // (Mirrors the same helper used in ProductController/AdoptController.)
        private async Task<RatingWidgetViewModel> BuildRatingWidgetAsync(string targetType, int targetId)
        {
            var ratings = await _context.Ratings
                .Include(r => r.Rater)
                .Where(r => r.TargetType == targetType && r.TargetId == targetId)
                .ToListAsync();

            var userId = User.Identity != null && User.Identity.IsAuthenticated
                ? _userManager.GetUserId(User)
                : null;

            var mine = userId != null ? ratings.FirstOrDefault(r => r.RaterUserId == userId) : null;

            return new RatingWidgetViewModel
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
