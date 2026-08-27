using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    // Handles "Feedback and Ratings" (SRS Common Feature): pet owners can
    // rate a Veterinarian, a Shelter, or a Product and leave a comment.
    // One controller/table for all three target types instead of three
    // near-identical copies.
    [Authorize(Roles = "PetOwner")]
    public class RatingController : Controller
    {
        private static readonly string[] AllowedTargetTypes = { "Product", "Veterinarian", "Shelter" };

        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RatingController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: Rating/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string targetType, int targetId, int score, string? comment, string? returnUrl)
        {
            if (!AllowedTargetTypes.Contains(targetType) || score < 1 || score > 5)
            {
                return BadRequest();
            }

            var userId = _userManager.GetUserId(User)!;

            // One rating per user per target: update it if they've already
            // rated this item before, otherwise create a new one.
            var existing = await _context.Ratings.FirstOrDefaultAsync(r =>
                r.TargetType == targetType && r.TargetId == targetId && r.RaterUserId == userId);

            if (existing != null)
            {
                existing.Score = score;
                existing.Comment = comment;
                existing.CreatedDate = DateTime.Now;
            }
            else
            {
                _context.Ratings.Add(new Rating
                {
                    TargetType = targetType,
                    TargetId = targetId,
                    RaterUserId = userId,
                    Score = score,
                    Comment = comment,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            // Only redirect back to a local URL (never an attacker-supplied
            // external one) - falls back to Home if anything looks off.
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
