using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;
using System.Security.Claims;

namespace FurShield.Controllers
{
    // Lets a Pet Owner start a "family", invite other pet owners into it with a
    // short invite code, and (optionally) share pet visibility with the members
    // of that family. This does not add any new database table - FamilyAccount
    // and ApplicationUser.FamilyAccountId already exist in the model/migration.
    [Authorize(Roles = "PetOwner")]
    public class FamilyController : Controller
    {
        private readonly PetShelterContext _context;

        public FamilyController(PetShelterContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET: Family
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            var currentUser = await _context.Users
                .Include(u => u.FamilyAccount)
                    .ThenInclude(f => f!.Members)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser == null) return NotFound();

            return View(currentUser);
        }

        // POST: Family/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string familyName)
        {
            var userId = GetCurrentUserId();
            var currentUser = await _context.Users.FindAsync(userId);

            if (currentUser == null) return NotFound();

            if (currentUser.FamilyAccountId != null)
            {
                TempData["FamilyError"] = "You are already part of a family. Leave it first to create a new one.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(familyName))
            {
                TempData["FamilyError"] = "Please enter a family name.";
                return RedirectToAction(nameof(Index));
            }

            var family = new FamilyAccount
            {
                FamilyName = familyName.Trim(),
                InviteCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
            };

            _context.FamilyAccounts.Add(family);
            await _context.SaveChangesAsync();

            currentUser.FamilyAccountId = family.Id;
            await _context.SaveChangesAsync();

            TempData["FamilySuccess"] = $"'{family.FamilyName}' family created! Share the invite code {family.InviteCode} with your family members.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Family/Join
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(string inviteCode)
        {
            var userId = GetCurrentUserId();
            var currentUser = await _context.Users.FindAsync(userId);

            if (currentUser == null) return NotFound();

            if (currentUser.FamilyAccountId != null)
            {
                TempData["FamilyError"] = "You are already part of a family. Leave it first to join another one.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                TempData["FamilyError"] = "Please enter an invite code.";
                return RedirectToAction(nameof(Index));
            }

            var family = await _context.FamilyAccounts
                .FirstOrDefaultAsync(f => f.InviteCode == inviteCode.Trim().ToUpper());

            if (family == null)
            {
                TempData["FamilyError"] = "No family found with that invite code.";
                return RedirectToAction(nameof(Index));
            }

            currentUser.FamilyAccountId = family.Id;
            await _context.SaveChangesAsync();

            TempData["FamilySuccess"] = $"You joined the '{family.FamilyName}' family!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Family/Leave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave()
        {
            var userId = GetCurrentUserId();
            var currentUser = await _context.Users.FindAsync(userId);

            if (currentUser == null || currentUser.FamilyAccountId == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var familyId = currentUser.FamilyAccountId.Value;
            currentUser.FamilyAccountId = null;
            await _context.SaveChangesAsync();

            // If no members are left in the family, remove the empty family record.
            var remainingMembers = await _context.Users.CountAsync(u => u.FamilyAccountId == familyId);
            if (remainingMembers == 0)
            {
                var family = await _context.FamilyAccounts.FindAsync(familyId);
                if (family != null)
                {
                    _context.FamilyAccounts.Remove(family);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["FamilySuccess"] = "You have left the family.";
            return RedirectToAction(nameof(Index));
        }
    }
}
