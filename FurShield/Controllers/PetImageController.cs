using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;
using System.Security.Claims;

namespace FurShield.Controllers
{
    [Authorize(Roles = "PetOwner")]
    public class PetImageController : Controller
    {
        private readonly PetShelterContext _context;
        private readonly IWebHostEnvironment _env;

        public PetImageController(PetShelterContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Family sharing support: returns current user ID + all family members' IDs
        private async Task<List<string>> GetFamilyUserIdsAsync(string currentUserId)
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);

            if (currentUser?.FamilyAccountId != null)
            {
                return await _context.Users
                    .Where(u => u.FamilyAccountId == currentUser.FamilyAccountId)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            return new List<string> { currentUserId };
        }

        // GET: PetImage/Gallery/5
        public async Task<IActionResult> Gallery(int petId)
        {
            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var pet = await _context.Pets
                .FirstOrDefaultAsync(p => p.PetId == petId && familyUserIds.Contains(p.OwnerId));

            if (pet == null) return NotFound();

            var images = await _context.PetImages
                .Where(i => i.PetId == petId)
                .OrderByDescending(i => i.UploadedDate)
                .ToListAsync();

            ViewBag.Pet = pet;
            return View(images);
        }

        // POST: PetImage/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int petId, IFormFile image)
        {
            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var ownsPet = await _context.Pets.AnyAsync(p => p.PetId == petId && familyUserIds.Contains(p.OwnerId));
            if (!ownsPet) return Forbid();

            if (image == null || image.Length == 0)
            {
                TempData["ErrorMessage"] = "Please choose an image file before clicking upload.";
                return RedirectToAction("Details", "Pet", new { id = petId, tab = "gallery" });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["ErrorMessage"] = "Only image files (jpg, jpeg, png, gif, webp) are allowed.";
                return RedirectToAction("Details", "Pet", new { id = petId, tab = "gallery" });
            }

            try
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "Uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                _context.PetImages.Add(new PetImage
                {
                    PetId = petId,
                    ImageUrl = "Uploads/" + uniqueFileName,
                    UploadedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Photo uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Upload failed: " + ex.Message;
            }

            return RedirectToAction("Details", "Pet", new { id = petId, tab = "gallery" });
        }

        // GET: PetImage/Delete/5 (Show Delete Confirmation View)
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var petImage = await _context.PetImages
                .Include(p => p.Pet)
                .FirstOrDefaultAsync(m => m.PetImageId == id && familyUserIds.Contains(m.Pet.OwnerId));

            if (petImage == null) return NotFound();

            return View(petImage);
        }

        // POST: PetImage/Delete/5 (Perform Actual Deletion)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            var familyUserIds = await GetFamilyUserIdsAsync(userId);

            var petImage = await _context.PetImages
                .Include(p => p.Pet)
                .FirstOrDefaultAsync(p => p.PetImageId == id && familyUserIds.Contains(p.Pet.OwnerId));

            if (petImage != null)
            {
                var petId = petImage.PetId;

                // Delete physical file from wwwroot/Uploads
                if (!string.IsNullOrEmpty(petImage.ImageUrl))
                {
                    var relativePath = petImage.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(_env.WebRootPath, relativePath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                _context.PetImages.Remove(petImage);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Photo deleted successfully.";
                return RedirectToAction("Details", "Pet", new { id = petId, tab = "gallery" });
            }

            return NotFound();
        }
    }
}