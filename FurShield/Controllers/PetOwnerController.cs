using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;
using System.Security.Claims;

namespace FurShield.Controllers
{
    [Authorize(Roles = "PetOwner")]
    public class PetOwnerController : Controller
    {
        private readonly PetShelterContext _context;

        public PetOwnerController(PetShelterContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            // 1. Current User aur uski Family ki Details fetch karein
            var currentUser = await _context.Users
                .Include(u => u.FamilyAccount)
                    .ThenInclude(f => f!.Members)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // 2. Family Members ki User IDs ki List tayar karein
            List<string> familyMemberIds = new List<string> { userId };

            if (currentUser?.FamilyAccountId != null)
            {
                familyMemberIds = currentUser.FamilyAccount.Members.Select(m => m.Id).ToList();
            }

            // 3. Complete Family ke Stats (Pets, Appointments, Health Records) calculate karein
            ViewBag.PetCount = await _context.Pets
                .CountAsync(p => familyMemberIds.Contains(p.OwnerId));

            ViewBag.UpcomingAppointments = await _context.Appointments
                .CountAsync(a => familyMemberIds.Contains(a.OwnerId)
                    && a.AppointmentDate >= DateTime.Now
                    && a.Status != "Cancelled" && a.Status != "Rejected");

            ViewBag.HealthRecordCount = await _context.HealthRecords
                .CountAsync(h => familyMemberIds.Contains(h.Pet.OwnerId));

            // 4. Personal Dashboard Stats (Cart, Notifications, Orders specific to logged-in user)
            ViewBag.UnreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            ViewBag.CartItemCount = await _context.CartItems
                .CountAsync(c => c.OwnerId == userId);

            ViewBag.OrderCount = await _context.Orders
                .CountAsync(o => o.OwnerId == userId);

            // 5. Family Info for Dashboard View
            ViewBag.FamilyName = currentUser?.FamilyAccount?.FamilyName;
            ViewBag.FamilyMemberCount = currentUser?.FamilyAccount?.Members.Count ?? 0;

            return View();
        }

        public async Task<IActionResult> AdoptPets(string searchString, string species)
        {
            var query = _context.AdoptionListings
                .Include(a => a.Shelter)
                .Where(a => a.Status == "Available");

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(a => a.PetName.Contains(searchString) || a.Breed.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(species))
            {
                query = query.Where(a => a.Species == species);
            }

            var availablePets = await query.ToListAsync();
            return View(availablePets);
        }
    }
}