using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;

namespace FurShield.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SuperAdminController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SuperAdminController(PetShelterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================= 1. PLATFORM ANALYTICS =================
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalPetOwners = (await _userManager.GetUsersInRoleAsync("PetOwner")).Count;
            ViewBag.TotalVets = (await _userManager.GetUsersInRoleAsync("Veterinarian")).Count;
            ViewBag.TotalShelters = (await _userManager.GetUsersInRoleAsync("Shelter")).Count;
            ViewBag.TotalPets = await _context.Pets.CountAsync();
            ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalAdoptions = await _context.AdoptionListings.CountAsync();

            var petOwners = await _userManager.GetUsersInRoleAsync("PetOwner");
            ViewBag.RecentUsers = petOwners.OrderByDescending(u => u.Id).Take(5).ToList();

            return View();
        }
        // ================= 2. USER MANAGEMENT (PET OWNERS ONLY) =================
        public async Task<IActionResult> Users(string searchString)
        {
            // Sirf PetOwner role ke users fetch karein
            var petOwners = await _userManager.GetUsersInRoleAsync("PetOwner");
            var users = petOwners.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.UserName) && u.UserName.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                ).AsQueryable();
            }

            return View(users.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (await _userManager.IsLockedOutAsync(user))
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                }
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Users));
        }

        // ================= 3. SHELTERS & VETS LISTING (READ-ONLY) =================
        public async Task<IActionResult> SheltersAndVets()
        {
            ViewBag.Vets = await _context.Veterinarians
                .Include(v => v.User) // <--- User record include karna zaroori hai
                .ToListAsync();

            var shelters = await _context.Shelters.ToListAsync();

            return View(shelters);
        }

        // ================= 4. ADOPTION LISTINGS =================
        public async Task<IActionResult> Adoptions()
        {
            var listings = await _context.AdoptionListings
                .Include(a => a.Shelter)
                .ToListAsync();

            return View(listings);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAdoptionListing(int id)
        {
            var listing = await _context.AdoptionListings.FindAsync(id);
            if (listing != null)
            {
                _context.AdoptionListings.Remove(listing);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Adoptions));
        }

        // ================= 5. PRODUCTS MODERATION =================
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Shelter)
                .ToListAsync();

            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Products));
        }

        // ================= 6. APPOINTMENTS & ORDERS OVERSIGHT =================
        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Pet)
                .Include(a => a.Veterinarian)
                    .ThenInclude(v => v.User)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ================= 7. CONTACT MESSAGES CRUD =================
        public async Task<IActionResult> ContactMessages()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.DateSent)
                .ToListAsync();

            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleContactRead(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null)
            {
                msg.IsRead = !msg.IsRead;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ContactMessages));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContactMessage(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null)
            {
                _context.ContactMessages.Remove(msg);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ContactMessages));
        }
    }
}