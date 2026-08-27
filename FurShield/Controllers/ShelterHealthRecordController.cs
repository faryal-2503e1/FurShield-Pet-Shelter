using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "Shelter")]
    public class ShelterHealthRecordController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShelterHealthRecordController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Sirf EK hi Index method rakhein (optional listingId parameter ke sath)
        [HttpGet]
        public async Task<IActionResult> Index(int? listingId)
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);

            if (shelter == null) return RedirectToAction("Index", "Shelter");

            var listings = await _context.AdoptionListings
                .Include(a => a.CareLogs)
                .Where(a => a.ShelterId == shelter.ShelterId)
                .ToListAsync();

            ViewBag.SelectedListingId = listingId;

            return View(listings);
        }

        // POST Action for Health Status Update — appends a dated log entry instead of
        // overwriting the previous one, so full care history is kept (SRS: "maintain
        // and update logs").
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int listingId, string healthStatus)
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shelter == null) return NotFound();

            var listing = await _context.AdoptionListings
                .FirstOrDefaultAsync(a => a.AdoptionListingId == listingId && a.ShelterId == shelter.ShelterId);
            if (listing == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(healthStatus))
            {
                _context.CareStatusLogs.Add(new CareStatusLog
                {
                    AdoptionListingId = listing.AdoptionListingId,
                    Note = healthStatus.Trim(),
                    CreatedAt = DateTime.Now
                });

                // Keep the "current" summary field in sync for the places that still
                // display it directly (adoption listing, adopter-facing pages).
                listing.HealthStatus = healthStatus.Trim();
                _context.AdoptionListings.Update(listing);

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Health record updated for {listing.PetName}!";
            return RedirectToAction(nameof(Index), new { listingId });
        }
    }
}
