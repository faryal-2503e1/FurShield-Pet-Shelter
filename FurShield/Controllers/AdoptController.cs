using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{

    // Public-facing browsing + "express interest" flow for adoptable pets.
    // (AdoptionController stays Shelter-only for managing their own listings.)
    public class AdoptController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdoptController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Adopt/Index - anyone can browse available pets
        public async Task<IActionResult> Index(string? species, string? searchString, string? sort)
        {
            var listings = _context.AdoptionListings
                .Include(a => a.Shelter)
                .Where(a => a.Status == "Available")
                .AsQueryable();

            if (!string.IsNullOrEmpty(species))
            {
                listings = listings.Where(a => a.Species == species);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                listings = listings.Where(a =>
                    a.PetName.Contains(searchString) ||
                    (a.Breed != null && a.Breed.Contains(searchString)));
            }

            listings = sort switch
            {
                "age_asc" => listings.OrderBy(a => a.Age),
                "age_desc" => listings.OrderByDescending(a => a.Age),
                "name" => listings.OrderBy(a => a.PetName),
                _ => listings.OrderByDescending(a => a.DateListed) // "newest" / default
            };

            ViewBag.Species = await _context.AdoptionListings
                .Select(a => a.Species)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.CurrentSort = sort;

            return View(await listings.ToListAsync());
        }

        // GET: Adopt/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var listing = await _context.AdoptionListings
                .Include(a => a.Shelter)
                .FirstOrDefaultAsync(a => a.AdoptionListingId == id);

            if (listing == null) return NotFound();

            // If the current user already expressed interest, show that instead of the form.
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.ExistingInterest = await _context.AdoptionInterests
                    .Where(i => i.AdoptionListingId == id && i.OwnerId == userId)
                    .OrderByDescending(i => i.DateSubmitted)
                    .FirstOrDefaultAsync();
            }

            if (listing.Shelter != null)
            {
                ViewBag.RatingWidget = await BuildRatingWidgetAsync("Shelter", listing.Shelter.ShelterId);
            }

            return View(listing);
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

        // POST: Adopt/SubmitInterest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PetOwner")]
        public async Task<IActionResult> SubmitInterest(int adoptionListingId, string? message, string? phoneNumber, string? address)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var listing = await _context.AdoptionListings
                .Include(a => a.Shelter)
                .FirstOrDefaultAsync(a => a.AdoptionListingId == adoptionListingId);

            if (listing == null) return NotFound();

            var alreadyPending = await _context.AdoptionInterests
                .AnyAsync(i => i.AdoptionListingId == adoptionListingId && i.OwnerId == userId && i.Status == "Pending");

            if (!alreadyPending)
            {
                // Concatenate details so Shelter receives complete applicant profile
                string fullMessage = $"[Phone: {phoneNumber}] | [Address: {address}] \nMessage: {message}";

                _context.AdoptionInterests.Add(new AdoptionInterest
                {
                    AdoptionListingId = adoptionListingId,
                    OwnerId = userId,
                    Message = fullMessage,
                    Status = "Pending",
                    DateSubmitted = DateTime.Now
                });

                _context.Notifications.Add(new AppNotification
                {
                    UserId = listing.Shelter.UserId,
                    Message = $"New adoption application received for {listing.PetName}.",
                    Link = "/Adoption/Interests",
                    DateCreated = DateTime.Now
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your adoption application form has been submitted!";
            }

            return RedirectToAction(nameof(Details), new { id = adoptionListingId });
        }

        // GET: Adopt/MyInterests - a pet owner's own adoption requests and their status
        [Authorize(Roles = "PetOwner")]
        public async Task<IActionResult> MyInterests()
        {
            var userId = _userManager.GetUserId(User);

            var interests = await _context.AdoptionInterests
                .Include(i => i.AdoptionListing)
                .ThenInclude(l => l.Shelter)
                .Where(i => i.OwnerId == userId)
                .OrderByDescending(i => i.DateSubmitted)
                .ToListAsync();

            return View(interests);
        }
    }
}
