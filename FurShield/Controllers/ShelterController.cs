using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "Shelter")]
    public class ShelterController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShelterController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Direct Index / Dashboard Page
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters
                .Include(s => s.AdoptionListings)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            // Agar DB mein Shelter entry nahi hai, toh automatic blank record bana do taake profile page par redirect na hona paray
            if (shelter == null)
            {
                var user = await _userManager.GetUserAsync(User);
                shelter = new Shelter
                {
                    UserId = userId!,
                    ShelterName = user?.FullName ?? (user?.Email?.Split('@')[0] + " Shelter") ?? "My Shelter",
                    ContactPerson = user?.FullName ?? "Not Specified",
                    Phone = user?.PhoneNumber ?? "Not Specified",
                    Address = user?.Address ?? "Not Specified",
                    Description = "Welcome to our shelter."
                };

                _context.Shelters.Add(shelter);
                await _context.SaveChangesAsync();
            }

            ViewBag.TotalListings = shelter.AdoptionListings?.Count ?? 0;
            ViewBag.ActiveListings = shelter.AdoptionListings?.Count(a => a.Status == "Available") ?? 0;
            ViewBag.AdoptedCount = shelter.AdoptionListings?.Count(a => a.Status == "Adopted") ?? 0;

            return View(shelter);
        }

        // Standard Profile Edit View (Sirf user ke Navbar click par khulega)
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);
            var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);

            if (shelter == null)
            {
                shelter = new Shelter { UserId = userId! };
            }

            return View(shelter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Shelter shelter)
        {
            var userId = _userManager.GetUserId(User);
            var existingShelter = await _context.Shelters.FirstOrDefaultAsync(s => s.UserId == userId);

            if (existingShelter != null)
            {
                existingShelter.ShelterName = shelter.ShelterName;
                existingShelter.ContactPerson = shelter.ContactPerson;
                existingShelter.Phone = shelter.Phone;
                existingShelter.Address = shelter.Address;
                existingShelter.Description = shelter.Description;

                _context.Shelters.Update(existingShelter);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}