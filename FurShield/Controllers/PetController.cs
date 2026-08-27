using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FurShield.Controllers
{
    [Authorize(Roles = "PetOwner")]
    public class PetController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly IWebHostEnvironment _env;

        public PetController(PetShelterContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Family Members ki IDs nikalne ke liye Helper Method
        private async Task<List<string>> GetFamilyUserIdsAsync(string userId)
        {
            var currentUser = await _context.Users
                .Include(u => u.FamilyAccount)
                    .ThenInclude(f => f!.Members)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser?.FamilyAccountId != null && currentUser.FamilyAccount?.Members != null)
            {
                return currentUser.FamilyAccount.Members.Select(m => m.Id).ToList();
            }

            return new List<string> { userId };
        }

        // GET: Pet (Family ke tamam pets dikhai denge)
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var pets = await _context.Pets
                .Include(p => p.Owner)
                .Include(p => p.Images)
                .Where(p => familyUserIds.Contains(p.OwnerId))
                .ToListAsync();

            return View(pets);
        }

        // GET: Pet/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var pet = await _context.Pets
                .Include(p => p.Owner)
                .Include(p => p.HealthRecords).ThenInclude(h => h.Veterinarian).ThenInclude(v => v.User)
                .Include(p => p.Appointments).ThenInclude(a => a.Veterinarian).ThenInclude(v => v.User)
                .Include(p => p.Documents)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(m => m.PetId == id && familyUserIds.Contains(m.OwnerId));

            if (pet == null) return NotFound();

            return View(pet);
        }

        // GET: Pet/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Pet/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PetId,Name,Species,Breed,Age,Gender,MedicalHistory,Allergies,ImageUrl,InsurancePolicyNumber,InsuranceDetails")] Pet pet, IFormFile? imageFile)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            pet.OwnerId = userId;

            ModelState.Remove("Owner");
            ModelState.Remove("OwnerId");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    pet.ImageUrl = await SaveUploadedImageAsync(imageFile);
                }

                _context.Add(pet);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pet);
        }

        // GET: Pet/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var pet = await _context.Pets.FirstOrDefaultAsync(p => p.PetId == id && familyUserIds.Contains(p.OwnerId));

            if (pet == null) return NotFound();

            return View(pet);
        }

        // POST: Pet/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PetId,Name,Species,Breed,Age,Gender,MedicalHistory,Allergies,ImageUrl,InsurancePolicyNumber,InsuranceDetails")] Pet pet, IFormFile? imageFile)
        {
            if (id != pet.PetId) return NotFound();

            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            // Access check
            var existingPet = await _context.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.PetId == id && familyUserIds.Contains(p.OwnerId));
            if (existingPet == null) return NotFound();

            // Original owner ko retain karne ke liye
            pet.OwnerId = existingPet.OwnerId;

            ModelState.Remove("Owner");
            ModelState.Remove("OwnerId");

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        pet.ImageUrl = await SaveUploadedImageAsync(imageFile);
                    }

                    _context.Update(pet);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PetExists(pet.PetId, familyUserIds)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pet);
        }

        // GET: Pet/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var pet = await _context.Pets
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(m => m.PetId == id && familyUserIds.Contains(m.OwnerId));

            if (pet == null) return NotFound();

            return View(pet);
        }

        // POST: Pet/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var pet = await _context.Pets.FirstOrDefaultAsync(p => p.PetId == id && familyUserIds.Contains(p.OwnerId));

            if (pet != null)
            {
                _context.Pets.Remove(pet);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveUploadedImageAsync(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "Uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return "Uploads/" + uniqueFileName;
        }

        private bool PetExists(int id, List<string> familyUserIds)
        {
            return _context.Pets.Any(e => e.PetId == id && familyUserIds.Contains(e.OwnerId));
        }
    }
}